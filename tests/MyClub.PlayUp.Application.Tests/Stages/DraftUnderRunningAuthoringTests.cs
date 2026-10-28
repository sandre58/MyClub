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
using MyClub.PlayUp.Domain.Rules;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Stages;

/// <summary>
/// Draft phase under a Running or Suspended competition: operational authoring stays open on a
/// downstream phase, extras outside that Ready operational set are denied on the application path,
/// and an orphan phase is rename-only.
/// </summary>
public sealed class DraftUnderRunningAuthoringTests
{
    private static readonly string[] DraftExtrasOutsideReadyOps =
    [
        StructureViewAssembler.ActionReplaceDrawRules,
        StructureViewAssembler.ActionAddSlot,
        StructureViewAssembler.ActionAddRound,
        StructureViewAssembler.ActionAddGroup,
        StructureViewAssembler.ActionAddMatchday,
        StructureViewAssembler.ActionRebuildStructure,
        StructureViewAssembler.ActionRemoveStage,
        StructureViewAssembler.ActionReplaceMatchRules,
        StructureViewAssembler.ActionBindToCompetition,
        StructureViewAssembler.ActionReplaceDefaultTieFormat,
        StructureViewAssembler.ActionReplaceRoundTieFormat,
        StructureViewAssembler.ActionReplaceStandingRules,
        StructureViewAssembler.ActionReplaceMatchGenerationFormat,
        StructureViewAssembler.ActionReplaceSwissSettings
    ];

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
        hub.Actions.Should().BeEquivalentTo(StructureViewAssembler.ActionRenameStage, StructureViewAssembler.ActionReplaceProgressionRules, StructureViewAssembler.ActionReplacePlacementAwardRules, StructureViewAssembler.ActionReplaceAffectationAuthoring, StructureViewAssembler.ActionAssignEntryToSlot, StructureViewAssembler.ActionClearSlotAssignment);

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

        hub.Actions.Should().BeEquivalentTo(StructureViewAssembler.ActionRenameStage, StructureViewAssembler.ActionReplaceQualificationRules, StructureViewAssembler.ActionReplaceAffectationAuthoring);
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

    [Theory]
    [InlineData(CompetitionStatus.Running)]
    [InlineData(CompetitionStatus.Suspended)]
    public void Downstream_cup_Draft_omits_extras_and_application_denies_them(CompetitionStatus liveStatus)
    {
        var competition = CreateCompetition.Execute($"cup-extras-{liveStatus}", _clock);
        competition.AddEntry(TeamId.New(), "A", _clock);
        competition.AddEntry(TeamId.New(), "B", _clock);
        var quarter = AddCompetitionStage.Execute(competition, StructureIntent.Cup(4, "Quart de finale"), _clock);
        var semi = AddCompetitionStage.Execute(competition, StructureIntent.Cup(4, "Demi-finale"), _clock);
        quarter.Prepare(_clock);
        quarter.Start(_clock);
        GoLive(competition, liveStatus);

        var view = StructureViewAssembler.Assemble(competition, [quarter, semi]);
        var hub = view.Stages.Single(stage => stage.StageId == semi.Id.Value);

        view.Actions.Should().NotContain(StructureViewAssembler.ActionConfigureStructure);
        hub.Actions.Should().NotIntersectWith(DraftExtrasOutsideReadyOps);
        hub.Actions.Should().BeEquivalentTo(
        [
            StructureViewAssembler.ActionRenameStage,
            StructureViewAssembler.ActionReplaceProgressionRules,
            StructureViewAssembler.ActionReplacePlacementAwardRules,
            StructureViewAssembler.ActionReplaceAffectationAuthoring,
            StructureViewAssembler.ActionAssignEntryToSlot,
            StructureViewAssembler.ActionClearSlotAssignment
        ]);

        AssertDraftExtrasDenied(competition, semi, [quarter, semi]);
        semi.Status.Should().Be(StageStatus.Draft);
    }

    [Theory]
    [InlineData(CompetitionStatus.Running)]
    [InlineData(CompetitionStatus.Suspended)]
    public void Downstream_classifying_Draft_omits_extras_and_application_denies_them(CompetitionStatus liveStatus)
    {
        var competition = CreateCompetition.Execute($"classifying-extras-{liveStatus}", _clock);
        competition.AddEntry(TeamId.New(), "A", _clock);
        competition.AddEntry(TeamId.New(), "B", _clock);
        var groups = AddCompetitionStage.Execute(competition, StructureIntent.Groups(2, 2, "Poules"), _clock);
        var finale = AddCompetitionStage.Execute(competition, StructureIntent.Championship("Finale"), _clock);
        groups.Prepare(_clock);
        groups.Start(_clock);
        groups.Complete(_clock);
        GoLive(competition, liveStatus);

        var view = StructureViewAssembler.Assemble(competition, [groups, finale]);
        var hub = view.Stages.Single(stage => stage.StageId == finale.Id.Value);

        view.Actions.Should().NotContain(StructureViewAssembler.ActionConfigureStructure);
        hub.Actions.Should().NotIntersectWith(DraftExtrasOutsideReadyOps);
        hub.Actions.Should().BeEquivalentTo(
        [
            StructureViewAssembler.ActionRenameStage,
            StructureViewAssembler.ActionReplaceQualificationRules,
            StructureViewAssembler.ActionReplaceAffectationAuthoring
        ]);

        AssertDraftExtrasDenied(competition, finale, [groups, finale]);
        finale.Status.Should().Be(StageStatus.Draft);
        finale.Regulation.StandingRules.Should().NotBeNull();
    }

