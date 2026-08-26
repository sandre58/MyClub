// -----------------------------------------------------------------------
// <copyright file="ScenarioAndTemplateIntegrationTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Pipeline;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Development.Runtime;
using MyClub.PlayUp.Development.Templates;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Development.Tests;

[Collection("DevelopmentPostgres")]
[Trait("Category", "Integration")]
public sealed class ScenarioAndTemplateIntegrationTests(DevelopmentPostgresFixture fixture)
{
    [Fact]
    public async Task Empty_workspace_has_no_competitionsAsync()
    {
        var runner = fixture.Services.GetRequiredService<ScenarioRunner>();
        await runner.ResetAndRunAsync([SeedSpec.Parse("empty-workspace")]);

        using var scope = fixture.Services.CreateScope();
        var list = await scope.ServiceProvider.GetRequiredService<ICompetitionRepository>().ListAsync();
        list.Should().BeEmpty();
    }

    [Fact]
    public async Task Draft_and_registration_fixed_scenariosAsync()
    {
        var runner = fixture.Services.GetRequiredService<ScenarioRunner>();
        await runner.ResetAndRunAsync(
        [
            SeedSpec.Parse("draft-empty"),
            SeedSpec.Parse("registration-open")
        ]);

        using var scope = fixture.Services.CreateScope();
        var list = await scope.ServiceProvider.GetRequiredService<ICompetitionRepository>().ListAsync();
        list.Should().HaveCount(2);
        list.Should().Contain(c => c.Entries.Count == 0 && c.Status == CompetitionStatus.Draft);
        list.Should().Contain(c => c.Entries.Count == 3 && c.Status == CompetitionStatus.Draft);
    }

    [Fact]
    public async Task Groups_prepared_running_finished_progressAsync()
    {
        var runner = fixture.Services.GetRequiredService<ScenarioRunner>();

        await runner.ResetAndRunAsync([SeedSpec.Parse("groups:prepared")]);
        await AssertMatchSplitAsync(finished: 0, requireScheduled: true);

        await runner.ResetAndRunAsync([SeedSpec.Parse("groups:running")]);
        await AssertPartialAsync();

        await runner.ResetAndRunAsync([SeedSpec.Parse("groups:finished")]);
        await AssertAllFinishedAsync();
    }

    [Fact]
    public async Task Cup_and_championship_accept_progressAsync()
    {
        var runner = fixture.Services.GetRequiredService<ScenarioRunner>();
        await runner.ResetAndRunAsync(
        [
            SeedSpec.Parse("cup:prepared"),
            SeedSpec.Parse("championship:running")
        ]);

        using var scope = fixture.Services.CreateScope();
        var list = await scope.ServiceProvider.GetRequiredService<ICompetitionRepository>().ListAsync();
        list.Should().HaveCount(2);
    }

    [Fact]
    public async Task Alias_group_stage_mid_maps_to_groups_runningAsync()
    {
        var runner = fixture.Services.GetRequiredService<ScenarioRunner>();
        await runner.ResetAndRunAsync([SeedSpec.Parse("group-stage-mid")]);
        await AssertPartialAsync();
    }

    [Fact]
    public async Task Ligue1_prepared_template_and_use_case_readAsync()
    {
        var templates = fixture.Services.GetRequiredService<TemplateRunner>();
        await templates.ResetAndRunAsync([SeedSpec.Parse("ligue-1:prepared")]);

        using var scope = fixture.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var summary = (await competitions.ListAsync()).Should().ContainSingle().Subject;
        var competition = await competitions.GetByIdAsync(summary.Id);
        competition.Should().NotBeNull();
        competition.Entries.Should().HaveCount(18);
        competition.Status.Should().Be(CompetitionStatus.Running);

        var executor = scope.ServiceProvider.GetRequiredService<UseCaseExecutor>();
        var overview = await executor.GetCompetitionOverviewAsync(competition.Id);
        overview.Id.Should().Be(competition.Id.Value);

        var stage = await scope.ServiceProvider.GetRequiredService<IStageRepository>()
            .GetByIdAsync(competition.StageIds[0]);
        var matches = await scope.ServiceProvider.GetRequiredService<IMatchRepository>()
            .ListByStageAsync(stage!.Id);
        matches.Should().OnlyContain(m => m.Status == MatchStatus.Scheduled);
        stage.MatchGenerationFormat.Should().Be(MatchGenerationFormat.DoubleRoundRobin);
        stage.Matchdays.Should().HaveCount(34);
        matches.Should().HaveCount(18 * 17); // N×(N−1) directed fixtures for Double RR
    }

