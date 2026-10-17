// -----------------------------------------------------------------------
// <copyright file="ReplaceStageProgressionRulesGOps3EndpointTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using MyClub.PlayUp.Application;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Host.Contracts;
using MyClub.PlayUp.TestKit;
using Xunit;

namespace MyClub.PlayUp.Host.Tests;

[Collection("host-postgres")]
[Trait("Category", "Integration")]
public sealed class ReplaceStageProgressionRulesGOps3EndpointTests(HostPostgresFixture fixture)
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 16, 20, 0, 0, TimeSpan.Zero));

    [IntegrationFact]
    public async Task Put_progression_intents_when_draw_applied_and_downstream_draft_returns_204_R_Prog_OPS3_A1_Async()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedAppliedDrawWithoutProgressionAsync(factory);

        using var client = factory.CreateClient();
        using (var publish = await client.PostAsync(PublishDrawUri(seed.QuarterStageId, seed.DrawId), content: null))
        {
            publish.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        using (var apply = await client.PostAsync(ApplyDrawUri(seed.QuarterStageId, seed.DrawId), content: null))
        {
            apply.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        using var response = await client.PutAsJsonAsync(
            ProgressionRulesUri(seed.QuarterStageId),
            new ReplaceStageProgressionRulesRequest(
            [
                new ProgressionIntentRequest(
                    IntentId: null,
                    Order: 1,
                    RoundId: seed.RoundId.Value,
                    Outcome: ProgressionOutcome.Winner,
                    DestinationStageId: seed.DownstreamStageId.Value)
            ]),
            HostJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [IntegrationFact]
    public async Task Put_clear_progression_when_downstream_production_participates_returns_409_ProgressionDownstreamProductionBlocked_R_Prog_OPS3_2c_Async()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedDownstreamProductionParticipationWithoutApplyProgressionAsync(factory);

        using var client = factory.CreateClient();
        using var response = await client.PutAsJsonAsync(
            ProgressionRulesUri(seed.SourceStageId),
            new ReplaceStageProgressionRulesRequest(Intents: []),
            HostJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options);
        GetCode(problem!).Should().Be(ApplicationErrorCodes.ProgressionDownstreamProductionBlocked);
    }

    [IntegrationFact]
    public async Task Put_clear_progression_after_population_consumed_returns_409_ProgressionFeedConsumed_R_Prog_OPS3_B2_Async()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedPopulationProgressionConsumedAsync(factory);

        using var client = factory.CreateClient();
        using var response = await client.PutAsJsonAsync(
            ProgressionRulesUri(seed.SourceStageId),
            new ReplaceStageProgressionRulesRequest(Intents: []),
            HostJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options);
        GetCode(problem!).Should().Be(ApplicationErrorCodes.ProgressionFeedConsumed);
    }

    private static Uri ProgressionRulesUri(StageId stageId) =>
        new($"/stages/{stageId.Value}/progression-rules", UriKind.Relative);

    private static Uri ApplyDrawUri(StageId stageId, DrawId drawId) =>
        new($"/stages/{stageId.Value}/draws/{drawId.Value}/apply", UriKind.Relative);

    private static Uri PublishDrawUri(StageId stageId, DrawId drawId) =>
        new($"/stages/{stageId.Value}/draws/{drawId.Value}/publish", UriKind.Relative);

    private static string? GetCode(ProblemDetails problem) =>
        !problem.Extensions.TryGetValue("code", out var raw) || raw is null
            ? null
            : raw switch
            {
                string text => text,
                JsonElement { ValueKind: JsonValueKind.String } element => element.GetString(),
                _ => raw.ToString()
            };

    private async Task<AppliedDrawSeed> SeedAppliedDrawWithoutProgressionAsync(PlayUpWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var situation = TestCompetition.Create("G-OPS3-prog-host-A1", _clock)
            .WithTeams("Home FC", "Away FC");

        var quarter = situation.AddKnockoutStage("QF", "R1", ["S1", "S2"]);
        var downstream = situation.AddBareStage("SF");
        var home = situation.Competition.Entries[0].Id;
        var away = situation.Competition.Entries[1].Id;
        var drawId = situation.CreateResolvedSlotDraw(quarter, [home, away], ["S1", "S2"]);
        var roundId = quarter.Rounds[0].Id;

        await HostTestPersist.PersistAsync(scope.ServiceProvider, situation);

        return new AppliedDrawSeed(quarter.Id, downstream.Id, drawId, roundId);
    }

    private async Task<DownstreamProductionSeed> SeedDownstreamProductionParticipationWithoutApplyProgressionAsync(
        PlayUpWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var situation = TestCompetition.Create("G-OPS3-prog-host-2c", _clock)
            .WithTeams("Home FC", "Away FC", "Other FC");

        var source = situation.AddKnockoutStage("QF", "R1", ["QF-A", "QF-B"]);
        var destination = situation.AddKnockoutStage("SF", "R1", ["SF-A", "SF-B"]);
        source.ReplaceProgressionRules(
            new ProgressionRules(
            [
                new ProgressionPath(
                    "P1",
                    ProgressionOutcome.Winner,
                    ProgressionDestination.ForPopulation(destination.Id))
            ]),
            _clock);

        var home = situation.Competition.Entries[0].Id;
        var away = situation.Competition.Entries[1].Id;
        var other = situation.Competition.Entries[2].Id;
        var upstreamFixture = source.AddFixture(source.Rounds[0].Id, _clock, "QF-A", "QF-B", "P1");
        var upstreamMatch = Match.Create(situation.Competition.Id, source.Id, home, away, _clock);
        source.AttachMatch(upstreamFixture.Id, upstreamMatch.Id, legIndex: 1, _clock);
        upstreamMatch.Start(_clock);
        upstreamMatch.Finish(new MatchResult(ResultType.Played, new Score(2, 0)), _clock);
        situation.TrackMatch(upstreamMatch);

        var downstreamMatch = Match.Create(situation.Competition.Id, destination.Id, home, other, _clock);
        var downstreamFixture = destination.AddFixture(destination.Rounds[0].Id, _clock, "SF-A", "SF-B", "P1");
        destination.AttachMatch(downstreamFixture.Id, downstreamMatch.Id, legIndex: 1, _clock);
        situation.TrackMatch(downstreamMatch);

        await HostTestPersist.PersistAsync(scope.ServiceProvider, situation);

        return new DownstreamProductionSeed(source.Id);
    }

    private async Task<ConsumedPopulationSeed> SeedPopulationProgressionConsumedAsync(PlayUpWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var situation = TestCompetition.Create("G-OPS3-prog-host-B2", _clock)
            .WithTeams("Home FC", "Away FC");

        var source = situation.AddKnockoutStage("QF", "R1", ["QF-A", "QF-B"]);
        var destination = situation.AddBareStage("Finale");
        source.ReplaceProgressionRules(
            new ProgressionRules(
            [
                new ProgressionPath(
                    "P1",
                    ProgressionOutcome.Winner,
                    ProgressionDestination.ForPopulation(destination.Id))
            ]),
            _clock);

        var home = situation.Competition.Entries[0].Id;
        var away = situation.Competition.Entries[1].Id;
        var addFixture = source.AddFixture(source.Rounds[0].Id, _clock, "QF-A", "QF-B", "P1");
        var match = Match.Create(situation.Competition.Id, source.Id, home, away, _clock);
        source.AttachMatch(addFixture.Id, match.Id, legIndex: 1, _clock);
        match.Start(_clock);
        match.Finish(new MatchResult(ResultType.Played, new Score(1, 0)), _clock);
        situation.TrackMatch(match);

        ApplyProgressionOutcome.Execute(source, addFixture.Id, [match], [source, destination], _clock);

        await HostTestPersist.PersistAsync(scope.ServiceProvider, situation);

        return new ConsumedPopulationSeed(source.Id);
    }

    private sealed record AppliedDrawSeed(
        StageId QuarterStageId,
        StageId DownstreamStageId,
        DrawId DrawId,
        RoundId RoundId);

    private sealed record ConsumedPopulationSeed(StageId SourceStageId);

    private sealed record DownstreamProductionSeed(StageId SourceStageId);
}
