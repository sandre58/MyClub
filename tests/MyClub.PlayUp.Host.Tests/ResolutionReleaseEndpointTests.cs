// -----------------------------------------------------------------------
// <copyright file="ResolutionReleaseEndpointTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using MyClub.PlayUp.Application;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Host.Contracts;
using Xunit;

namespace MyClub.PlayUp.Host.Tests;

[Collection("host-postgres")]
[Trait("Category", "Integration")]
public sealed class ResolutionReleaseEndpointTests(HostPostgresFixture fixture)
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 11, 11, 18, 0, 0, TimeSpan.Zero));

    [IntegrationFact]
    public async Task Preview_then_confirm_clears_the_place_and_a_second_confirm_conflictsAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seeded = await SeedSelectionVideAsync(factory);
        using var client = factory.CreateClient();

        var before = await ReadOccupantsAsync(client, seeded.CompetitionId);
        before.Should().Contain(occupant => occupant.SlotKey == "Champ" && occupant.Releasable);

        var preview = await PreviewAsync(client, seeded);
        preview.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await preview.Content.ReadFromJsonAsync<ResolutionReleaseResponse>(HostJson.Options);
        body.Should().NotBeNull();
        body.Releases.Should().ContainSingle().Which.EntryId.Should().Be(seeded.EntryId);

        var confirm = await ConfirmAsync(client, seeded, body.VersionToken);
        confirm.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadOccupantsAsync(client, seeded.CompetitionId))
            .Should().NotContain(occupant => occupant.SlotKey == "Champ");

        var again = await ConfirmAsync(client, seeded, body.VersionToken);
        again.StatusCode.Should().Be(HttpStatusCode.Conflict);
        GetCode(await again.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options))
            .Should().Be(ApplicationErrorCodes.ResolutionReleaseConflict);

        var empty = await PreviewAsync(client, seeded);
        empty.StatusCode.Should().Be(HttpStatusCode.NotFound);
        GetCode(await empty.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options))
            .Should().Be(ApplicationErrorCodes.ResolutionReleaseOccupantNotFound);
    }

    [IntegrationFact]
    public async Task A_changed_token_conflicts_and_leaves_the_placeAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seeded = await SeedSelectionVideAsync(factory);
        using var client = factory.CreateClient();
        var preview = await PreviewAsync(client, seeded);
        var body = await preview.Content.ReadFromJsonAsync<ResolutionReleaseResponse>(HostJson.Options);

        using (var scope = factory.Services.CreateScope())
        {
            var matches = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var match = await matches.GetByIdForUpdateAsync(new MatchId(seeded.MatchId));
            match!.CorrectMatchResult(new MatchResult(ResultType.Played, new Score(3, 0)), _clock);
            await unitOfWork.SaveChangesAsync();
        }

        var confirm = await ConfirmAsync(client, seeded, body!.VersionToken);
        confirm.StatusCode.Should().Be(HttpStatusCode.Conflict);
        GetCode(await confirm.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options))
            .Should().Be(ApplicationErrorCodes.ResolutionReleaseConflict);
        (await ReadOccupantsAsync(client, seeded.CompetitionId))
            .Should().Contain(occupant => occupant.SlotKey == "Champ" && occupant.EntryId == seeded.EntryId);
    }

    [IntegrationFact]
    public async Task Each_refusal_is_a_conflictAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var cases = new (Func<PlayUpWebApplicationFactory, Task<Seed>> Seed, string Code)[]
        {
            (SeedCurrentAsync, ApplicationErrorCodes.ResolutionReleaseNotIndeterminate),
            (SeedDirectAsync, ApplicationErrorCodes.ResolutionReleaseDirectAssignment),
            (SeedCompletedAsync, ApplicationErrorCodes.ResolutionReleaseCompleted),
            (SeedPlayedAsync, ApplicationErrorCodes.ResolutionReleaseBlockedByPlayedMatch),
            (SeedAttachedAsync, ApplicationErrorCodes.ResolutionReleaseBlockedByMatches),
            (SeedRunningAsync, ApplicationErrorCodes.ResolutionReleaseBlockedByMatchesRunning),
            (SeedDrawAsync, ApplicationErrorCodes.ResolutionReleaseBlockedByDraw),
            (SeedConfigurationAsync, ApplicationErrorCodes.ResolutionReleaseNotReleasable),
            (SeedReferencedPopulationAsync, ApplicationErrorCodes.ResolutionReleaseNotReleasable),
        };

        using var client = factory.CreateClient();
        foreach (var (seed, code) in cases)
        {
            var seeded = await seed(factory);
            var preview = await PreviewAsync(client, seeded);
            preview.StatusCode.Should().Be(HttpStatusCode.Conflict);
            GetCode(await preview.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options)).Should().Be(code);
        }
    }

    private async Task<Seed> SeedSelectionVideAsync(PlayUpWebApplicationFactory factory) =>
        await SaveAsync(factory, position: 9);

    private async Task<Seed> SeedCurrentAsync(PlayUpWebApplicationFactory factory) =>
        await SaveAsync(factory, position: 1);

    private async Task<Seed> SeedDirectAsync(PlayUpWebApplicationFactory factory)
    {
        var (competition, league, terminal, entries, match) = Build(position: null);
        terminal.AddResolvedPopulationEntry(entries[1], _clock);
        terminal.AssignEntryToSlot("Champ", entries[1]);
        await PersistAsync(factory, competition, [league, terminal], [match]);
        return new Seed(competition.Id.Value, terminal.Id.Value, entries[1].Value, "Champ", null, StructureResolutionOccupantTarget.Slot, match.Id.Value);
    }

    private async Task<Seed> SeedCompletedAsync(PlayUpWebApplicationFactory factory)
    {
        var seeded = await SaveAsync(factory, position: 9, persist: false);
        return seeded;
    }

    private async Task<Seed> SeedPlayedAsync(PlayUpWebApplicationFactory factory)
    {
        var (competition, league, terminal, entries, match) = Build(position: 9);
        var played = Match.Create(competition.Id, terminal.Id, entries[0], entries[1], _clock);
        played.Start(_clock);
        played.Finish(new MatchResult(ResultType.Played, new Score(1, 0)), _clock);
        await PersistAsync(factory, competition, [league, terminal], [match, played]);
        return SlotSeed(competition, terminal, entries[0], match);
    }

    private async Task<Seed> SeedAttachedAsync(PlayUpWebApplicationFactory factory) =>
        await SeedAttachedAsync(factory, running: false);

    private async Task<Seed> SeedRunningAsync(PlayUpWebApplicationFactory factory) =>
        await SeedAttachedAsync(factory, running: true);

    private async Task<Seed> SeedAttachedAsync(PlayUpWebApplicationFactory factory, bool running)
    {
        var (competition, league, terminal, entries, match) = Build(position: 9);
        var matchday = terminal.AddMatchday(1, _clock);
        var fixture = terminal.AddFixture(matchday.Id, _clock);
        var attached = Match.Create(competition.Id, terminal.Id, entries[0], entries[1], _clock);
        terminal.AttachMatch(fixture.Id, attached.Id, 1, _clock);
        if (running)
        {
            terminal.Prepare(_clock);
            terminal.Start(_clock);
        }

        await PersistAsync(factory, competition, [league, terminal], [match, attached]);
        return SlotSeed(competition, terminal, entries[0], match);
    }

    private async Task<Seed> SeedDrawAsync(PlayUpWebApplicationFactory factory)
    {
        var (competition, league, terminal, entries, match) = Build(position: 9);
        var draw = terminal.CreateDraw(DrawResolutionKind.Slot, _clock);
        terminal.ConfigureDrawInputs(draw.Id, DrawInputs.ForSlot([entries[0]]));
        terminal.RecordDrawResolution(
            draw.Id,
            DrawResolution.ResolvedSlots([new SlotDrawPlacement(entries[0], "Champ")]),
            _clock);
        terminal.PublishDraw(draw.Id, _clock);
        await PersistAsync(factory, competition, [league, terminal], [match]);
        return SlotSeed(competition, terminal, entries[0], match);
    }

    private async Task<Seed> SeedConfigurationAsync(PlayUpWebApplicationFactory factory)
    {
        var competition = Competition.Create(new CompetitionName($"Config {Guid.NewGuid():N}"), SampleRegulations.Standard(), _clock);
        var entry = competition.AddEntry(TeamId.New(), "Nord", _clock);
        var source = Stage.Create(competition.Id, new StageName("Tableau"), SampleRegulations.Standard(), _clock);
        source.AddRound("Tour", new TieFormat(TieFormat.SingleLeg, aggregateScoring: false), _clock);
        competition.AddStage(source.Id, _clock);
        var terminal = Stage.Create(competition.Id, new StageName("Terminal"), SampleRegulations.Standard(), _clock);
        terminal.AddSlot("Champ");
        competition.AddStage(terminal.Id, _clock);
        var path = new QualificationPath(
            1,
            QualificationSource.Overall(),
            new QualificationSelection(SelectionMode.Position, 1),
            QualificationDestination.ForSlot(terminal.Id, "Champ"));
        source.ReplaceQualificationRules(new QualificationRules([path]), _clock);
        var provenance = new ResolutionProvenance(
            ResolutionProvenanceKind.Qualification,
            ResolutionPathIdentifier.ForQualification(source.Id, path),
            "seed");
        terminal.AddResolvedPopulationEntry(entry.Id, _clock, provenance);
        terminal.ApplyResolvedEntry("Champ", entry.Id, _clock, provenance);
        await PersistAsync(factory, competition, [source, terminal], []);
        return new Seed(competition.Id.Value, terminal.Id.Value, entry.Id.Value, "Champ", null, StructureResolutionOccupantTarget.Slot, Guid.Empty);
    }

    private async Task<Seed> SeedReferencedPopulationAsync(PlayUpWebApplicationFactory factory)
    {
        var (competition, league, terminal, entries, match) = Build(position: null);
        var path = new QualificationPath(
            1,
            QualificationSource.Overall(),
            new QualificationSelection(SelectionMode.Position, 9),
            QualificationDestination.ForPopulation(terminal.Id));
        league.ReplaceQualificationRules(new QualificationRules([path]), _clock);
        var provenance = new ResolutionProvenance(
            ResolutionProvenanceKind.Qualification,
            ResolutionPathIdentifier.ForQualification(league.Id, path),
            "seed");
        var group = terminal.AddGroup("A", _clock);
        terminal.AddResolvedPopulationEntry(entries[0], _clock, provenance);
        terminal.ApplyResolvedGroupEntry(group.Id, entries[0], provenance);
        await PersistAsync(factory, competition, [league, terminal], [match]);
        return new Seed(
            competition.Id.Value,
            terminal.Id.Value,
            entries[0].Value,
            null,
            null,
            StructureResolutionOccupantTarget.Population,
            match.Id.Value);
    }

    private async Task<Seed> SaveAsync(PlayUpWebApplicationFactory factory, int position, bool persist = true)
    {
        var (competition, league, terminal, entries, match) = Build(position);
        if (position == 9 && !persist)
        {
            terminal.AddMatchday(1, _clock);
            terminal.Prepare(_clock);
            terminal.Start(_clock);
            terminal.Complete(_clock);
        }

        await PersistAsync(factory, competition, [league, terminal], [match]);
        return SlotSeed(competition, terminal, entries[0], match);
    }

    private (Competition Competition, Stage League, Stage Terminal, EntryId[] Entries, Match Match) Build(int? position)
    {
        var competition = Competition.Create(
            new CompetitionName($"Release {Guid.NewGuid():N}"),
            SampleRegulations.Standard(),
            _clock);
        var entries = new[]
        {
            competition.AddEntry(TeamId.New(), "Nord", _clock).Id,
            competition.AddEntry(TeamId.New(), "Sud", _clock).Id,
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
        if (position is int selected)
        {
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
        }

        return (competition, league, terminal, entries, match);
    }

    private static async Task<Seed> PersistAsync(
        PlayUpWebApplicationFactory factory,
        Competition competition,
        IReadOnlyList<Stage> stages,
        IReadOnlyList<Match> matches)
    {
        using var scope = factory.Services.CreateScope();
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
        return new Seed(competition.Id.Value, Guid.Empty, Guid.Empty, null, null, StructureResolutionOccupantTarget.Slot, Guid.Empty);
    }

    private static Seed SlotSeed(Competition competition, Stage terminal, EntryId entry, Match match) =>
        new(competition.Id.Value, terminal.Id.Value, entry.Value, "Champ", null, StructureResolutionOccupantTarget.Slot, match.Id.Value);

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
        return view.Stages.SelectMany(stage => stage.ResolutionOccupants ?? []).ToArray();
    }

    private static string? GetCode(ProblemDetails? problem) =>
        problem is null || !problem.Extensions.TryGetValue("code", out var raw) || raw is null
            ? null
            : raw switch
            {
                string text => text,
                JsonElement { ValueKind: JsonValueKind.String } element => element.GetString(),
                _ => raw.ToString()
            };

    private sealed record Seed(
        Guid CompetitionId,
        Guid StageId,
        Guid EntryId,
        string? SlotKey,
        Guid? GroupId,
        StructureResolutionOccupantTarget Target,
        Guid MatchId);
}
