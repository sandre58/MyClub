// -----------------------------------------------------------------------
// <copyright file="ResolutionFreshnessStructureEndpointTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Competitions;
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
public sealed class ResolutionFreshnessStructureEndpointTests(HostPostgresFixture fixture)
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 30, 20, 0, 0, TimeSpan.Zero));

    [IntegrationFact]
    public async Task Structure_reports_current_then_stale_after_score_correctionAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seeded = await SeedAppliedQualificationAsync(factory, startLeague: false);

        using var client = factory.CreateClient();
        var before = await ReadOccupantsAsync(client, seeded.CompetitionId, "Terminal");
        before.Should().NotBeEmpty();
        before.Should().OnlyContain(occupant =>
            occupant.State == StructureResolutionFreshness.AJour
            && occupant.ProvenanceKind == ResolutionProvenanceKind.Qualification);

        using (var scope = factory.Services.CreateScope())
        {
            var matches = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var match = await matches.GetByIdForUpdateAsync(seeded.MatchId);
            match.Should().NotBeNull();
            match.CorrectMatchResult(new MatchResult(ResultType.Played, new Score(0, 2)), _clock);
            await unitOfWork.SaveChangesAsync();
        }

        var after = await ReadOccupantsAsync(client, seeded.CompetitionId, "Terminal");
        after.Should().Contain(occupant =>
            occupant.Target == StructureResolutionOccupantTarget.Slot
            && occupant.EntryId == seeded.QualifierId
            && occupant.State == StructureResolutionFreshness.Perime
            && occupant.StalenessCause == StructureResolutionStalenessCause.ResultatAmont);
    }

    [IntegrationFact]
    public async Task Structure_reports_rule_staleness_after_running_qualification_replaceAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seeded = await SeedAppliedQualificationAsync(factory, startLeague: true);

        using var client = factory.CreateClient();
        using var response = await client.PutAsJsonAsync(
            $"/stages/{seeded.LeagueId}/qualification-rules",
            new ReplaceStageQualificationRulesRequest(
            [
                new QualificationIntentRequest(
                    Guid.NewGuid(),
                    1,
                    QualificationIntentSourceKind.Overall,
                    2,
                    2,
                    seeded.TerminalId,
                    DestinationSlotKeys: ["Champ"]),
            ]),
            HostJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var occupants = await ReadOccupantsAsync(client, seeded.CompetitionId, "Terminal");
        occupants.Should().Contain(occupant =>
            occupant.Target == StructureResolutionOccupantTarget.Slot
            && occupant.EntryId == seeded.QualifierId
            && occupant.State == StructureResolutionFreshness.Perime
            && occupant.StalenessCause == StructureResolutionStalenessCause.RegleQualification);
    }

    private async Task<Seed> SeedAppliedQualificationAsync(PlayUpWebApplicationFactory factory, bool startLeague)
    {
        using var scope = factory.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var matches = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var competition = Competition.Create(
            new CompetitionName($"Freshness {Guid.NewGuid():N}"),
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
        if (startLeague)
        {
            PrepareStage.Execute(league, [league, terminal], _clock);
            league.Start(_clock);
        }

        stages.Add(league);
        stages.Add(terminal);
        matches.Add(match);
        await unitOfWork.SaveChangesAsync();
        return new Seed(competition.Id.Value, league.Id.Value, terminal.Id.Value, match.Id, home.Id.Value);
    }

    private static async Task<IReadOnlyList<StructureResolutionOccupantDto>> ReadOccupantsAsync(
        HttpClient client,
        Guid competitionId,
        string stageName)
    {
        using var response = await client.GetAsync($"/competitions/{competitionId}/structure");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var view = await response.Content.ReadFromJsonAsync<StructureViewDto>(HostJson.Options);
        view.Should().NotBeNull();
        var stage = view.Stages.Should().ContainSingle(candidate => candidate.Name == stageName).Subject;
        return stage.ResolutionOccupants ?? [];
    }

    private sealed record Seed(
        Guid CompetitionId,
        Guid LeagueId,
        Guid TerminalId,
        MatchId MatchId,
        Guid QualifierId);
}
