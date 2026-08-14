// -----------------------------------------------------------------------
// <copyright file="ReadSurfaceEndpointTests.cs" company="Stéphane ANDRE">
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
using Xunit;

namespace MyClub.PlayUp.Host.Tests;

[Collection("host-postgres")]
[Trait("Category", "Integration")]
public sealed class ReadSurfaceEndpointTests(HostPostgresFixture fixture)
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 14, 23, 0, 0, TimeSpan.Zero));

    [IntegrationFact]
    public async Task Get_competition_returns_200_overviewAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedCompetitionWithStageAsync(factory);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync($"/competitions/{seed.CompetitionId.Value}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<CompetitionOverviewDto>();
        body.Should().NotBeNull();
        body.Id.Should().Be(seed.CompetitionId.Value);
        body.Name.Should().Be("Read Cup");
        body.Entries.Should().Contain(entry => entry.DisplayName == "Alpha");
        body.Stages.Should().Contain(stage => stage.StageId == seed.StageId.Value && stage.Name == "QF");
        await AssertNoWinnerPropertyAsync(response);
    }

    [IntegrationFact]
    public async Task Get_competition_unknown_returns_404Async()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync($"/competitions/{Guid.CreateVersion7()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        GetCode(problem!).Should().Be(ApplicationErrorCodes.CompetitionNotFound);
    }

    [IntegrationFact]
    public async Task Get_stage_returns_200_overviewAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedCompetitionWithStageAsync(factory);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync($"/stages/{seed.StageId.Value}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<StageOverviewDto>();
        body.Should().NotBeNull();
        body.Id.Should().Be(seed.StageId.Value);
        body.Slots.Should().Contain(slot => slot.SlotKey == "SF1-A");
        body.Draws.Should().ContainSingle();
        await AssertNoWinnerPropertyAsync(response);
    }

    [IntegrationFact]
    public async Task Get_stage_unknown_returns_404Async()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync($"/stages/{Guid.CreateVersion7()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        GetCode(problem!).Should().Be(ApplicationErrorCodes.StageNotFound);
    }

    [IntegrationFact]
    public async Task Get_stage_matches_returns_200_and_empty_arrayAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedCompetitionWithStageAsync(factory);
        using var client = factory.CreateClient();

        using var empty = await client.GetAsync($"/stages/{seed.StageId.Value}/matches");
        empty.StatusCode.Should().Be(HttpStatusCode.OK);
        var emptyBody = await empty.Content.ReadFromJsonAsync<List<MatchSummaryDto>>();
        emptyBody.Should().NotBeNull().And.BeEmpty();

        MatchId matchId;
        using (var scope = factory.Services.CreateScope())
        {
            var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
            var matches = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var stage = await stages.GetByIdAsync(seed.StageId);
            var match = Match.Create(seed.CompetitionId, seed.StageId, seed.HomeEntryId, seed.AwayEntryId, _clock);
            matchId = match.Id;
            matches.Add(match);
            stage!.AttachMatch(seed.FixtureId, match.Id, legIndex: 1, _clock);
            await unitOfWork.SaveChangesAsync();
        }

        using var listed = await client.GetAsync($"/stages/{seed.StageId.Value}/matches");
        listed.StatusCode.Should().Be(HttpStatusCode.OK);
        var listBody = await listed.Content.ReadFromJsonAsync<List<MatchSummaryDto>>();
        listBody.Should().ContainSingle();
        listBody[0].MatchId.Should().Be(matchId.Value);
        listBody[0].Home.DisplayName.Should().Be("Alpha");
        await AssertNoWinnerPropertyAsync(listed);
    }

    [IntegrationFact]
    public async Task Get_match_returns_200_detail_and_unknown_404Async()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedCompetitionWithStageAsync(factory);

        MatchId matchId;
        using (var scope = factory.Services.CreateScope())
        {
            var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
            var matches = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var stage = await stages.GetByIdAsync(seed.StageId);
            var match = Match.Create(seed.CompetitionId, seed.StageId, seed.HomeEntryId, seed.AwayEntryId, _clock);
            match.Start(_clock);
            match.Finish(new MatchResult(ResultType.Played, new Score(2, 0)), _clock);
            matchId = match.Id;
            matches.Add(match);
            stage!.AttachMatch(seed.FixtureId, match.Id, legIndex: 1, _clock);
            await unitOfWork.SaveChangesAsync();
        }

        using var client = factory.CreateClient();
        using var response = await client.GetAsync($"/matches/{matchId.Value}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<MatchDetailDto>();
        body.Should().NotBeNull();
        body.MatchId.Should().Be(matchId.Value);
        body.Result.Should().NotBeNull();
        body.Result!.HomeGoals.Should().Be(2);
        body.FixtureId.Should().Be(seed.FixtureId.Value);
        body.Home.DisplayName.Should().Be("Alpha");
        await AssertNoWinnerPropertyAsync(response);

        using var missing = await client.GetAsync($"/matches/{Guid.CreateVersion7()}");
        missing.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = await missing.Content.ReadFromJsonAsync<ProblemDetails>();
        GetCode(problem!).Should().Be(ApplicationErrorCodes.MatchNotFound);
    }

    private async Task<ReadSeed> SeedCompetitionWithStageAsync(PlayUpWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var competition = Competition.Create(new CompetitionName("Read Cup"), SampleRegulations.Standard(), _clock);
        var home = competition.AddEntry(TeamId.New(), "Alpha", _clock);
        var away = competition.AddEntry(TeamId.New(), "Beta", _clock);

        var stage = Stage.Create(competition.Id, new StageName("QF"), SampleRegulations.Standard(), _clock);
        stage.AddRound("R1", new TieFormat(TieFormat.SingleLeg, aggregateScoring: false), _clock);
        var addFixture = stage.AddFixture(stage.Rounds[0].Id, _clock);
        stage.AddSlot("SF1-A", _clock);
        var draw = stage.CreateDraw(DrawResolutionKind.Pairing, _clock);
        stage.ConfigureDrawInputs(draw.Id, DrawInputs.ForPairing([home.Id, away.Id]), _clock);
        stage.RecordDrawResolution(
            draw.Id,
            DrawResolution.ResolvedPairings([new PairingDrawResult(home.Id, away.Id)]),
            _clock);

        competition.AddStage(stage.Id, _clock);
        competitions.Add(competition);
        stages.Add(stage);
        await unitOfWork.SaveChangesAsync();

        return new ReadSeed(competition.Id, stage.Id, addFixture.Id, home.Id, away.Id);
    }

    private static async Task AssertNoWinnerPropertyAsync(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
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
                        property.Name.Should().NotBe("winner",
                            because: "Match Winner must not appear on the read surface");
                        property.Name.Should().NotBe("Winner",
                            because: "Match Winner must not appear on the read surface");
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
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(element), element.ValueKind, "Unexpected element type");
        }
    }

    private static string? GetCode(ProblemDetails problem) =>
        !problem.Extensions.TryGetValue("code", out var raw) || raw is null
            ? null
            : raw switch
            {
                string text => text,
                JsonElement { ValueKind: JsonValueKind.String } element => element.GetString(),
                _ => raw.ToString()
            };

    private sealed record ReadSeed(
        CompetitionId CompetitionId,
        StageId StageId,
        FixtureId FixtureId,
        EntryId HomeEntryId,
        EntryId AwayEntryId);
}
