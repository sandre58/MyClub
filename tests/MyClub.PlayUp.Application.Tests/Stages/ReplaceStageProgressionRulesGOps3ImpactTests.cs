// -----------------------------------------------------------------------
// <copyright file="ReplaceStageProgressionRulesGOps3ImpactTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Stages;

/// <summary>
/// R-Prog-OPS3-* — progression impact gate (WC-1 / WC-2).
/// </summary>
public sealed class ReplaceStageProgressionRulesGOps3ImpactTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 16, 20, 0, 0, TimeSpan.Zero));

    [Fact]
    public void R_Prog_OPS3_A1_allows_first_path_when_draw_applied_and_downstream_draft()
    {
        var (quarter, final, stages) = CreateCupQuarterWithAppliedDraw();
        DrawAppliedState.IsApplied(quarter.Draws[0], quarter).Should().BeTrue();
        quarter.Regulation.ProgressionRules.Should().BeNull();

        var act = () => ReplaceStageProgressionRules.Execute(
            quarter,
            [
                new ProgressionPathSpec("P1", ProgressionOutcome.Winner, final.Id)
            ],
            _clock,
            stages,
            [],
            EmptyDestinationMatches(stages, quarter.Id));

        act.Should().NotThrow();
        quarter.Regulation.ProgressionRules.Paths.Should().ContainSingle();
    }

    [Fact]
    public void R_Prog_OPS3_A2_allows_additive_path_when_downstream_ready_with_zero_composition()
    {
        var (quarter, final, stages) = CreateCupQuarterAndFinalWithRules();
        quarter.AddSlot("KO-E");
        quarter.AddSlot("KO-F");
        quarter.ReplaceBracketPairs(
        [
            new BracketPair("P1", "KO-A", "KO-B"),
            new BracketPair("P2", "KO-C", "KO-D"),
            new BracketPair("P3", "KO-E", "KO-F")
        ]);
        quarter.AddFixture(quarter.Rounds[0].Id, _clock, "KO-E", "KO-F", "P3");
        quarter.Prepare(_clock);
        final.Prepare(_clock);
        final.CompositionEntries.Should().BeEmpty();

        var act = () => ReplaceStageProgressionRules.Execute(
            quarter,
            [
                new ProgressionPathSpec("P1", ProgressionOutcome.Winner, final.Id),
                new ProgressionPathSpec("P2", ProgressionOutcome.Winner, final.Id),
                new ProgressionPathSpec("P3", ProgressionOutcome.Winner, final.Id)
            ],
            _clock,
            stages,
            [],
            EmptyDestinationMatches(stages, quarter.Id));

        act.Should().NotThrow();
        quarter.Regulation.ProgressionRules!.Paths.Should().HaveCount(3);
    }

    [Fact]
    public void R_Prog_OPS3_NOOP_allows_identical_rules_when_draw_applied()
    {
        var (quarter, final, stages) = CreateCupQuarterAndFinalWithRules();
        var draw = PublishSlotDraw(quarter, [EntryId.New(), EntryId.New(), EntryId.New(), EntryId.New()]);
        ApplyDraw.Execute(quarter, draw.Id, _clock);
        quarter.Prepare(_clock);

        var before = quarter.Regulation.ProgressionRules;
        var act = () => ReplaceStageProgressionRules.Execute(
            quarter,
            [
                new ProgressionPathSpec("P1", ProgressionOutcome.Winner, final.Id),
                new ProgressionPathSpec("P2", ProgressionOutcome.Winner, final.Id)
            ],
            _clock,
            stages,
            [],
            EmptyDestinationMatches(stages, quarter.Id));

        act.Should().NotThrow();
        quarter.Regulation.ProgressionRules.Should().BeEquivalentTo(before);
    }

    [Fact]
    public void R_Prog_OPS3_B2_refuses_removing_consumed_population_path()
    {
        var (source, destination, stages, match) = CreateAppliedPopulationProgression();
        var before = source.Regulation.ProgressionRules;

        var act = () => ReplaceStageProgressionRules.Execute(
            source,
            (IReadOnlyList<ProgressionPathSpec>?)null,
            _clock,
            stages,
            [match],
            DestinationMatches(stages, source.Id, destination.Id, []));

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.ProgressionFeedConsumed);
        source.Regulation.ProgressionRules.Should().BeEquivalentTo(before);
    }

    [Fact]
    public void R_Prog_OPS3_2c_refuses_clear_when_downstream_match_participates_without_wc1_consumption()
    {
        var (source, destination, stages, upstreamMatch, downstreamMatch) =
            CreateUpstreamDecidedWithDownstreamProductionWithoutApplyProgression();

        destination.CompositionEntries.Should().BeEmpty();

        var act = () => ReplaceStageProgressionRules.Execute(
            source,
            (IReadOnlyList<ProgressionPathSpec>?)null,
            _clock,
            stages,
            [upstreamMatch],
            DestinationMatches(stages, source.Id, destination.Id, [downstreamMatch]));

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.ProgressionDownstreamProductionBlocked);
    }

    [Fact]
    public void R_Prog_OPS3_B1neg_allows_additive_path_when_downstream_slot_has_direct_feed_only()
    {
        var (source, destination, final, stages) = CreateCupWithDirectSlotOccupantAndProgressionToOtherSlot();
        var draw = PublishSlotDraw(source, [.. source.AffectationAuthoring.Select(e => e.EntryId)]);
        ApplyDraw.Execute(source, draw.Id, _clock);

        var act = () => ReplaceStageProgressionRules.Execute(
            source,
            [
                new ProgressionPathSpec("P1", ProgressionOutcome.Winner, destination.Id, DestinationSlotKey: "SF1-B"),
                new ProgressionPathSpec("P2", ProgressionOutcome.Winner, final.Id)
            ],
            _clock,
            stages,
            [],
            EmptyDestinationMatches(stages, source.Id));

        act.Should().NotThrow();
        source.Regulation.ProgressionRules!.Paths.Should().HaveCount(2);
    }

    [Fact]
    public void R_Prog_OPS3_B1_refuses_clear_when_progression_fed_slot_is_consumed()
    {
        var (source, destination, stages, match) = CreateAppliedSlotProgression();
        var before = source.Regulation.ProgressionRules;

        var act = () => ReplaceStageProgressionRules.Execute(
            source,
            (IReadOnlyList<ProgressionPathSpec>?)null,
            _clock,
            stages,
            [match],
            DestinationMatches(stages, source.Id, destination.Id, []));

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.ProgressionFeedConsumed);
        source.Regulation.ProgressionRules.Should().BeEquivalentTo(before);
    }

    [Fact]
    public void R_Prog_OPS3_C1_refuses_reduction_when_ready_consumer_overfed()
    {
        var (quarter, final, stages) = CreateCupQuarterAndFinalWithRules();
        final.AddResolvedPopulationEntry(EntryId.New(), _clock);
        final.AddResolvedPopulationEntry(EntryId.New(), _clock);
        quarter.Prepare(_clock);
        final.Prepare(_clock);

        var act = () => ReplaceStageProgressionRules.Execute(
            quarter,
            [
                new ProgressionPathSpec("P1", ProgressionOutcome.Winner, final.Id)
            ],
            _clock,
            stages,
            [],
            EmptyDestinationMatches(stages, quarter.Id));

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.ReadyConsumerWouldBreak);
    }

    [Fact]
    public void R_Prog_OPS3_C2_refuses_clear_when_ready_consumer_populated()
    {
        var (quarter, final, stages) = CreateCupQuarterAndFinalWithRules();
        final.AddResolvedPopulationEntry(EntryId.New(), _clock);
        final.AddResolvedPopulationEntry(EntryId.New(), _clock);
        quarter.Prepare(_clock);
        final.Prepare(_clock);

        var act = () => ReplaceStageProgressionRules.Execute(
            quarter,
            (IReadOnlyList<ProgressionPathSpec>?)null,
            _clock,
            stages,
            [],
            EmptyDestinationMatches(stages, quarter.Id));

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.ReadyConsumerWouldBreak);
    }

    [Fact]
    public void R_Prog_OPS3_D2_refuses_progression_when_upstream_has_attached_matches()
    {
        var competition = CreateCompetition.Execute("R-Prog-D2", _clock);
        var quarter = Stage.Create(competition.Id, new StageName("QF"), SampleRegulations.Standard(), _clock);
        var round = quarter.AddRound("QF", tieFormat: null, _clock);
        quarter.AddFixture(round.Id, _clock);
        quarter.AddSlot("KO-A");
        quarter.AddSlot("KO-B");
        quarter.ReplaceBracketPairs([new BracketPair("P1", "KO-A", "KO-B")]);
        var a = EntryId.New();
        var b = EntryId.New();
        quarter.ReplaceAffectationAuthoring([a, b], _clock);
        quarter.AssignEntryToSlot("KO-A", a);
        quarter.AssignEntryToSlot("KO-B", b);
        PrepareStage.Execute(quarter, [quarter], _clock);

        var materialize = MaterializeCupFromOccupiedSlots.Execute(competition, quarter, ["P1"], [], _clock);
        var attached = materialize.CreatedMatches.ToList();

        var act = () => ReplaceStageProgressionRules.Execute(
            quarter,
            (IReadOnlyList<ProgressionPathSpec>?)null,
            _clock,
            [quarter],
            attached,
            new Dictionary<StageId, IReadOnlyList<Match>>());

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.StructureNotMutable);
    }

    private static Dictionary<StageId, IReadOnlyList<Match>> EmptyDestinationMatches(
        IReadOnlyList<Stage> stages,
        StageId upstreamId)
    {
        var map = new Dictionary<StageId, IReadOnlyList<Match>>();
        foreach (var stage in stages)
        {
            if (!stage.Id.Equals(upstreamId))
            {
                map[stage.Id] = [];
            }
        }

        return map;
    }

    private static Dictionary<StageId, IReadOnlyList<Match>> DestinationMatches(
        IReadOnlyList<Stage> stages,
        StageId upstreamId,
        StageId destinationId,
        IReadOnlyList<Match> destinationMatches)
    {
        var map = EmptyDestinationMatches(stages, upstreamId);
        map[destinationId] = destinationMatches;
        return map;
    }

    private (Stage Quarter, Stage Final, List<Stage> Stages) CreateCupQuarterWithAppliedDraw()
    {
        var (quarter, final, stages) = CreateCupQuarterAndFinalSkeleton();
        var entries = new[] { EntryId.New(), EntryId.New(), EntryId.New(), EntryId.New() };
        var draw = PublishSlotDraw(quarter, entries);
        ApplyDraw.Execute(quarter, draw.Id, _clock);
        quarter.Prepare(_clock);
        return (quarter, final, stages);
    }

    private (Stage Quarter, Stage Final, List<Stage> Stages) CreateCupQuarterAndFinalWithRules()
    {
        var (quarter, final, stages) = CreateCupQuarterAndFinalSkeleton();
        quarter.ReplaceProgressionRules(
            new ProgressionRules(
            [
                new ProgressionPath("P1", ProgressionOutcome.Winner, ProgressionDestination.ForPopulation(final.Id)),
                new ProgressionPath("P2", ProgressionOutcome.Winner, ProgressionDestination.ForPopulation(final.Id))
            ]),
            _clock);
        return (quarter, final, stages);
    }

    private (Stage Quarter, Stage Final, List<Stage> Stages) CreateCupQuarterAndFinalSkeleton()
    {
        var competition = CreateCompetition.Execute("R-Prog-OPS3", _clock);
        var quarter = Stage.Create(competition.Id, new StageName("QF"), SampleRegulations.Standard(), _clock);
        var round = quarter.AddRound("QF", tieFormat: null, _clock);
        quarter.AddSlot("KO-A");
        quarter.AddSlot("KO-B");
        quarter.AddSlot("KO-C");
        quarter.AddSlot("KO-D");
        quarter.ReplaceBracketPairs(
        [
            new BracketPair("P1", "KO-A", "KO-B"),
            new BracketPair("P2", "KO-C", "KO-D")
        ]);
        quarter.AddFixture(round.Id, _clock, "KO-A", "KO-B", "P1");
        quarter.AddFixture(round.Id, _clock, "KO-C", "KO-D", "P2");

        var final = Stage.Create(competition.Id, new StageName("Finale"), SampleRegulations.Standard(), _clock);
        final.AddMatchday(1, _clock);
        return (quarter, final, [quarter, final]);
    }

    private (Stage Source, Stage Destination, List<Stage> Stages, Match Match) CreateAppliedPopulationProgression()
    {
        var competition = CreateCompetition.Execute("R-Prog-B2", _clock);
        var source = Stage.Create(competition.Id, new StageName("QF"), SampleRegulations.Standard(), _clock);
        source.AddRound("R1", new TieFormat(TieFormat.SingleLeg, aggregateScoring: false), _clock);
        source.AddSlot("QF-A");
        source.AddSlot("QF-B");
        source.ReplaceBracketPairs([new BracketPair("P1", "QF-A", "QF-B")]);
        var destination = Stage.Create(competition.Id, new StageName("SF"), SampleRegulations.Standard(), _clock);
        var path = new ProgressionPath(
            "P1",
            ProgressionOutcome.Winner,
            ProgressionDestination.ForPopulation(destination.Id));
        source.ReplaceProgressionRules(new ProgressionRules([path]), _clock);
        var home = EntryId.New();
        var away = EntryId.New();
        var fixture = source.AddFixture(source.Rounds[0].Id, _clock, "QF-A", "QF-B", "P1");
        var match = Match.Create(competition.Id, source.Id, home, away, _clock);
        source.AttachMatch(fixture.Id, match.Id, legIndex: 1, _clock);
        match.Start(_clock);
        match.Finish(new MatchResult(ResultType.Played, new Score(1, 0)), _clock);
        ApplyProgressionOutcome.Execute(source, fixture.Id, [match], [source, destination], _clock);
        return (source, destination, [source, destination], match);
    }

    private (
        Stage Source,
        Stage Destination,
        List<Stage> Stages,
        Match UpstreamMatch,
        Match DownstreamMatch)
        CreateUpstreamDecidedWithDownstreamProductionWithoutApplyProgression()
    {
        var competition = CreateCompetition.Execute("R-Prog-2c", _clock);
        var source = Stage.Create(competition.Id, new StageName("QF"), SampleRegulations.Standard(), _clock);
        source.AddRound("R1", new TieFormat(TieFormat.SingleLeg, aggregateScoring: false), _clock);
        source.AddSlot("QF-A");
        source.AddSlot("QF-B");
        source.ReplaceBracketPairs([new BracketPair("P1", "QF-A", "QF-B")]);
        var destination = Stage.Create(competition.Id, new StageName("SF"), SampleRegulations.Standard(), _clock);
        destination.AddRound("R1", new TieFormat(TieFormat.SingleLeg, aggregateScoring: false), _clock);
        destination.AddSlot("SF-A");
        destination.AddSlot("SF-B");
        destination.ReplaceBracketPairs([new BracketPair("P1", "SF-A", "SF-B")]);
        source.ReplaceProgressionRules(
            new ProgressionRules(
            [
                new ProgressionPath(
                    "P1",
                    ProgressionOutcome.Winner,
                    ProgressionDestination.ForPopulation(destination.Id))
            ]),
            _clock);
        var home = EntryId.New();
        var away = EntryId.New();
        var upstreamFixture = source.AddFixture(source.Rounds[0].Id, _clock, "QF-A", "QF-B", "P1");
        var upstreamMatch = Match.Create(competition.Id, source.Id, home, away, _clock);
        source.AttachMatch(upstreamFixture.Id, upstreamMatch.Id, legIndex: 1, _clock);
        upstreamMatch.Start(_clock);
        upstreamMatch.Finish(new MatchResult(ResultType.Played, new Score(2, 0)), _clock);

        var other = EntryId.New();
        var downstreamMatch = Match.Create(competition.Id, destination.Id, home, other, _clock);
        var downstreamFixture = destination.AddFixture(destination.Rounds[0].Id, _clock, "SF-A", "SF-B", "P1");
        destination.AttachMatch(downstreamFixture.Id, downstreamMatch.Id, legIndex: 1, _clock);

        return (source, destination, [source, destination], upstreamMatch, downstreamMatch);
    }

    private (Stage Source, Stage Destination, Stage Final, List<Stage> Stages) CreateCupWithDirectSlotOccupantAndProgressionToOtherSlot()
    {
        var competition = CreateCompetition.Execute("R-Prog-B1neg", _clock);
        var source = Stage.Create(competition.Id, new StageName("QF"), SampleRegulations.Standard(), _clock);
        var round = source.AddRound("QF", tieFormat: null, _clock);
        source.AddSlot("KO-A");
        source.AddSlot("KO-B");
        source.AddSlot("KO-C");
        source.AddSlot("KO-D");
        source.ReplaceBracketPairs(
        [
            new BracketPair("P1", "KO-A", "KO-B"),
            new BracketPair("P2", "KO-C", "KO-D")
        ]);
        source.AddFixture(round.Id, _clock, "KO-A", "KO-B", "P1");
        source.AddFixture(round.Id, _clock, "KO-C", "KO-D", "P2");

        var destination = Stage.Create(competition.Id, new StageName("SF"), SampleRegulations.Standard(), _clock);
        destination.AddSlot("SF1-A");
        destination.AddSlot("SF1-B");
        var direct = EntryId.New();
        destination.ReplaceAffectationAuthoring([direct], _clock);
        destination.AssignEntryToSlot("SF1-A", direct);

        var final = Stage.Create(competition.Id, new StageName("Finale"), SampleRegulations.Standard(), _clock);
        source.ReplaceProgressionRules(
            new ProgressionRules(
            [
                new ProgressionPath(
                    "P1",
                    ProgressionOutcome.Winner,
                    ProgressionDestination.ForSlot(destination.Id, "SF1-B"))
            ]),
            _clock);

        var pool = new[] { EntryId.New(), EntryId.New(), EntryId.New(), EntryId.New() };
        source.ReplaceAffectationAuthoring(pool, _clock);
        DrawTestPoolSync.EnsureEffectivePool(source, pool, _clock);

        return (source, destination, final, [source, destination, final]);
    }

    private (Stage Source, Stage Destination, List<Stage> Stages, Match Match) CreateAppliedSlotProgression()
    {
        var competition = CreateCompetition.Execute("R-Prog-B1", _clock);
        var source = Stage.Create(competition.Id, new StageName("QF"), SampleRegulations.Standard(), _clock);
        source.AddRound("R1", new TieFormat(TieFormat.SingleLeg, aggregateScoring: false), _clock);
        source.AddSlot("QF-A");
        source.AddSlot("QF-B");
        source.ReplaceBracketPairs([new BracketPair("P1", "QF-A", "QF-B")]);
        var destination = Stage.Create(competition.Id, new StageName("SF"), SampleRegulations.Standard(), _clock);
        destination.AddRound("R1", new TieFormat(TieFormat.SingleLeg, aggregateScoring: false), _clock);
        destination.AddSlot("SF1-A");
        destination.AddSlot("SF1-B");
        destination.ReplaceBracketPairs([new BracketPair("P1", "SF1-A", "SF1-B")]);
        source.ReplaceProgressionRules(
            new ProgressionRules(
            [
                new ProgressionPath(
                    "P1",
                    ProgressionOutcome.Winner,
                    ProgressionDestination.ForSlot(destination.Id, "SF1-A"))
            ]),
            _clock);
        var home = EntryId.New();
        var away = EntryId.New();
        var fixture = source.AddFixture(source.Rounds[0].Id, _clock, "QF-A", "QF-B", "P1");
        var match = Match.Create(competition.Id, source.Id, home, away, _clock);
        source.AttachMatch(fixture.Id, match.Id, legIndex: 1, _clock);
        match.Start(_clock);
        match.Finish(new MatchResult(ResultType.Played, new Score(1, 0)), _clock);
        ApplyProgressionOutcome.Execute(source, fixture.Id, [match], [source, destination], _clock);
        return (source, destination, [source, destination], match);
    }

    private Draw PublishSlotDraw(Stage stage, EntryId[] pool)
    {
        DrawTestPoolSync.EnsureEffectivePool(stage, pool, _clock);
        var draw = stage.CreateDraw(DrawResolutionKind.Slot, _clock);
        stage.ConfigureDrawInputs(draw.Id, DrawInputs.ForSlot(pool));
        var placements = stage.Slots
            .Zip(pool, (slot, entry) => new SlotDrawPlacement(entry, slot.SlotKey))
            .ToArray();
        stage.RecordDrawResolution(draw.Id, DrawResolution.ResolvedSlots(placements), _clock);
        stage.PublishDraw(draw.Id, _clock);
        return draw;
    }
}
