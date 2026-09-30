// -----------------------------------------------------------------------
// <copyright file="R2HostEndToEndTests.cs" company="Stéphane ANDRE">
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
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Host.Contracts;
using MyClub.PlayUp.TestKit;
using Xunit;

namespace MyClub.PlayUp.Host.Tests;

/// <summary>
/// Vertical R2 proof with Read Surface observability
/// (Prepare → Publish → Apply → Start → Finish → Progression → GET Stage/Matches/Match).
/// </summary>
[Collection("host-postgres")]
[Trait("Category", "Integration")]
public sealed class R2HostEndToEndTests(HostPostgresFixture fixture)
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 14, 21, 0, 0, TimeSpan.Zero));

    [IntegrationFact]
    public async Task R2_Host_EndToEnd_PublishApplyPlayProgressAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedResolvedDraftSlotCupAsync(factory);
        using var client = factory.CreateClient();

        // PublishDraw (stage stays Draft until fixtures + progression are ready for Prepare).
        using (var publish = await client.PostAsync(PublishUri(seed.QuarterStageId, seed.DrawId), content: null))
        {
            publish.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        using (var scope = factory.Services.CreateScope())
        {
            var draw = (await scope.ServiceProvider.GetRequiredService<IStageRepository>()
                .GetByIdForUpdateAsync(seed.QuarterStageId))!.GetDraw(seed.DrawId);
            draw.Status.Should().Be(DrawStatus.Published);
            draw.Resolution.State.Should().Be(DrawResolutionState.Resolved);
        }

        // ApplyDraw (Slot occupancy) then materialize confrontations.
        using (var apply = await client.PostAsync(
                   ApplyDrawUri(seed.QuarterStageId, seed.DrawId),
                   content: null))
        {
            apply.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        using (var materialize = await client.PostAsJsonAsync(
                   MaterializeUri(seed.QuarterStageId),
                   new MaterializeCupFromOccupiedSlotsRequest(["P1"])))
        {
            materialize.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        MatchId matchId;
        FixtureId fixtureId;
        using (var scope = factory.Services.CreateScope())
        {
            var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
            var matches = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var quarter = await stages.GetByIdForUpdateAsync(seed.QuarterStageId);
            var pairFixture = quarter!.FindFixtureByBracketPairKey("P1");
            pairFixture.Should().NotBeNull();
            fixtureId = pairFixture.Id;
            matchId = pairFixture.MatchIds.Should().ContainSingle().Subject;

            quarter.ReplaceProgressionRules(
                new ProgressionRules(
                [
                    new ProgressionPath(
                        pairFixture.BracketPairKey!,
                        ProgressionOutcome.Winner,
                        ProgressionDestination.ForPopulation(seed.SemiStageId))
                ]),
                _clock);
            await unitOfWork.SaveChangesAsync();

            var match = await matches.GetByIdForUpdateAsync(matchId);
            match.Should().NotBeNull();
            match.Status.Should().Be(MatchStatus.Scheduled);
            match.HomeEntryId.Should().Be(seed.Home);
            match.AwayEntryId.Should().Be(seed.Away);
            match.StageId.Should().Be(seed.QuarterStageId);
            match.CompetitionId.Should().Be(seed.CompetitionId);
            match.Result.Should().BeNull();
        }

        // Prepare after fixtures + progression exist (Draft → Ready).
        using (var prepare = await client.PostAsync(PrepareUri(seed.QuarterStageId), content: null))
        {
            prepare.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        using (var scope = factory.Services.CreateScope())
        {
            var quarter = await scope.ServiceProvider.GetRequiredService<IStageRepository>()
                .GetByIdForUpdateAsync(seed.QuarterStageId);
            quarter!.Status.Should().Be(StageStatus.Ready);
        }

        // Before progression — destination population and slots empty (observable via Read Surface).
        using (var stageBefore = await client.GetAsync($"/stages/{seed.SemiStageId.Value}"))
        {
            stageBefore.StatusCode.Should().Be(HttpStatusCode.OK);
            var semiOverview = await stageBefore.Content.ReadFromJsonAsync<StageOverviewDto>(HostJson.Options);
            semiOverview.Should().NotBeNull();
            semiOverview.Slots.Single(slot => slot.SlotKey == "SF1-A").EntryId.Should().BeNull();
            semiOverview.Slots.Single(slot => slot.SlotKey == "SF1-B").EntryId.Should().BeNull();
        }

        // StartMatch
        using (var start = await client.PostAsync(StartUri(matchId), content: null))
        {
            start.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        using (var scope = factory.Services.CreateScope())
        {
            var match = await scope.ServiceProvider.GetRequiredService<IMatchRepository>().GetByIdForUpdateAsync(matchId);
            match!.Status.Should().Be(MatchStatus.Live);
            match.Result.Should().BeNull();
        }

        // FinishMatch — Match holds the score; Winner is FixtureOutcome, not Match.
        var finishBody = new FinishMatchRequest(ResultType.Played, HomeGoals: 2, AwayGoals: 0);
        using (var finish = await client.PostAsJsonAsync(FinishUri(matchId), finishBody))
        {
            finish.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        using (var scope = factory.Services.CreateScope())
        {
            var match = await scope.ServiceProvider.GetRequiredService<IMatchRepository>().GetByIdForUpdateAsync(matchId);
            match!.Status.Should().Be(MatchStatus.Finished);
            match.Result.Should().NotBeNull();
            match.Result!.Type.Should().Be(ResultType.Played);
            match.Result.Score.HomeGoals.Should().Be(2);
            match.Result.Score.AwayGoals.Should().Be(0);
        }

        // ApplyProgressionOutcome — Winner derived from finished Match(es), not from HTTP body.
        using (var progress = await client.PostAsync(
                   ProgressUri(seed.QuarterStageId, fixtureId),
                   content: null))
        {
            progress.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        using (var scope = factory.Services.CreateScope())
        {
            var semi = await scope.ServiceProvider.GetRequiredService<IStageRepository>()
                .GetByIdForUpdateAsync(seed.SemiStageId);
            semi.Should().NotBeNull();
            semi.CompositionEntries.Select(e => e.EntryId).Should().Equal(seed.Home);
            semi.FindSlot("SF1-A")!.EntryId.Should().BeNull();
            semi.FindSlot("SF1-B")!.EntryId.Should().BeNull();
        }

        // After progression — slots remain empty; population holds the winner (domain assertion above).
        using (var stageAfter = await client.GetAsync($"/stages/{seed.SemiStageId.Value}"))
        {
            stageAfter.StatusCode.Should().Be(HttpStatusCode.OK);
            var semiOverview = await stageAfter.Content.ReadFromJsonAsync<StageOverviewDto>(HostJson.Options);
            semiOverview.Should().NotBeNull();
            semiOverview.Slots.Single(slot => slot.SlotKey == "SF1-A").EntryId.Should().BeNull();
            semiOverview.Slots.Single(slot => slot.SlotKey == "SF1-B").EntryId.Should().BeNull();
        }

        using (var matchesResponse = await client.GetAsync($"/stages/{seed.QuarterStageId.Value}/matches"))
        {
            matchesResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var summaries = await matchesResponse.Content.ReadFromJsonAsync<List<MatchSummaryDto>>(HostJson.Options);
            summaries.Should().ContainSingle();
            summaries[0].MatchId.Should().Be(matchId.Value);
            summaries[0].Status.Should().Be(MatchStatus.Finished);
            summaries[0].Score.Should().Be(new MatchScoreDto(2, 0));
            summaries[0].Home.DisplayName.Should().Be("Home FC");
            AssertNoWinnerInJson(await matchesResponse.Content.ReadAsStringAsync());
        }

        using var matchResponse = await client.GetAsync($"/matches/{matchId.Value}");
        matchResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var detail = await matchResponse.Content.ReadFromJsonAsync<MatchDetailDto>(HostJson.Options);
        detail.Should().NotBeNull();
        detail.Result.Should().NotBeNull();
        detail.Result!.HomeGoals.Should().Be(2);
        detail.Result.AwayGoals.Should().Be(0);
        detail.Home.DisplayName.Should().Be("Home FC");
        detail.Away.DisplayName.Should().Be("Away FC");
        AssertNoWinnerInJson(await matchResponse.Content.ReadAsStringAsync());
    }

    [IntegrationFact]
    public async Task ApplyProgression_when_fixture_has_no_match_leaves_destination_slot_emptyAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedResolvedDraftSlotCupAsync(factory, createEmptyFixture: true);
        using var client = factory.CreateClient();

        using (var publish = await client.PostAsync(PublishUri(seed.QuarterStageId, seed.DrawId), content: null))
        {
            publish.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        using (var apply = await client.PostAsync(
                   ApplyDrawUri(seed.QuarterStageId, seed.DrawId),
                   content: null))
        {
            apply.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        using (var prepare = await client.PostAsync(PrepareUri(seed.QuarterStageId), content: null))
        {
            prepare.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        // Empty P1 fixture (no match) — progression must fail-closed without coherent fixture/match set.
        using (var progress = await client.PostAsync(
                   ProgressUri(seed.QuarterStageId, seed.FixtureId!.Value),
                   content: null))
        {
            progress.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var problem = await progress.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options);
            problem.Should().NotBeNull();
            GetCode(problem).Should().Be(ApplicationErrorCodes.FixtureInvalid);
        }

        using var scope = factory.Services.CreateScope();
        var semi = await scope.ServiceProvider.GetRequiredService<IStageRepository>()
            .GetByIdForUpdateAsync(seed.SemiStageId);
        semi!.CompositionEntries.Should().BeEmpty();
        semi.FindSlot("SF1-A")!.EntryId.Should().BeNull();
        semi.FindSlot("SF1-B")!.EntryId.Should().BeNull();
    }

    private static Uri PrepareUri(StageId stageId) =>
        new($"/stages/{stageId.Value}/prepare", UriKind.Relative);

    private static Uri PublishUri(StageId stageId, DrawId drawId) =>
        new($"/stages/{stageId.Value}/draws/{drawId.Value}/publish", UriKind.Relative);

    private static Uri ApplyDrawUri(StageId stageId, DrawId drawId) =>
        new($"/stages/{stageId.Value}/draws/{drawId.Value}/apply", UriKind.Relative);

    private static Uri MaterializeUri(StageId stageId) =>
        new($"/stages/{stageId.Value}/matches/materialize-from-slots", UriKind.Relative);

    private static Uri StartUri(MatchId matchId) =>
        new($"/matches/{matchId.Value}/start", UriKind.Relative);

    private static Uri FinishUri(MatchId matchId) =>
        new($"/matches/{matchId.Value}/finish", UriKind.Relative);

    private static Uri ProgressUri(StageId stageId, FixtureId fixtureId) =>
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

    private static void AssertNoWinnerInJson(string json)
    {
        using var document = JsonDocument.Parse(json);
        AssertNoWinner(document.RootElement);
    }

    private static void AssertNoWinner(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                {
                    foreach (var property in element.EnumerateObject())
                    {
                        property.Name.Equals("winner", StringComparison.OrdinalIgnoreCase).Should().BeFalse();
                        AssertNoWinner(property.Value);
                    }

                    break;
                }

            case JsonValueKind.Array:
                {
                    foreach (var item in element.EnumerateArray())
                    {
                        AssertNoWinner(item);
                    }

                    break;
                }

            case JsonValueKind.Undefined:
            case JsonValueKind.String:
            case JsonValueKind.Number:
            case JsonValueKind.True:
            case JsonValueKind.False:
            case JsonValueKind.Null:
            default:
                break;
        }
    }

    /// <summary>
    /// Seeds structure + Slot Draw Draft+Resolved. HTTP workflow starts at Publish / Apply.
    /// When <paramref name="createEmptyFixture"/> is true, also seeds an empty P1 fixture + Winner progression
    /// (for progression-without-match negative cases).
    /// </summary>
    private async Task<R2CupSeed> SeedResolvedDraftSlotCupAsync(
        PlayUpWebApplicationFactory factory,
        bool createEmptyFixture = false)
    {
        using var scope = factory.Services.CreateScope();
        var situation = TestCompetition.Create("R2 Host Cup", _clock)
            .WithTeams("Home FC", "Away FC");

        var quarter = situation.AddKnockoutStage("QF", "R1", ["S1", "S2"]);
        var semi = situation.AddKnockoutStage("SF", "R1", ["SF1-A", "SF1-B"]);

        FixtureId? fixtureId = null;
        if (createEmptyFixture)
        {
            var empty = quarter.AddFixture(quarter.Rounds[0].Id, _clock, "S1", "S2", "P1");
            fixtureId = empty.Id;
            situation.WithProgressionPaths(
                quarter,
                new ProgressionPathSpec(
                    empty.BracketPairKey!,
                    ProgressionOutcome.Winner,
                    semi.Id));
        }

        var home = situation.Competition.Entries[0].Id;
        var away = situation.Competition.Entries[1].Id;
        var drawId = situation.CreateResolvedSlotDraw(quarter, [home, away], ["S1", "S2"]);

        await HostTestPersist.PersistAsync(scope.ServiceProvider, situation);

        return new R2CupSeed(
            situation.Competition.Id,
            quarter.Id,
            semi.Id,
            drawId,
            fixtureId,
            home,
            away);
    }

    private sealed record R2CupSeed(
        CompetitionId CompetitionId,
        StageId QuarterStageId,
        StageId SemiStageId,
        DrawId DrawId,
        FixtureId? FixtureId,
        EntryId Home,
        EntryId Away);
}
