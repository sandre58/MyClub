// -----------------------------------------------------------------------
// <copyright file="MaterializeCupFromOccupiedSlotsGMat2Tests.cs" company="Stéphane ANDRE">
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
/// Production-scoped <see cref="CompetitionFlowAnalyzer.AuthoredAwardUnreachable"/> at materialize.
/// </summary>
public sealed class MaterializeCupFromOccupiedSlotsGMat2Tests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 15, 14, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Execute_refuses_targeted_FINAL_when_placement_unreachable_R_Mat_M2_plus()
    {
        var (competition, stage) = CreateFedCupFinal(
            [new PlacementAwardPath("FINAL", ProgressionOutcome.Winner, rank: 1)],
            bracketIntact: false);
        stage.Prepare(_clock);
        var fixtureCountBefore = stage.Rounds.Sum(r => r.Fixtures.Count);

        var act = () => MaterializeCupFromOccupiedSlots.Execute(
            competition,
            stage,
            ["FINAL"],
            [],
            _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.AttributionUnreachable);
        stage.Rounds.Sum(r => r.Fixtures.Count).Should().Be(fixtureCountBefore);
    }

    [Fact]
    public void Execute_succeeds_on_P2_when_P1_placement_unreachable_R_Mat_M2_partial()
    {
        var competition = CreateCompetition.Execute("G-MAT2-partial", _clock);
        var stage = CreateSemiStage(competition.Id);
        stage.ReplacePlacementAwardRules(
            new PlacementAwardRules(
            [
                new PlacementAwardPath("P1", ProgressionOutcome.Winner, rank: 1)
            ]),
            _clock);
        stage.ReplaceBracketPairs([new BracketPair("P2", "SF2-A", "SF2-B")]);
        var home = EntryId.New();
        var away = EntryId.New();
        stage.ApplyResolvedEntry("SF2-A", home, _clock);
        stage.ApplyResolvedEntry("SF2-B", away, _clock);
        stage.Prepare(_clock);

        var result = MaterializeCupFromOccupiedSlots.Execute(
            competition,
            stage,
            ["P2"],
            [],
            _clock);

        result.CreatedMatches.Should().HaveCount(1);
    }

    [Fact]
    public void Execute_global_does_not_refuse_when_unreachable_off_eligible_set_R_Mat_M2_global_ok()
    {
        var competition = CreateCompetition.Execute("G-MAT2-global", _clock);
        var stage = CreateSemiStage(competition.Id);
        stage.ReplacePlacementAwardRules(
            new PlacementAwardRules(
            [
                new PlacementAwardPath("P1", ProgressionOutcome.Winner, rank: 1)
            ]),
            _clock);
        stage.ReplaceBracketPairs([new BracketPair("P2", "SF2-A", "SF2-B")]);
        stage.ApplyResolvedEntry("SF2-A", EntryId.New(), _clock);
        stage.ApplyResolvedEntry("SF2-B", EntryId.New(), _clock);
        stage.Prepare(_clock);

        var result = MaterializeCupFromOccupiedSlots.Execute(
            competition,
            stage,
            null,
            [],
            _clock);

        result.CreatedMatches.Should().HaveCount(1);
    }

    [Fact]
    public void Execute_succeeds_when_placement_coherent_R2()
    {
        var (competition, stage) = CreateFedCupFinal(
            [
                new PlacementAwardPath("FINAL", ProgressionOutcome.Winner, rank: 1),
                new PlacementAwardPath("FINAL", ProgressionOutcome.Loser, rank: 2)
            ],
            bracketIntact: true);
        PrepareStage.Execute(stage, [stage], _clock);

        var result = MaterializeCupFromOccupiedSlots.Execute(
            competition,
            stage,
            ["FINAL"],
            [],
            _clock);

        result.CreatedMatches.Should().HaveCount(1);
    }

    [Fact]
    public void Execute_refuses_ConfrontationUndecidable_before_Attribution_on_undecidable_graph()
    {
        var competition = CreateCompetition.Execute("G-MAT2-vs-1", _clock);
        var stage = Stage.Create(
            competition.Id,
            new StageName("Cup"),
            SampleRegulations.Standard(),
            _clock);
        var round = stage.AddRound("QF", tieFormat: null, _clock);
        stage.AddFixture(round.Id, _clock);
        stage.AddSlot("KO-A");
        stage.AddSlot("KO-B");
        stage.AddSlot("SF1-A");
        stage.ReplaceBracketPairs([new BracketPair("P1", "KO-A", "KO-B")]);
        var a = EntryId.New();
        var b = EntryId.New();
        stage.ReplaceAffectationAuthoring([a, b], _clock);
        stage.AssignEntryToSlot("KO-A", a);
        stage.AssignEntryToSlot("KO-B", b);
        stage.ReplaceProgressionRules(
            new ProgressionRules(
            [
                new ProgressionPath(
                    "P1",
                    ProgressionOutcome.Winner,
                    new ProgressionDestination(stage.Id, "SF1-A"))
            ]),
            _clock);
        stage.Prepare(_clock);

        var act = () => MaterializeCupFromOccupiedSlots.Execute(
            competition,
            stage,
            ["P1"],
            [],
            _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.ConfrontationUndecidable);
    }

    private (Competition Competition, Stage Stage) CreateFedCupFinal(
        IReadOnlyList<PlacementAwardPath> placementRules,
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
        stage.ReplacePlacementAwardRules(new PlacementAwardRules(placementRules), _clock);
        if (!bracketIntact)
        {
            stage.ClearBracketPairs();
        }

        var a = EntryId.New();
        var b = EntryId.New();
        stage.ReplaceAffectationAuthoring([a, b], _clock);
        stage.AssignEntryToSlot("F-A", a);
        stage.AssignEntryToSlot("F-B", b);
        return (competition, stage);
    }

    private Stage CreateSemiStage(CompetitionId competitionId)
    {
        var stage = Stage.Create(competitionId, new StageName("SF"), SampleRegulations.Standard(), _clock);
        stage.AddRound("Semi-Finals", tieFormat: null, _clock);
        stage.AddSlot("SF1-A");
        stage.AddSlot("SF1-B");
        stage.AddSlot("SF2-A");
        stage.AddSlot("SF2-B");
        stage.SeedEntryRoundBracketPairs();
        return stage;
    }
}
