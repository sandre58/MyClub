// -----------------------------------------------------------------------
// <copyright file="StructurePrepareAttributionTests.cs" company="Stéphane ANDRE">
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
using MyClub.PlayUp.TestKit;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Reads;

/// <summary>
/// Reference cases R1–R5 — Prepare gate Attribution (option C).
/// </summary>
public sealed class StructurePrepareAttributionTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 13, 10, 0, 0, TimeSpan.Zero));

    [Fact]
    public void R1_cup_without_placement_award_rules_allows_Prepare()
    {
        var (competition, stage) = CreateFedCupFinal(placementRules: null, bracketIntact: true);

        var hub = AssembleHub(competition, stage);

        hub.Lifecycle!.Transition.Enabled.Should().BeTrue();
        hub.Lifecycle.Transition.DisabledReasonCode.Should().BeNull();
        PrepareStage.Execute(stage, [stage], _clock);
        stage.Status.Should().Be(StageStatus.Ready);
    }

    [Fact]
    public void R2_coherent_placement_award_allows_Prepare()
    {
        var (competition, stage) = CreateFedCupFinal(
            placementRules:
            [
                new PlacementAwardPath("FINAL", ProgressionOutcome.Winner, rank: 1),
                new PlacementAwardPath("FINAL", ProgressionOutcome.Loser, rank: 2)
            ],
            bracketIntact: true);

        var hub = AssembleHub(competition, stage);

        hub.Lifecycle!.Transition.Enabled.Should().BeTrue();
        PrepareStage.Execute(stage, [stage], _clock);
        stage.Status.Should().Be(StageStatus.Ready);
    }

    [Fact]
    public void R3_authored_award_unreachable_blocks_Prepare_with_Attribution()
    {
        var (competition, stage) = CreateFedCupFinal(
            placementRules: [new PlacementAwardPath("FINAL", ProgressionOutcome.Winner, rank: 1)],
            bracketIntact: false);

        var hub = AssembleHub(competition, stage);

        hub.Lifecycle!.Transition.Enabled.Should().BeFalse();
        hub.Lifecycle.Transition.DisabledReasonCode.Should()
            .Be(StructureStageLifecycleAssembler.ChecklistAttribution);
        hub.Lifecycle.Transition.Remediation.Should().NotBeNull();
        hub.Lifecycle.Transition.Remediation!.Href.Should().Contain("focus=rail-attribution");
        hub.StructureIssues.Should().Contain(CompetitionFlowAnalyzer.AuthoredAwardUnreachable);

        var act = () => PrepareStage.Execute(stage, [stage], _clock);
        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.AttributionUnreachable);
        stage.Status.Should().Be(StageStatus.Draft);
    }

    [Fact]
    public void R4_competition_ranks_above_local_places_n_does_not_block_Prepare()
    {
        var competition = CreateCompetition.Execute("Bronze", _clock);
        var stage = Stage.Create(
            competition.Id,
            new StageName("Petite finale"),
            StageRegulation.MaterializeFrom(competition.Regulation, isClassifyingPhase: false),
            _clock);
        competition.AddStage(stage.Id, _clock);
        var tie = new TieFormat(
            TieFormat.SingleLeg,
            aggregateScoring: false,
            penaltyShootoutRule: new PenaltyShootoutRule());
        var round = stage.AddRound("B", tie, _clock);
        stage.AddFixture(round.Id, _clock);
        stage.AddSlot("B-A");
        stage.AddSlot("B-B");
        stage.ReplaceBracketPairs([new BracketPair("BRONZE", "B-A", "B-B")]);
        stage.ReplacePlacementAwardRules(
            new PlacementAwardRules(
            [
                new PlacementAwardPath("BRONZE", ProgressionOutcome.Winner, rank: 3),
                new PlacementAwardPath("BRONZE", ProgressionOutcome.Loser, rank: 4)
            ]),
            _clock);
        FeedSlots(stage, "B-A", "B-B");

        var hub = AssembleHub(competition, stage);

        hub.Lifecycle!.Transition.Enabled.Should().BeTrue();
        hub.StructureIssues.Should().NotContain(CompetitionFlowAnalyzer.AuthoredAwardUnreachable);
        PrepareStage.Execute(stage, [stage], _clock);
        stage.Status.Should().Be(StageStatus.Ready);
    }

    [Fact]
    public void R5_incomplete_match_results_do_not_block_Prepare_when_bracket_coherent()
    {
        var (competition, stage) = CreateFedCupFinal(
            placementRules: [new PlacementAwardPath("FINAL", ProgressionOutcome.Winner, rank: 1)],
            bracketIntact: true);

        var hub = AssembleHub(competition, stage);

        hub.Lifecycle!.Transition.Enabled.Should().BeTrue();
        hub.StructureIssues.Should().NotContain(CompetitionFlowAnalyzer.AuthoredAwardUnreachable);
        PrepareStage.Execute(stage, [stage], _clock);
        stage.Status.Should().Be(StageStatus.Ready);
    }

    [Fact]
    public void BlocksPrepare_ignores_authored_award_on_classifying_phase()
    {
        var situation = TestCompetition.Create("Champ", _clock)
            .WithTeams("A", "B", "C", "D")
            .WithStructure(StructureIntent.Championship(stageName: "League"));
        var stage = situation.PrimaryStage!;
        foreach (var entry in situation.Competition.Entries)
        {
            stage.AddResolvedPopulationEntry(entry.Id, _clock);
        }

        StructurePrepareAttributionGate.BlocksPrepare(
                new StructureStageLifecycleInput(
                    situation.Competition,
                    stage,
                    situation.Stages,
                    [CompetitionFlowAnalyzer.AuthoredAwardUnreachable],
                    [],
                    StructureFormatKind.Championship,
                    HasDrawRules: false,
                    HasQualificationRules: false,
                    HasProgressionRules: false,
                    IsRootComposition: true,
                    CompositionEntryCount: situation.Competition.Entries.Count,
                    CompositionCapacity: situation.Competition.Entries.Count,
                    AffectationEntryCount: 0,
                    DrawExecutionBadge: null,
                    Materialize: null))
            .Should().BeFalse();
    }

    private static StructureStageHubSummaryDto AssembleHub(Competition competition, Stage stage)
    {
        var view = StructureViewAssembler.Assemble(competition, [stage]);
        return view.Stages.Single();
    }

    private (Competition Competition, Stage Stage) CreateFedCupFinal(
        IReadOnlyList<PlacementAwardPath>? placementRules,
        bool bracketIntact)
    {
        var competition = CreateCompetition.Execute("Cup final", _clock);
        var stage = Stage.Create(
            competition.Id,
            new StageName("Finale"),
            StageRegulation.MaterializeFrom(competition.Regulation, isClassifyingPhase: false),
            _clock);
        competition.AddStage(stage.Id, _clock);
        var tie = new TieFormat(
            TieFormat.SingleLeg,
            aggregateScoring: false,
            penaltyShootoutRule: new PenaltyShootoutRule());
        var round = stage.AddRound("F", tie, _clock);
        stage.AddFixture(round.Id, _clock);
        stage.AddSlot("F-A");
        stage.AddSlot("F-B");
        stage.ReplaceBracketPairs([new BracketPair("FINAL", "F-A", "F-B")]);

        if (placementRules is { Count: > 0 })
        {
            stage.ReplacePlacementAwardRules(new PlacementAwardRules(placementRules), _clock);
        }

        if (!bracketIntact)
        {
            stage.ClearBracketPairs();
        }

        FeedBothSlots(stage);
        return (competition, stage);
    }

    private void FeedBothSlots(Stage stage) => FeedSlots(stage, "F-A", "F-B");

    private void FeedSlots(Stage stage, string slotA, string slotB)
    {
        var a = EntryId.New();
        var b = EntryId.New();
        stage.ReplaceAffectationAuthoring([a, b], _clock);
        stage.AssignEntryToSlot(slotA, a);
        stage.AssignEntryToSlot(slotB, b);
    }
}
