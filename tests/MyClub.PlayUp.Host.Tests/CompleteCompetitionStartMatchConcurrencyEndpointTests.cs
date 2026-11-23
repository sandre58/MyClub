// -----------------------------------------------------------------------
// <copyright file="CompleteCompetitionStartMatchConcurrencyEndpointTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MyClub.PlayUp.Application;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Host.Contracts;
using MyClub.PlayUp.TestKit;
using Xunit;

namespace MyClub.PlayUp.Host.Tests;

/// <summary>
/// A10-REST-6 — CompleteCompetition (Administrative) versus StartMatch under competition lock.
/// </summary>
[Collection("host-postgres")]
[Trait("Category", "Integration")]
public sealed class CompleteCompetitionStartMatchConcurrencyEndpointTests(HostPostgresFixture fixture)
{
    private const int StressIterationCount = 50;

    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 11, 23, 18, 45, 0, TimeSpan.Zero));

    [IntegrationFact]
    public async Task Complete_then_startMatch_imposes_lock_order_and_outcomes_over_many_iterationsAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();

        for (var index = 0; index < StressIterationCount; index++)
        {
            var seed = await SeedRunningWithScheduledMatchAsync(factory);
            var (completeResponse, startResponse) =
                await CompetitionOrderedWriterConcurrencySupport.RunOrderedCompetitionWritersAsync(
                    fixture.ConnectionString,
                    seed.CompetitionId,
                    () => client.PostAsJsonAsync(
                        CompleteUri(seed.CompetitionId),
                        new CompleteCompetitionRequest("Administrative")),
                    () => client.PostAsync(StartUri(seed.MatchId), content: null));

            using (completeResponse)
            using (startResponse)
            {
                CompetitionOrderedWriterConcurrencySupport.AssertNoUnhandledPostgresContention(completeResponse);
                CompetitionOrderedWriterConcurrencySupport.AssertNoUnhandledPostgresContention(startResponse);

                completeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
                startResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
                (await CompetitionOrderedWriterConcurrencySupport.ReadCodeAsync(startResponse))
                    .Should().Be(ApplicationErrorCodes.MatchOperationNotAllowed);

                using var scope = factory.Services.CreateScope();
                var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
                var matches = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
                var competition = await competitions.GetByIdForUpdateAsync(seed.CompetitionId);
                competition!.Status.Should().Be(CompetitionStatus.Completed);
                competition.CompletionMode.Should().Be(CompletionMode.Administrative);
                var match = await matches.GetByIdForUpdateAsync(seed.MatchId);
                match!.Status.Should().Be(MatchStatus.Scheduled);
            }
        }
    }

    [IntegrationFact]
    public async Task StartMatch_then_complete_imposes_lock_order_and_outcomes_over_many_iterationsAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();

        for (var index = 0; index < StressIterationCount; index++)
        {
            var seed = await SeedRunningWithScheduledMatchAsync(factory);
            var (startResponse, completeResponse) =
                await CompetitionOrderedWriterConcurrencySupport.RunOrderedCompetitionWritersAsync(
                    fixture.ConnectionString,
                    seed.CompetitionId,
                    () => client.PostAsync(StartUri(seed.MatchId), content: null),
                    () => client.PostAsJsonAsync(
                        CompleteUri(seed.CompetitionId),
                        new CompleteCompetitionRequest("Administrative")));

            using (startResponse)
            using (completeResponse)
            {
                CompetitionOrderedWriterConcurrencySupport.AssertNoUnhandledPostgresContention(startResponse);
                CompetitionOrderedWriterConcurrencySupport.AssertNoUnhandledPostgresContention(completeResponse);

                startResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
                completeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

                using var scope = factory.Services.CreateScope();
                var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
                var matches = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
                var competition = await competitions.GetByIdForUpdateAsync(seed.CompetitionId);
                competition!.Status.Should().Be(CompetitionStatus.Completed);
                competition.CompletionMode.Should().Be(CompletionMode.Administrative);
                var match = await matches.GetByIdForUpdateAsync(seed.MatchId);
                match!.Status.Should().Be(MatchStatus.Live);
            }
        }
    }

    private async Task<CompleteStartMatchSeed> SeedRunningWithScheduledMatchAsync(PlayUpWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var situation = TestCompetition.Create($"REST-6 complete-start {Guid.NewGuid():N}", _clock)
            .WithTeams("A", "B")
            .WithStructure(StructureIntent.Championship(stageName: "League"))
            .PreparePrimaryStage();
        var stage = situation.RequirePrimaryStage();
        var materialized = MaterializeMatches.Execute(situation.Competition, stage, [], _clock, [stage]);
        foreach (var match in materialized.CreatedMatches)
        {
            situation.TrackMatch(match);
        }

        situation.PrepareCompetition();
        situation.StartCompetition();
        situation.StartPrimaryStage();
        await HostTestPersist.PersistAsync(scope.ServiceProvider, situation);

        var scheduled = materialized.CreatedMatches.First(match => match.Status == MatchStatus.Scheduled);
        return new CompleteStartMatchSeed(situation.Competition.Id, scheduled.Id);
    }

    private static Uri CompleteUri(CompetitionId competitionId) =>
        new($"/competitions/{competitionId.Value}/complete", UriKind.Relative);

    private static Uri StartUri(MatchId matchId) =>
        new($"/matches/{matchId.Value}/start", UriKind.Relative);

    private sealed record CompleteStartMatchSeed(CompetitionId CompetitionId, MatchId MatchId);
}
