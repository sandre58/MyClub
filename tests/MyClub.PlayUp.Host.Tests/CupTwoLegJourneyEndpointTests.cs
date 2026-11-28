// -----------------------------------------------------------------------
// <copyright file="CupTwoLegJourneyEndpointTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using MyClub.PlayUp.Application;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Host.Contracts;
using Xunit;

namespace MyClub.PlayUp.Host.Tests;

/// <summary>
/// Cup four teams, two legs on entry round: HTTP chain through progression to the next stage.
/// </summary>
[Collection("host-postgres")]
[Trait("Category", "Integration")]
public sealed class CupTwoLegJourneyEndpointTests(HostPostgresFixture fixture)
{
    [IntegrationFact]
    public async Task Two_leg_P1_fixture_apply_progression_fills_next_round_and_rejects_et_on_first_legAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();

        var seed = await CupTwoLegHttpSeed.CreateSemiAndFinalAsync(client);
        await CupTwoLegHttpSeed.SetDefaultTwoLegsAsync(client, seed.SemiStageId);
        await CupTwoLegHttpSeed.WireSemiWinnerToFinalSlotAsync(client, seed);
        await CupTwoLegHttpSeed.RunSlotDrawAndMaterializeP1Async(client, seed.SemiStageId);
        await CupTwoLegHttpSeed.StartExploitationAsync(client, seed.CompetitionId, seed.SemiStageId);

        var legs = await CupTwoLegHttpSeed.ReadTwoLegAttachmentsAsync(client, seed.SemiStageId);
        legs.Leg1Index.Should().Be(1);
        legs.Leg2Index.Should().Be(2);

        using (var startLeg1 = await client.PostAsync($"/matches/{legs.Leg1MatchId}/start", null))
        {
            startLeg1.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        using (var etLeg1 = await client.PostAsJsonAsync(
                   $"/matches/{legs.Leg1MatchId}/finish",
                   new FinishMatchRequest(ResultType.Played, 1, 1, ExtraTimePlayed: true)))
        {
            etLeg1.StatusCode.Should().Be(HttpStatusCode.Conflict);
            var problem = await etLeg1.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options);
            GetCode(problem).Should().Be(MatchErrorCodes.ExtraTimeOrShootoutOnNonFinalLeg);
        }

