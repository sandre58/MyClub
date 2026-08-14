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
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Host.Contracts;
using Xunit;

namespace MyClub.PlayUp.Host.Tests;

/// <summary>
/// Phase 10.9 — vertical R2 proof: Publish → Apply → Start → Finish → Progression → destination Slot
/// via Host Minimal APIs and PostgreSQL (no Domain / Infrastructure changes beyond Prepare multi-stage load).
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
        var seed = await SeedResolvedDraftPairingCupAsync(factory);
        using var client = factory.CreateClient();

        // Prepare (Draft → Ready), including cross-stage progression destination validation.
        using (var prepare = await client.PostAsync(PrepareUri(seed.QuarterStageId), content: null))
        {
            prepare.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        using (var scope = factory.Services.CreateScope())
        {
            var quarter = await scope.ServiceProvider.GetRequiredService<IStageRepository>()
                .GetByIdAsync(seed.QuarterStageId);
            quarter!.Status.Should().Be(StageStatus.Ready);
            quarter.GetDraw(seed.DrawId).Status.Should().Be(DrawStatus.Draft);
            quarter.GetDraw(seed.DrawId).Resolution.State.Should().Be(DrawResolutionState.Resolved);
        }

        // PublishDraw
        using (var publish = await client.PostAsync(PublishUri(seed.QuarterStageId, seed.DrawId), content: null))
        {
            publish.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        using (var scope = factory.Services.CreateScope())
        {
            var draw = (await scope.ServiceProvider.GetRequiredService<IStageRepository>()
                .GetByIdAsync(seed.QuarterStageId))!.GetDraw(seed.DrawId);
            draw.Status.Should().Be(DrawStatus.Published);
            draw.Resolution.State.Should().Be(DrawResolutionState.Resolved);
        }

        // ApplyDraw (Pairing → Scheduled Match)
        using (var apply = await client.PostAsJsonAsync(
                   ApplyDrawUri(seed.QuarterStageId, seed.DrawId),
                   new ApplyDrawRequest([seed.FixtureId.Value])))
        {
            apply.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        MatchId matchId;
        using (var scope = factory.Services.CreateScope())
        {
            var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
            var matches = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
            var quarter = await stages.GetByIdAsync(seed.QuarterStageId);
            matchId = quarter!.GetFixture(seed.FixtureId).MatchIds.Should().ContainSingle().Subject;

            var match = await matches.GetByIdAsync(matchId);
            match.Should().NotBeNull();
            match.Status.Should().Be(MatchStatus.Scheduled);
            match.HomeEntryId.Should().Be(seed.Home);
            match.AwayEntryId.Should().Be(seed.Away);
            match.StageId.Should().Be(seed.QuarterStageId);
            match.CompetitionId.Should().Be(seed.CompetitionId);
            match.Result.Should().BeNull();
        }

        // StartMatch
        using (var start = await client.PostAsync(StartUri(matchId), content: null))
        {
            start.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        using (var scope = factory.Services.CreateScope())
        {
            var match = await scope.ServiceProvider.GetRequiredService<IMatchRepository>().GetByIdAsync(matchId);
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
            var match = await scope.ServiceProvider.GetRequiredService<IMatchRepository>().GetByIdAsync(matchId);
            match!.Status.Should().Be(MatchStatus.Finished);
            match.Result.Should().NotBeNull();
            match.Result!.Type.Should().Be(ResultType.Played);
            match.Result.Score.HomeGoals.Should().Be(2);
            match.Result.Score.AwayGoals.Should().Be(0);
        }

        // ApplyProgressionOutcome — Winner derived from finished Match(es), not from HTTP body.
        using (var progress = await client.PostAsync(
                   ProgressUri(seed.QuarterStageId, seed.FixtureId),
                   content: null))
        {
            progress.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        using (var scope = factory.Services.CreateScope())
        {
            var semi = await scope.ServiceProvider.GetRequiredService<IStageRepository>()
                .GetByIdAsync(seed.SemiStageId);
            semi.Should().NotBeNull();
            semi.FindSlot("SF1-A")!.EntryId.Should().Be(seed.Home);
            semi.FindSlot("SF1-B")!.EntryId.Should().BeNull();
        }
    }

    [IntegrationFact]
    public async Task ApplyProgression_when_fixture_has_no_match_leaves_destination_slot_emptyAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedResolvedDraftPairingCupAsync(factory);
        using var client = factory.CreateClient();

        using (var prepare = await client.PostAsync(PrepareUri(seed.QuarterStageId), content: null))
        {
            prepare.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        using (var publish = await client.PostAsync(PublishUri(seed.QuarterStageId, seed.DrawId), content: null))
        {
            publish.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        // Intentionally skip ApplyDraw — progression must fail without a coherent fixture/match set.
        using (var progress = await client.PostAsync(
                   ProgressUri(seed.QuarterStageId, seed.FixtureId),
                   content: null))
        {
            progress.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var problem = await progress.Content.ReadFromJsonAsync<ProblemDetails>();
            problem.Should().NotBeNull();
            GetCode(problem!).Should().Be(ApplicationErrorCodes.FixtureInvalid);
        }

        using (var scope = factory.Services.CreateScope())
        {
            var semi = await scope.ServiceProvider.GetRequiredService<IStageRepository>()
                .GetByIdAsync(seed.SemiStageId);
            semi!.FindSlot("SF1-A")!.EntryId.Should().BeNull();
            semi.FindSlot("SF1-B")!.EntryId.Should().BeNull();
        }
    }

    private static Uri PrepareUri(StageId stageId) =>
        new($"/stages/{stageId.Value}/prepare", UriKind.Relative);

    private static Uri PublishUri(StageId stageId, DrawId drawId) =>
        new($"/stages/{stageId.Value}/draws/{drawId.Value}/publish", UriKind.Relative);

    private static Uri ApplyDrawUri(StageId stageId, DrawId drawId) =>
        new($"/stages/{stageId.Value}/draws/{drawId.Value}/apply", UriKind.Relative);

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

    /// <summary>
    /// Seeds structure + Pairing Draw Draft+Resolved. HTTP workflow starts at Prepare / Publish.
    /// </summary>
    private async Task<R2CupSeed> SeedResolvedDraftPairingCupAsync(PlayUpWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var competition = Competition.Create(new CompetitionName("R2 Host Cup"), SampleRegulations.Standard(), _clock);
        competitions.Add(competition);

        var quarter = Stage.Create(competition.Id, new StageName("QF"), SampleRegulations.Standard(), _clock);
        quarter.AddRound("R1", new TieFormat(TieFormat.SingleLeg, aggregateScoring: false), _clock);
        var fixture = quarter.AddFixture(quarter.Rounds[0].Id, _clock);

        var semi = Stage.Create(competition.Id, new StageName("SF"), SampleRegulations.Standard(), _clock);
        semi.AddRound("R1", new TieFormat(TieFormat.SingleLeg, aggregateScoring: false), _clock);
        semi.AddSlot("SF1-A", _clock);
        semi.AddSlot("SF1-B", _clock);

        competition.AddStage(quarter.Id, _clock);
        competition.AddStage(semi.Id, _clock);

        quarter.ReplaceProgressionRules(
            new ProgressionRules(
            [
                new ProgressionPath(
                    fixture.Id,
                    ProgressionOutcome.Winner,
                    new ProgressionDestination(semi.Id, "SF1-A"))
            ]),
            _clock);

        var home = EntryId.New();
        var away = EntryId.New();
        var draw = quarter.CreateDraw(DrawResolutionKind.Pairing, _clock);
        quarter.ConfigureDrawInputs(draw.Id, DrawInputs.ForPairing([home, away]), _clock);
        quarter.RecordDrawResolution(
            draw.Id,
            DrawResolution.ResolvedPairings([new PairingDrawResult(home, away)]),
            _clock);

        stages.Add(quarter);
        stages.Add(semi);
        await unitOfWork.SaveChangesAsync();

        return new R2CupSeed(
            competition.Id,
            quarter.Id,
            semi.Id,
            draw.Id,
            fixture.Id,
            home,
            away);
    }

    private sealed record R2CupSeed(
        CompetitionId CompetitionId,
        StageId QuarterStageId,
        StageId SemiStageId,
        DrawId DrawId,
        FixtureId FixtureId,
        EntryId Home,
        EntryId Away);
}
