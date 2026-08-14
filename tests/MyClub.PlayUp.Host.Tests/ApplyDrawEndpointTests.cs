// -----------------------------------------------------------------------
// <copyright file="ApplyDrawEndpointTests.cs" company="Stéphane ANDRE">
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
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Host.Contracts;
using Xunit;

namespace MyClub.PlayUp.Host.Tests;

[Collection("host-postgres")]
[Trait("Category", "Integration")]
public sealed class ApplyDrawEndpointTests(HostPostgresFixture fixture)
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 14, 20, 0, 0, TimeSpan.Zero));

    [IntegrationFact]
    public async Task Apply_pairing_returns_204_and_persists_Scheduled_matchAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedPublishedPairingAsync(factory);

        using var client = factory.CreateClient();
        using var response = await client.PostAsJsonAsync(
            ApplyUri(seed.StageId, seed.DrawId),
            new ApplyDrawRequest([seed.FixtureId.Value]));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = factory.Services.CreateScope();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var matches = scope.ServiceProvider.GetRequiredService<IMatchRepository>();

        var stage = await stages.GetByIdAsync(seed.StageId);
        stage.Should().NotBeNull();
        var matchId = stage.GetFixture(seed.FixtureId).MatchIds.Should().ContainSingle().Subject;

        var match = await matches.GetByIdAsync(matchId);
        match.Should().NotBeNull();
        match.Status.Should().Be(MatchStatus.Scheduled);
        match.HomeEntryId.Should().Be(seed.EntryA);
        match.AwayEntryId.Should().Be(seed.EntryB);
        match.Result.Should().BeNull();
    }

    [IntegrationFact]
    public async Task Apply_when_stage_missing_returns_404Async()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);

        using var client = factory.CreateClient();
        using var response = await client.PostAsJsonAsync(
            ApplyUri(StageId.New(), DrawId.New()),
            new ApplyDrawRequest([Guid.NewGuid()]));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        GetCode(problem!).Should().Be(ApplicationErrorCodes.StageNotFound);
    }

    [IntegrationFact]
    public async Task Apply_when_draw_draft_returns_400_and_creates_no_matchAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedDraftPairingAsync(factory);

        using var client = factory.CreateClient();
        using var response = await client.PostAsJsonAsync(
            ApplyUri(seed.StageId, seed.DrawId),
            new ApplyDrawRequest([seed.FixtureId.Value]));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        GetCode(problem!).Should().Be(ApplicationErrorCodes.DrawApplyFailure);

        using var scope = factory.Services.CreateScope();
        var stage = await scope.ServiceProvider.GetRequiredService<IStageRepository>().GetByIdAsync(seed.StageId);
        stage!.GetFixture(seed.FixtureId).MatchIds.Should().BeEmpty();
    }

    [IntegrationFact]
    public async Task Apply_then_start_persists_Live_matchAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedPublishedPairingAsync(factory);

        using var client = factory.CreateClient();
        using (var apply = await client.PostAsJsonAsync(
                   ApplyUri(seed.StageId, seed.DrawId),
                   new ApplyDrawRequest([seed.FixtureId.Value])))
        {
            apply.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        MatchId matchId;
        using (var scope = factory.Services.CreateScope())
        {
            var stage = await scope.ServiceProvider.GetRequiredService<IStageRepository>().GetByIdAsync(seed.StageId);
            matchId = stage!.GetFixture(seed.FixtureId).MatchIds.Should().ContainSingle().Subject;
        }

        using (var start = await client.PostAsync(new Uri($"/matches/{matchId.Value}/start", UriKind.Relative), content: null))
        {
            start.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        using (var scope = factory.Services.CreateScope())
        {
            var match = await scope.ServiceProvider.GetRequiredService<IMatchRepository>().GetByIdAsync(matchId);
            match!.Status.Should().Be(MatchStatus.Live);
        }
    }

    private static Uri ApplyUri(StageId stageId, DrawId drawId) =>
        new($"/stages/{stageId.Value}/draws/{drawId.Value}/apply", UriKind.Relative);

    private static string? GetCode(ProblemDetails problem) =>
        !problem.Extensions.TryGetValue("code", out var raw) || raw is null
            ? null
            : raw switch
            {
                string text => text,
                JsonElement { ValueKind: JsonValueKind.String } element => element.GetString(),
                _ => raw.ToString()
            };

    private Task<PairingSeed> SeedPublishedPairingAsync(PlayUpWebApplicationFactory factory) =>
        SeedPairingAsync(factory, publish: true);

    private Task<PairingSeed> SeedDraftPairingAsync(PlayUpWebApplicationFactory factory) =>
        SeedPairingAsync(factory, publish: false);

    private async Task<PairingSeed> SeedPairingAsync(PlayUpWebApplicationFactory factory, bool publish)
    {
        using var scope = factory.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var competition = Competition.Create(new CompetitionName("Apply Draw Cup"), SampleRegulations.Standard(), _clock);
        competitions.Add(competition);

        var stage = Stage.Create(competition.Id, new StageName("QF"), SampleRegulations.Standard(), _clock);
        var round = stage.AddRound("R1", _clock);
        var fixture = stage.AddFixture(round.Id, _clock);
        var entryA = EntryId.New();
        var entryB = EntryId.New();
        var draw = stage.CreateDraw(DrawResolutionKind.Pairing, _clock);
        stage.ConfigureDrawInputs(draw.Id, DrawInputs.ForPairing([entryA, entryB]), _clock);
        stage.RecordDrawResolution(
            draw.Id,
            DrawResolution.ResolvedPairings([new PairingDrawResult(entryA, entryB)]),
            _clock);
        if (publish)
        {
            stage.PublishDraw(draw.Id, _clock);
        }

        competition.AddStage(stage.Id, _clock);
        stages.Add(stage);
        await unitOfWork.SaveChangesAsync();

        return new PairingSeed(stage.Id, draw.Id, fixture.Id, entryA, entryB);
    }

    private sealed record PairingSeed(
        StageId StageId,
        DrawId DrawId,
        FixtureId FixtureId,
        EntryId EntryA,
        EntryId EntryB);
}