    [Fact]
    public async Task Cup_qf_sf_fills_semi_slots_and_projects_from_slots_cockpit_actionAsync()
    {
        var runner = fixture.Services.GetRequiredService<ScenarioRunner>();
        await runner.ResetAndRunAsync([SeedSpec.Parse("cup-qf-sf")]);

        using var scope = fixture.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var summary = (await competitions.ListAsync()).Should().ContainSingle().Subject;
        var competition = await competitions.GetByIdAsync(summary.Id);
        competition.Should().NotBeNull();
        competition.Status.Should().Be(CompetitionStatus.Running);
        competition.StageIds.Should().HaveCount(2);

        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var quarter = await stages.GetByIdAsync(competition.StageIds[0]);
        var semi = await stages.GetByIdAsync(competition.StageIds[1]);
        quarter.Should().NotBeNull();
        semi.Should().NotBeNull();
        semi.Status.Should().Be(StageStatus.Draft);
        semi.Slots.Count(slot => slot.EntryId is not null).Should().Be(4);
        semi.Rounds[0].Fixtures.Should().BeEmpty();

        var matchesByStage = new Dictionary<StageId, IReadOnlyList<Match>>();
        foreach (var stageId in competition.StageIds)
        {
            var list = await scope.ServiceProvider.GetRequiredService<IMatchRepository>()
                .ListByStageAsync(stageId);
            matchesByStage[stageId] = list;
        }

        var cockpit = CockpitAssembler.Assemble(
            competition,
            [quarter!, semi!],
            matchesByStage);
        cockpit.AvailableActions.Should().Contain(action =>
            action.Code == CockpitAssembler.ActionMaterializeFromOccupiedSlots
            && action.StageId == semi!.Id.Value);
    }

    [Fact]
    public async Task Double_run_groups_running_is_deterministicAsync()
    {
        var runner = fixture.Services.GetRequiredService<ScenarioRunner>();
        await runner.ResetAndRunAsync([SeedSpec.Parse("groups:running")]);
        var first = await CaptureAsync();
        await runner.ResetAndRunAsync([SeedSpec.Parse("groups:running")]);
        var second = await CaptureAsync();
        second.Should().BeEquivalentTo(first);
    }

    private async Task AssertPartialAsync()
    {
        using var scope = fixture.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var summary = (await competitions.ListAsync()).Single();
        var competition = await competitions.GetByIdAsync(summary.Id);
        competition.Should().NotBeNull();
        var stage = await scope.ServiceProvider.GetRequiredService<IStageRepository>()
            .GetByIdAsync(competition.StageIds[0]);
        var matches = await scope.ServiceProvider.GetRequiredService<IMatchRepository>()
            .ListByStageAsync(stage!.Id);
        matches.Count(m => m.Status == MatchStatus.Finished).Should().Be(matches.Count / 2);
    }

    private async Task AssertAllFinishedAsync()
    {
        using var scope = fixture.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var summary = (await competitions.ListAsync()).Single();
        var competition = await competitions.GetByIdAsync(summary.Id);
        competition.Should().NotBeNull();
        competition.Status.Should().Be(CompetitionStatus.Completed);
        var stage = await scope.ServiceProvider.GetRequiredService<IStageRepository>()
            .GetByIdAsync(competition.StageIds[0]);
        var matches = await scope.ServiceProvider.GetRequiredService<IMatchRepository>()
            .ListByStageAsync(stage!.Id);
        matches.Should().OnlyContain(m => m.Status == MatchStatus.Finished);
    }

    private async Task AssertMatchSplitAsync(int finished, bool requireScheduled)
    {
        using var scope = fixture.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var summary = (await competitions.ListAsync()).Single();
        var competition = await competitions.GetByIdAsync(summary.Id);
        competition.Should().NotBeNull();
        var stage = await scope.ServiceProvider.GetRequiredService<IStageRepository>()
            .GetByIdAsync(competition.StageIds[0]);
        var matches = await scope.ServiceProvider.GetRequiredService<IMatchRepository>()
            .ListByStageAsync(stage!.Id);
        matches.Count(m => m.Status == MatchStatus.Finished).Should().Be(finished);
        if (requireScheduled)
        {
            matches.Should().OnlyContain(m => m.Status == MatchStatus.Scheduled);
        }
    }

    private async Task<object> CaptureAsync()
    {
        using var scope = fixture.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var summary = (await competitions.ListAsync()).Single();
        var competition = await competitions.GetByIdAsync(summary.Id);
        competition.Should().NotBeNull();
        var stage = await scope.ServiceProvider.GetRequiredService<IStageRepository>()
            .GetByIdAsync(competition.StageIds[0]);
        var matches = await scope.ServiceProvider.GetRequiredService<IMatchRepository>()
            .ListByStageAsync(stage!.Id);
        return new
        {
            CompetitionId = competition.Id.Value,
            StageId = stage.Id.Value,
            EntryIds = competition.Entries.Select(e => e.Id.Value).Order().ToArray(),
            Names = competition.Entries.Select(e => e.DisplayName).Order().ToArray(),
            MatchIds = matches.Select(m => m.Id.Value).Order().ToArray(),
            Results = matches
                .Where(m => m.Result is not null)
                .OrderBy(m => m.Id.Value)
                .Select(m => (m.Result!.Score.HomeGoals, m.Result.Score.AwayGoals))
                .ToArray()
        };
    }
}
