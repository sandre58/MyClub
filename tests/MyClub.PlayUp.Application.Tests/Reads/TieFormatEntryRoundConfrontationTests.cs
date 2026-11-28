// -----------------------------------------------------------------------
// <copyright file="TieFormatEntryRoundConfrontationTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Reads;

public sealed class TieFormatEntryRoundConfrontationTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 11, 27, 20, 0, 0, TimeSpan.Zero));

    private static CompetitionFlowDiagnostic[] ExecutionDiagnosticsForPair(Stage stage, string pairKey) =>
        CompetitionFlowAnalyzer
            .AnalyzeExecution([stage])
            .Where(diagnostic => diagnostic.SlotKey == pairKey)
            .ToArray();

    [Fact]
    public void Entry_confrontation_undecidable_with_progression_when_entry_non_decisive_and_default_decisive()
    {
        var (competition, stage, pairKey) = CreateTwoRoundCupWithProgression(
            entryTie: new TieFormat(TieFormat.SingleLeg, aggregateScoring: false),
            sfTie: new TieFormat(TieFormat.SingleLeg, aggregateScoring: false, penaltyShootoutRule: new PenaltyShootoutRule()),
            defaultTie: new TieFormat(TieFormat.SingleLeg, aggregateScoring: false, penaltyShootoutRule: new PenaltyShootoutRule()),
            prepare: false);

        var before = ExecutionDiagnosticsForPair(stage, pairKey);
        before.Should().ContainSingle(diagnostic =>
            diagnostic.Code == CompetitionFlowAnalyzer.ConfrontationUndecidable
            && diagnostic.SlotKey == pairKey);

        var effectiveBefore = stage.ResolveEffectiveTieFormatForKey(pairKey);
        effectiveBefore.CanAlwaysDecideDraw().Should().BeFalse();

        var actPrepare = () => PrepareStage.Execute(stage, [stage], _clock);
        actPrepare.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.ConfrontationUndecidable);

        var afterPrepare = ExecutionDiagnosticsForPair(stage, pairKey);
        afterPrepare.Should().BeEquivalentTo(before);

        var actMaterialize = () => MaterializeCupFromOccupiedSlots.Execute(competition, stage, [pairKey], [], _clock);
        actMaterialize.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.StructureNotMutable);

        var afterMaterialize = ExecutionDiagnosticsForPair(stage, pairKey);
        afterMaterialize.Should().BeEquivalentTo(before);
        stage.ResolveEffectiveTieFormatForKey(pairKey).Should().BeEquivalentTo(effectiveBefore);
    }

    [Fact]
    public void Entry_confrontation_decidable_with_progression_when_entry_has_shootout()
    {
        var (competition, stage, pairKey) = CreateTwoRoundCupWithProgression(
            entryTie: new TieFormat(
                TieFormat.SingleLeg,
                aggregateScoring: false,
                penaltyShootoutRule: new PenaltyShootoutRule()),
            sfTie: null,
            defaultTie: new TieFormat(TieFormat.SingleLeg, aggregateScoring: false),
            prepare: true);

        var before = ExecutionDiagnosticsForPair(stage, pairKey);
        before.Should().BeEmpty();
        stage.ResolveEffectiveTieFormatForKey(pairKey).CanAlwaysDecideDraw().Should().BeTrue();

        MaterializeCupFromOccupiedSlots.Execute(competition, stage, [pairKey], [], _clock);

        ExecutionDiagnosticsForPair(stage, pairKey).Should().BeEmpty();
        stage.ResolveEffectiveTieFormatForKey(pairKey).NumberOfLegs.Should().Be(TieFormat.SingleLeg);
    }

    private (Competition Competition, Stage Stage, string PairKey) CreateTwoRoundCupWithProgression(
        TieFormat? entryTie,
        TieFormat? sfTie,
        TieFormat defaultTie,
        bool prepare)
    {
        var competition = Competition.Create(new CompetitionName("Cup"), SampleRegulations.Standard(), _clock);
        var home = competition.AddEntry(TeamId.New(), "Alpha", _clock).Id;
        var away = competition.AddEntry(TeamId.New(), "Bravo", _clock).Id;

        var regulation = new StageRegulation(
            SampleRegulations.Standard().MatchRules,
            standingRules: null,
            defaultTie);
        var stage = Stage.Create(competition.Id, new StageName("Cup"), regulation, _clock);
        competition.AddStage(stage.Id, _clock);

        _ = stage.AddRound("QF", entryTie, _clock);
        _ = stage.AddRound("SF", sfTie, _clock);
        stage.AddSlot("QF-A");
        stage.AddSlot("QF-B");
        stage.AddSlot("SF1-A");
        const string pairKey = "P1";
        stage.ReplaceBracketPairs([new BracketPair(pairKey, "QF-A", "QF-B")]);
        stage.ApplyResolvedEntry("QF-A", home, _clock);
        stage.ApplyResolvedEntry("QF-B", away, _clock);
        stage.ReplacePlacementAwardRules(
            new PlacementAwardRules(
            [
                new PlacementAwardPath(pairKey, ProgressionOutcome.Winner, rank: 1),
            ]),
            _clock);
        if (prepare)
        {
            PrepareStage.Execute(stage, [stage], _clock);
        }

        return (competition, stage, pairKey);
    }
}
