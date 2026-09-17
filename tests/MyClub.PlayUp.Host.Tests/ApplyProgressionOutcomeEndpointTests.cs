// -----------------------------------------------------------------------
// <copyright file="ApplyProgressionOutcomeEndpointTests.cs" company="Stéphane ANDRE">
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
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
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
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var matches = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var competition = Competition.Create(new CompetitionName("Coupe du club"), SampleRegulations.Standard(), _clock);
        competitions.Add(competition);

        var quarter = Stage.Create(competition.Id, new StageName("QF"), SampleRegulations.Standard(), _clock);
        quarter.AddRound("R1", new TieFormat(TieFormat.SingleLeg, aggregateScoring: false), _clock);
        quarter.AddSlot("QF1-A");
        quarter.AddSlot("QF1-B");

        var semi = Stage.Create(competition.Id, new StageName("SF"), SampleRegulations.Standard(), _clock);
        semi.AddRound("R1", new TieFormat(TieFormat.SingleLeg, aggregateScoring: false), _clock);
        semi.AddSlot("SF1-A");
        semi.AddSlot("SF1-B");

        competition.AddStage(quarter.Id, _clock);
        competition.AddStage(semi.Id, _clock);

        var home = EntryId.New();
        var away = EntryId.New();
        var addFixture = quarter.AddFixture(quarter.Rounds[0].Id, _clock);
        var match = Match.Create(competition.Id, quarter.Id, home, away, _clock);
        quarter.AttachMatch(addFixture.Id, match.Id, legIndex: 1, _clock);
        match.Start(_clock);
        match.Finish(new MatchResult(ResultType.Played, new Score(2, 0)), _clock);

        quarter.ReplaceProgressionRules(
            new ProgressionRules(
            [
                new ProgressionPath(
                    addFixture.Id,
                    ProgressionOutcome.Winner,
                    ProgressionDestination.ForPopulation(semi.Id))
            ]),
            _clock);

        stages.Add(quarter);
        stages.Add(semi);
        matches.Add(match);
        await unitOfWork.SaveChangesAsync();

        return new R2Seed(quarter.Id, semi.Id, addFixture.Id, home);
    }

    private sealed record R2Seed(
        StageId SourceStageId,
        StageId DestinationStageId,
        FixtureId FixtureId,
        EntryId Home);
}
