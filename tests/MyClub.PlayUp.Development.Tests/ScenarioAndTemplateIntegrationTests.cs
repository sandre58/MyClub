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
using MyClub.PlayUp.Domain.Rules;
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
        var competition = await competitions.GetByIdForUpdateAsync(summary.Id);
        competition.Should().NotBeNull();
        competition.Entries.Should().HaveCount(18);
        competition.Status.Should().Be(CompetitionStatus.Running);
        competition.LogoMediaId.Should().NotBeNull();
        competition.Entries.Select(e => e.LogoMediaId).Should().OnlyContain(id => id.HasValue);

        var executor = scope.ServiceProvider.GetRequiredService<UseCaseExecutor>();
        var overview = await executor.GetCompetitionDetailAsync(competition.Id);
        overview.Id.Should().Be(competition.Id.Value);

        var stage = await scope.ServiceProvider.GetRequiredService<IStageRepository>()
            .GetByIdForUpdateAsync(competition.StageIds[0]);
        var matches = await scope.ServiceProvider.GetRequiredService<IMatchRepository>()
            .ListByStageForUpdateAsync(stage!.Id);
        matches.Should().OnlyContain(m => m.Status == MatchStatus.Scheduled);
        stage.MatchGenerationFormat.Should().Be(MatchGenerationFormat.DoubleRoundRobin);
        stage.Matchdays.Should().HaveCount(34);
        matches.Should().HaveCount(18 * 17); // N×(N−1) directed fixtures for Double RR
        competition.Entries.Should().OnlyContain(entry =>
            entry.DeclaredMembers.Count(member => member.Role == DeclaredMemberRole.Player) >= 11);
        competition.Regulation.DisciplinaryRules.AllowedTypes.Should().Contain(DisciplinaryType.Yellow);
        competition.Regulation.DisciplinaryRules.AllowedTypes.Should().Contain(DisciplinaryType.Red);
    }

    [Fact]
    public async Task ChampionsLeague_running_seeds_match_factsAsync()
    {
        var templates = fixture.Services.GetRequiredService<TemplateRunner>();
        await templates.ResetAndRunAsync([SeedSpec.Parse("champions-league:running")]);

        using var scope = fixture.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var summary = (await competitions.ListAsync()).Should().ContainSingle().Subject;
        var competition = await competitions.GetByIdForUpdateAsync(summary.Id);
        competition.Should().NotBeNull();
        var stage = await scope.ServiceProvider.GetRequiredService<IStageRepository>()
            .GetByIdForUpdateAsync(competition.StageIds[0]);
        var matches = await scope.ServiceProvider.GetRequiredService<IMatchRepository>()
            .ListByStageForUpdateAsync(stage!.Id);

        var finished = matches.Where(match => match.Status == MatchStatus.Finished).ToList();
        finished.Should().NotBeEmpty();
        finished.Should().OnlyContain(match => match.DeclaredParticipations.Count >= 22);
        foreach (var match in finished)
        {
            match.Result.Should().NotBeNull();
            match.RecordedGoals.Should().HaveCount(
                match.Result!.Score.HomeGoals + match.Result.Score.AwayGoals);
        }

        finished.SelectMany(match => match.RecordedDisciplinaryEvents).Should().NotBeEmpty();
        stage.MatchPlacements.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Structure_lifecycle_and_draw_pending_scenariosAsync()
    {
        var runner = fixture.Services.GetRequiredService<ScenarioRunner>();
        await runner.ResetAndRunAsync(
        [
            SeedSpec.Parse("championship-ready"),
            SeedSpec.Parse("groups-suspended"),
            SeedSpec.Parse("championship-archived"),
            SeedSpec.Parse("cup-draw-pending"),
            SeedSpec.Parse("groups-draw-pending"),
            SeedSpec.Parse("registration-withdrawn")
        ]);

        using var scope = fixture.Services.CreateScope();
        var list = await scope.ServiceProvider.GetRequiredService<ICompetitionRepository>().ListAsync();
        list.Should().HaveCount(6);
        list.Should().Contain(c => c.Status == CompetitionStatus.Ready);
        list.Should().Contain(c => c.Status == CompetitionStatus.Suspended);
        list.Should().Contain(c => c.Status == CompetitionStatus.Archived);
        list.Should().Contain(c =>
            c.Status == CompetitionStatus.Running
            && c.Entries.Count(e => e.Status == EntryStatus.Withdrawn) == 1);
    }

    [Fact]
    public async Task Groups_to_ko_mid_and_cup_sf_running_multi_phaseAsync()
    {
        var runner = fixture.Services.GetRequiredService<ScenarioRunner>();
        await runner.ResetAndRunAsync(
        [
            SeedSpec.Parse("groups-to-ko-mid"),
            SeedSpec.Parse("cup-sf-running")
        ]);

        using var scope = fixture.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var matches = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
        var list = await competitions.ListAsync();
        list.Should().HaveCount(2);

        var groupsToKoSummary = list.Single(c => c.Name.Value.Contains("Groupes → QF", StringComparison.Ordinal));
        var groupsToKo = await competitions.GetByIdForUpdateAsync(groupsToKoSummary.Id);
        groupsToKo.Should().NotBeNull();
        groupsToKo.Status.Should().Be(CompetitionStatus.Running);
        groupsToKo.StageIds.Should().HaveCount(2);
        var ko = await stages.GetByIdForUpdateAsync(groupsToKo.StageIds[1]);
        ko.Should().NotBeNull();
        ko.Status.Should().Be(StageStatus.Draft);
        ko.CompositionEntries.Should().HaveCount(4);
        ko.Slots.Count(slot => slot.EntryId is not null).Should().Be(4);
        ko.Draws.Should().Contain(draw =>
            draw.Kind == DrawResolutionKind.Slot && draw.Status == DrawStatus.Published);

        var cupSfSummary = list.Single(c => c.Name.Value.Contains("SF (running)", StringComparison.Ordinal));
        var cupSf = await competitions.GetByIdForUpdateAsync(cupSfSummary.Id);
        cupSf.Should().NotBeNull();
        cupSf.Status.Should().Be(CompetitionStatus.Running);
        var semi = await stages.GetByIdForUpdateAsync(cupSf.StageIds[1]);
        semi.Should().NotBeNull();
        semi.Status.Should().Be(StageStatus.Running);
        var sfMatches = await matches.ListByStageForUpdateAsync(semi.Id);
        sfMatches.Should().NotBeEmpty();
        sfMatches.Should().Contain(m => m.Status == MatchStatus.Finished);
        sfMatches.Should().Contain(m => m.Status == MatchStatus.Scheduled);
    }

    [Fact]
    public async Task Qual_auto_place_and_hybrid_draw_mid_scenariosAsync()
    {
        var runner = fixture.Services.GetRequiredService<ScenarioRunner>();
        await runner.ResetAndRunAsync(
        [
            SeedSpec.Parse("qual-auto-place-mid"),
            SeedSpec.Parse("qual-hybrid-auto-draw-mid"),
            SeedSpec.Parse("prog-auto-place-mid")
        ]);

        using var scope = fixture.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var list = await competitions.ListAsync();
        list.Should().HaveCount(3);

        var autoSummary = list.Single(c => c.Name.Value.Contains("Qual Auto Place", StringComparison.Ordinal));
        var autoComp = await competitions.GetByIdForUpdateAsync(autoSummary.Id);
        autoComp.Should().NotBeNull();
        var autoQf = await stages.GetByIdForUpdateAsync(autoComp.StageIds[1]);
        autoQf.Should().NotBeNull();
        autoQf.Status.Should().Be(StageStatus.Draft);
        autoQf.CompositionEntries.Should().HaveCount(4);
        autoQf.Slots.Count(slot => slot.EntryId is not null).Should().Be(4);
        autoQf.Draws.Should().BeEmpty();
        var autoOccupants = autoQf.Slots
            .SelectMany(slot => slot.EntryId is { } entryId ? new[] { entryId } : [])
            .ToArray();
        autoOccupants.Should().OnlyHaveUniqueItems();
        autoOccupants.Should().BeSubsetOf(autoQf.CompositionEntries.Select(entry => entry.EntryId));

        var hybridSummary = list.Single(c => c.Name.Value.Contains("hybride", StringComparison.Ordinal));
        var hybridComp = await competitions.GetByIdForUpdateAsync(hybridSummary.Id);
        hybridComp.Should().NotBeNull();
        var hybridQf = await stages.GetByIdForUpdateAsync(hybridComp.StageIds[1]);
        hybridQf.Should().NotBeNull();
        hybridQf.CompositionEntries.Should().HaveCount(4);
        hybridQf.Slots.Count(slot => slot.EntryId is not null).Should().Be(4);
        hybridQf.Draws.Should().ContainSingle(draw =>
            draw.Kind == DrawResolutionKind.Slot && draw.Status == DrawStatus.Published);
        hybridQf.Draws.Single().Inputs!.Entries.Should().HaveCount(2);
        hybridQf.Slots
            .SelectMany(slot => slot.EntryId is { } entryId ? new[] { entryId } : [])
            .Should().BeSubsetOf(hybridQf.CompositionEntries.Select(entry => entry.EntryId));

        var progSummary = list.Single(c => c.Name.Value.Contains("Prog Auto Place", StringComparison.Ordinal));
        var progComp = await competitions.GetByIdForUpdateAsync(progSummary.Id);
        progComp.Should().NotBeNull();
        var progSf = await stages.GetByIdForUpdateAsync(progComp.StageIds[1]);
        progSf.Should().NotBeNull();
        progSf.Status.Should().Be(StageStatus.Draft);
        progSf.CompositionEntries.Should().HaveCount(4);
        progSf.Slots.Count(slot => slot.EntryId is not null).Should().Be(4);
        progSf.Draws.Should().BeEmpty();
        progSf.Slots
            .SelectMany(slot => slot.EntryId is { } entryId ? new[] { entryId } : [])
            .Should().BeSubsetOf(progSf.CompositionEntries.Select(entry => entry.EntryId));
    }

    [Fact]
    public async Task Flux_qualif_draft_wires_auto_place_intentsAsync()
    {
        var runner = fixture.Services.GetRequiredService<ScenarioRunner>();
        await runner.ResetAndRunAsync([SeedSpec.Parse("flux-qualif-draft")]);

        using var scope = fixture.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var summary = (await competitions.ListAsync()).Should().ContainSingle().Subject;
        var competition = await competitions.GetByIdForUpdateAsync(summary.Id);
        competition.Should().NotBeNull();
        var groups = await stages.GetByIdForUpdateAsync(competition.StageIds[0]);
        var quarter = await stages.GetByIdForUpdateAsync(competition.StageIds[1]);
        groups.Should().NotBeNull();
        quarter.Should().NotBeNull();
        groups.Regulation.QualificationRules.Should().NotBeNull();
        groups.Regulation.QualificationRules!.Intents.Should().ContainSingle();
        groups.Regulation.QualificationRules.Intents.Should().OnlyContain(intent => !intent.TargetsPopulation);
        groups.Regulation.QualificationRules.Intents[0].DestinationSlotKeys.Should().HaveCount(4);
        groups.Regulation.QualificationRules.Paths.Should().OnlyContain(path => !path.Destination.TargetsPopulation);
        quarter.Slots.Should().HaveCount(4);
    }

    [Fact]
    public async Task Flux_qual_form_draft_wires_for_form_intentsAsync()
    {
        var runner = fixture.Services.GetRequiredService<ScenarioRunner>();
        await runner.ResetAndRunAsync([SeedSpec.Parse("flux-qual-form-draft")]);

        using var scope = fixture.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var summary = (await competitions.ListAsync()).Should().ContainSingle().Subject;
        var competition = await competitions.GetByIdForUpdateAsync(summary.Id);
        competition.Should().NotBeNull();
        competition.Entries.Should().HaveCount(6);
        competition.StageIds.Should().HaveCount(2);

        var groups = await stages.GetByIdForUpdateAsync(competition.StageIds[0]);
        var champ = await stages.GetByIdForUpdateAsync(competition.StageIds[1]);
        groups.Should().NotBeNull();
        champ.Should().NotBeNull();
        groups.Groups.Should().HaveCount(2);
        groups.Regulation.QualificationRules.Should().NotBeNull();
        groups.Regulation.QualificationRules!.Intents.Should().ContainSingle(intent => intent.TargetsForm);
        groups.Regulation.QualificationRules.Paths.Should().HaveCount(2);
        groups.Regulation.QualificationRules.Paths.Should().OnlyContain(path => path.Destination.TargetsForm);
        champ.CompositionEntries.Should().HaveCount(2);
        champ.FormPathResolutions.Should().BeEmpty();
        champ.Slots.Should().BeEmpty();
        champ.Groups.Should().BeEmpty();
    }

    [Fact]
    public async Task Flux_prog_group_draft_wires_for_group_progressionAsync()
    {
        var runner = fixture.Services.GetRequiredService<ScenarioRunner>();
        await runner.ResetAndRunAsync([SeedSpec.Parse("flux-prog-group-draft")]);

        using var scope = fixture.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var summary = (await competitions.ListAsync()).Should().ContainSingle().Subject;
        var competition = await competitions.GetByIdForUpdateAsync(summary.Id);
        competition.Should().NotBeNull();
        competition.StageIds.Should().HaveCount(2);

        var semi = await stages.GetByIdForUpdateAsync(competition.StageIds[0]);
        var groups = await stages.GetByIdForUpdateAsync(competition.StageIds[1]);
        semi.Should().NotBeNull();
        groups.Should().NotBeNull();
        groups.Groups.Should().HaveCount(2);
        semi.Regulation.ProgressionRules.Should().NotBeNull();
        semi.Regulation.ProgressionRules!.Paths.Should().HaveCount(2);
        semi.Regulation.ProgressionRules.Paths.Should().OnlyContain(path =>
            path.Destination.TargetsGroup && path.Outcome == ProgressionOutcome.Winner);
        groups.CompositionEntries.Should().BeEmpty();
    }

    [Fact]
    public async Task Qual_form_to_champ_mid_records_form_path_resolutionsAsync()
    {
        var runner = fixture.Services.GetRequiredService<ScenarioRunner>();
        await runner.ResetAndRunAsync([SeedSpec.Parse("qual-form-to-champ-mid")]);

        using var scope = fixture.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var summary = (await competitions.ListAsync()).Should().ContainSingle().Subject;
        var competition = await competitions.GetByIdForUpdateAsync(summary.Id);
        competition.Should().NotBeNull();

        var groups = await stages.GetByIdForUpdateAsync(competition.StageIds[0]);
        var champ = await stages.GetByIdForUpdateAsync(competition.StageIds[1]);
        groups.Should().NotBeNull();
        champ.Should().NotBeNull();
        groups.Status.Should().Be(StageStatus.Completed);
        champ.Status.Should().Be(StageStatus.Draft);
        champ.CompositionEntries.Should().HaveCount(4);
        champ.FormPathResolutions.Should().HaveCount(2);
        groups.Regulation.QualificationRules!.Paths.Should().OnlyContain(path => path.Destination.TargetsForm);
    }

    [Fact]
    public async Task Flux_form_and_group_draft_scenarios_seed_draftAsync()
    {
        var runner = fixture.Services.GetRequiredService<ScenarioRunner>();
        await runner.ResetAndRunAsync(
        [
            SeedSpec.Parse("flux-qual-form-draft"),
            SeedSpec.Parse("flux-prog-group-draft")
        ]);

        using var scope = fixture.Services.CreateScope();
        var list = await scope.ServiceProvider.GetRequiredService<ICompetitionRepository>().ListAsync();
        list.Should().HaveCount(2);
        list.Should().OnlyContain(c => c.Status == CompetitionStatus.Draft);
    }

    [Fact]
    public async Task Structure_qa_scenario_matrix_seeds_statusesAsync()
    {
        var runner = fixture.Services.GetRequiredService<ScenarioRunner>();
        await runner.ResetAndRunAsync(
        [
            SeedSpec.Parse("championship-ready"),
            SeedSpec.Parse("groups-suspended"),
            SeedSpec.Parse("championship-archived"),
            SeedSpec.Parse("swiss-ready"),
            SeedSpec.Parse("championship-structure-draft"),
            SeedSpec.Parse("groups-draw-pending"),
            SeedSpec.Parse("cup-draw-pending"),
            SeedSpec.Parse("registration-withdrawn"),
            SeedSpec.Parse("groups-to-ko-mid"),
            SeedSpec.Parse("cup-sf-running")
        ]);

        using var scope = fixture.Services.CreateScope();
        var list = await scope.ServiceProvider.GetRequiredService<ICompetitionRepository>().ListAsync();
        list.Should().HaveCount(10);
        list.Should().Contain(c => c.Status == CompetitionStatus.Ready);
        list.Should().Contain(c => c.Status == CompetitionStatus.Suspended);
        list.Should().Contain(c => c.Status == CompetitionStatus.Archived);
        list.Should().Contain(c => c.Status == CompetitionStatus.Running);
        list.Should().Contain(c => c.Status == CompetitionStatus.Draft);
    }

    [Fact]
    public async Task Cup_qf_sf_fills_semi_slots_and_projects_from_slots_overview_actionAsync()
    {
        var runner = fixture.Services.GetRequiredService<ScenarioRunner>();
        await runner.ResetAndRunAsync([SeedSpec.Parse("cup-qf-sf")]);

        using var scope = fixture.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var summary = (await competitions.ListAsync()).Should().ContainSingle().Subject;
        var competition = await competitions.GetByIdForUpdateAsync(summary.Id);
        competition.Should().NotBeNull();
        competition.Status.Should().Be(CompetitionStatus.Running);
        competition.StageIds.Should().HaveCount(2);

        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var quarter = await stages.GetByIdForUpdateAsync(competition.StageIds[0]);
        var semi = await stages.GetByIdForUpdateAsync(competition.StageIds[1]);
        quarter.Should().NotBeNull();
        semi.Should().NotBeNull();
        semi.Status.Should().Be(StageStatus.Draft);
        semi.Slots.Count(slot => slot.EntryId is not null).Should().Be(4);
        semi.Rounds[0].Fixtures.Should().BeEmpty();

        var matchesByStage = new Dictionary<StageId, IReadOnlyList<Match>>();
        foreach (var stageId in competition.StageIds)
        {
            var list = await scope.ServiceProvider.GetRequiredService<IMatchRepository>()
                .ListByStageForUpdateAsync(stageId);
            matchesByStage[stageId] = list;
        }

        var overview = OverviewAssembler.Assemble(
            competition,
            [quarter, semi],
            matchesByStage);
        overview.AvailableActions.Should().Contain(action =>
            action.Code == OverviewAssembler.ActionMaterializeFromOccupiedSlots
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
        var competition = await competitions.GetByIdForUpdateAsync(summary.Id);
        competition.Should().NotBeNull();
        competition.Status.Should().Be(CompetitionStatus.Completed);
        competition.StageIds.Should().HaveCount(5);

        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var loaded = new List<Stage>(5);
        foreach (var stageId in competition.StageIds)
        {
            var stage = await stages.GetByIdForUpdateAsync(stageId);
            stage.Should().NotBeNull();
            loaded.Add(stage);
        }

        var roundOf32 = loaded[0];
        roundOf32.Regulation.DrawRules.Should().NotBeNull();
        roundOf32.Regulation.ProgressionRules.Should().NotBeNull();
        roundOf32.Regulation.ProgressionRules!.Intents.Should().ContainSingle();
        roundOf32.Regulation.ProgressionRules.Intents[0].TargetsPopulation.Should().BeTrue();
        roundOf32.Draws.Should().Contain(draw => draw.Kind == DrawResolutionKind.Pairing);

        var roundOf16 = loaded[1];
        roundOf16.Regulation.DrawRules.Should().NotBeNull();
        roundOf16.Draws.Should().Contain(draw => draw.Kind == DrawResolutionKind.Slot);

        var final = loaded[^1];
        final.Status.Should().Be(StageStatus.Completed);
        final.Regulation.PlacementAwardRules.Should().NotBeNull();
        final.Regulation.PlacementAwardRules!.Paths.Should().HaveCount(2);

        var matchesByStage = new Dictionary<StageId, IReadOnlyList<Match>>();
        foreach (var stage in loaded)
        {
            matchesByStage[stage.Id] = await scope.ServiceProvider
                .GetRequiredService<IMatchRepository>()
                .ListByStageForUpdateAsync(stage.Id);
        }

        matchesByStage[loaded[0].Id].Should().HaveCount(16);
        matchesByStage[loaded[1].Id].Should().HaveCount(8);
        matchesByStage[loaded[2].Id].Should().HaveCount(4);
        matchesByStage[loaded[3].Id].Should().HaveCount(2);
        matchesByStage[final.Id].Should().HaveCount(1);
        matchesByStage[final.Id].Should().OnlyContain(m => m.Status == MatchStatus.Finished);

        var overview = OverviewAssembler.Assemble(competition, loaded, matchesByStage);
        overview.CompetitionOutcome.Should().NotBeNull();
        overview.CompetitionOutcome!.Presentation.Should().Be(OverviewAssembler.OutcomePresentationWinner);
        overview.CompetitionOutcome.Places.Should().HaveCount(2);
        overview.CompetitionOutcome.Places.Select(p => p.Rank).Should().BeEquivalentTo([1, 2]);
    }

    [Fact]
    public async Task Euro_across_groups_completes_with_across_groups_qualification_and_outcomeAsync()
    {
        var runner = fixture.Services.GetRequiredService<TemplateRunner>();
        await runner.ResetAndRunAsync([SeedSpec.Parse("euro-across-groups")]);

        using var scope = fixture.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var summary = (await competitions.ListAsync()).Should().ContainSingle().Subject;
        var competition = await competitions.GetByIdForUpdateAsync(summary.Id);
        competition.Should().NotBeNull();
        competition.Status.Should().Be(CompetitionStatus.Completed);
        competition.Entries.Should().HaveCount(24);
        competition.StageIds.Should().HaveCount(5);

        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var loaded = new List<Stage>(5);
        foreach (var stageId in competition.StageIds)
        {
            var stage = await stages.GetByIdForUpdateAsync(stageId);
            stage.Should().NotBeNull();
            loaded.Add(stage);
        }

        var groups = loaded[0];
        groups.Groups.Should().HaveCount(6);
        groups.Regulation.QualificationRules.Should().NotBeNull();
        groups.Regulation.QualificationRules!.Intents.Should().HaveCount(2);
        groups.Regulation.QualificationRules.Intents.Should().Contain(intent =>
            intent.SourceKind == QualificationIntentSourceKind.EachGroup
            && intent.PositionFrom == 1
            && intent.PositionTo == 2);
        groups.Regulation.QualificationRules.Intents.Should().Contain(intent =>
            intent.SourceKind == QualificationIntentSourceKind.AcrossGroups
            && intent.AcrossGroupsPosition == 3
            && intent.PositionFrom == 1
            && intent.PositionTo == 4);
        groups.Regulation.QualificationRules.Paths.Should().HaveCount(16);

        var roundOf16 = loaded[1];
        roundOf16.Regulation.DrawRules.Should().NotBeNull();
        roundOf16.Draws.Should().Contain(draw => draw.Kind == DrawResolutionKind.Slot);
        roundOf16.Slots.Should().OnlyContain(slot => slot.EntryId != null);

        var final = loaded[^1];
        final.Status.Should().Be(StageStatus.Completed);
        final.Regulation.PlacementAwardRules.Should().NotBeNull();
        final.Regulation.PlacementAwardRules!.Paths.Should().HaveCount(2);

        var matchesByStage = new Dictionary<StageId, IReadOnlyList<Match>>();
        foreach (var stage in loaded)
        {
            matchesByStage[stage.Id] = await scope.ServiceProvider
                .GetRequiredService<IMatchRepository>()
                .ListByStageForUpdateAsync(stage.Id);
        }

        var overview = OverviewAssembler.Assemble(competition, loaded, matchesByStage);
        overview.CompetitionOutcome.Should().NotBeNull();
        overview.CompetitionOutcome!.Presentation.Should().Be(OverviewAssembler.OutcomePresentationWinner);
        overview.CompetitionOutcome.Places.Should().HaveCount(2);
    }

    [Fact]
    public async Task World_cup_completes_with_final_and_bronze_placement_outcomeAsync()
    {
        var runner = fixture.Services.GetRequiredService<TemplateRunner>();
        await runner.ResetAndRunAsync([SeedSpec.Parse("world-cup")]);

        using var scope = fixture.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var summary = (await competitions.ListAsync()).Should().ContainSingle().Subject;
        var competition = await competitions.GetByIdForUpdateAsync(summary.Id);
        competition.Should().NotBeNull();
        competition.Status.Should().Be(CompetitionStatus.Completed);
        competition.StageIds.Should().HaveCount(6);

        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var loaded = new List<Stage>(6);
        foreach (var stageId in competition.StageIds)
        {
            var stage = await stages.GetByIdForUpdateAsync(stageId);
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
                .ListByStageForUpdateAsync(stage.Id);
        }

        matchesByStage[roundOf16.Id].Should().HaveCount(8);
        matchesByStage[quarter.Id].Should().HaveCount(4);
        matchesByStage[semi.Id].Should().HaveCount(2);
        matchesByStage[final.Id].Should().HaveCount(1);
        matchesByStage[bronze.Id].Should().HaveCount(1);
        matchesByStage[final.Id].Should().OnlyContain(m => m.Status == MatchStatus.Finished);
        matchesByStage[bronze.Id].Should().OnlyContain(m => m.Status == MatchStatus.Finished);

        var overview = OverviewAssembler.Assemble(competition, loaded, matchesByStage);
        overview.CompetitionOutcome.Should().NotBeNull();
        overview.CompetitionOutcome!.Presentation.Should().Be(OverviewAssembler.OutcomePresentationPodium);
        overview.CompetitionOutcome.Places.Should().HaveCount(4);
        overview.CompetitionOutcome.Places.Select(p => p.Rank).Should().BeEquivalentTo([1, 2, 3, 4]);
        overview.CompetitionOutcome.Places.Select(p => p.EntryId).Should().OnlyHaveUniqueItems();
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

            var overview = OverviewAssembler.Assemble(
                competition,
                [stage],
                new Dictionary<StageId, IReadOnlyList<Match>> { [stage.Id] = matches });
            overview.AvailableActions.Should().Contain(action =>
                action.Code == OverviewAssembler.ActionGenerateNextRound);
            overview.NaturalProgression!.Code.Should().Be(OverviewAssembler.ActionGenerateNextRound);
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
        var competition = await competitions.GetByIdForUpdateAsync(summary.Id);
        competition.Should().NotBeNull();
        var stage = await scope.ServiceProvider.GetRequiredService<IStageRepository>()
            .GetByIdForUpdateAsync(competition.StageIds[0]);
        stage.Should().NotBeNull();
        var matches = await scope.ServiceProvider.GetRequiredService<IMatchRepository>()
            .ListByStageForUpdateAsync(stage.Id);
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
        var competition = await competitions.GetByIdForUpdateAsync(summary.Id);
        competition.Should().NotBeNull();
        var stage = await scope.ServiceProvider.GetRequiredService<IStageRepository>()
            .GetByIdForUpdateAsync(competition.StageIds[0]);
        var matches = await scope.ServiceProvider.GetRequiredService<IMatchRepository>()
            .ListByStageForUpdateAsync(stage!.Id);
        matches.Count(m => m.Status == MatchStatus.Finished).Should().Be(matches.Count / 2);
    }

    private async Task AssertAllFinishedAsync()
    {
        using var scope = fixture.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var summary = (await competitions.ListAsync()).Single();
        var competition = await competitions.GetByIdForUpdateAsync(summary.Id);
        competition.Should().NotBeNull();
        competition.Status.Should().Be(CompetitionStatus.Completed);
        var stage = await scope.ServiceProvider.GetRequiredService<IStageRepository>()
            .GetByIdForUpdateAsync(competition.StageIds[0]);
        var matches = await scope.ServiceProvider.GetRequiredService<IMatchRepository>()
            .ListByStageForUpdateAsync(stage!.Id);
        matches.Should().OnlyContain(m => m.Status == MatchStatus.Finished);
    }

    private async Task AssertMatchSplitAsync(int finished, bool requireScheduled)
    {
        using var scope = fixture.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var summary = (await competitions.ListAsync()).Single();
        var competition = await competitions.GetByIdForUpdateAsync(summary.Id);
        competition.Should().NotBeNull();
        var stage = await scope.ServiceProvider.GetRequiredService<IStageRepository>()
            .GetByIdForUpdateAsync(competition.StageIds[0]);
        var matches = await scope.ServiceProvider.GetRequiredService<IMatchRepository>()
            .ListByStageForUpdateAsync(stage!.Id);
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
        var competition = await competitions.GetByIdForUpdateAsync(summary.Id);
        competition.Should().NotBeNull();
        var stage = await scope.ServiceProvider.GetRequiredService<IStageRepository>()
            .GetByIdForUpdateAsync(competition.StageIds[0]);
        var matches = await scope.ServiceProvider.GetRequiredService<IMatchRepository>()
            .ListByStageForUpdateAsync(stage!.Id);
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
