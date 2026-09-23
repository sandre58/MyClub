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
        var body = await response.Content.ReadFromJsonAsync<CompetitionDetailDto>(HostJson.Options);
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
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options);
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
        var body = await response.Content.ReadFromJsonAsync<StageOverviewDto>(HostJson.Options);
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
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options);
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
        var emptyBody = await empty.Content.ReadFromJsonAsync<List<MatchSummaryDto>>(HostJson.Options);
        emptyBody.Should().NotBeNull().And.BeEmpty();

        MatchId matchId;
        using (var scope = factory.Services.CreateScope())
        {
            var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
            var matches = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var stage = await stages.GetByIdForUpdateAsync(seed.StageId);
            var match = Match.Create(seed.CompetitionId, seed.StageId, seed.HomeEntryId, seed.AwayEntryId, _clock);
            matchId = match.Id;
            matches.Add(match);
            stage!.AttachMatch(seed.FixtureId, match.Id, legIndex: 1, _clock);
            await unitOfWork.SaveChangesAsync();
        }

        using var listed = await client.GetAsync($"/stages/{seed.StageId.Value}/matches");
        listed.StatusCode.Should().Be(HttpStatusCode.OK);
        var listBody = await listed.Content.ReadFromJsonAsync<List<MatchSummaryDto>>(HostJson.Options);
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
            var stage = await stages.GetByIdForUpdateAsync(seed.StageId);
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
        var body = await response.Content.ReadFromJsonAsync<MatchDetailDto>(HostJson.Options);
        body.Should().NotBeNull();
        body.MatchId.Should().Be(matchId.Value);
        body.Result.Should().NotBeNull();
        body.Result!.HomeGoals.Should().Be(2);
        body.FixtureId.Should().Be(seed.FixtureId.Value);
        body.Home.DisplayName.Should().Be("Alpha");
        await AssertNoWinnerPropertyAsync(response);

        using var missing = await client.GetAsync($"/matches/{Guid.CreateVersion7()}");
        missing.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = await missing.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options);
        GetCode(problem!).Should().Be(ApplicationErrorCodes.MatchNotFound);
    }

    [IntegrationFact]
    public async Task Get_overview_returns_200_projectionAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedCompetitionWithStageAsync(factory);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync($"/competitions/{seed.CompetitionId.Value}/overview");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<OverviewViewDto>(HostJson.Options);
        body.Should().NotBeNull();
        body.CompetitionId.Should().Be(seed.CompetitionId.Value);
        body.Name.Should().Be("Read Cup");
        body.Status.Should().Be(CompetitionStatus.Draft);
        body.CycleReading.Code.Should().Be(OverviewAssembler.CycleConstruction);
        body.ConstructionDimensions.Teams.Should().NotBeNull();
        body.ConstructionDimensions.Regulation.Competition.MinimumTeams.Should().BeGreaterThan(0);
        body.ConstructionDimensions.Regulation.CompetitionRegulationMutable.Should().BeTrue();
        body.ConstructionDimensions.Regulation.TransitionReadiness.Should().NotBeEmpty();
        body.ConstructionDimensions.Regulation.Stage.Should().NotBeNull();
        body.ConstructionDimensions.Regulation.Stage!.StageId.Should().Be(seed.StageId.Value);
        body.OperationalFocus.Stages.Should().Contain(stage => stage.StageId == seed.StageId.Value);
        body.OperationalFocus.Draws.Should().ContainSingle(draw => !draw.IsApplied);
        body.Situations.Should().NotBeNull();
        body.Situations.Should().OnlyContain(item =>
            item.Nature == OverviewAssembler.NatureBlocking
            || item.Nature == OverviewAssembler.NatureInformational);
        body.AttentionSummary.Count.Should().Be(body.AttentionSummary.Items.Count);
        body.AttentionSummary.Items.Should().OnlyContain(item => item.Nature == OverviewAssembler.NatureBlocking);
        body.AttentionSummary.Items.Should().OnlyContain(item =>
            body.Situations.Any(situation =>
                situation.Source == item.Source
                && situation.TargetType == item.TargetType
                && situation.TargetId == item.TargetId));
        body.AvailableActions.Should().NotBeNull();
        body.NaturalProgression.Should().NotBeNull();
        body.ClosureHint.Should().NotBeNull();
        body.NavigationHints.Should().Contain(hint => hint.TargetType == "Competition");
        await AssertNoWinnerPropertyAsync(response);
    }

    [IntegrationFact]
    public async Task Get_overview_running_projects_in_progress_without_open_matches_tipAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedRunningWithScheduledMatchAsync(factory);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync($"/competitions/{seed.CompetitionId.Value}/overview");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<OverviewViewDto>(HostJson.Options);
        body.Should().NotBeNull();
        body.Status.Should().Be(CompetitionStatus.Running);
        body.CycleReading.Code.Should().Be(OverviewAssembler.CycleInProgress);
        body.NaturalProgression.Should().BeNull();
        body.ClosureHint.CanCompleteNormally.Should().BeFalse();
        body.ClosureHint.BlockerCodes.Should().Contain(CompletionAnalyzer.ReasonScheduledMatches);
        body.ConstructionDimensions.Teams.Prominence.Should().Be(OverviewAssembler.ProminenceCondensed);
        body.ConstructionDimensions.Structure.Prominence.Should().Be(OverviewAssembler.ProminenceCondensed);
        body.ConstructionDimensions.Regulation.Prominence.Should().Be(OverviewAssembler.ProminenceCondensed);
        body.ConstructionDimensions.Regulation.CompetitionRegulationMutable.Should().BeFalse();
        body.ConstructionDimensions.Regulation.TransitionReadiness.Should().BeEmpty();
        body.AvailableActions.Should().NotContain(action =>
            action.Code == OverviewAssembler.ActionPrepareCompetition
            || action.Code == OverviewAssembler.ActionStartCompetition
            || action.Code == "OpenMatches");
        body.Situations.Should().NotContain(item =>
            item.Source == "InsufficientParticipants" || item.Source == "MissingStage");
        body.OperationalFocus.MatchCounts.Scheduled.Should().Be(1);
        await AssertNoWinnerPropertyAsync(response);
    }

    [IntegrationFact]
    public async Task Get_overview_suspended_projects_in_progress_informational_not_in_attentionAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedSuspendedWithScheduledMatchAsync(factory);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync($"/competitions/{seed.CompetitionId.Value}/overview");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<OverviewViewDto>(HostJson.Options);
        body.Should().NotBeNull();
        body.Status.Should().Be(CompetitionStatus.Suspended);
        body.CycleReading.Code.Should().Be(OverviewAssembler.CycleInProgress);
        body.NaturalProgression.Should().BeNull();
        body.Situations.Should().Contain(item =>
            item.Source == OverviewAssembler.SourceCompetitionSuspended
            && item.Nature == OverviewAssembler.NatureInformational);
        body.AttentionSummary.Items.Should().NotContain(item =>
            item.Source == OverviewAssembler.SourceCompetitionSuspended);
        body.AttentionSummary.Items.Should().OnlyContain(item => item.Nature == OverviewAssembler.NatureBlocking);
        await AssertNoWinnerPropertyAsync(response);
    }

    [IntegrationFact]
    public async Task Get_overview_unknown_returns_404Async()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync($"/competitions/{Guid.CreateVersion7()}/overview");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options);
        GetCode(problem!).Should().Be(ApplicationErrorCodes.CompetitionNotFound);
    }

    [IntegrationFact]
    public async Task Get_overview_resolves_fixture_to_match_navigationAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedCompetitionWithStageAsync(factory);

        MatchId matchId;
        using (var scope = factory.Services.CreateScope())
        {
            var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
            var matches = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var stage = await stages.GetByIdForUpdateAsync(seed.StageId);
            var match = Match.Create(seed.CompetitionId, seed.StageId, seed.HomeEntryId, seed.AwayEntryId, _clock);
            matchId = match.Id;
            matches.Add(match);
            stage!.AttachMatch(seed.FixtureId, match.Id, legIndex: 1, _clock);
            await unitOfWork.SaveChangesAsync();
        }

        using var client = factory.CreateClient();
        using var response = await client.GetAsync($"/competitions/{seed.CompetitionId.Value}/overview");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<OverviewViewDto>(HostJson.Options);
        body.Should().NotBeNull();
        body.NavigationHints.Should().Contain(hint =>
            hint.TargetType == "Fixture"
            && hint.TargetId == seed.FixtureId.Value.ToString()
            && hint.MatchId == matchId.Value);
        body.OperationalFocus.MatchCounts.Scheduled.Should().Be(1);
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
        stage.AddSlot("SF1-A");
        stage.AddSlot("S1");
        stage.AddSlot("S2");
        var draw = stage.CreateDraw(DrawResolutionKind.Slot, _clock);
        stage.ConfigureDrawInputs(draw.Id, DrawInputs.ForSlot([home.Id, away.Id]));
        stage.RecordDrawResolution(
            draw.Id,
            DrawResolution.ResolvedSlots(
            [
                new SlotDrawPlacement(home.Id, "S1"),
                new SlotDrawPlacement(away.Id, "S2")
            ]),
            _clock);

        competition.AddStage(stage.Id, _clock);
        competitions.Add(competition);
        stages.Add(stage);
        await unitOfWork.SaveChangesAsync();

        return new ReadSeed(competition.Id, stage.Id, addFixture.Id, home.Id, away.Id);
    }

    private async Task<ReadSeed> SeedRunningWithScheduledMatchAsync(PlayUpWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var matches = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var competition = Competition.Create(new CompetitionName("Running Cup"), SampleRegulations.Standard(), _clock);
        var home = competition.AddEntry(TeamId.New(), "Alpha", _clock);
        var away = competition.AddEntry(TeamId.New(), "Beta", _clock);
        competitions.Add(competition);

        var stage = Stage.Create(competition.Id, new StageName("MD"), SampleRegulations.Standard(), _clock);
        stage.AddMatchday(1, _clock);
        competition.AddStage(stage.Id, _clock);
        competition.Prepare(_clock);
        competition.Start(_clock);
        stage.Prepare(_clock);
        stage.Start(_clock);
        var match = Match.Create(competition.Id, stage.Id, home.Id, away.Id, _clock);
        stages.Add(stage);
        matches.Add(match);
        await unitOfWork.SaveChangesAsync();

        return new ReadSeed(competition.Id, stage.Id, FixtureId: default, home.Id, away.Id);
    }

    private async Task<ReadSeed> SeedSuspendedWithScheduledMatchAsync(PlayUpWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var matches = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var competition =
            Competition.Create(new CompetitionName("Suspended Cup"), SampleRegulations.Standard(), _clock);
        var home = competition.AddEntry(TeamId.New(), "Alpha", _clock);
        var away = competition.AddEntry(TeamId.New(), "Beta", _clock);
        competitions.Add(competition);

        var stage = Stage.Create(competition.Id, new StageName("MD"), SampleRegulations.Standard(), _clock);
        stage.AddMatchday(1, _clock);
        competition.AddStage(stage.Id, _clock);
        competition.Prepare(_clock);
        competition.Start(_clock);
        stage.Prepare(_clock);
        stage.Start(_clock);
        competition.Suspend(_clock);
        var match = Match.Create(competition.Id, stage.Id, home.Id, away.Id, _clock);
        stages.Add(stage);
        matches.Add(match);
        await unitOfWork.SaveChangesAsync();

        return new ReadSeed(competition.Id, stage.Id, FixtureId: default, home.Id, away.Id);
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
