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
using MyClub.PlayUp.Domain.Competitions;
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
        competition.LogoMediaId.Should().NotBeNull();
        competition.Entries.Select(e => e.LogoMediaId).Should().OnlyContain(id => id.HasValue);

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
            [quarter, semi],
            matchesByStage);
        cockpit.AvailableActions.Should().Contain(action =>
            action.Code == CockpitAssembler.ActionMaterializeFromOccupiedSlots
            && action.StageId == semi.Id.Value);
    }

    [Fact]
    public async Task Coupe_de_france_completes_with_final_placement_outcomeAsync()
    {
        var runner = fixture.Services.GetRequiredService<TemplateRunner>();
        await runner.ResetAndRunAsync([SeedSpec.Parse("coupe-de-france")]);

        using var scope = fixture.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var summary = (await competitions.ListAsync()).Should().ContainSingle().Subject;
        var competition = await competitions.GetByIdAsync(summary.Id);
        competition.Should().NotBeNull();
        competition.Status.Should().Be(CompetitionStatus.Completed);
        competition.StageIds.Should().HaveCount(5);

        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var loaded = new List<Stage>(5);
        foreach (var stageId in competition.StageIds)
        {
            var stage = await stages.GetByIdAsync(stageId);
            stage.Should().NotBeNull();
            loaded.Add(stage);
        }

        var final = loaded[^1];
        final.Status.Should().Be(StageStatus.Completed);
        final.Regulation.PlacementAwardRules.Should().NotBeNull();
        final.Regulation.PlacementAwardRules!.Paths.Should().HaveCount(2);

        var matchesByStage = new Dictionary<StageId, IReadOnlyList<Match>>();
        foreach (var stage in loaded)
        {
            matchesByStage[stage.Id] = await scope.ServiceProvider
                .GetRequiredService<IMatchRepository>()
                .ListByStageAsync(stage.Id);
        }

        matchesByStage[loaded[0].Id].Should().HaveCount(16);
        matchesByStage[loaded[1].Id].Should().HaveCount(8);
        matchesByStage[loaded[2].Id].Should().HaveCount(4);
        matchesByStage[loaded[3].Id].Should().HaveCount(2);
        matchesByStage[final.Id].Should().HaveCount(1);
        matchesByStage[final.Id].Should().OnlyContain(m => m.Status == MatchStatus.Finished);

        var cockpit = CockpitAssembler.Assemble(competition, loaded, matchesByStage);
        cockpit.CompetitionOutcome.Should().NotBeNull();
        cockpit.CompetitionOutcome!.Presentation.Should().Be(CockpitAssembler.OutcomePresentationWinner);
        cockpit.CompetitionOutcome.Places.Should().HaveCount(2);
        cockpit.CompetitionOutcome.Places.Select(p => p.Rank).Should().BeEquivalentTo([1, 2]);
    }

    [Fact]
    public async Task World_cup_completes_with_final_and_bronze_placement_outcomeAsync()
    {
        var runner = fixture.Services.GetRequiredService<TemplateRunner>();
        await runner.ResetAndRunAsync([SeedSpec.Parse("world-cup")]);

        using var scope = fixture.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var summary = (await competitions.ListAsync()).Should().ContainSingle().Subject;
        var competition = await competitions.GetByIdAsync(summary.Id);
        competition.Should().NotBeNull();
        competition.Status.Should().Be(CompetitionStatus.Completed);
        competition.StageIds.Should().HaveCount(6);

        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var loaded = new List<Stage>(6);
        foreach (var stageId in competition.StageIds)
        {
            var stage = await stages.GetByIdAsync(stageId);
            stage.Should().NotBeNull();
            loaded.Add(stage);
        }

        var groups = loaded[0];
        var roundOf16 = loaded[1];
        var quarter = loaded[2];
        var semi = loaded[3];
        var final = loaded[4];
        var bronze = loaded[5];

        groups.Groups.Should().HaveCount(8);
        loaded.Should().OnlyContain(stage => stage.Status == StageStatus.Completed);
        final.Regulation.PlacementAwardRules.Should().NotBeNull();
        bronze.Regulation.PlacementAwardRules.Should().NotBeNull();

        var matchesByStage = new Dictionary<StageId, IReadOnlyList<Match>>();
        foreach (var stage in loaded)
        {
            matchesByStage[stage.Id] = await scope.ServiceProvider
                .GetRequiredService<IMatchRepository>()
                .ListByStageAsync(stage.Id);
        }

        matchesByStage[roundOf16.Id].Should().HaveCount(8);
        matchesByStage[quarter.Id].Should().HaveCount(4);
        matchesByStage[semi.Id].Should().HaveCount(2);
        matchesByStage[final.Id].Should().HaveCount(1);
        matchesByStage[bronze.Id].Should().HaveCount(1);
        matchesByStage[final.Id].Should().OnlyContain(m => m.Status == MatchStatus.Finished);
        matchesByStage[bronze.Id].Should().OnlyContain(m => m.Status == MatchStatus.Finished);

        var cockpit = CockpitAssembler.Assemble(competition, loaded, matchesByStage);
        cockpit.CompetitionOutcome.Should().NotBeNull();
        cockpit.CompetitionOutcome!.Presentation.Should().Be(CockpitAssembler.OutcomePresentationPodium);
        cockpit.CompetitionOutcome.Places.Should().HaveCount(4);
        cockpit.CompetitionOutcome.Places.Select(p => p.Rank).Should().BeEquivalentTo([1, 2, 3, 4]);
        cockpit.CompetitionOutcome.Places.Select(p => p.EntryId).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task Swiss_8x3_prepared_running_finished_progressAsync()
    {
        var runner = fixture.Services.GetRequiredService<ScenarioRunner>();

        await runner.ResetAndRunAsync([SeedSpec.Parse("swiss-8x3:prepared")]);
        using (var scope = fixture.Services.CreateScope())
        {
            var (competition, stage, matches) = await LoadPrimaryAsync(scope);
            competition.Status.Should().Be(CompetitionStatus.Running);
            stage.IsSwiss.Should().BeTrue();
            stage.SwissSettings!.RoundCount.Should().Be(3);
            stage.Matchdays.Should().BeEmpty();
            matches.Should().BeEmpty();

            var cockpit = CockpitAssembler.Assemble(
                competition,
                [stage],
                new Dictionary<StageId, IReadOnlyList<Match>> { [stage.Id] = matches });
            cockpit.AvailableActions.Should().Contain(action =>
                action.Code == CockpitAssembler.ActionGenerateNextRound);
            cockpit.NaturalProgression!.Code.Should().Be(CockpitAssembler.ActionGenerateNextRound);
        }

        await runner.ResetAndRunAsync([SeedSpec.Parse("swiss-8x3:running")]);
        using (var scope = fixture.Services.CreateScope())
        {
            var (competition, stage, matches) = await LoadPrimaryAsync(scope);
            competition.Status.Should().Be(CompetitionStatus.Running);
            stage.Matchdays.Should().HaveCount(2);
            matches.Should().HaveCount(8); // 4+4 for 8 teams
            matches.Count(match => match.Status == MatchStatus.Finished).Should().BeGreaterThan(0);
            matches.Count(match => match.Status == MatchStatus.Scheduled).Should().BeGreaterThan(0);
        }

        await runner.ResetAndRunAsync([SeedSpec.Parse("swiss-8x3:finished")]);
        using (var scope = fixture.Services.CreateScope())
        {
            var (competition, stage, matches) = await LoadPrimaryAsync(scope);
            competition.Status.Should().Be(CompetitionStatus.Completed);
            stage.Matchdays.Should().HaveCount(3);
            matches.Should().HaveCount(12);
            matches.Should().OnlyContain(match => match.Status == MatchStatus.Finished);
        }
    }

    private static async Task<(Competition Competition, Stage Stage, IReadOnlyList<Match> Matches)>
        LoadPrimaryAsync(IServiceScope scope)
    {
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var summary = (await competitions.ListAsync()).Should().ContainSingle().Subject;
        var competition = await competitions.GetByIdAsync(summary.Id);
        competition.Should().NotBeNull();
        var stage = await scope.ServiceProvider.GetRequiredService<IStageRepository>()
            .GetByIdAsync(competition.StageIds[0]);
        stage.Should().NotBeNull();
        var matches = await scope.ServiceProvider.GetRequiredService<IMatchRepository>()
            .ListByStageAsync(stage.Id);
        return (competition, stage, matches);
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
