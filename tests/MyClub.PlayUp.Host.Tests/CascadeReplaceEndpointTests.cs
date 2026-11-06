// -----------------------------------------------------------------------
// <copyright file="CascadeReplaceEndpointTests.cs" company="Stéphane ANDRE">
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
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Standings;
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
public sealed class CascadeReplaceEndpointTests(HostPostgresFixture fixture)
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 11, 5, 18, 0, 0, TimeSpan.Zero));

    [IntegrationFact]
    public async Task Preview_then_confirm_replaces_the_slot_and_a_second_confirm_conflictsAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seeded = await SeedAsync(factory);

        using var client = factory.CreateClient();
        using var correct = await client.PutAsJsonAsync(
            $"/matches/{seeded.MatchId}/result",
            new FinishMatchRequest(ResultType.Played, 0, 2),
            HostJson.Options);
        correct.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var body = new CascadeReplacePreviewRequest(
            StructureResolutionOccupantTarget.Slot,
            seeded.QualifierId,
            "Champ",
            null);
        using var preview = await client.PostAsJsonAsync(
            $"/stages/{seeded.TerminalId}/resolution/cascade-preview",
            body,
            HostJson.Options);
        preview.StatusCode.Should().Be(HttpStatusCode.OK);
        var previewBody = await preview.Content.ReadFromJsonAsync<CascadeReplaceResponse>(HostJson.Options);
        previewBody.Should().NotBeNull();
        previewBody.PlayedMatches.Should().BeEmpty();
        previewBody.VersionToken.Should().NotBeNullOrWhiteSpace();

        using var confirm = await client.PostAsJsonAsync(
            $"/stages/{seeded.TerminalId}/resolution/cascade-replace",
            new CascadeReplaceConfirmRequest(
                StructureResolutionOccupantTarget.Slot,
                seeded.QualifierId,
                "Champ",
                null,
                previewBody.VersionToken),
            HostJson.Options);
        confirm.StatusCode.Should().Be(HttpStatusCode.OK);

        var occupants = await ReadOccupantsAsync(client, seeded.CompetitionId);
        occupants.Should().Contain(occupant =>
            occupant.SlotKey == "Champ" && occupant.EntryId == seeded.ChallengerId);

        using var again = await client.PostAsJsonAsync(
            $"/stages/{seeded.TerminalId}/resolution/cascade-replace",
            new CascadeReplaceConfirmRequest(
                StructureResolutionOccupantTarget.Slot,
                seeded.QualifierId,
                "Champ",
                null,
                previewBody.VersionToken),
            HostJson.Options);
        again.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await again.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options);
        problem.Should().NotBeNull();
        GetCode(problem).Should().Be(ApplicationErrorCodes.CascadeReplaceConflict);
    }

    [IntegrationFact]
    public async Task Confirm_invalidates_a_finished_downstream_matchAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seeded = await SeedAsync(factory, DownstreamMatch.Finished);
        var finalMatchId = seeded.FinalMatchId!.Value;

        using var client = factory.CreateClient();
        using var correct = await client.PutAsJsonAsync(
            $"/matches/{seeded.MatchId}/result",
            new FinishMatchRequest(ResultType.Played, 0, 2),
            HostJson.Options);
        correct.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var body = new CascadeReplacePreviewRequest(
            StructureResolutionOccupantTarget.Slot,
            seeded.QualifierId,
            "Champ",
            null);
        using var preview = await client.PostAsJsonAsync(
            $"/stages/{seeded.TerminalId}/resolution/cascade-preview",
            body,
            HostJson.Options);
        preview.StatusCode.Should().Be(HttpStatusCode.OK);
        var previewBody = await preview.Content.ReadFromJsonAsync<CascadeReplaceResponse>(HostJson.Options);
        previewBody.Should().NotBeNull();
        previewBody.PlayedMatches.Should().ContainSingle().Which.MatchId.Should().Be(finalMatchId);

        using var confirm = await client.PostAsJsonAsync(
            $"/stages/{seeded.TerminalId}/resolution/cascade-replace",
            new CascadeReplaceConfirmRequest(
                StructureResolutionOccupantTarget.Slot,
                seeded.QualifierId,
                "Champ",
                null,
                previewBody.VersionToken),
            HostJson.Options);
        confirm.StatusCode.Should().Be(HttpStatusCode.OK);

        using var detail = await client.GetAsync($"/matches/{finalMatchId}");
        detail.StatusCode.Should().Be(HttpStatusCode.OK);
        var match = await detail.Content.ReadFromJsonAsync<MatchDetailDto>(HostJson.Options);
        match.Should().NotBeNull();
        match.Status.Should().Be(MatchStatus.Scheduled);
        match.Result.Should().BeNull();
        match.Home.EntryId.Should().Be(seeded.ChallengerId);
        match.DeclaredParticipations.Should().BeNullOrEmpty();
        match.RecordedGoals.Should().BeNullOrEmpty();
        match.RecordedSubstitutions.Should().BeNullOrEmpty();
        match.RecordedDisciplinaryEvents.Should().BeNullOrEmpty();

        using var scope = factory.Services.CreateScope();
        var matches = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
        var reloaded = await matches.GetByIdReadOnlyAsync(new MatchId(finalMatchId));
        reloaded.Should().NotBeNull();
        StandingMatchAssembler.Assemble([reloaded]).Should().BeEmpty();
    }

    [IntegrationFact]
    public async Task Preview_refuses_a_live_downstream_matchAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seeded = await SeedAsync(factory, DownstreamMatch.Live);
        var finalMatchId = seeded.FinalMatchId!.Value;

        using var client = factory.CreateClient();
        using var correct = await client.PutAsJsonAsync(
            $"/matches/{seeded.MatchId}/result",
            new FinishMatchRequest(ResultType.Played, 0, 2),
            HostJson.Options);
        correct.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var preview = await client.PostAsJsonAsync(
            $"/stages/{seeded.TerminalId}/resolution/cascade-preview",
            new CascadeReplacePreviewRequest(
                StructureResolutionOccupantTarget.Slot,
                seeded.QualifierId,
                "Champ",
                null),
            HostJson.Options);
        preview.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await preview.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options);
        GetCode(problem!).Should().Be(ApplicationErrorCodes.CascadeReplaceLiveBlocked);

        using var detail = await client.GetAsync($"/matches/{finalMatchId}");
        detail.StatusCode.Should().Be(HttpStatusCode.OK);
        var match = await detail.Content.ReadFromJsonAsync<MatchDetailDto>(HostJson.Options);
        match.Should().NotBeNull();
        match.Status.Should().Be(MatchStatus.Live);
        match.Home.EntryId.Should().Be(seeded.QualifierId);

        var occupants = await ReadOccupantsAsync(client, seeded.CompetitionId);
        occupants.Should().Contain(occupant =>
            occupant.SlotKey == "Champ" && occupant.EntryId == seeded.QualifierId);
    }

    private async Task<Seed> SeedAsync(
        PlayUpWebApplicationFactory factory,
        DownstreamMatch downstream = DownstreamMatch.None)
    {
        using var scope = factory.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var matches = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var competition = Competition.Create(
            new CompetitionName($"Cascade {Guid.NewGuid():N}"),
            SampleRegulations.Standard(),
            _clock);
        var home = competition.AddEntry(TeamId.New(), "Alpha", _clock);
        var away = competition.AddEntry(TeamId.New(), "Beta", _clock);
        competitions.Add(competition);

        var league = Stage.Create(competition.Id, new StageName("League"), SampleRegulations.Standard(), _clock);
        league.AddMatchday(1, _clock);
        competition.AddStage(league.Id, _clock);
        var terminal = Stage.Create(competition.Id, new StageName("Terminal"), SampleRegulations.Standard(), _clock);
        terminal.AddSlot("Champ");
        competition.AddStage(terminal.Id, _clock);

        var match = Match.Create(competition.Id, league.Id, home.Id, away.Id, _clock);
        match.Start(_clock);
        match.Finish(new MatchResult(ResultType.Played, new Score(2, 0)), _clock);
        var standing = CalculateStanding.Execute(
            [home.Id, away.Id],
            [match],
            league.Regulation.StandingRules!);
        league.ReplaceQualificationRules(
            new QualificationRules(
            [
                new QualificationPath(
                    1,
                    QualificationSource.Overall(),
                    new QualificationSelection(SelectionMode.Position, 1),
                    QualificationDestination.ForSlot(terminal.Id, "Champ")),
            ]),
            _clock);
        ApplyQualification.Execute(league, standing, [league, terminal], _clock);
        Match? final = null;
        if (downstream != DownstreamMatch.None)
        {
            var gamma = competition.AddEntry(TeamId.New(), "Gamma", _clock);
            var round = terminal.AddRound("Final", new TieFormat(TieFormat.SingleLeg, aggregateScoring: false), _clock);
            terminal.AddSlot("Finalist");
            terminal.ReplaceBracketPairs([new BracketPair("P1", "Champ", "Finalist")]);
            var fixture = terminal.AddFixture(round.Id, _clock, "Champ", "Finalist", "P1");
            terminal.AddResolvedPopulationEntry(gamma.Id, _clock);
            terminal.AssignEntryToSlot("Finalist", gamma.Id);
            final = Match.Create(competition.Id, terminal.Id, home.Id, gamma.Id, _clock);
            terminal.AttachMatch(fixture.Id, final.Id, legIndex: 1, _clock);
            if (downstream == DownstreamMatch.Live)
            {
                final.Start(_clock);
            }
            else
            {
                final.Finish(new MatchResult(ResultType.Played, new Score(1, 0)), _clock);
                var scorer = MemberId.New();
                final.AddDeclaredParticipation(scorer, Side.Home, CompositionStatus.Starter, _clock, jerseyNumber: 9);
                final.RecordGoal(scorer, Side.Home, _clock);
            }
        }

        PrepareStage.Execute(league, [league, terminal], _clock);
        league.Start(_clock);

        stages.Add(league);
        stages.Add(terminal);
        matches.Add(match);
        if (final is not null)
        {
            matches.Add(final);
        }

        await unitOfWork.SaveChangesAsync();
        return new Seed(
            competition.Id.Value,
            terminal.Id.Value,
            match.Id.Value,
            home.Id.Value,
            away.Id.Value,
            final?.Id.Value);
    }

    private static async Task<IReadOnlyList<StructureResolutionOccupantDto>> ReadOccupantsAsync(
        HttpClient client,
        Guid competitionId)
    {
        using var response = await client.GetAsync($"/competitions/{competitionId}/structure");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var view = await response.Content.ReadFromJsonAsync<StructureViewDto>(HostJson.Options);
        view.Should().NotBeNull();
        var stage = view.Stages.Should().ContainSingle(candidate => candidate.Name == "Terminal").Subject;
        return stage.ResolutionOccupants ?? [];
    }

    private static string? GetCode(ProblemDetails problem) =>
        !problem.Extensions.TryGetValue("code", out var raw) || raw is null
            ? null
            : raw switch
            {
                string text => text,
                JsonElement { ValueKind: JsonValueKind.String } element => element.GetString(),
                _ => raw.ToString()
            };

    private sealed record Seed(
        Guid CompetitionId,
        Guid TerminalId,
        Guid MatchId,
        Guid QualifierId,
        Guid ChallengerId,
        Guid? FinalMatchId = null);

    private enum DownstreamMatch
    {
        None,
        Finished,
        Live,
    }
}
