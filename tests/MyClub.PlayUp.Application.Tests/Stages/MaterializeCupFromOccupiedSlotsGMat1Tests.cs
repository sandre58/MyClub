// -----------------------------------------------------------------------
// <copyright file="MaterializeCupFromOccupiedSlotsGMat1Tests.cs" company="Stéphane ANDRE">
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
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Stages;

/// <summary>
/// G-MAT1 command contracts — <see cref="MaterializeCupFromOccupiedSlots"/> + undecidable necessary confrontation.
/// </summary>
public sealed class MaterializeCupFromOccupiedSlotsGMat1Tests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 15, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Execute_refuses_when_necessary_confrontation_cannot_decide_draw_P_plus()
    {
        var competition = CreateCompetition.Execute("G-MAT1-P+", _clock);
        var stage = Stage.Create(
            competition.Id,
            new StageName("Cup"),
            SampleRegulations.Standard(),
            _clock);
        var round = stage.AddRound("QuarterFinal", tieFormat: null, _clock);
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
        stage.Status.Should().Be(StageStatus.Ready);
        var fixtureCountBefore = stage.Rounds.Sum(r => r.Fixtures.Count);

        var act = () => MaterializeCupFromOccupiedSlots.Execute(
            competition,
            stage,
            ["P1"],
            [],
            _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.ConfrontationUndecidable);
        stage.Rounds.Sum(r => r.Fixtures.Count).Should().Be(fixtureCountBefore);
        stage.Rounds.SelectMany(r => r.Fixtures).Should().NotContain(f => f.BracketPairKey == "P1");
    }

    [Fact]
    public void Execute_succeeds_when_confrontation_is_not_necessary_N1()
    {
        var competition = CreateCompetition.Execute("G-MAT1-N1", _clock);
        var stage = Stage.Create(
            competition.Id,
            new StageName("Cup"),
            SampleRegulations.Standard(),
            _clock);
        var round = stage.AddRound("QuarterFinal", tieFormat: null, _clock);
        stage.AddFixture(round.Id, _clock);
        stage.AddSlot("KO-A");
        stage.AddSlot("KO-B");
        stage.ReplaceBracketPairs([new BracketPair("P1", "KO-A", "KO-B")]);
        var a = EntryId.New();
        var b = EntryId.New();
        stage.ReplaceAffectationAuthoring([a, b], _clock);
        stage.AssignEntryToSlot("KO-A", a);
        stage.AssignEntryToSlot("KO-B", b);

        PrepareStage.Execute(stage, [stage], _clock);

        var result = MaterializeCupFromOccupiedSlots.Execute(
            competition,
            stage,
            ["P1"],
            [],
            _clock);

        result.CreatedMatches.Should().HaveCount(1);
    }

    [Fact]
    public void Execute_succeeds_on_P2_when_P1_necessary_but_not_addressable_N2()
    {
        var competition = CreateCompetition.Execute("G-MAT1-N2", _clock);
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

        CompetitionFlowAnalyzer.AnalyzeExecution([stage]).Should().BeEmpty();

        stage.Prepare(_clock);
        stage.Status.Should().Be(StageStatus.Ready);

        var result = MaterializeCupFromOccupiedSlots.Execute(
            competition,
            stage,
            ["P2"],
            [],
            _clock);

        result.CreatedMatches.Should().HaveCount(1);
        result.CreatedMatches[0].HomeEntryId.Should().Be(home);
        result.CreatedMatches[0].AwayEntryId.Should().Be(away);
    }

    [Fact]
    public void Execute_succeeds_when_necessary_confrontation_has_penalty_shootout_N3()
    {
        var competition = CreateCompetition.Execute("G-MAT1-N3", _clock);
        var stage = Stage.Create(
            competition.Id,
            new StageName("Cup"),
            SampleRegulations.Standard(),
            _clock);
        var tie = new TieFormat(
            TieFormat.SingleLeg,
            aggregateScoring: false,
            penaltyShootoutRule: new PenaltyShootoutRule());
        var round = stage.AddRound("QuarterFinal", tie, _clock);
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

        PrepareStage.Execute(stage, [stage], _clock);

        var result = MaterializeCupFromOccupiedSlots.Execute(
            competition,
            stage,
            ["P1"],
            [],
            _clock);

        result.CreatedMatches.Should().HaveCount(1);
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