    [Theory]
    [InlineData(CompetitionStatus.Running)]
    [InlineData(CompetitionStatus.Suspended)]
    public void Orphan_Draft_is_rename_only_while_competition_is_live(CompetitionStatus liveStatus)
    {
        var competition = CreateCompetition.Execute($"orphan-{liveStatus}", _clock);
        competition.AddEntry(TeamId.New(), "A", _clock);
        var stage = AddCompetitionStage.Execute(competition, StructureIntent.Championship("Seule"), _clock);
        GoLive(competition, liveStatus);

        var view = StructureViewAssembler.Assemble(competition, [stage]);
        var hub = view.Stages.Single();

        view.Actions.Should().NotContain(StructureViewAssembler.ActionConfigureStructure);
        hub.Status.Should().Be(StageStatus.Draft);
        hub.Actions.Should().Equal(StructureViewAssembler.ActionRenameStage);
        hub.Actions.Should().NotIntersectWith(DraftExtrasOutsideReadyOps);
        hub.Lifecycle!.Transition.Enabled.Should().BeFalse();
        hub.Lifecycle.Transition.DisabledReasonCode.Should()
            .Be(StructureStageLifecycleAssembler.DisabledConstructionFrozen);

        AssertDraftExtrasDenied(competition, stage, [stage]);
    }

    [Fact]
    public void ReplaceStandingRules_on_Running_stage_stays_allowed_while_competition_Suspended()
    {
        var competition = CreateCompetition.Execute("standing-suspended", _clock);
        competition.AddEntry(TeamId.New(), "A", _clock);
        var stage = AddCompetitionStage.Execute(competition, StructureIntent.Championship("League"), _clock);
        stage.Prepare(_clock);
        stage.Start(_clock);
        competition.Prepare(_clock);
        competition.Start(_clock);
        competition.Suspend(_clock);

        var replacement = new StandingRules(
            new PointsPolicy(2, 1, 0),
            [RankingCriterion.Points, RankingCriterion.Wins]);

        ReplaceStageStandingRules.Execute(stage, competition, replacement, _clock);

        competition.Status.Should().Be(CompetitionStatus.Suspended);
        stage.Status.Should().Be(StageStatus.Running);
        stage.Regulation.StandingRules.Should().Be(replacement);
    }

    private void GoLive(Competition competition, CompetitionStatus liveStatus)
    {
        competition.Prepare(_clock);
        competition.Start(_clock);
        if (liveStatus == CompetitionStatus.Suspended)
        {
            competition.Suspend(_clock);
        }

        competition.Status.Should().Be(liveStatus);
    }

    private void AssertDraftExtrasDenied(
        Competition competition,
        Stage draft,
        IReadOnlyList<Stage> peers)
    {
        var roundId = draft.Rounds.Count > 0 ? draft.Rounds[0].Id : RoundId.New();
        var tie = new TieFormat(TieFormat.SingleLeg, aggregateScoring: false);
        var standing = new StandingRules(
            new PointsPolicy(3, 1, 0),
            [RankingCriterion.Points]);

        Action[] denied =
        [
            () => ReplaceStageDrawRules.Execute(draft, competition, new DrawRules(DrawMode.Random), _clock),
            () => AddStageSlot.Execute(draft, competition, "EXTRA"),
            () => AddStageRound.Execute(draft, competition, "Extra", null, _clock),
            () => AddStageGroup.Execute(draft, competition, "C", _clock),
            () => AddStageMatchday.Execute(draft, competition, number: null, _clock),
            () => RebuildStageStructure.Execute(competition, draft, StructureIntent.Championship("Rebuild"), _clock),
            () => RemoveCompetitionStage.Execute(competition, draft, peers, _clock),
            () => ReplaceStageMatchRules.Execute(draft, competition, draft.Regulation.MatchRules, _clock),
            () => BindStageRegulation.Execute(draft, competition, BindStageRegulation.ScopeMatch, _clock),
            () => ReplaceStageDefaultTieFormat.Execute(draft, competition, tie, _clock),
            () => ReplaceRoundTieFormat.Execute(draft, competition, roundId, tie, _clock),
            () => ReplaceStageStandingRules.Execute(draft, competition, standing, _clock),
            () => ReplaceStageMatchGenerationFormat.Execute(
                draft,
                competition,
                MatchGenerationFormat.DoubleRoundRobin),
            () => ReplaceStageSwissSettings.Execute(draft, competition, 3),
            () => ConfigureStructure.Execute(competition, draft, StructureIntent.Championship("Configure"), _clock)
        ];

        foreach (var act in denied)
        {
            act.Should().Throw<ApplicationFailureException>()
                .Which.Code.Should().Be(ApplicationErrorCodes.StructureNotMutable);
        }
    }
}
