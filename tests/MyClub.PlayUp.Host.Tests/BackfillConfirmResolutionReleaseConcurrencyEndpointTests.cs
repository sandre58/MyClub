// -----------------------------------------------------------------------
// <copyright file="BackfillConfirmResolutionReleaseConcurrencyEndpointTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Pipeline;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Host.Contracts;
using Xunit;

namespace MyClub.PlayUp.Host.Tests;

/// <summary>
/// A10-REST-7 — BackfillResolutionProvenance versus ConfirmResolutionRelease under competition lock.
/// </summary>
[Collection("host-postgres")]
[Trait("Category", "Integration")]
public sealed class BackfillConfirmResolutionReleaseConcurrencyEndpointTests(HostPostgresFixture fixture)
{
    private const int StressIterationCount = 50;

    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 11, 23, 21, 0, 0, TimeSpan.Zero));

    [IntegrationFact]
    public async Task Backfill_then_confirm_imposes_lock_order_and_outcomes_over_many_iterationsAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();

        for (var index = 0; index < StressIterationCount; index++)
        {
            var seeded = await SeedSelectionVideAsync(factory);
            var competitionId = new CompetitionId(seeded.CompetitionId);
            var preview = await PreviewAsync(client, seeded);
            preview.StatusCode.Should().Be(HttpStatusCode.OK);
            var previewBody = await preview.Content.ReadFromJsonAsync<ResolutionReleaseResponse>(HostJson.Options);
            previewBody.Should().NotBeNull();

            var (_, confirmResponse) = await CompetitionOrderedWriterConcurrencySupport.RunOrderedCompetitionWritersAsync(
                fixture.ConnectionString,
                competitionId,
                () => BackfillAndAssertOccupantStillPresentAsync(factory, client, seeded),
                () => ConfirmAsync(client, seeded, previewBody.VersionToken));

            using (confirmResponse)
            {
                CompetitionOrderedWriterConcurrencySupport.AssertNoUnhandledPostgresContention(confirmResponse);
                confirmResponse.StatusCode.Should().Be(HttpStatusCode.OK);
                (await ReadOccupantsAsync(client, seeded.CompetitionId))
                    .Should().NotContain(occupant => occupant.SlotKey == "Champ");
            }
        }
    }

    [IntegrationFact]
    public async Task Confirm_then_backfill_imposes_lock_order_and_outcomes_over_many_iterationsAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();

        for (var index = 0; index < StressIterationCount; index++)
        {
            var seeded = await SeedSelectionVideAsync(factory);
            var competitionId = new CompetitionId(seeded.CompetitionId);
            var preview = await PreviewAsync(client, seeded);
            preview.StatusCode.Should().Be(HttpStatusCode.OK);
            var previewBody = await preview.Content.ReadFromJsonAsync<ResolutionReleaseResponse>(HostJson.Options);
            previewBody.Should().NotBeNull();

            var (confirmResponse, _) = await CompetitionOrderedWriterConcurrencySupport.RunOrderedCompetitionWritersAsync(
                fixture.ConnectionString,
                competitionId,
                () => ConfirmAsync(client, seeded, previewBody.VersionToken),
                () => BackfillInProcessAsync(factory, competitionId));

            using (confirmResponse)
            {
                CompetitionOrderedWriterConcurrencySupport.AssertNoUnhandledPostgresContention(confirmResponse);
                confirmResponse.StatusCode.Should().Be(HttpStatusCode.OK);
                (await ReadOccupantsAsync(client, seeded.CompetitionId))
                    .Should().NotContain(occupant => occupant.SlotKey == "Champ");
            }
        }
    }

    private static async Task BackfillAndAssertOccupantStillPresentAsync(
        PlayUpWebApplicationFactory factory,
        HttpClient client,
        Seed seeded)
    {
        await BackfillInProcessAsync(factory, new CompetitionId(seeded.CompetitionId));
        (await ReadOccupantsAsync(client, seeded.CompetitionId))
            .Should().Contain(occupant =>
                occupant.SlotKey == "Champ" && occupant.EntryId == seeded.EntryId);
    }

    private static async Task BackfillInProcessAsync(PlayUpWebApplicationFactory factory, CompetitionId competitionId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var executor = scope.ServiceProvider.GetRequiredService<UseCaseExecutor>();
        _ = await executor.BackfillResolutionProvenanceAsync(competitionId);
    }

    private async Task<Seed> SeedSelectionVideAsync(PlayUpWebApplicationFactory factory)
    {
        var (competition, league, terminal, entries, match) = Build(position: 9);
        await PersistAsync(factory, competition, [league, terminal], [match]);
        return new Seed(
            competition.Id.Value,
            terminal.Id.Value,
            entries[0].Value,
            "Champ",
            null,
            StructureResolutionOccupantTarget.Slot);
    }

    private (Competition Competition, Stage League, Stage Terminal, EntryId[] Entries, Match Match) Build(int? position)
    {
        var competition = Competition.Create(
            new CompetitionName($"Backfill overlap {Guid.NewGuid():N}"),
            SampleRegulations.Standard(),
            _clock);
        var entries = new[]
        {
            competition.AddEntry(TeamId.New(), "Nord", _clock).Id,
            competition.AddEntry(TeamId.New(), "Sud", _clock).Id
        };
        var league = Stage.Create(competition.Id, new StageName("League"), SampleRegulations.Standard(), _clock);
        league.AddMatchday(1, _clock);
        competition.AddStage(league.Id, _clock);
        var terminal = Stage.Create(competition.Id, new StageName("Terminal"), SampleRegulations.Standard(), _clock);
        terminal.AddSlot("Champ");
        competition.AddStage(terminal.Id, _clock);
        var match = Match.Create(competition.Id, league.Id, entries[0], entries[1], _clock);
        match.Start(_clock);
        match.Finish(new MatchResult(ResultType.Played, new Score(2, 0)), _clock);
        if (position is not { } selected)
        {
            return (competition, league, terminal, entries, match);
        }

        var path = new QualificationPath(
            1,
            QualificationSource.Overall(),
            new QualificationSelection(SelectionMode.Position, selected),
            QualificationDestination.ForSlot(terminal.Id, "Champ"));
        league.ReplaceQualificationRules(new QualificationRules([path]), _clock);
        var provenance = new ResolutionProvenance(
            ResolutionProvenanceKind.Qualification,
            ResolutionPathIdentifier.ForQualification(league.Id, path),
            "seed");
        terminal.AddResolvedPopulationEntry(entries[0], _clock, provenance);
        terminal.ApplyResolvedEntry("Champ", entries[0], _clock, provenance);

        return (competition, league, terminal, entries, match);
    }

    private static async Task PersistAsync(
        PlayUpWebApplicationFactory factory,
        Competition competition,
        IReadOnlyList<Stage> stages,
        IReadOnlyList<Match> matches)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stageRepository = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var matchRepository = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        competitions.Add(competition);
        foreach (var stage in stages)
        {
            stageRepository.Add(stage);
        }

        foreach (var match in matches)
        {
            matchRepository.Add(match);
        }

        await unitOfWork.SaveChangesAsync();
    }

    private static async Task<HttpResponseMessage> PreviewAsync(HttpClient client, Seed seeded) =>
        await client.PostAsJsonAsync(
            $"/stages/{seeded.StageId}/resolution/release-preview",
            new ResolutionReleasePreviewRequest(seeded.Target, seeded.EntryId, seeded.SlotKey, seeded.GroupId),
            HostJson.Options);

    private static async Task<HttpResponseMessage> ConfirmAsync(HttpClient client, Seed seeded, string token) =>
        await client.PostAsJsonAsync(
            $"/stages/{seeded.StageId}/resolution/release",
            new ResolutionReleaseConfirmRequest(seeded.Target, seeded.EntryId, seeded.SlotKey, seeded.GroupId, token),
            HostJson.Options);

    private static async Task<IReadOnlyList<StructureResolutionOccupantDto>> ReadOccupantsAsync(
        HttpClient client,
        Guid competitionId)
    {
        using var response = await client.GetAsync($"/competitions/{competitionId}/structure");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var view = await response.Content.ReadFromJsonAsync<StructureViewDto>(HostJson.Options);
        view.Should().NotBeNull();
        return [.. view.Stages.SelectMany(stage => stage.ResolutionOccupants ?? [])];
    }

    private sealed record Seed(
        Guid CompetitionId,
        Guid StageId,
        Guid EntryId,
        string? SlotKey,
        Guid? GroupId,
        StructureResolutionOccupantTarget Target);
}
