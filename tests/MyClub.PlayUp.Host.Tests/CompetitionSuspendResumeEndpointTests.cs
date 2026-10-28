// -----------------------------------------------------------------------
// <copyright file="CompetitionSuspendResumeEndpointTests.cs" company="Stéphane ANDRE">
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
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.TestKit;
using Xunit;

namespace MyClub.PlayUp.Host.Tests;

[Collection("host-postgres")]
[Trait("Category", "Integration")]
public sealed class CompetitionSuspendResumeEndpointTests(HostPostgresFixture fixture)
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 21, 12, 0, 0, TimeSpan.Zero));

    [IntegrationFact]
    public async Task Suspend_returns_204_and_overview_projects_ResumeAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var competitionId = await SeedRunningAsync(factory);
        using var client = factory.CreateClient();

        using var response = await client.PostAsync(SuspendUri(competitionId), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var overview = await client.GetAsync($"/competitions/{competitionId.Value}/overview");
        overview.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await overview.Content.ReadFromJsonAsync<OverviewViewDto>(HostJson.Options);
        body.Should().NotBeNull();
        body.Status.Should().Be(CompetitionStatus.Suspended);
        body.AvailableActions.Should().Contain(action =>
            action.Code == OverviewAssembler.ActionResumeCompetition);
        body.AvailableActions.Should().NotContain(action =>
            action.Code == OverviewAssembler.ActionSuspendCompetition);
        body.Situations.Should().Contain(item =>
            item.Source == OverviewAssembler.SourceCompetitionSuspended
            && item.Nature == OverviewAssembler.NatureInformational
            && item.Actionable
            && item.ActionCode == OverviewAssembler.ActionResumeCompetition);
        body.AttentionSummary.Items.Should().NotContain(item =>
            item.Source == OverviewAssembler.SourceCompetitionSuspended);
    }

    [IntegrationFact]
    public async Task Resume_returns_204_and_leaves_suspended_stage_suspendedAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var (competitionId, stageId) = await SeedCompetitionAndStageSuspendedAsync(factory);
        using var client = factory.CreateClient();

        using var before = await client.GetAsync($"/competitions/{competitionId.Value}/overview");
        before.StatusCode.Should().Be(HttpStatusCode.OK);
        var beforeBody = await before.Content.ReadFromJsonAsync<OverviewViewDto>(HostJson.Options);
        beforeBody!.AvailableActions.Should().Contain(action =>
            action.Code == OverviewAssembler.ActionResumeCompetition);
        beforeBody.OperationalFocus.Stages.Should().Contain(stage =>
            stage.StageId == stageId.Value && stage.Status == StageStatus.Suspended);

        using var response = await client.PostAsync(ResumeUri(competitionId), content: null);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = factory.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var competition = await competitions.GetByIdForUpdateAsync(competitionId);
        var stage = await stages.GetByIdForUpdateAsync(stageId);
        competition!.Status.Should().Be(CompetitionStatus.Running);
        stage!.Status.Should().Be(StageStatus.Suspended);

        using var after = await client.GetAsync($"/competitions/{competitionId.Value}/overview");
        var afterBody = await after.Content.ReadFromJsonAsync<OverviewViewDto>(HostJson.Options);
        afterBody!.Status.Should().Be(CompetitionStatus.Running);
        afterBody.AvailableActions.Should().Contain(action =>
            action.Code == OverviewAssembler.ActionSuspendCompetition);
        afterBody.AvailableActions.Should().NotContain(action =>
            action.Code == OverviewAssembler.ActionResumeCompetition);
        afterBody.Situations.Should().NotContain(item =>
            item.Source == OverviewAssembler.SourceCompetitionSuspended);
        afterBody.OperationalFocus.Stages.Should().Contain(row =>
            row.StageId == stageId.Value && row.Status == StageStatus.Suspended);
    }

    [IntegrationFact]
    public async Task Suspend_from_ready_returns_409Async()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var competitionId = await SeedReadyAsync(factory);
        using var client = factory.CreateClient();

        using var response = await client.PostAsync(SuspendUri(competitionId), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options);
        GetCode(problem!).Should().Be(CompetitionErrorCodes.InvalidTransition);
    }

    [IntegrationFact]
    public async Task Resume_from_running_returns_409Async()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var competitionId = await SeedRunningAsync(factory);
        using var client = factory.CreateClient();

        using var response = await client.PostAsync(ResumeUri(competitionId), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options);
        GetCode(problem!).Should().Be(CompetitionErrorCodes.InvalidTransition);
    }

    [IntegrationFact]
    public async Task Suspend_when_missing_returns_404Async()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();

        using var response = await client.PostAsync(SuspendUri(CompetitionId.New()), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options);
        GetCode(problem!).Should().Be(ApplicationErrorCodes.CompetitionNotFound);
    }

    private static Uri SuspendUri(CompetitionId competitionId) =>
        new($"/competitions/{competitionId.Value}/suspend", UriKind.Relative);

    private static Uri ResumeUri(CompetitionId competitionId) =>
        new($"/competitions/{competitionId.Value}/resume", UriKind.Relative);

    private static string? GetCode(ProblemDetails problem) =>
        !problem.Extensions.TryGetValue("code", out var raw) || raw is null
            ? null
            : raw switch
            {
                string text => text,
                JsonElement { ValueKind: JsonValueKind.String } element => element.GetString(),
                _ => raw.ToString()
            };

    private async Task<CompetitionId> SeedReadyAsync(PlayUpWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var situation = TestCompetition.Create("Ready Cup", _clock)
            .WithTeams("Alpha")
            .WithStructure(StructureIntent.Championship(stageName: "League"))
            .PrepareCompetition();
        await HostTestPersist.PersistAsync(scope.ServiceProvider, situation);
        return situation.Competition.Id;
    }

    private async Task<CompetitionId> SeedRunningAsync(PlayUpWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var situation = TestCompetition.Create("Running Cup", _clock)
            .WithTeams("Alpha")
            .WithStructure(StructureIntent.Championship(stageName: "League"))
            .PrepareCompetition()
            .StartCompetition();
        await HostTestPersist.PersistAsync(scope.ServiceProvider, situation);
        return situation.Competition.Id;
    }

    private async Task<(CompetitionId CompetitionId, StageId StageId)> SeedCompetitionAndStageSuspendedAsync(
        PlayUpWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var situation = TestCompetition.Create("Groups Suspended", _clock)
            .WithTeams("A", "B", "C", "D")
            .WithStructure(StructureIntent.Championship(stageName: "League"))
            .PrepareCompetition()
            .PreparePrimaryStage()
            .StartCompetition()
            .StartPrimaryStage();
        var stage = situation.RequirePrimaryStage();
        stage.Suspend(_clock);
        situation.Competition.Suspend(_clock);
        await HostTestPersist.PersistAsync(scope.ServiceProvider, situation);
        return (situation.Competition.Id, stage.Id);
    }
}
