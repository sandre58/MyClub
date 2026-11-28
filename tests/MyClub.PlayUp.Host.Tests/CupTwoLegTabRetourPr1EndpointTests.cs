// -----------------------------------------------------------------------
// <copyright file="CupTwoLegTabRetourPr1EndpointTests.cs" company="Stéphane ANDRE">
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

/// <summary>
/// Cup two-leg return-leg TAB when aggregate is still level after an unequal play score (PR1).
/// </summary>
[Collection("host-postgres")]
[Trait("Category", "Integration")]
public sealed class CupTwoLegTabRetourPr1EndpointTests(HostPostgresFixture fixture)
{
    private static readonly FakeClock Clock = new(new DateTimeOffset(2026, 11, 28, 16, 0, 0, TimeSpan.Zero));

    [IntegrationFact]
    public async Task Return_leg_TAB_when_aggregate_level_promotes_shootout_winnerAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();

        var seed = await CupTwoLegHttpSeed.CreateSemiAndFinalAsync(client);
        await CupTwoLegHttpSeed.SetDefaultTwoLegsAsync(client, seed.SemiStageId, requireExtraTime: false);
        await CupTwoLegHttpSeed.WireSemiWinnerToFinalAsync(client, seed);
        await CupTwoLegHttpSeed.RunSlotDrawAndMaterializeP1Async(client, seed.SemiStageId);
        await CupTwoLegHttpSeed.StartExploitationAsync(client, seed.CompetitionId, seed.SemiStageId);

        var legs = await CupTwoLegHttpSeed.ReadTwoLegAttachmentsAsync(client, seed.SemiStageId);
        var teamA = legs.Leg1HomeEntryId;
        var teamB = legs.Leg1AwayEntryId;

        await StartAndFinishAsync(client, legs.Leg1MatchId, homeGoals: 1, awayGoals: 0);
        await client.PostAsync($"/matches/{legs.Leg2MatchId}/start", null);

        using (var finishLeg2 = await client.PostAsJsonAsync(
                   $"/matches/{legs.Leg2MatchId}/finish",
                   new FinishMatchRequest(
                       ResultType.Played,
                       HomeGoals: 1,
                       AwayGoals: 0,
                       PenaltyShootoutHomeGoals: 5,
                       PenaltyShootoutAwayGoals: 4)))
        {
            finishLeg2.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        using (var scope = factory.Services.CreateScope())
        {
            var matches = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
            var leg2 = await matches.GetByIdReadOnlyAsync(new MatchId(legs.Leg2MatchId));
            leg2!.Result!.ConfrontationTiePenaltyShootout.Should().BeTrue();
            leg2.Result.PenaltyShootoutScore.Should().Be(new PenaltyShootoutScore(5, 4));
        }

        using var progression = await client.PostAsync(
            $"/stages/{seed.SemiStageId}/fixtures/{legs.FixtureId}/apply-progression",
            null);
        progression.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var finalOverview = await client.GetAsync($"/stages/{seed.FinalStageId}");
        var final = await finalOverview.Content.ReadFromJsonAsync<StageOverviewDto>(HostJson.Options);
        final!.Slots.Single(s => s.SlotKey == "S1").EntryId.Should().Be(teamB);
        final.Slots.Single(s => s.SlotKey == "S1").EntryId.Should().NotBe(teamA);
    }

    [IntegrationFact]
    public async Task Finish_return_leg_TAB_when_aggregate_not_level_without_away_goals_returns_409Async()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();

        var seed = await PrepareLiveTwoLegCupAsync(client);
        var legs = await CupTwoLegHttpSeed.ReadTwoLegAttachmentsAsync(client, seed.SemiStageId);
        await StartAndFinishAsync(client, legs.Leg1MatchId, homeGoals: 2, awayGoals: 0);
        await client.PostAsync($"/matches/{legs.Leg2MatchId}/start", null);

