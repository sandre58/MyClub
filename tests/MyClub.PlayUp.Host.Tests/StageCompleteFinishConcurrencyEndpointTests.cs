// -----------------------------------------------------------------------
// <copyright file="StageCompleteFinishConcurrencyEndpointTests.cs" company="Stéphane ANDRE">
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
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Host.Contracts;
using MyClub.PlayUp.TestKit;
using Xunit;
using DomainMatchResult = MyClub.PlayUp.Domain.Matches.MatchResult;
using DomainScore = MyClub.PlayUp.Domain.Matches.Score;

namespace MyClub.PlayUp.Host.Tests;

/// <summary>
/// A10-REST-2 — Running stage: CompleteStage versus FinishMatch under competition lock.
/// </summary>
[Collection("host-postgres")]
[Trait("Category", "Integration")]
public sealed class StageCompleteFinishConcurrencyEndpointTests(HostPostgresFixture fixture)
{
    private const int StressIterationCount = 50;

    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 11, 23, 16, 0, 0, TimeSpan.Zero));

    [IntegrationFact]
    public async Task Finish_then_complete_imposes_lock_order_and_outcomes_over_many_iterationsAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();

        for (var index = 0; index < StressIterationCount; index++)
        {
            var seed = await SeedRunningWithOneLiveAndOneFinishedAsync(factory);
            var (finishResponse, completeResponse) =
                await CompetitionOrderedWriterConcurrencySupport.RunOrderedCompetitionWritersAsync(
                    fixture.ConnectionString,
                    seed.CompetitionId,
                    () => client.PostAsJsonAsync(
                        FinishUri(seed.LiveMatchId),
                        new FinishMatchRequest(ResultType.Played, HomeGoals: 1, AwayGoals: 0)),
                    () => client.PostAsync(CompleteUri(seed.StageId), content: null));

            using (finishResponse)
            using (completeResponse)
            {
                CompetitionOrderedWriterConcurrencySupport.AssertNoUnhandledPostgresContention(finishResponse);
                CompetitionOrderedWriterConcurrencySupport.AssertNoUnhandledPostgresContention(completeResponse);

                finishResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
                completeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

                using var scope = factory.Services.CreateScope();
                var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
                var loaded = await stages.GetByIdForUpdateAsync(seed.StageId);
                loaded!.Status.Should().Be(StageStatus.Completed);
            }
        }
    }

    [IntegrationFact]
    public async Task Complete_then_finish_imposes_lock_order_and_outcomes_over_many_iterationsAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();

        for (var index = 0; index < StressIterationCount; index++)
        {
            var seed = await SeedRunningWithOneLiveAndOneFinishedAsync(factory);
            var (completeResponse, finishResponse) =
                await CompetitionOrderedWriterConcurrencySupport.RunOrderedCompetitionWritersAsync(
                    fixture.ConnectionString,
                    seed.CompetitionId,
                    () => client.PostAsync(CompleteUri(seed.StageId), content: null),
                    () => client.PostAsJsonAsync(
                        FinishUri(seed.LiveMatchId),
                        new FinishMatchRequest(ResultType.Played, HomeGoals: 2, AwayGoals: 1)));

            using (completeResponse)
            using (finishResponse)
            {
                CompetitionOrderedWriterConcurrencySupport.AssertNoUnhandledPostgresContention(completeResponse);
                CompetitionOrderedWriterConcurrencySupport.AssertNoUnhandledPostgresContention(finishResponse);

                completeResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
                var completeProblem =
                    await completeResponse.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options);
                GetCode(completeProblem).Should().Be(ApplicationErrorCodes.StageCompletionBlocked);
                GetReasons(completeProblem!).Should().Contain(CompleteStage.ReasonOpenMatches);
                finishResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

                using var scope = factory.Services.CreateScope();
                var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
                var matches = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
                var loaded = await stages.GetByIdForUpdateAsync(seed.StageId);
                loaded!.Status.Should().Be(StageStatus.Running);
                var match = await matches.GetByIdForUpdateAsync(seed.LiveMatchId);
                match!.Status.Should().Be(MatchStatus.Finished);
            }
        }
    }

    private async Task<CompleteFinishSeed> SeedRunningWithOneLiveAndOneFinishedAsync(PlayUpWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var situation = TestCompetition.Create($"REST-2 complete-finish {Guid.NewGuid():N}", _clock)
            .WithTeams("A", "B", "C", "D")
            .WithStructure(StructureIntent.Championship(stageName: "League"))
            .PreparePrimaryStage();
        var stage = situation.RequirePrimaryStage();
        var materialized = MaterializeMatches.Execute(situation.Competition, stage, [], _clock, [stage]);
        var created = materialized.CreatedMatches;
        var live = created[created.Count - 1];
        for (var index = 0; index < created.Count - 1; index++)
        {
            created[index].Start(_clock);
            created[index].Finish(new DomainMatchResult(ResultType.Played, new DomainScore(1, 0)), _clock);
        }

        live.Start(_clock);
        foreach (var match in materialized.CreatedMatches)
        {
            situation.TrackMatch(match);
        }

        situation.StartPrimaryStage();
        await HostTestPersist.PersistAsync(scope.ServiceProvider, situation);

        return new CompleteFinishSeed(situation.Competition.Id, stage.Id, live.Id);
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

    private static IReadOnlyList<string> GetReasons(ProblemDetails problem) =>
        !problem.Extensions.TryGetValue("reasons", out var raw) || raw is null
            ? []
            : raw switch
            {
                JsonElement { ValueKind: JsonValueKind.Array } element =>
                [
                    .. element.EnumerateArray().Select(item => item.GetString()!)
                ]!,
                IEnumerable<string> texts => [.. texts],
                _ => []
            };

    private static Uri CompleteUri(StageId stageId) =>
        new($"/stages/{stageId.Value}/complete", UriKind.Relative);

    private static Uri FinishUri(MatchId matchId) =>
        new($"/matches/{matchId.Value}/finish", UriKind.Relative);

    private sealed record CompleteFinishSeed(CompetitionId CompetitionId, StageId StageId, MatchId LiveMatchId);
}
