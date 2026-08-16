// -----------------------------------------------------------------------
// <copyright file="CompetitionSlice6EndpointTests.cs" company="Stéphane ANDRE">
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
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Host.Tests;

[Collection("host-postgres")]
[Trait("Category", "Integration")]
public sealed class CompetitionSlice6EndpointTests(HostPostgresFixture fixture)
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 16, 15, 0, 0, TimeSpan.Zero));

    [IntegrationFact]
    public async Task Complete_normal_ok_then_archive_persists_statusAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedRunningFinishedAsync(factory);
        using var client = factory.CreateClient();

        using var completeResponse = await client.PostAsJsonAsync(
            $"/competitions/{seed.CompetitionId.Value}/complete",
            new { mode = "Normal" });
        completeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var workspaceCompleted = await client.GetAsync($"/competitions/{seed.CompetitionId.Value}/workspace");
        var completedSummary = await workspaceCompleted.Content.ReadFromJsonAsync<WorkspaceSummaryDto>();
        completedSummary!.Status.Should().Be(CompetitionStatus.Completed);
        completedSummary.CompletionMode.Should().Be(CompletionMode.Normal);
        completedSummary.NextActionCode.Should().Be("ArchiveCompetition");

        using var overviewCompleted = await client.GetAsync($"/competitions/{seed.CompetitionId.Value}");
        var overview = await overviewCompleted.Content.ReadFromJsonAsync<CompetitionOverviewDto>();
        overview!.CompletionMode.Should().Be(CompletionMode.Normal);

        using var archiveResponse = await client.PostAsync(
            $"/competitions/{seed.CompetitionId.Value}/archive",
            content: null);
        archiveResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var workspaceArchived = await client.GetAsync($"/competitions/{seed.CompetitionId.Value}/workspace");
        var archivedSummary = await workspaceArchived.Content.ReadFromJsonAsync<WorkspaceSummaryDto>();
        archivedSummary!.Status.Should().Be(CompetitionStatus.Archived);
        archivedSummary.CompletionMode.Should().Be(CompletionMode.Normal);

        using var scope = factory.Services.CreateScope();
        var loaded = await scope.ServiceProvider
            .GetRequiredService<ICompetitionRepository>()
            .GetByIdAsync(seed.CompetitionId);
        loaded!.Status.Should().Be(CompetitionStatus.Archived);
        loaded.CompletionMode.Should().Be(CompletionMode.Normal);
    }

    [IntegrationFact]
    public async Task Complete_normal_when_incomplete_returns_409_with_reasonsAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedRunningScheduledAsync(factory);
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            $"/competitions/{seed.CompetitionId.Value}/complete",
            new { mode = "Normal" });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        GetCode(problem!).Should().Be(ApplicationErrorCodes.CompletionNotAllowed);
        GetReasons(problem!).Should().Contain(CompletionAnalyzer.ReasonScheduledMatches);

        using var scope = factory.Services.CreateScope();
        var loaded = await scope.ServiceProvider
            .GetRequiredService<ICompetitionRepository>()
            .GetByIdAsync(seed.CompetitionId);
        loaded!.Status.Should().Be(CompetitionStatus.Running);
    }

    [IntegrationFact]
    public async Task Complete_administrative_allows_incompleteAsync()
    {
        await Complete_exceptional_mode_allows_incompleteAsync("Administrative");
    }

    [IntegrationFact]
    public async Task Complete_abandoned_allows_incompleteAsync()
    {
        await Complete_exceptional_mode_allows_incompleteAsync("Abandoned");
    }

    private async Task Complete_exceptional_mode_allows_incompleteAsync(string mode)
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedRunningScheduledAsync(factory);
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            $"/competitions/{seed.CompetitionId.Value}/complete",
            new { mode });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = factory.Services.CreateScope();
        var loaded = await scope.ServiceProvider
            .GetRequiredService<ICompetitionRepository>()
            .GetByIdAsync(seed.CompetitionId);
        loaded!.Status.Should().Be(CompetitionStatus.Completed);
        loaded.CompletionMode.Should().Be(Enum.Parse<CompletionMode>(mode));
    }

    [IntegrationFact]
    public async Task Archive_from_running_returns_409Async()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedRunningScheduledAsync(factory);
        using var client = factory.CreateClient();

        using var response = await client.PostAsync(
            $"/competitions/{seed.CompetitionId.Value}/archive",
            content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        GetCode(problem!).Should().Be(CompetitionErrorCodes.InvalidTransition);
    }

    [IntegrationFact]
    public async Task Schedule_apply_when_completed_returns_409Async()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedRunningFinishedAsync(factory, complete: true);
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            $"/stages/{seed.StageId.Value}/schedule/apply",
            new
            {
                assignments = new[]
                {
                    new
                    {
                        matchId = seed.MatchId.Value,
                        resourceId = Guid.NewGuid(),
                        start = _clock.UtcNow
                    }
                },
                targetMatchIds = new[] { seed.MatchId.Value }
            });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        GetCode(problem!).Should().Be(ApplicationErrorCodes.CompetitionClosed);
    }

    [IntegrationFact]
    public async Task Complete_missing_competition_returns_404Async()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            $"/competitions/{Guid.NewGuid()}/complete",
            new { mode = "Normal" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
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

    private static IReadOnlyList<string> GetReasons(ProblemDetails problem)
    {
        if (!problem.Extensions.TryGetValue("reasons", out var raw) || raw is null)
        {
            return [];
        }

        if (raw is JsonElement { ValueKind: JsonValueKind.Array } element)
        {
            return element.EnumerateArray()
                .Select(item => item.GetString()!)
                .Where(text => text is not null)
                .ToArray()!;
        }

        if (raw is IEnumerable<string> texts)
        {
            return texts.ToArray();
        }

        return [];
    }

    private async Task<(CompetitionId CompetitionId, StageId StageId, MatchId MatchId)> SeedRunningFinishedAsync(
        PlayUpWebApplicationFactory factory,
        bool complete = false)
    {
        using var scope = factory.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var matches = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var competition = Competition.Create(new CompetitionName("Slice6 Done"), SampleRegulations.Standard(), _clock);
        var home = competition.AddEntry(TeamId.New(), "A", _clock);
        var away = competition.AddEntry(TeamId.New(), "B", _clock);
        competitions.Add(competition);

        var stage = Stage.Create(competition.Id, new StageName("MD"), SampleRegulations.Standard(), _clock);
        stage.AddMatchday(1, _clock);
        competition.AddStage(stage.Id, _clock);
        competition.Prepare(_clock);
        competition.Start(_clock);
        var match = Match.Create(competition.Id, stage.Id, home.Id, away.Id, _clock);
        match.Start(_clock);
        match.Finish(new MatchResult(ResultType.Played, new Score(1, 0)), _clock);
        stages.Add(stage);
        matches.Add(match);

        if (complete)
        {
            competition.Complete(CompletionMode.Administrative, _clock);
        }

        await unitOfWork.SaveChangesAsync();
        return (competition.Id, stage.Id, match.Id);
    }

    private async Task<(CompetitionId CompetitionId, StageId StageId, MatchId MatchId)> SeedRunningScheduledAsync(
        PlayUpWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var matches = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var competition = Competition.Create(new CompetitionName("Slice6 Open"), SampleRegulations.Standard(), _clock);
        var home = competition.AddEntry(TeamId.New(), "A", _clock);
        var away = competition.AddEntry(TeamId.New(), "B", _clock);
        competitions.Add(competition);

        var stage = Stage.Create(competition.Id, new StageName("MD"), SampleRegulations.Standard(), _clock);
        competition.AddStage(stage.Id, _clock);
        competition.Prepare(_clock);
        competition.Start(_clock);
        var match = Match.Create(competition.Id, stage.Id, home.Id, away.Id, _clock);
        stages.Add(stage);
        matches.Add(match);
        await unitOfWork.SaveChangesAsync();
        return (competition.Id, stage.Id, match.Id);
    }
}