        using var finishLeg2 = await client.PostAsJsonAsync(
            $"/matches/{legs.Leg2MatchId}/finish",
            new FinishMatchRequest(
                ResultType.Played,
                HomeGoals: 1,
                AwayGoals: 0,
                PenaltyShootoutHomeGoals: 5,
                PenaltyShootoutAwayGoals: 4));
        finishLeg2.StatusCode.Should().Be(HttpStatusCode.Conflict);
        GetCode(await finishLeg2.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options))
            .Should().Be(MatchErrorCodes.InvalidResult);
    }

    [IntegrationFact]
    public async Task Finish_return_leg_TAB_when_away_goals_break_aggregate_returns_409Async()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();

        var seed = await CupTwoLegHttpSeed.CreateSemiAndFinalAsync(client);
        await client.PutAsJsonAsync(
            $"/stages/{seed.SemiStageId}/tie-format",
            new ReplaceStageDefaultTieFormatRequest(
                Clear: false,
                NumberOfLegs: 2,
                HasAwayGoalsRule: true,
                HasExtraTimeRule: true,
                HasPenaltyShootoutRule: true));
        await CupTwoLegHttpSeed.WireSemiWinnerToFinalAsync(client, seed);
        await CupTwoLegHttpSeed.RunSlotDrawAndMaterializeP1Async(client, seed.SemiStageId);
        await CupTwoLegHttpSeed.StartExploitationAsync(client, seed.CompetitionId, seed.SemiStageId);

        var legs = await CupTwoLegHttpSeed.ReadTwoLegAttachmentsAsync(client, seed.SemiStageId);
        await StartAndFinishAsync(client, legs.Leg1MatchId, homeGoals: 2, awayGoals: 1);
        await client.PostAsync($"/matches/{legs.Leg2MatchId}/start", null);

        using var finishLeg2 = await client.PostAsJsonAsync(
            $"/matches/{legs.Leg2MatchId}/finish",
            new FinishMatchRequest(
                ResultType.Played,
                HomeGoals: 1,
                AwayGoals: 0,
                PenaltyShootoutHomeGoals: 5,
                PenaltyShootoutAwayGoals: 4));
        finishLeg2.StatusCode.Should().Be(HttpStatusCode.Conflict);
        GetCode(await finishLeg2.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options))
            .Should().Be(MatchErrorCodes.InvalidResult);
    }

    [IntegrationFact]
    public async Task Finish_isolated_match_unequal_score_with_TAB_returns_409Async()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var matchId = await SeedIsolatedLiveMatchAsync(factory);
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            $"/matches/{matchId.Value}/finish",
            new FinishMatchRequest(
                ResultType.Played,
                HomeGoals: 2,
                AwayGoals: 1,
                PenaltyShootoutHomeGoals: 5,
                PenaltyShootoutAwayGoals: 4));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        GetCode(await response.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options))
            .Should().Be(MatchErrorCodes.InvalidResult);
    }

    [IntegrationFact]
    public async Task Finish_first_leg_TAB_returns_409Async()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();

        var seed = await PrepareLiveTwoLegCupAsync(client);
        var legs = await CupTwoLegHttpSeed.ReadTwoLegAttachmentsAsync(client, seed.SemiStageId);
        using (var start = await client.PostAsync($"/matches/{legs.Leg1MatchId}/start", null))
        {
            start.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        using var finishLeg1 = await client.PostAsJsonAsync(
            $"/matches/{legs.Leg1MatchId}/finish",
            new FinishMatchRequest(
                ResultType.Played,
                HomeGoals: 1,
                AwayGoals: 1,
                PenaltyShootoutHomeGoals: 5,
                PenaltyShootoutAwayGoals: 4));
        finishLeg1.StatusCode.Should().Be(HttpStatusCode.Conflict);
        GetCode(await finishLeg1.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options))
            .Should().Be(MatchErrorCodes.ExtraTimeOrShootoutOnNonFinalLeg);
    }

    [IntegrationFact]
    public async Task Finish_return_leg_TAB_when_first_leg_not_finished_returns_409Async()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();

        var seed = await PrepareLiveTwoLegCupAsync(client);
        var legs = await CupTwoLegHttpSeed.ReadTwoLegAttachmentsAsync(client, seed.SemiStageId);
        await client.PostAsync($"/matches/{legs.Leg2MatchId}/start", null);

        using var finishLeg2 = await client.PostAsJsonAsync(
            $"/matches/{legs.Leg2MatchId}/finish",
            new FinishMatchRequest(
                ResultType.Played,
                HomeGoals: 1,
                AwayGoals: 0,
                PenaltyShootoutHomeGoals: 5,
                PenaltyShootoutAwayGoals: 4));
        finishLeg2.StatusCode.Should().Be(HttpStatusCode.Conflict);
        GetCode(await finishLeg2.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options))
            .Should().Be(MatchErrorCodes.InvalidResult);
    }

    [IntegrationFact]
    public async Task Finish_championship_match_unequal_TAB_returns_409Async()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var matchId = await SeedChampionshipTwoLegDefaultLiveMatchAsync(factory);
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            $"/matches/{matchId.Value}/finish",
            new FinishMatchRequest(
                ResultType.Played,
                HomeGoals: 2,
                AwayGoals: 1,
                PenaltyShootoutHomeGoals: 5,
                PenaltyShootoutAwayGoals: 4));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        GetCode(await response.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options))
            .Should().Be(MatchErrorCodes.InvalidResult);
    }

    [IntegrationFact]
    public async Task Finish_single_leg_cup_unequal_TAB_returns_409Async()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();

        var seed = await CupTwoLegHttpSeed.CreateSemiAndFinalAsync(client);
        using var tie = await client.PutAsJsonAsync(
            $"/stages/{seed.SemiStageId}/tie-format",
            new ReplaceStageDefaultTieFormatRequest(
                Clear: false,
                NumberOfLegs: 1,
                HasPenaltyShootoutRule: true));
        tie.StatusCode.Should().Be(HttpStatusCode.NoContent);
        await CupTwoLegHttpSeed.WireSemiWinnerToFinalAsync(client, seed);
        await CupTwoLegHttpSeed.RunSlotDrawAndMaterializeP1Async(client, seed.SemiStageId);
        await CupTwoLegHttpSeed.StartExploitationAsync(client, seed.CompetitionId, seed.SemiStageId);

        var legs = await CupTwoLegHttpSeed.ReadSingleLegAttachmentsAsync(client, seed.SemiStageId);
        await client.PostAsync($"/matches/{legs.MatchId}/start", null);

        using var finish = await client.PostAsJsonAsync(
            $"/matches/{legs.MatchId}/finish",
            new FinishMatchRequest(
                ResultType.Played,
                HomeGoals: 2,
                AwayGoals: 1,
                PenaltyShootoutHomeGoals: 5,
                PenaltyShootoutAwayGoals: 4));
        finish.StatusCode.Should().Be(HttpStatusCode.Conflict);
        GetCode(await finish.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options))
            .Should().Be(MatchErrorCodes.InvalidResult);
    }

    [IntegrationFact]
    public async Task Finish_return_leg_TAB_without_rule_returns_409Async()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var legs = await SeedTwoLegCupWithoutShootoutRuleAsync(factory);
        using var client = factory.CreateClient();

        await StartAndFinishAsync(client, legs.FirstLegId.Value, 1, 0);
        await client.PostAsync($"/matches/{legs.SecondLegId.Value}/start", null);

        using var finish = await client.PostAsJsonAsync(
            $"/matches/{legs.SecondLegId.Value}/finish",
            new FinishMatchRequest(
                ResultType.Played,
                HomeGoals: 1,
                AwayGoals: 0,
                PenaltyShootoutHomeGoals: 5,
                PenaltyShootoutAwayGoals: 4));
        finish.StatusCode.Should().Be(HttpStatusCode.Conflict);
        GetCode(await finish.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options))
            .Should().Be(MatchErrorCodes.InvalidResult);
    }

    [IntegrationFact]
    public async Task Finish_return_leg_forfeit_with_TAB_returns_409Async()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();

        var seed = await PrepareLiveTwoLegCupAsync(client);
        var legs = await CupTwoLegHttpSeed.ReadTwoLegAttachmentsAsync(client, seed.SemiStageId);
        await StartAndFinishAsync(client, legs.Leg1MatchId, 1, 0);
        await client.PostAsync($"/matches/{legs.Leg2MatchId}/start", null);

        using var finish = await client.PostAsJsonAsync(
            $"/matches/{legs.Leg2MatchId}/finish",
            new FinishMatchRequest(
                ResultType.Forfeit,
                HomeGoals: 0,
                AwayGoals: 3,
                PenaltyShootoutHomeGoals: 5,
                PenaltyShootoutAwayGoals: 4));
        finish.StatusCode.Should().Be(HttpStatusCode.Conflict);
        GetCode(await finish.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options))
            .Should().Be(MatchErrorCodes.InvalidResult);
    }

    [IntegrationFact]
    public async Task Finish_return_leg_equal_at_90_unequal_after_ET_with_TAB_succeedsAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();

        var seed = await PrepareLiveTwoLegCupAsync(client);
        var legs = await CupTwoLegHttpSeed.ReadTwoLegAttachmentsAsync(client, seed.SemiStageId);
        await StartAndFinishAsync(client, legs.Leg1MatchId, 1, 0);
        await client.PostAsync($"/matches/{legs.Leg2MatchId}/start", null);

        using var finish = await client.PostAsJsonAsync(
            $"/matches/{legs.Leg2MatchId}/finish",
            new FinishMatchRequest(
                ResultType.Played,
                HomeGoals: 1,
                AwayGoals: 0,
                ExtraTimePlayed: true,
                PenaltyShootoutHomeGoals: 5,
                PenaltyShootoutAwayGoals: 4));
        finish.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [IntegrationFact]
    public async Task Put_return_leg_confrontation_tie_result_succeedsAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();

        var seed = await PrepareLiveTwoLegCupAsync(client, requireExtraTime: false);
        var legs = await CupTwoLegHttpSeed.ReadTwoLegAttachmentsAsync(client, seed.SemiStageId);
        await StartAndFinishAsync(client, legs.Leg1MatchId, 1, 0);
        await StartAndFinishReturnLegTabAsync(client, legs.Leg2MatchId, shootoutHome: 5, shootoutAway: 4);

        using var correct = await client.PutAsJsonAsync(
            $"/matches/{legs.Leg2MatchId}/result",
            new FinishMatchRequest(
                ResultType.Played,
                HomeGoals: 1,
                AwayGoals: 0,
                PenaltyShootoutHomeGoals: 4,
                PenaltyShootoutAwayGoals: 3));
        correct.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [IntegrationFact]
    public async Task Put_return_leg_ineligible_TAB_refuses_with_409Async()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();

        var seed = await PrepareLiveTwoLegCupAsync(client, awayGoals: true);
        var legs = await CupTwoLegHttpSeed.ReadTwoLegAttachmentsAsync(client, seed.SemiStageId);
        await StartAndFinishAsync(client, legs.Leg1MatchId, 2, 1);
        await StartAndFinishAsync(client, legs.Leg2MatchId, 1, 0);

        using var correct = await client.PutAsJsonAsync(
            $"/matches/{legs.Leg2MatchId}/result",
            new FinishMatchRequest(
                ResultType.Played,
                HomeGoals: 1,
                AwayGoals: 0,
                PenaltyShootoutHomeGoals: 5,
                PenaltyShootoutAwayGoals: 4));
        correct.StatusCode.Should().Be(HttpStatusCode.Conflict);
        GetCode(await correct.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options))
            .Should().Be(MatchErrorCodes.InvalidResult);
    }

    [IntegrationFact]
    public async Task Finish_groups_match_with_two_leg_default_unequal_TAB_returns_409Async()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var matchId = await SeedGroupsTwoLegDefaultLiveMatchAsync(factory);
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            $"/matches/{matchId.Value}/finish",
            new FinishMatchRequest(
                ResultType.Played,
                HomeGoals: 2,
                AwayGoals: 1,
                PenaltyShootoutHomeGoals: 5,
                PenaltyShootoutAwayGoals: 4));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        GetCode(await response.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options))
            .Should().Be(MatchErrorCodes.InvalidResult);
    }

    [IntegrationFact]
    public async Task Put_return_leg_after_first_leg_correction_breaks_aggregate_refuses_identical_replayAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();

        var seed = await PrepareLiveTwoLegCupAsync(client, requireExtraTime: false);
        var legs = await CupTwoLegHttpSeed.ReadTwoLegAttachmentsAsync(client, seed.SemiStageId);
        await StartAndFinishAsync(client, legs.Leg1MatchId, 1, 0);
        var returnBody = new FinishMatchRequest(
            ResultType.Played,
            HomeGoals: 1,
            AwayGoals: 0,
            PenaltyShootoutHomeGoals: 5,
            PenaltyShootoutAwayGoals: 4);
        await client.PostAsync($"/matches/{legs.Leg2MatchId}/start", null);
        using (var finish = await client.PostAsJsonAsync($"/matches/{legs.Leg2MatchId}/finish", returnBody))
        {
            finish.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        using (var correctFirst = await client.PutAsJsonAsync(
                   $"/matches/{legs.Leg1MatchId}/result",
                   new FinishMatchRequest(ResultType.Played, HomeGoals: 2, AwayGoals: 0)))
        {
            correctFirst.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        using var replayReturn = await client.PutAsJsonAsync($"/matches/{legs.Leg2MatchId}/result", returnBody);
        replayReturn.StatusCode.Should().Be(HttpStatusCode.Conflict);
        GetCode(await replayReturn.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options))
            .Should().Be(MatchErrorCodes.InvalidResult);
    }

    [IntegrationFact]
    public async Task Put_first_leg_with_ET_after_progression_refuses_replayAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();

        var seed = await PrepareLiveTwoLegCupAsync(client, requireExtraTime: false);
        var legs = await CupTwoLegHttpSeed.ReadTwoLegAttachmentsAsync(client, seed.SemiStageId);
        await StartAndFinishAsync(client, legs.Leg1MatchId, 1, 0);
        await StartAndFinishReturnLegTabAsync(client, legs.Leg2MatchId, 5, 4);

        using var progression = await client.PostAsync(
            $"/stages/{seed.SemiStageId}/fixtures/{legs.FixtureId}/apply-progression",
            null);
        progression.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var correct = await client.PutAsJsonAsync(
            $"/matches/{legs.Leg1MatchId}/result",
            new FinishMatchRequest(
                ResultType.Played,
                HomeGoals: 1,
                AwayGoals: 1,
                ExtraTimePlayed: true));
        correct.StatusCode.Should().Be(HttpStatusCode.Conflict);
        GetCode(await correct.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options))
            .Should().Be(MatchErrorCodes.ExtraTimeOrShootoutOnNonFinalLeg);
    }

    [IntegrationFact]
    public async Task Finish_return_leg_TAB_without_ET_then_apply_progression_is_undecidedAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();

        var seed = await PrepareLiveTwoLegCupAsync(client);
        var legs = await CupTwoLegHttpSeed.ReadTwoLegAttachmentsAsync(client, seed.SemiStageId);
        await StartAndFinishAsync(client, legs.Leg1MatchId, 1, 0);
        await client.PostAsync($"/matches/{legs.Leg2MatchId}/start", null);

        using (var finish = await client.PostAsJsonAsync(
                   $"/matches/{legs.Leg2MatchId}/finish",
                   new FinishMatchRequest(
                       ResultType.Played,
                       HomeGoals: 1,
                       AwayGoals: 0,
                       PenaltyShootoutHomeGoals: 5,
                       PenaltyShootoutAwayGoals: 4)))
        {
            finish.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        using var progression = await client.PostAsync(
            $"/stages/{seed.SemiStageId}/fixtures/{legs.FixtureId}/apply-progression",
            null);
        progression.StatusCode.Should().Be(HttpStatusCode.Conflict);
        GetCode(await progression.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options))
            .Should().Be(StageErrorCodes.FixtureOutcomeUndecided);
    }

    private static async Task<CupSemiFinalSeed> PrepareLiveTwoLegCupAsync(
        HttpClient client,
        bool awayGoals = false,
        bool requireExtraTime = true)
    {
        var seed = await CupTwoLegHttpSeed.CreateSemiAndFinalAsync(client);
        using var tie = await client.PutAsJsonAsync(
            $"/stages/{seed.SemiStageId}/tie-format",
            new ReplaceStageDefaultTieFormatRequest(
                Clear: false,
                NumberOfLegs: 2,
                HasAwayGoalsRule: awayGoals,
                HasExtraTimeRule: requireExtraTime,
                HasPenaltyShootoutRule: true));
        tie.StatusCode.Should().Be(HttpStatusCode.NoContent);
        await CupTwoLegHttpSeed.WireSemiWinnerToFinalAsync(client, seed);
        await CupTwoLegHttpSeed.RunSlotDrawAndMaterializeP1Async(client, seed.SemiStageId);
        await CupTwoLegHttpSeed.StartExploitationAsync(client, seed.CompetitionId, seed.SemiStageId);
        return seed;
    }

    private static async Task StartAndFinishAsync(
        HttpClient client,
        Guid matchId,
        int homeGoals,
        int awayGoals)
    {
        using (var start = await client.PostAsync($"/matches/{matchId}/start", null))
        {
            start.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        using var finish = await client.PostAsJsonAsync(
            $"/matches/{matchId}/finish",
            new FinishMatchRequest(ResultType.Played, homeGoals, awayGoals));
        finish.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    private static async Task StartAndFinishReturnLegTabAsync(
        HttpClient client,
        Guid matchId,
        int shootoutHome,
        int shootoutAway)
    {
        using (var start = await client.PostAsync($"/matches/{matchId}/start", null))
        {
            start.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        using var finish = await client.PostAsJsonAsync(
            $"/matches/{matchId}/finish",
            new FinishMatchRequest(
                ResultType.Played,
                HomeGoals: 1,
                AwayGoals: 0,
                PenaltyShootoutHomeGoals: shootoutHome,
                PenaltyShootoutAwayGoals: shootoutAway));
        finish.StatusCode.Should().Be(HttpStatusCode.NoContent);
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

    private static async Task<(MatchId FirstLegId, MatchId SecondLegId)> SeedTwoLegCupWithoutShootoutRuleAsync(
        PlayUpWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var matches = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var competition = Competition.Create(new CompetitionName("Cup no TAB"), SampleRegulations.Standard(), Clock);
        var home = competition.AddEntry(TeamId.New(), "Home", Clock);
        var away = competition.AddEntry(TeamId.New(), "Away", Clock);
        competitions.Add(competition);

        var stage = Stage.Create(competition.Id, new StageName("Cup"), SampleRegulations.Standard(), Clock);
        competition.AddStage(stage.Id, Clock);
        stage.AddRound("R1", new TieFormat(TieFormat.TwoLegs, aggregateScoring: true), Clock);
        stage.AddSlot("A");
        stage.AddSlot("B");
        stage.ReplaceBracketPairs([new BracketPair("P1", "A", "B")]);
        var fixture = stage.AddFixture(stage.Rounds[0].Id, Clock, "A", "B", "P1");
        stage.AddResolvedPopulationEntry(home.Id, Clock);
        stage.AddResolvedPopulationEntry(away.Id, Clock);
        stage.AssignEntryToSlot("A", home.Id);
        stage.AssignEntryToSlot("B", away.Id);
        var first = Match.Create(competition.Id, stage.Id, home.Id, away.Id, Clock);
        var second = Match.Create(competition.Id, stage.Id, away.Id, home.Id, Clock);
        stage.AttachMatch(fixture.Id, first.Id, legIndex: 1, Clock);
        stage.AttachMatch(fixture.Id, second.Id, legIndex: 2, Clock);
        stage.Prepare(Clock);
        stage.Start(Clock);
        competition.Prepare(Clock);
        competition.Start(Clock);

        stages.Add(stage);
        matches.Add(first);
        matches.Add(second);
        await unitOfWork.SaveChangesAsync();
        return (first.Id, second.Id);
    }

    private static async Task<MatchId> SeedIsolatedLiveMatchAsync(PlayUpWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var matches = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var competition = Competition.Create(new CompetitionName("Isolated"), SampleRegulations.Standard(), Clock);
        var home = competition.AddEntry(TeamId.New(), "Home", Clock);
        var away = competition.AddEntry(TeamId.New(), "Away", Clock);
        competitions.Add(competition);
        var stage = Stage.Create(competition.Id, new StageName("Stage"), SampleRegulations.Standard(), Clock);
        competition.AddStage(stage.Id, Clock);
        stage.AddMatchday(1, Clock);
        stage.Prepare(Clock);
        stage.Start(Clock);
        competition.Prepare(Clock);
        competition.Start(Clock);
        stages.Add(stage);

        var match = Match.Create(competition.Id, stage.Id, home.Id, away.Id, Clock);
        match.Start(Clock);
        matches.Add(match);
        await unitOfWork.SaveChangesAsync();
        return match.Id;
    }

    private static async Task<MatchId> SeedChampionshipTwoLegDefaultLiveMatchAsync(PlayUpWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var matches = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var competition = Competition.Create(new CompetitionName("League"), SampleRegulations.Standard(), Clock);
        var home = competition.AddEntry(TeamId.New(), "Home", Clock);
        var away = competition.AddEntry(TeamId.New(), "Away", Clock);
        competitions.Add(competition);
        var stage = Stage.Create(competition.Id, new StageName("League"), SampleRegulations.Standard(), Clock);
        competition.AddStage(stage.Id, Clock);
        stage.ReplaceDefaultTieFormat(
            new TieFormat(
                TieFormat.TwoLegs,
                aggregateScoring: true,
                penaltyShootoutRule: new PenaltyShootoutRule()),
            Clock);
        stage.AddMatchday(1, Clock);
        stage.Prepare(Clock);
        stage.Start(Clock);
        competition.Prepare(Clock);
        competition.Start(Clock);
        stages.Add(stage);

        var match = Match.Create(competition.Id, stage.Id, home.Id, away.Id, Clock);
        match.Start(Clock);
        matches.Add(match);
        await unitOfWork.SaveChangesAsync();
        return match.Id;
    }

    private static async Task<MatchId> SeedGroupsTwoLegDefaultLiveMatchAsync(PlayUpWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var matches = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var competition = Competition.Create(new CompetitionName("Groups"), SampleRegulations.Standard(), Clock);
        var home = competition.AddEntry(TeamId.New(), "Home", Clock);
        var away = competition.AddEntry(TeamId.New(), "Away", Clock);
        competitions.Add(competition);
        var stage = Stage.Create(competition.Id, new StageName("Groups"), SampleRegulations.Standard(), Clock);
        competition.AddStage(stage.Id, Clock);
        stage.ReplaceDefaultTieFormat(
            new TieFormat(
                TieFormat.TwoLegs,
                aggregateScoring: true,
                penaltyShootoutRule: new PenaltyShootoutRule()),
            Clock);
        stage.AddGroup("G1", Clock);
        var matchday = stage.AddMatchday(1, Clock);
        var fixture = stage.AddFixture(matchday.Id, Clock);
        var match = Match.Create(competition.Id, stage.Id, home.Id, away.Id, Clock);
        stage.AttachMatch(fixture.Id, match.Id, legIndex: 1, Clock);
        stage.Prepare(Clock);
        stage.Start(Clock);
        competition.Prepare(Clock);
        competition.Start(Clock);
        stages.Add(stage);
        match.Start(Clock);
        matches.Add(match);
        await unitOfWork.SaveChangesAsync();
        return match.Id;
    }

    private sealed record CupSemiFinalSeed(
        Guid CompetitionId,
        Guid SemiStageId,
        Guid FinalStageId,
        Guid SemiRoundId);

    private sealed record SingleLegAttachments(Guid FixtureId, Guid MatchId);

    private static class CupTwoLegHttpSeed
    {
        public static async Task<CupSemiFinalSeed> CreateSemiAndFinalAsync(HttpClient client)
        {
            using var create = await client.PostAsJsonAsync(
                "/competitions",
                new CreateCompetitionRequest($"Cup-TAB {Guid.CreateVersion7():N}"));
            create.EnsureSuccessStatusCode();
            var competitionId =
                (await create.Content.ReadFromJsonAsync<WorkspaceSummaryDto>(HostJson.Options))!.Id;

            for (var i = 0; i < 4; i++)
            {
                using var entry = await client.PostAsJsonAsync(
                    $"/competitions/{competitionId}/entries",
                    new AddEntryRequest($"Équipe {i + 1}"));
                entry.EnsureSuccessStatusCode();
            }

            using var structure = await client.PostAsJsonAsync(
                $"/competitions/{competitionId}/structure",
                new ConfigureStructureRequest("Cup", BracketSize: 4));
            structure.EnsureSuccessStatusCode();
            var semiStageId = (await structure.Content.ReadFromJsonAsync<ConfigureStructureResponse>(HostJson.Options))!
                .Structure.Format.PrimaryStageId!.Value;

            using var finalStage = await client.PostAsJsonAsync(
                $"/competitions/{competitionId}/stages",
                new AddCompetitionStageRequest("Cup", "Finale", BracketSize: 2));
            finalStage.StatusCode.Should().Be(HttpStatusCode.Created);
            var finalStageId = (await finalStage.Content.ReadFromJsonAsync<AddCompetitionStageResponse>(HostJson.Options))!
                .StageId;

            using var overview = await client.GetAsync($"/stages/{semiStageId}");
            var roundId = (await overview.Content.ReadFromJsonAsync<StageOverviewDto>(HostJson.Options))!
                .Rounds[0]
                .Id;

            return new CupSemiFinalSeed(competitionId, semiStageId, finalStageId, roundId);
        }

        public static async Task SetDefaultTwoLegsAsync(
            HttpClient client,
            Guid semiStageId,
            bool requireExtraTime = true)
        {
            using var response = await client.PutAsJsonAsync(
                $"/stages/{semiStageId}/tie-format",
                new ReplaceStageDefaultTieFormatRequest(
                    Clear: false,
                    NumberOfLegs: 2,
                    HasExtraTimeRule: requireExtraTime,
                    HasPenaltyShootoutRule: true));
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        public static async Task WireSemiWinnerToFinalAsync(HttpClient client, CupSemiFinalSeed seed)
        {
            using var progression = await client.PutAsJsonAsync(
                $"/stages/{seed.SemiStageId}/progression-rules",
                new ReplaceStageProgressionRulesRequest(
                [
                    new ProgressionIntentRequest(
                        IntentId: null,
                        Order: 1,
                        RoundId: seed.SemiRoundId,
                        Outcome: ProgressionOutcome.Winner,
                        DestinationStageId: seed.FinalStageId,
                        DestinationSlotKeys: ["S1", "S2"])
                ]));
            progression.StatusCode.Should().Be(HttpStatusCode.NoContent);

            using var affectation = await client.PutAsJsonAsync(
                $"/stages/{seed.SemiStageId}/affectation",
                new EntryIdsRequest(await ReadActiveEntryIdsAsync(client, seed.CompetitionId)));
            affectation.EnsureSuccessStatusCode();
        }

        public static async Task RunSlotDrawAndMaterializeP1Async(HttpClient client, Guid semiStageId)
        {
            using (var drawRules = await client.PutAsJsonAsync(
                       $"/stages/{semiStageId}/draw-rules",
                       new ReplaceStageDrawRulesRequest(Clear: false, Mode: DrawMode.Random)))
            {
                drawRules.EnsureSuccessStatusCode();
            }

            using var createDraw = await client.PostAsJsonAsync(
                $"/stages/{semiStageId}/draws",
                new CreateDrawRequest("Slot"));
            createDraw.EnsureSuccessStatusCode();
            var draw = await createDraw.Content.ReadFromJsonAsync<DrawSummaryDto>(HostJson.Options);

            using (var inputs = await client.PostAsync($"/stages/{semiStageId}/draws/{draw!.DrawId}/inputs", null))
            {
                inputs.EnsureSuccessStatusCode();
            }

            using (var generate = await client.PostAsync(
                       $"/stages/{semiStageId}/draws/{draw.DrawId}/generate",
                       null))
            {
                generate.EnsureSuccessStatusCode();
            }

            using (var publish = await client.PostAsync(
                       $"/stages/{semiStageId}/draws/{draw.DrawId}/publish",
                       null))
            {
                publish.EnsureSuccessStatusCode();
            }

            using (var apply = await client.PostAsync($"/stages/{semiStageId}/draws/{draw.DrawId}/apply", null))
            {
                apply.StatusCode.Should().Be(HttpStatusCode.NoContent);
            }

            using var prepare = await HostStageHttpHelpers.PostPrepareAsync(client, semiStageId);
            prepare.StatusCode.Should().Be(HttpStatusCode.NoContent);

            using var materialize = await client.PostAsJsonAsync(
                $"/stages/{semiStageId}/matches/materialize-from-slots",
                new MaterializeCupFromOccupiedSlotsRequest(["P1", "P2"]));
            materialize.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        public static async Task StartExploitationAsync(HttpClient client, Guid competitionId, Guid semiStageId)
        {
            using (var compPrepare = await client.PostAsync($"/competitions/{competitionId}/prepare", null))
            {
                compPrepare.StatusCode.Should().Be(HttpStatusCode.NoContent);
            }

            using var stageStart = await HostStageHttpHelpers.PostStartAsync(client, semiStageId);
            stageStart.StatusCode.Should().Be(HttpStatusCode.NoContent);

            using var competitionStart = await client.PostAsync($"/competitions/{competitionId}/start", null);
            competitionStart.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        public static async Task<TwoLegFixtureAttachments> ReadTwoLegAttachmentsAsync(
            HttpClient client,
            Guid semiStageId)
        {
            using var overview = await client.GetAsync($"/stages/{semiStageId}");
            var stage = (await overview.Content.ReadFromJsonAsync<StageOverviewDto>(HostJson.Options))!;
            var p1 = stage.BracketPairs.OrderBy(p => p.PairKey, StringComparer.Ordinal).First();
            var fixture = stage.Rounds
                .SelectMany(round => round.Fixtures)
                .Single(f =>
                    f.SlotAKey == p1.SlotAKey
                    && f.SlotBKey == p1.SlotBKey
                    && f.Attachments.Count == 2);

            var leg1 = fixture.Attachments.Single(a => a.LegIndex == 1);
            var leg2 = fixture.Attachments.Single(a => a.LegIndex == 2);

            using var leg1Detail = await client.GetAsync($"/matches/{leg1.MatchId}");
            var leg1Body = await leg1Detail.Content.ReadFromJsonAsync<MatchDetailDto>(HostJson.Options);
            using var leg2Detail = await client.GetAsync($"/matches/{leg2.MatchId}");
            var leg2Body = await leg2Detail.Content.ReadFromJsonAsync<MatchDetailDto>(HostJson.Options);

            return new TwoLegFixtureAttachments(
                fixture.Id,
                leg1.MatchId,
                leg2.MatchId,
                leg1Body!.Home.EntryId!.Value,
                leg1Body.Away.EntryId!.Value);
        }

        public static async Task<SingleLegAttachments> ReadSingleLegAttachmentsAsync(
            HttpClient client,
            Guid semiStageId)
        {
            using var overview = await client.GetAsync($"/stages/{semiStageId}");
            var stage = (await overview.Content.ReadFromJsonAsync<StageOverviewDto>(HostJson.Options))!;
            var p1 = stage.BracketPairs.OrderBy(p => p.PairKey, StringComparer.Ordinal).First();
            var fixture = stage.Rounds
                .SelectMany(round => round.Fixtures)
                .Single(f =>
                    f.SlotAKey == p1.SlotAKey
                    && f.SlotBKey == p1.SlotBKey
                    && f.Attachments.Count == 1);
            var leg = fixture.Attachments.Single();
            return new SingleLegAttachments(fixture.Id, leg.MatchId);
        }

        private static async Task<Guid[]> ReadActiveEntryIdsAsync(HttpClient client, Guid competitionId)
        {
            using var structure = await client.GetAsync($"/competitions/{competitionId}/structure");
            var view = await structure.Content.ReadFromJsonAsync<StructureViewDto>(HostJson.Options);
            return [.. view!.Participants.Entries.Select(entry => entry.EntryId)];
        }
    }

    private sealed record TwoLegFixtureAttachments(
        Guid FixtureId,
        Guid Leg1MatchId,
        Guid Leg2MatchId,
        Guid Leg1HomeEntryId,
        Guid Leg1AwayEntryId);
}
