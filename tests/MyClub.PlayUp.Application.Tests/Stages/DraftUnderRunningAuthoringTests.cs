// -----------------------------------------------------------------------
// <copyright file="DraftUnderRunningAuthoringTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Stages;

/// <summary>
/// Draft phase under a Running competition: operational authoring stays open on a downstream phase,
/// heavy structure edits are denied on the application path, and an orphan phase is rename-only.
/// </summary>
public sealed class DraftUnderRunningAuthoringTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 27, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Cup_qf_sf_aval_Draft_keeps_ready_operational_actions_while_competition_Running()
    {
        var competition = CreateCompetition.Execute("cup-qf-sf", _clock);
        competition.AddEntry(TeamId.New(), "A", _clock);
        competition.AddEntry(TeamId.New(), "B", _clock);
        var quarter = AddCompetitionStage.Execute(competition, StructureIntent.Cup(4, "Quart de finale"), _clock);
        var semi = AddCompetitionStage.Execute(competition, StructureIntent.Cup(4, "Demi-finale"), _clock);
        quarter.Prepare(_clock);
        quarter.Start(_clock);
        competition.Prepare(_clock);
        competition.Start(_clock);

        competition.Status.Should().Be(CompetitionStatus.Running);
        quarter.Status.Should().Be(StageStatus.Running);
        semi.Status.Should().Be(StageStatus.Draft);

        var hub = StructureViewAssembler.Assemble(competition, [quarter, semi]).Stages
            .Single(stage => stage.StageId == semi.Id.Value);

        hub.Lifecycle!.Transition.Code.Should().Be(OverviewAssembler.ActionPrepareStage);
        hub.Lifecycle.Transition.DisabledReasonCode.Should()
            .NotBe(StructureStageLifecycleAssembler.DisabledConstructionFrozen);
        hub.Actions.Should().BeEquivalentTo(
        [
            StructureViewAssembler.ActionRenameStage,
            StructureViewAssembler.ActionReplaceProgressionRules,
            StructureViewAssembler.ActionReplacePlacementAwardRules,
            StructureViewAssembler.ActionReplaceAffectationAuthoring,
            StructureViewAssembler.ActionAssignEntryToSlot,
            StructureViewAssembler.ActionClearSlotAssignment
        ]);

        var addRound = () => AddStageRound.Execute(semi, competition, "Extra", null, _clock);
        addRound.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.StructureNotMutable);

        var addSlot = () => AddStageSlot.Execute(semi, competition, "EXTRA");
        addSlot.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.StructureNotMutable);

        var replaceDraw = () => ReplaceStageDrawRules.Execute(
            semi,
            competition,
            new DrawRules(DrawMode.Random),
            _clock);
        replaceDraw.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.StructureNotMutable);

        ReplaceStageProgressionRules.Execute(semi, (IReadOnlyList<ProgressionPathSpec>?)null, _clock);
        semi.Status.Should().Be(StageStatus.Draft);
        semi.Rounds.Should().ContainSingle();
    }

    [Fact]
    public void Qual_form_aval_Draft_keeps_ready_operational_actions_while_competition_Running()
    {
        var competition = CreateCompetition.Execute("qual-form", _clock);
        competition.AddEntry(TeamId.New(), "A", _clock);
        competition.AddEntry(TeamId.New(), "B", _clock);
        var groups = AddCompetitionStage.Execute(competition, StructureIntent.Groups(2, 2, "Poules"), _clock);
        var finale = AddCompetitionStage.Execute(competition, StructureIntent.Championship("Finale"), _clock);
        groups.Prepare(_clock);
        groups.Start(_clock);
        groups.Complete(_clock);
        competition.Prepare(_clock);
        competition.Start(_clock);

        competition.Status.Should().Be(CompetitionStatus.Running);
        groups.Status.Should().Be(StageStatus.Completed);
        finale.Status.Should().Be(StageStatus.Draft);

        var hub = StructureViewAssembler.Assemble(competition, [groups, finale]).Stages
            .Single(stage => stage.StageId == finale.Id.Value);

        hub.Actions.Should().BeEquivalentTo(
        [
            StructureViewAssembler.ActionRenameStage,
            StructureViewAssembler.ActionReplaceQualificationRules,
            StructureViewAssembler.ActionReplaceAffectationAuthoring
        ]);
        hub.Actions.Should().NotContain(StructureViewAssembler.ActionAddMatchday);
        hub.Actions.Should().NotContain(StructureViewAssembler.ActionRebuildStructure);
        hub.Actions.Should().NotContain(StructureViewAssembler.ActionReplaceDrawRules);
        hub.Actions.Should().NotContain(StructureViewAssembler.ActionReplaceStandingRules);
        hub.Actions.Should().NotContain(StructureViewAssembler.ActionReplaceMatchRules);

        var addMatchday = () => AddStageMatchday.Execute(finale, competition, number: null, _clock);
        addMatchday.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.StructureNotMutable);

        ReplaceStageQualificationRules.Execute(
            finale,
            (IReadOnlyList<QualificationPathSpec>?)null,
            _clock);
        finale.Status.Should().Be(StageStatus.Draft);
    }

    [Fact]
    public void Orphan_Draft_under_Running_competition_is_rename_only()
    {
        var competition = CreateCompetition.Execute("orphan", _clock);
        competition.AddEntry(TeamId.New(), "A", _clock);
        var stage = AddCompetitionStage.Execute(competition, StructureIntent.Championship("Seule"), _clock);
        competition.Prepare(_clock);
        competition.Start(_clock);

        var hub = StructureViewAssembler.Assemble(competition, [stage]).Stages.Single();

        hub.Status.Should().Be(StageStatus.Draft);
        hub.Actions.Should().Equal(StructureViewAssembler.ActionRenameStage);
        hub.Lifecycle!.Transition.Enabled.Should().BeFalse();
        hub.Lifecycle.Transition.DisabledReasonCode.Should()
            .Be(StructureStageLifecycleAssembler.DisabledConstructionFrozen);

        var addMatchday = () => AddStageMatchday.Execute(stage, competition, number: 2, _clock);
        addMatchday.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.StructureNotMutable);
    }

    [Fact]
    public void ReplaceStandingRules_on_Running_stage_stays_allowed_while_competition_Running()
    {
        var competition = CreateCompetition.Execute("standing-live", _clock);
        competition.AddEntry(TeamId.New(), "A", _clock);
        var stage = AddCompetitionStage.Execute(competition, StructureIntent.Championship("League"), _clock);
        stage.Prepare(_clock);
        stage.Start(_clock);
        competition.Prepare(_clock);
        competition.Start(_clock);

        var replacement = new StandingRules(
            new PointsPolicy(2, 1, 0),
            [RankingCriterion.Points, RankingCriterion.Wins]);

        ReplaceStageStandingRules.Execute(stage, competition, replacement, _clock);

        stage.Regulation.StandingRules.Should().Be(replacement);
    }
}
