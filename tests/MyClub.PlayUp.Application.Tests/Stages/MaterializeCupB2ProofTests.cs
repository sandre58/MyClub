// -----------------------------------------------------------------------
// <copyright file="MaterializeCupB2ProofTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Standings;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Stages;

/// <summary>Proof tests (materialization eligibility contract).</summary>
public sealed class MaterializeCupB2ProofTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 20, 10, 0, 0, TimeSpan.Zero));

    [Fact]
    public void N1_draw_apply_then_materialize_succeeds_without_compensating_slot_writes()
    {
        var competition = CreateCompetition.Execute("B2-N1", _clock);
        var stage = Stage.Create(competition.Id, new StageName("SF"), SampleRegulations.Standard(), _clock);
        stage.AddRound("SF", new TieFormat(TieFormat.SingleLeg, aggregateScoring: false), _clock);
        stage.AddSlot("SF-A");
        stage.AddSlot("SF-B");
        competition.AddStage(stage.Id, _clock);
        var entries = Enumerable.Range(0, 2)
            .Select(i => competition.AddEntry(TeamId.New(), $"T{i}", _clock).Id)
            .ToArray();
        ReplaceStageDrawRules.Execute(stage, new DrawRules(DrawMode.Random), _clock);
        stage.ReplaceAffectationAuthoring(entries, _clock);
        var draw = CreateDraw.Execute(stage, DrawResolutionKind.Slot, _clock);
        ConfigureDrawInputs.Execute(stage, draw.Id, DrawInputs.ForSlot(entries));
        stage.RecordDrawResolution(
            draw.Id,
            DrawResolution.ResolvedSlots(
            [
                new SlotDrawPlacement(entries[0], "SF-A"),
                new SlotDrawPlacement(entries[1], "SF-B")
            ]),
            _clock);
        PublishDraw.Execute(stage, draw.Id, _clock);
        ApplyDraw.Execute(stage, draw.Id, _clock);
        stage.SeedEntryRoundBracketPairs();
        PrepareStage.Execute(stage, [stage], _clock);

        var beforeA = stage.FindSlot("SF-A")!.EntryId;
        var beforeB = stage.FindSlot("SF-B")!.EntryId;

        var result = MaterializeCupFromOccupiedSlots.Execute(competition, stage, ["P1"], [], _clock);

        result.CreatedMatches.Should().ContainSingle();
        stage.FindSlot("SF-A")!.EntryId.Should().Be(beforeA);
        stage.FindSlot("SF-B")!.EntryId.Should().Be(beforeB);
    }

    [Fact]
    public void N2_projection_only_pair_is_determined_in_hub_but_not_eligible_for_command()
    {
        var (competition, source, destination, summaries) = BuildProjectedSemiFinalPair();
        var view = StructureViewAssembler.Assemble(
            competition,
            [source, destination],
            matchSummariesByStage: summaries);
        var hub = view.Stages.Single(stage => stage.StageId == destination.Id.Value);
        hub.Materialize!.Confrontations.Should().ContainSingle(row =>
            row.Key == "P1" && row.Determined && !row.Eligible);

        var act = () => MaterializeCupFromOccupiedSlots.Execute(
            competition,
            destination,
            ["P1"],
            [],
            _clock,
            [source, destination],
            summaries);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.ProgressionNotAppliedForMaterialization);
    }

    [Fact]
    public void N3_materialize_does_not_write_empty_slots_via_apply_resolved_entry()
    {
        var (competition, source, destination, summaries) = BuildProjectedSemiFinalPair();
        destination.FindSlot("S1")!.EntryId.Should().BeNull();
        destination.FindSlot("S2")!.EntryId.Should().BeNull();

        var act = () => MaterializeCupFromOccupiedSlots.Execute(
            competition,
            destination,
            ["P1"],
            [],
            _clock,
            [source, destination],
            summaries);

        act.Should().Throw<ApplicationFailureException>();
        destination.FindSlot("S1")!.EntryId.Should().BeNull();
        destination.FindSlot("S2")!.EntryId.Should().BeNull();
    }

    [Fact]
    public void N4_qualification_rejects_when_replay_ambiguous()
    {
        var competition = CreateCompetition.Execute("B2-N4-reject", _clock);
        var source = Stage.Create(competition.Id, new StageName("Pool"), SampleRegulations.Standard(), _clock);
        source.AddRound("R", new TieFormat(TieFormat.SingleLeg, aggregateScoring: false), _clock);
        var cup = Stage.Create(competition.Id, new StageName("Cup"), SampleRegulations.Standard(), _clock);
        cup.AddRound("R", new TieFormat(TieFormat.SingleLeg, aggregateScoring: false), _clock);
        cup.AddSlot("S1");
        cup.AddSlot("S2");
        cup.SeedEntryRoundBracketPairs();
        competition.AddStage(source.Id, _clock);
        competition.AddStage(cup.Id, _clock);
        source.ReplaceQualificationRules(
            new QualificationRules(
            [
                new QualificationPath(
                    1,
                    QualificationSource.Overall(),
                    new QualificationSelection(SelectionMode.Position, 1),
                    QualificationDestination.ForSlot(cup.Id, "S1"))
            ]),
            _clock);
        cup.ApplyResolvedEntry("S1", competition.AddEntry(TeamId.New(), "A", _clock).Id, _clock);
        cup.ApplyResolvedEntry("S2", competition.AddEntry(TeamId.New(), "B", _clock).Id, _clock);
        cup.Prepare(_clock);

        var act = () => MaterializeCupFromOccupiedSlots.Execute(
            competition,
            cup,
            ["P1"],
            [],
            _clock,
            [source, cup]);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().BeOneOf(
                ApplicationErrorCodes.InboundResolutionAmbiguous,
                ApplicationErrorCodes.InboundDestinationNotSatisfied);
    }

    [Fact]
    public void N4_qualification_accepts_when_slot_feed_satisfied()
    {
        var competition = CreateCompetition.Execute("B2-N4-accept", _clock);
        var league = Stage.Create(competition.Id, new StageName("League"), SampleRegulations.Standard(), _clock);
        var cup = Stage.Create(competition.Id, new StageName("Cup"), SampleRegulations.Standard(), _clock);
        cup.AddRound("R", new TieFormat(TieFormat.SingleLeg, aggregateScoring: false), _clock);
        cup.AddSlot("S1");
        cup.AddSlot("S2");
        cup.SeedEntryRoundBracketPairs();
        competition.AddStage(league.Id, _clock);
        competition.AddStage(cup.Id, _clock);
        var winner = competition.AddEntry(TeamId.New(), "Winner", _clock);
        var runnerUp = competition.AddEntry(TeamId.New(), "Runner", _clock);
        var match = Match.Create(competition.Id, league.Id, winner.Id, runnerUp.Id, _clock);
        match.Start(_clock);
        match.Finish(new MatchResult(ResultType.Played, new Score(2, 0)), _clock);
        var standing = CalculateStanding.Execute(
            [winner.Id, runnerUp.Id],
            [match],
            league.Regulation.StandingRules.OrThrow());
        league.ReplaceQualificationRules(
            new QualificationRules(
            [
                new QualificationPath(
                    1,
                    QualificationSource.Overall(),
                    new QualificationSelection(SelectionMode.Position, 1),
                    QualificationDestination.ForSlot(cup.Id, "S1")),
                new QualificationPath(
                    2,
                    QualificationSource.Overall(),
                    new QualificationSelection(SelectionMode.Position, 2),
                    QualificationDestination.ForSlot(cup.Id, "S2"))
            ]),
            _clock);
        ApplyQualification.Execute(league, standing, [league, cup], _clock);
        cup.Prepare(_clock);

        MaterializeCupFromOccupiedSlots.Execute(competition, cup, ["P1"], [], _clock, [league, cup])
            .CreatedMatches.Should().ContainSingle();
    }

    [Fact]
    public void N5_progression_accepts_after_slots_match_replay()
    {
        var (competition, source, destination, summaries) = BuildProjectedSemiFinalPair();
        var menton = competition.Entries.First(entry => entry.DisplayName == "FC Menton").Id;
        var hyeres = competition.Entries.First(entry => entry.DisplayName == "ES Hyères").Id;
        destination.ApplyResolvedEntry("S1", hyeres, _clock);
        destination.ApplyResolvedEntry("S2", menton, _clock);
        destination.AddResolvedPopulationEntry(hyeres, _clock);
        destination.AddResolvedPopulationEntry(menton, _clock);

        MaterializeCupFromOccupiedSlots.Execute(
                competition,
                destination,
                ["P1"],
                [],
                _clock,
                [source, destination],
                summaries)
            .CreatedMatches.Should().ContainSingle();
    }

    [Fact]
    public void N6_for_population_manual_slots_materialize_without_reapplying_progression_on_slots()
    {
        var competition = CreateCompetition.Execute("B2-N6", _clock);
        var qf = Stage.Create(competition.Id, new StageName("QF"), SampleRegulations.Standard(), _clock);
        var sf = Stage.Create(competition.Id, new StageName("SF"), SampleRegulations.Standard(), _clock);
        var home = competition.AddEntry(TeamId.New(), "H", _clock).Id;
        var away = competition.AddEntry(TeamId.New(), "A", _clock).Id;
        var qfRound = qf.AddRound("R", new TieFormat(TieFormat.SingleLeg, aggregateScoring: false), _clock);
        qf.AddSlot("A");
        qf.AddSlot("B");
        qf.ReplaceBracketPairs([new BracketPair("P1", "A", "B")]);
        qf.ApplyResolvedEntry("A", home, _clock);
        qf.ApplyResolvedEntry("B", away, _clock);
        var fixture = qf.AddFixture(qfRound.Id, _clock, "A", "B", "P1");
        var match = Match.Create(competition.Id, qf.Id, home, away, _clock);
        qf.AttachMatch(fixture.Id, match.Id, legIndex: 1, _clock);
        match.Start(_clock);
        match.Finish(new MatchResult(ResultType.Played, new Score(1, 0)), _clock);
        sf.AddRound("SF", new TieFormat(TieFormat.SingleLeg, aggregateScoring: false), _clock);
        sf.AddSlot("S1");
        sf.AddSlot("S2");
        sf.SeedEntryRoundBracketPairs();
        competition.AddStage(qf.Id, _clock);
        competition.AddStage(sf.Id, _clock);
        qf.ReplaceProgressionRules(
            new ProgressionRules(
            [
                new ProgressionPath("P1", ProgressionOutcome.Winner, ProgressionDestination.ForPopulation(sf.Id))
            ]),
            _clock);
        ApplyProgressionOutcome.Execute(qf, fixture.Id, [match], [qf, sf], _clock);
        sf.ApplyResolvedEntry("S1", home, _clock);
        sf.ApplyResolvedEntry("S2", away, _clock);
        PrepareStage.Execute(sf, [qf, sf], _clock);

        MaterializeCupFromOccupiedSlots.Execute(competition, sf, ["P1"], [], _clock, [qf, sf])
            .CreatedMatches.Should().ContainSingle();
    }

    private (Competition Competition, Stage Source, Stage Destination, Dictionary<StageId, IReadOnlyList<MatchSummaryRow>> Summaries)
        BuildProjectedSemiFinalPair()
    {
        var competition = CreateCompetition.Execute("B2-proj", _clock);
        var hyeres = competition.AddEntry(TeamId.New(), "ES Hyères", _clock);
        var menton = competition.AddEntry(TeamId.New(), "FC Menton", _clock);
        var fillerA = competition.AddEntry(TeamId.New(), "Filler A", _clock);
        var fillerB = competition.AddEntry(TeamId.New(), "Filler B", _clock);
        var source = Stage.Create(competition.Id, new StageName("QF"), SampleRegulations.Standard(), _clock);
        source.AddRound("QF", new TieFormat(TieFormat.SingleLeg, aggregateScoring: false), _clock);
        source.AddSlot("KO-A");
        source.AddSlot("KO-B");
        source.AddSlot("KO-C");
        source.AddSlot("KO-D");
        source.ReplaceBracketPairs(
        [
            new BracketPair("P1", "KO-A", "KO-B"),
            new BracketPair("P2", "KO-C", "KO-D")
        ]);
        var destination = Stage.Create(competition.Id, new StageName("SF"), SampleRegulations.Standard(), _clock);
        destination.AddRound("SF", new TieFormat(TieFormat.SingleLeg, aggregateScoring: false), _clock);
        destination.AddSlot("S1");
        destination.AddSlot("S2");
        destination.AddSlot("S3");
        destination.AddSlot("S4");
        destination.ReplaceBracketPairs(
        [
            new BracketPair("P1", "S1", "S2"),
            new BracketPair("P2", "S3", "S4")
        ]);
        competition.AddStage(source.Id, _clock);
        competition.AddStage(destination.Id, _clock);

        var fixtureP1 = source.AddFixture(source.Rounds[0].Id, _clock, "KO-A", "KO-B", "P1");
        source.ApplyResolvedEntry("KO-A", menton.Id, _clock);
        source.ApplyResolvedEntry("KO-B", fillerA.Id, _clock);
        var matchP1 = Match.Create(competition.Id, source.Id, menton.Id, fillerA.Id, _clock);
        matchP1.Start(_clock);
        matchP1.Finish(new MatchResult(ResultType.Played, new Score(2, 0)), _clock);
        source.AttachMatch(fixtureP1.Id, matchP1.Id, legIndex: 1, _clock);

        var fixtureP2 = source.AddFixture(source.Rounds[0].Id, _clock, "KO-C", "KO-D", "P2");
        source.ApplyResolvedEntry("KO-C", hyeres.Id, _clock);
        source.ApplyResolvedEntry("KO-D", fillerB.Id, _clock);
        var matchP2 = Match.Create(competition.Id, source.Id, hyeres.Id, fillerB.Id, _clock);
        matchP2.Start(_clock);
        matchP2.Finish(new MatchResult(ResultType.Played, new Score(1, 0)), _clock);
        source.AttachMatch(fixtureP2.Id, matchP2.Id, legIndex: 1, _clock);

        source.ReplaceProgressionRules(
            new ProgressionRules(
            [
                new ProgressionPath("P2", ProgressionOutcome.Winner, ProgressionDestination.ForSlot(destination.Id, "S1")),
                new ProgressionPath("P1", ProgressionOutcome.Winner, ProgressionDestination.ForSlot(destination.Id, "S2"))
            ]),
            _clock);
        destination.Prepare(_clock);

        var summaries = new Dictionary<StageId, IReadOnlyList<MatchSummaryRow>>
        {
            [source.Id] =
            [
                new MatchSummaryRow(matchP1.Id, matchP1.StageId, matchP1.Status, matchP1.HomeEntryId, matchP1.AwayEntryId, matchP1.Result),
                new MatchSummaryRow(matchP2.Id, matchP2.StageId, matchP2.Status, matchP2.HomeEntryId, matchP2.AwayEntryId, matchP2.Result)
            ]
        };

        return (competition, source, destination, summaries);
    }
}