        using (var finishLeg1 = await client.PostAsJsonAsync(
                   $"/matches/{legs.Leg1MatchId}/finish",
                   new FinishMatchRequest(ResultType.Played, 2, 0)))
        {
            finishLeg1.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        using (var startLeg2 = await client.PostAsync($"/matches/{legs.Leg2MatchId}/start", null))
        {
            startLeg2.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        using (var finishLeg2 = await client.PostAsJsonAsync(
                   $"/matches/{legs.Leg2MatchId}/finish",
                   new FinishMatchRequest(ResultType.Played, 0, 1)))
        {
            finishLeg2.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        using var progression = await client.PostAsync(
            $"/stages/{seed.SemiStageId}/fixtures/{legs.FixtureId}/apply-progression",
            null);
        progression.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var finalOverview = await client.GetAsync($"/stages/{seed.FinalStageId}");
        finalOverview.StatusCode.Should().Be(HttpStatusCode.OK);
        var final = await finalOverview.Content.ReadFromJsonAsync<StageOverviewDto>(HostJson.Options);
        var qualifiedSlot = final!.Slots.Single(s => s.SlotKey == "S1");
        qualifiedSlot.EntryId.Should().NotBeNull();
        qualifiedSlot.EntryId.Should().Be(legs.Leg1HomeEntryId);
    }

    [IntegrationFact]
    public async Task Two_leg_aggregate_tie_ET_on_return_leg_applies_progressionAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();

        var seed = await CupTwoLegHttpSeed.CreateSemiAndFinalAsync(client);
        await CupTwoLegHttpSeed.SetDefaultTwoLegsAsync(client, seed.SemiStageId);
        await CupTwoLegHttpSeed.WireSemiWinnerToFinalSlotAsync(client, seed);
        await CupTwoLegHttpSeed.RunSlotDrawAndMaterializeP1Async(client, seed.SemiStageId);
        await CupTwoLegHttpSeed.StartExploitationAsync(client, seed.CompetitionId, seed.SemiStageId);

        var legs = await CupTwoLegHttpSeed.ReadTwoLegAttachmentsAsync(client, seed.SemiStageId);

        await StartAndFinishAsync(client, legs.Leg1MatchId, 1, 0);
        await client.PostAsync($"/matches/{legs.Leg2MatchId}/start", null);
        using (var finishLeg2 = await client.PostAsJsonAsync(
                   $"/matches/{legs.Leg2MatchId}/finish",
                   new FinishMatchRequest(
                       ResultType.Played,
                       HomeGoals: 0,
                       AwayGoals: 0,
                       ExtraTimePlayed: true,
                       PenaltyShootoutHomeGoals: 5,
                       PenaltyShootoutAwayGoals: 4)))
        {
            finishLeg2.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        using var progression = await client.PostAsync(
            $"/stages/{seed.SemiStageId}/fixtures/{legs.FixtureId}/apply-progression",
            null);
        progression.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var finalOverview = await client.GetAsync($"/stages/{seed.FinalStageId}");
        var final = await finalOverview.Content.ReadFromJsonAsync<StageOverviewDto>(HostJson.Options);
        final!.Slots.Single(s => s.SlotKey == "S1").EntryId.Should().Be(legs.Leg1HomeEntryId);
    }

    [IntegrationFact]
    public async Task Put_stage_tie_format_persists_defaultAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();

        var seed = await CupTwoLegHttpSeed.CreateSemiAndFinalAsync(client);

        using var response = await client.PutAsJsonAsync(
            $"/stages/{seed.SemiStageId}/tie-format",
            new ReplaceStageDefaultTieFormatRequest(Clear: false, NumberOfLegs: 2));
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var structure = await GetStructureAsync(client, seed.CompetitionId);
        var semi = structure.Stages.Single(s => s.StageId == seed.SemiStageId);
        semi.DefaultTieFormat.Should().NotBeNull();
        semi.DefaultTieFormat!.NumberOfLegs.Should().Be(2);
        semi.DefaultTieFormat.AggregateScoring.Should().BeTrue();
    }

    [IntegrationFact]
    public async Task Put_round_tie_format_persists_round_overrideAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();

        var seed = await CupTwoLegHttpSeed.CreateSemiAndFinalAsync(client);
        await CupTwoLegHttpSeed.SetDefaultTwoLegsAsync(client, seed.SemiStageId);

        using var overview = await client.GetAsync($"/stages/{seed.SemiStageId}");
        var roundId = (await overview.Content.ReadFromJsonAsync<StageOverviewDto>(HostJson.Options))!
            .Rounds[0]
            .Id;

        using var response = await client.PutAsJsonAsync(
            $"/stages/{seed.SemiStageId}/rounds/{roundId}/tie-format",
            new ReplaceRoundTieFormatRequest(Clear: false, NumberOfLegs: 1));
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var structure = await GetStructureAsync(client, seed.CompetitionId);
        var semi = structure.Stages.Single(s => s.StageId == seed.SemiStageId);
        var roundRef = semi.ConfrontationSegments!
            .SelectMany(segment => segment.Rounds)
            .Single(round => round.RoundId == roundId);
        roundRef.InheritsDefaultTieFormat.Should().BeFalse();
        semi.ConfrontationSegments!
            .Single(segment => segment.Rounds.Any(r => r.RoundId == roundId))
            .NumberOfLegs.Should().Be(1);
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

    private static async Task<StructureViewDto> GetStructureAsync(HttpClient client, Guid competitionId)
    {
        using var response = await client.GetAsync($"/competitions/{competitionId}/structure");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<StructureViewDto>(HostJson.Options))!;
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

    private sealed record CupSemiFinalSeed(
        Guid CompetitionId,
        Guid SemiStageId,
        Guid FinalStageId,
        Guid SemiRoundId);

    private static class CupTwoLegHttpSeed
    {
        public static async Task<CupSemiFinalSeed> CreateSemiAndFinalAsync(HttpClient client)
        {
            using var create = await client.PostAsJsonAsync(
                "/competitions",
                new CreateCompetitionRequest($"Cup-AR {Guid.CreateVersion7():N}"));
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

        public static async Task SetDefaultTwoLegsAsync(HttpClient client, Guid semiStageId)
        {
            using var response = await client.PutAsJsonAsync(
                $"/stages/{semiStageId}/tie-format",
                new ReplaceStageDefaultTieFormatRequest(
                    Clear: false,
                    NumberOfLegs: 2,
                    HasExtraTimeRule: true,
                    HasPenaltyShootoutRule: true));
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        public static async Task WireSemiWinnerToFinalSlotAsync(HttpClient client, CupSemiFinalSeed seed)
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
            if (progression.StatusCode != HttpStatusCode.NoContent)
            {
                var body = await progression.Content.ReadAsStringAsync();
                throw new InvalidOperationException(
                    $"PUT progression-rules failed {(int)progression.StatusCode}: {body}");
            }

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

            using (var prepare = await HostStageHttpHelpers.PostPrepareAsync(client, semiStageId))
            {
                if (prepare.StatusCode != HttpStatusCode.NoContent)
                {
                    throw new InvalidOperationException(
                        $"POST prepare failed {(int)prepare.StatusCode}: {await prepare.Content.ReadAsStringAsync()}");
                }
            }

            using var materialize = await client.PostAsJsonAsync(
                $"/stages/{semiStageId}/matches/materialize-from-slots",
                new MaterializeCupFromOccupiedSlotsRequest(["P1", "P2"]));
            materialize.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await materialize.Content.ReadFromJsonAsync<MaterializeMatchesResponse>(HostJson.Options);
            body!.CreatedCount.Should().Be(4);
        }

        public static async Task StartExploitationAsync(HttpClient client, Guid competitionId, Guid semiStageId)
        {
            using (var compPrepare = await client.PostAsync(
                       $"/competitions/{competitionId}/prepare",
                       null))
            {
                compPrepare.StatusCode.Should().Be(HttpStatusCode.NoContent);
            }

            using (var stageStart = await HostStageHttpHelpers.PostStartAsync(client, semiStageId))
            {
                if (stageStart.StatusCode != HttpStatusCode.NoContent)
                {
                    throw new InvalidOperationException(
                        $"POST stage start failed {(int)stageStart.StatusCode}: {await stageStart.Content.ReadAsStringAsync()}");
                }
            }

            using var competitionStart = await client.PostAsync(
                $"/competitions/{competitionId}/start",
                null);
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
                leg1.LegIndex,
                leg2.LegIndex,
                leg1Body!.Home.EntryId!.Value,
                leg1Body.Away.EntryId!.Value,
                leg2Body!.Home.EntryId!.Value);
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
        int Leg1Index,
        int Leg2Index,
        Guid Leg1HomeEntryId,
        Guid Leg1AwayEntryId,
        Guid Leg2HomeEntryId);
}