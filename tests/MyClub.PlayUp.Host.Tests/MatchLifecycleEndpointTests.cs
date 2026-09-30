// -----------------------------------------------------------------------
// <copyright file="MatchLifecycleEndpointTests.cs" company="Stéphane ANDRE">
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
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Host.Contracts;
using Xunit;

namespace MyClub.PlayUp.Host.Tests;

[Collection("host-postgres")]
[Trait("Category", "Integration")]
public sealed class MatchLifecycleEndpointTests(HostPostgresFixture fixture)
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 14, 19, 0, 0, TimeSpan.Zero));

    [IntegrationFact]
    public async Task Start_returns_204_and_persists_LiveAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var matchId = await SeedScheduledMatchAsync(factory);

        using var client = factory.CreateClient();
        using var response = await client.PostAsync(StartUri(matchId), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = factory.Services.CreateScope();
        var loaded = await scope.ServiceProvider.GetRequiredService<IMatchRepository>().GetByIdForUpdateAsync(matchId);
        loaded.Should().NotBeNull();
        loaded.Status.Should().Be(MatchStatus.Live);
        loaded.Result.Should().BeNull();
    }

    [IntegrationFact]
    public async Task Start_when_missing_returns_404_MatchNotFoundAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);

        using var client = factory.CreateClient();
        using var response = await client.PostAsync(StartUri(MatchId.New()), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options);
        problem.Should().NotBeNull();
        GetCode(problem).Should().Be(ApplicationErrorCodes.MatchNotFound);
    }

    [IntegrationFact]
    public async Task Start_when_already_Live_returns_409_InvalidTransitionAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var matchId = await SeedLiveMatchAsync(factory);

        using var client = factory.CreateClient();
        using var response = await client.PostAsync(StartUri(matchId), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options);
        problem.Should().NotBeNull();
        GetCode(problem).Should().Be(MatchErrorCodes.InvalidTransition);

        using var scope = factory.Services.CreateScope();
        var loaded = await scope.ServiceProvider.GetRequiredService<IMatchRepository>().GetByIdForUpdateAsync(matchId);
        loaded!.Status.Should().Be(MatchStatus.Live);
    }

    [IntegrationFact]
    public async Task Finish_returns_204_and_persists_Finished_with_resultAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var matchId = await SeedLiveMatchAsync(factory);
        var body = new FinishMatchRequest(ResultType.Played, HomeGoals: 2, AwayGoals: 1);

        using var client = factory.CreateClient();
        using var response = await client.PostAsJsonAsync(FinishUri(matchId), body);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = factory.Services.CreateScope();
        var loaded = await scope.ServiceProvider.GetRequiredService<IMatchRepository>().GetByIdForUpdateAsync(matchId);
        loaded.Should().NotBeNull();
        loaded.Status.Should().Be(MatchStatus.Finished);
        loaded.Result.Should().NotBeNull();
        loaded.Result.Type.Should().Be(ResultType.Played);
        loaded.Result.Score.HomeGoals.Should().Be(2);
        loaded.Result.Score.AwayGoals.Should().Be(1);
        loaded.Result.ExtraTimePlayed.Should().BeFalse();
        loaded.Result.PenaltyShootoutScore.Should().BeNull();
    }

    [IntegrationFact]
    public async Task Finish_when_missing_returns_404_MatchNotFoundAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var body = new FinishMatchRequest(ResultType.Played, 1, 0);

        using var client = factory.CreateClient();
        using var response = await client.PostAsJsonAsync(FinishUri(MatchId.New()), body);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options);
        problem.Should().NotBeNull();
        GetCode(problem).Should().Be(ApplicationErrorCodes.MatchNotFound);
    }

    [IntegrationFact]
    public async Task Finish_when_Cancelled_returns_409_InvalidTransitionAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var matchId = await SeedCancelledMatchAsync(factory);
        var body = new FinishMatchRequest(ResultType.Played, 1, 0);

        using var client = factory.CreateClient();
        using var response = await client.PostAsJsonAsync(FinishUri(matchId), body);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options);
        problem.Should().NotBeNull();
        GetCode(problem).Should().Be(MatchErrorCodes.InvalidTransition);

        using var scope = factory.Services.CreateScope();
        var loaded = await scope.ServiceProvider.GetRequiredService<IMatchRepository>().GetByIdForUpdateAsync(matchId);
        loaded!.Status.Should().Be(MatchStatus.Cancelled);
        loaded.Result.Should().BeNull();
    }

    [IntegrationFact]
    public async Task Finish_when_shootout_on_unequal_score_returns_409_InvalidResultAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var matchId = await SeedLiveMatchAsync(factory);
        var body = new FinishMatchRequest(
            ResultType.Played,
            HomeGoals: 2,
            AwayGoals: 1,
            ExtraTimePlayed: false,
            PenaltyShootoutHomeGoals: 5,
            PenaltyShootoutAwayGoals: 4);

        using var client = factory.CreateClient();
        using var response = await client.PostAsJsonAsync(FinishUri(matchId), body);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options);
        problem.Should().NotBeNull();
        GetCode(problem).Should().Be(MatchErrorCodes.InvalidResult);

        using var scope = factory.Services.CreateScope();
        var loaded = await scope.ServiceProvider.GetRequiredService<IMatchRepository>().GetByIdForUpdateAsync(matchId);
        loaded!.Status.Should().Be(MatchStatus.Live);
        loaded.Result.Should().BeNull();
    }

    [IntegrationFact]
    public async Task Start_then_finish_workflow_persists_Finished_resultAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var matchId = await SeedScheduledMatchAsync(factory);
        var body = new FinishMatchRequest(
            ResultType.Played,
            HomeGoals: 1,
            AwayGoals: 1,
            ExtraTimePlayed: true,
            PenaltyShootoutHomeGoals: 4,
            PenaltyShootoutAwayGoals: 3);

        using var client = factory.CreateClient();

        using (var start = await client.PostAsync(StartUri(matchId), content: null))
        {
            start.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        using (var scope = factory.Services.CreateScope())
        {
            var mid = await scope.ServiceProvider.GetRequiredService<IMatchRepository>().GetByIdForUpdateAsync(matchId);
            mid!.Status.Should().Be(MatchStatus.Live);
        }

        using (var finish = await client.PostAsJsonAsync(FinishUri(matchId), body))
        {
            finish.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        using (var scope = factory.Services.CreateScope())
        {
            var loaded = await scope.ServiceProvider.GetRequiredService<IMatchRepository>().GetByIdForUpdateAsync(matchId);
            loaded!.Status.Should().Be(MatchStatus.Finished);
            loaded.Result.Should().NotBeNull();
            loaded.Result!.Score.Should().Be(new Score(1, 1));
            loaded.Result.ExtraTimePlayed.Should().BeTrue();
            loaded.Result.PenaltyShootoutScore.Should().Be(new PenaltyShootoutScore(4, 3));
        }
    }

    [IntegrationFact]
    public async Task Start_when_competition_completed_returns_409_MatchOperationNotAllowedAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var matchId = await SeedMatchOnClosedCompetitionAsync(factory, archive: false);

        using var client = factory.CreateClient();
        using var response = await client.PostAsync(StartUri(matchId), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options);
        problem.Should().NotBeNull();
        GetCode(problem).Should().Be(ApplicationErrorCodes.MatchOperationNotAllowed);

        using var scope = factory.Services.CreateScope();
        var loaded = await scope.ServiceProvider.GetRequiredService<IMatchRepository>().GetByIdForUpdateAsync(matchId);
        loaded!.Status.Should().Be(MatchStatus.Scheduled);
    }

    [IntegrationFact]
    public async Task Finish_when_competition_archived_returns_409_MatchOperationNotAllowedAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var matchId = await SeedMatchOnClosedCompetitionAsync(factory, archive: true, startMatch: true);

        using var client = factory.CreateClient();
        using var response = await client.PostAsJsonAsync(
            FinishUri(matchId),
            new FinishMatchRequest(ResultType.Played, 1, 0));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options);
        problem.Should().NotBeNull();
        GetCode(problem).Should().Be(ApplicationErrorCodes.MatchOperationNotAllowed);

        using var scope = factory.Services.CreateScope();
        var loaded = await scope.ServiceProvider.GetRequiredService<IMatchRepository>().GetByIdForUpdateAsync(matchId);
        loaded!.Status.Should().Be(MatchStatus.Live);
        loaded.Result.Should().BeNull();
    }

    [IntegrationFact]
    public async Task List_detail_start_finish_exposes_optional_placement_and_persists_lifecycleAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedAttachedMatchWithPlacementAsync(factory);
        using var client = factory.CreateClient();

        using (var listed = await client.GetAsync($"/stages/{seed.StageId.Value}/matches"))
        {
            listed.StatusCode.Should().Be(HttpStatusCode.OK);
            var summaries = await listed.Content.ReadFromJsonAsync<MatchSummaryDto[]>(HostJson.Options);
            summaries.Should().ContainSingle();
            summaries[0].MatchId.Should().Be(seed.MatchId.Value);
            summaries[0].Status.Should().Be(MatchStatus.Scheduled);
            summaries[0].ScheduledAt.Should().Be(seed.Kickoff);
            summaries[0].ResourceId.Should().Be(seed.ResourceId.Value);
        }

        using (var detailBefore = await client.GetAsync($"/matches/{seed.MatchId.Value}"))
        {
            detailBefore.StatusCode.Should().Be(HttpStatusCode.OK);
            var detail = await detailBefore.Content.ReadFromJsonAsync<MatchDetailDto>(HostJson.Options);
            detail.Should().NotBeNull();
            detail.ScheduledAt.Should().Be(seed.Kickoff);
            detail.ResourceId.Should().Be(seed.ResourceId.Value);
            detail.Status.Should().Be(MatchStatus.Scheduled);
            detail.Result.Should().BeNull();
        }

        using (var start = await client.PostAsync(StartUri(seed.MatchId), content: null))
        {
            start.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        using (var detailLive = await client.GetAsync($"/matches/{seed.MatchId.Value}"))
        {
            var detail = await detailLive.Content.ReadFromJsonAsync<MatchDetailDto>(HostJson.Options);
            detail!.Status.Should().Be(MatchStatus.Live);
            detail.ScheduledAt.Should().Be(seed.Kickoff);
        }

        using (var finish = await client.PostAsJsonAsync(
            FinishUri(seed.MatchId),
            new FinishMatchRequest(ResultType.Played, 2, 0)))
        {
            finish.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        using (var detailFinished = await client.GetAsync($"/matches/{seed.MatchId.Value}"))
        {
            var detail = await detailFinished.Content.ReadFromJsonAsync<MatchDetailDto>(HostJson.Options);
            detail!.Status.Should().Be(MatchStatus.Finished);
            detail.Result.Should().NotBeNull();
            detail.Result!.HomeGoals.Should().Be(2);
            detail.Result.AwayGoals.Should().Be(0);
            detail.ScheduledAt.Should().Be(seed.Kickoff);
        }
    }

    private static Uri StartUri(MatchId matchId) =>
        new($"/matches/{matchId.Value}/start", UriKind.Relative);

    private static Uri FinishUri(MatchId matchId) =>
        new($"/matches/{matchId.Value}/finish", UriKind.Relative);

    private static string? GetCode(ProblemDetails problem) =>
        !problem.Extensions.TryGetValue("code", out var raw) || raw is null
            ? null
            : raw switch
            {
                string text => text,
                JsonElement { ValueKind: JsonValueKind.String } element => element.GetString(),
                _ => raw.ToString()
            };

    private Task<MatchId> SeedScheduledMatchAsync(PlayUpWebApplicationFactory factory) =>
        SeedMatchAsync(factory, start: false);

    private Task<MatchId> SeedLiveMatchAsync(PlayUpWebApplicationFactory factory) =>
        SeedMatchAsync(factory, start: true);

    private Task<MatchId> SeedCancelledMatchAsync(PlayUpWebApplicationFactory factory) =>
        SeedMatchAsync(factory, cancel: true);

    private async Task<MatchId> SeedMatchAsync(PlayUpWebApplicationFactory factory, bool start = false, bool cancel = false)
    {
        using var scope = factory.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var matches = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var competition = Competition.Create(new CompetitionName("Host Match League"), SampleRegulations.Standard(), _clock);
        competitions.Add(competition);

        var stage = Stage.Create(competition.Id, new StageName("Matchday 1"), SampleRegulations.Standard(), _clock);
        stage.AddMatchday(1, _clock);
        competition.AddStage(stage.Id, _clock);
        stages.Add(stage);

        var match = Match.Create(competition.Id, stage.Id, EntryId.New(), EntryId.New(), _clock);
        if (start)
        {
            match.Start(_clock);
        }

        if (cancel)
        {
            match.Cancel(_clock);
        }

        matches.Add(match);
        await unitOfWork.SaveChangesAsync();
        return match.Id;
    }

    private async Task<MatchId> SeedMatchOnClosedCompetitionAsync(
        PlayUpWebApplicationFactory factory,
        bool archive,
        bool startMatch = false)
    {
        using var scope = factory.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var matches = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var competition = Competition.Create(new CompetitionName("Closed Match League"), SampleRegulations.Standard(), _clock);
        var home = competition.AddEntry(TeamId.New(), "Home", _clock);
        var away = competition.AddEntry(TeamId.New(), "Away", _clock);
        competitions.Add(competition);

        var stage = Stage.Create(competition.Id, new StageName("Matchday 1"), SampleRegulations.Standard(), _clock);
        stage.AddMatchday(1, _clock);
        competition.AddStage(stage.Id, _clock);
        stages.Add(stage);

        var match = Match.Create(competition.Id, stage.Id, home.Id, away.Id, _clock);
        if (startMatch)
        {
            match.Start(_clock);
        }

        matches.Add(match);

        competition.Prepare(_clock);
        competition.Start(_clock);
        competition.Complete(CompletionMode.Administrative, _clock);
        if (archive)
        {
            competition.Archive(_clock);
        }

        await unitOfWork.SaveChangesAsync();
        return match.Id;
    }

    private async Task<(StageId StageId, MatchId MatchId, DateTimeOffset Kickoff, ResourceId ResourceId)>
        SeedAttachedMatchWithPlacementAsync(PlayUpWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var matches = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var competition = Competition.Create(new CompetitionName("Placed Match League"), SampleRegulations.Standard(), _clock);
        var home = competition.AddEntry(TeamId.New(), "Home", _clock);
        var away = competition.AddEntry(TeamId.New(), "Away", _clock);
        competitions.Add(competition);

        var stage = Stage.Create(competition.Id, new StageName("Matchday 1"), SampleRegulations.Standard(), _clock);
        var matchday = stage.AddMatchday(1, _clock);
        var addFixture = stage.AddFixture(matchday.Id, _clock);
        competition.AddStage(stage.Id, _clock);

        var match = Match.Create(competition.Id, stage.Id, home.Id, away.Id, _clock);
        stage.AttachMatch(addFixture.Id, match.Id, legIndex: 1, _clock);

        var kickoff = new DateTimeOffset(2026, 9, 12, 18, 30, 0, TimeSpan.Zero);
        var resourceId = ResourceId.New();
        stage.ApplyMatchPlacements([new MatchPlacement(match.Id, kickoff, resourceId)], [match.Id]);

        stages.Add(stage);
        matches.Add(match);
        await unitOfWork.SaveChangesAsync();
        return (stage.Id, match.Id, kickoff, resourceId);
    }
}
