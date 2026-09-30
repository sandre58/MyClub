// -----------------------------------------------------------------------
// <copyright file="ApplyProgressionOutcomeEndpointTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using MyClub.PlayUp.Application;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.TestKit;
using Xunit;

namespace MyClub.PlayUp.Host.Tests;

[Collection("host-postgres")]
[Trait("Category", "Integration")]
public sealed class ApplyProgressionOutcomeEndpointTests(HostPostgresFixture fixture)
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 14, 18, 0, 0, TimeSpan.Zero));

    [IntegrationFact]
    public async Task Apply_progression_returns_204_and_persists_destination_populationAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedR2QuarterToSemiAsync(factory);

        using var client = factory.CreateClient();
        using var response = await client.PostAsync(ApplyUri(seed.SourceStageId, seed.FixtureId), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = factory.Services.CreateScope();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var destination = await stages.GetByIdForUpdateAsync(seed.DestinationStageId);
        destination.Should().NotBeNull();
        destination.CompositionEntries.Select(e => e.EntryId).Should().Equal(seed.Home);
        destination.FindSlot("SF1-A")!.EntryId.Should().BeNull();
        destination.FindSlot("SF1-B")!.EntryId.Should().BeNull();
    }

    [IntegrationFact]
    public async Task Apply_progression_when_stage_missing_returns_404Async()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);

        using var client = factory.CreateClient();
        using var response = await client.PostAsync(
            ApplyUri(StageId.New(), FixtureId.New()),
            content: null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options);
        problem.Should().NotBeNull();
        GetCode(problem).Should().Be(ApplicationErrorCodes.StageNotFound);
    }

    [IntegrationFact]
    public async Task Apply_progression_when_fixture_missing_returns_409Async()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedR2QuarterToSemiAsync(factory);

        using var client = factory.CreateClient();
        using var response = await client.PostAsync(
            ApplyUri(seed.SourceStageId, FixtureId.New()),
            content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options);
        problem.Should().NotBeNull();
        GetCode(problem).Should().Be(StageErrorCodes.FixtureNotFound);
    }

    private static Uri ApplyUri(StageId stageId, FixtureId fixtureId) =>
        new($"/stages/{stageId.Value}/fixtures/{fixtureId.Value}/apply-progression", UriKind.Relative);

    private static string? GetCode(ProblemDetails problem) =>
        !problem.Extensions.TryGetValue("code", out var raw) || raw is null
            ? null
            : raw switch
            {
                string text => text,
                JsonElement { ValueKind: JsonValueKind.String } element => element.GetString(),
                _ => raw.ToString()
            };

    private async Task<R2Seed> SeedR2QuarterToSemiAsync(PlayUpWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var situation = TestCompetition.Create("Coupe du club", _clock);

        var quarter = situation.AddKnockoutStage(
            "QF",
            "R1",
            ["QF1-A", "QF1-B"],
            seedBracketPairs: false);
        var semi = situation.AddKnockoutStage(
            "SF",
            "R1",
            ["SF1-A", "SF1-B"],
            seedBracketPairs: false);

        var home = EntryId.New();
        var away = EntryId.New();
        quarter.ReplaceBracketPairs([new BracketPair("P1", "QF1-A", "QF1-B")]);
        var addFixture = quarter.AddFixture(quarter.Rounds[0].Id, _clock, "QF1-A", "QF1-B", "P1");
        var match = Match.Create(situation.Competition.Id, quarter.Id, home, away, _clock);
        quarter.AttachMatch(addFixture.Id, match.Id, legIndex: 1, _clock);
        match.Start(_clock);
        match.Finish(new MatchResult(ResultType.Played, new Score(2, 0)), _clock);
        situation.TrackMatch(match);

        situation.WithProgressionPaths(
            quarter,
            new ProgressionPathSpec("P1", ProgressionOutcome.Winner, semi.Id));

        await HostTestPersist.PersistAsync(scope.ServiceProvider, situation);

        return new R2Seed(quarter.Id, semi.Id, addFixture.Id, home);
    }

    private sealed record R2Seed(
        StageId SourceStageId,
        StageId DestinationStageId,
        FixtureId FixtureId,
        EntryId Home);
}
