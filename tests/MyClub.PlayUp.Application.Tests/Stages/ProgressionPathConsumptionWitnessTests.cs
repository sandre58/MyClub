// -----------------------------------------------------------------------
// <copyright file="ProgressionPathConsumptionWitnessTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Stages.ProgressionImpact;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Stages;

public sealed class ProgressionPathConsumptionWitnessTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 16, 18, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Wc1_satisfied_path_removed_yields_consumed_loss()
    {
        var (source, destination, path, home, away, fixtureId, match, summaries) = CreateAppliedPopulationScenario();
        ApplyProgressionOutcome.Execute(source, fixtureId, [match], [source, destination], _clock);

        var diff = ProgressionRulesPathDiff.Compare(source.Regulation.ProgressionRules, afterRules: null);
        diff.Touched.Should().ContainSingle();

        var verdict = ProgressionPathConsumptionWitness.EvaluateTouchedPath(
            diff.Touched[0],
            afterRules: null,
            source,
            destination,
            summaries);

        verdict.Should().Be(ProgressionPathConsumptionWitness.Verdict.ConsumedLoss);
    }

    [Fact]
    public void Wc1_undecided_fixture_yields_ambiguous()
    {
        var (source, destination, path, _, _, fixtureId, match, _) = CreateAppliedPopulationScenario();
        ApplyProgressionOutcome.Execute(source, fixtureId, [match], [source, destination], _clock);

        var diff = ProgressionRulesPathDiff.Compare(source.Regulation.ProgressionRules, afterRules: null);
        var emptySummaries = new Dictionary<StageId, IReadOnlyList<MatchSummaryRow>>();

        var verdict = ProgressionPathConsumptionWitness.EvaluateTouchedPath(
            diff.Touched[0],
            afterRules: null,
            source,
            destination,
            emptySummaries);

        verdict.Should().Be(ProgressionPathConsumptionWitness.Verdict.ConsumedAmbiguous);
    }

    [Fact]
    public void Wc1_destination_change_on_consumed_path_yields_consumed_loss()
    {
        var competitionId = CompetitionId.New();
        var source = CreateKnockoutStage(competitionId, "QF", ["QF-A", "QF-B"]);
        source.ReplaceBracketPairs([new BracketPair("P1", "QF-A", "QF-B")]);
        var sf = Stage.Create(competitionId, new StageName("SF"), SampleRegulations.Standard(), _clock);
        var final = Stage.Create(competitionId, new StageName("Final"), SampleRegulations.Standard(), _clock);
        var pathBefore = new ProgressionPath(
            "P1",
            ProgressionOutcome.Winner,
            ProgressionDestination.ForPopulation(sf.Id));
        source.ReplaceProgressionRules(new ProgressionRules([pathBefore]), _clock);

        var home = EntryId.New();
        var away = EntryId.New();
        var (fixtureId, match, _) = AttachFinishedMatch(source, "P1", "QF-A", "QF-B", home, away);
        var summaries = BuildSummaries(source, match);
        ApplyProgressionOutcome.Execute(source, fixtureId, [match], [source, sf], _clock);

        var pathAfter = new ProgressionPath(
            "P1",
            ProgressionOutcome.Winner,
            ProgressionDestination.ForPopulation(final.Id));
        var afterRules = new ProgressionRules([pathAfter]);
        var diff = ProgressionRulesPathDiff.Compare(source.Regulation.ProgressionRules, afterRules);

        var verdict = ProgressionPathConsumptionWitness.EvaluateTouchedPath(
            diff.Touched[0],
            afterRules,
            source,
            sf,
            summaries);

        verdict.Should().Be(ProgressionPathConsumptionWitness.Verdict.ConsumedLoss);
    }

    [Fact]
    public void Wc1_add_only_diff_does_not_touch_consumed_sibling()
    {
        var competitionId = CompetitionId.New();
        var source = CreateKnockoutStage(competitionId, "QF", ["P1-A", "P1-B", "P2-A", "P2-B"]);
        source.ReplaceBracketPairs(
        [
            new BracketPair("P1", "P1-A", "P1-B"),
            new BracketPair("P2", "P2-A", "P2-B")
        ]);
        var destination = Stage.Create(competitionId, new StageName("SF"), SampleRegulations.Standard(), _clock);
        var pathP1 = new ProgressionPath("P1", ProgressionOutcome.Winner, ProgressionDestination.ForPopulation(destination.Id));
        var pathP2 = new ProgressionPath("P2", ProgressionOutcome.Winner, ProgressionDestination.ForPopulation(destination.Id));
        source.ReplaceProgressionRules(new ProgressionRules([pathP1, pathP2]), _clock);

        var (fixtureP1, matchP1, _) = AttachFinishedMatch(source, "P1", "P1-A", "P1-B", EntryId.New(), EntryId.New());
        ApplyProgressionOutcome.Execute(source, fixtureP1, [matchP1], [source, destination], _clock);

        var after = new ProgressionRules(
        [
            pathP1,
            pathP2,
            new ProgressionPath("P3", ProgressionOutcome.Winner, ProgressionDestination.ForPopulation(destination.Id))
        ]);
        var diff = ProgressionRulesPathDiff.Compare(source.Regulation.ProgressionRules, after);
        diff.Touched.Should().BeEmpty();

        var summaries = BuildSummaries(source, matchP1);
        ProgressionPathConsumptionWitness.EvaluateTouchedPaths(
                diff.Touched,
                after,
                source,
                new Dictionary<StageId, Stage> { [destination.Id] = destination },
                summaries)
            .Should()
            .Be(ProgressionPathConsumptionWitness.Verdict.NotConsumed);
    }

    [Fact]
    public void Wc1_no_op_rules_yields_not_consumed()
    {
        var (source, destination, _, _, _, fixtureId, match, summaries) = CreateAppliedPopulationScenario();
        ApplyProgressionOutcome.Execute(source, fixtureId, [match], [source, destination], _clock);

        var before = source.Regulation.ProgressionRules!;
        var after = before.Copy();
        var diff = ProgressionRulesPathDiff.Compare(before, after);
        diff.Touched.Should().BeEmpty();
    }

    [Fact]
    public void Wc1_path_not_applied_yields_not_consumed_on_removal()
    {
        var (source, destination, _, _, _, _, _, summaries) = CreateAppliedPopulationScenario();
        var before = source.Regulation.ProgressionRules!;

        var diff = ProgressionRulesPathDiff.Compare(before, afterRules: null);
        var verdict = ProgressionPathConsumptionWitness.EvaluateTouchedPath(
            diff.Touched[0],
            afterRules: null,
            source,
            destination,
            summaries);

        verdict.Should().Be(ProgressionPathConsumptionWitness.Verdict.NotConsumed);
    }

    private (
        Stage Source,
        Stage Destination,
        ProgressionPath Path,
        EntryId Home,
        EntryId Away,
        FixtureId FixtureId,
        Match Match,
        Dictionary<StageId, IReadOnlyList<MatchSummaryRow>> Summaries)
        CreateAppliedPopulationScenario()
    {
        var competitionId = CompetitionId.New();
        var source = CreateKnockoutStage(competitionId, "QF", ["QF-A", "QF-B"]);
        source.ReplaceBracketPairs([new BracketPair("P1", "QF-A", "QF-B")]);
        var destination = Stage.Create(competitionId, new StageName("SF"), SampleRegulations.Standard(), _clock);
        var path = new ProgressionPath(
            "P1",
            ProgressionOutcome.Winner,
            ProgressionDestination.ForPopulation(destination.Id));
        source.ReplaceProgressionRules(new ProgressionRules([path]), _clock);
        var home = EntryId.New();
        var away = EntryId.New();
        var (fixtureId, match, _) = AttachFinishedMatch(source, "P1", "QF-A", "QF-B", home, away);
        var summaries = BuildSummaries(source, match);
        return (source, destination, path, home, away, fixtureId, match, summaries);
    }

    private static Dictionary<StageId, IReadOnlyList<MatchSummaryRow>> BuildSummaries(Stage source, Match match) =>
        new()
        {
            [source.Id] =
            [
                new MatchSummaryRow(
                    match.Id,
                    source.Id,
                    match.Status,
                    match.HomeEntryId,
                    match.AwayEntryId,
                    match.Result)
            ]
        };

    private (FixtureId FixtureId, Match Match, EntryId Home) AttachFinishedMatch(
        Stage stage,
        string pairKey,
        string slotA,
        string slotB,
        EntryId home,
        EntryId away)
    {
        var fixture = stage.AddFixture(stage.Rounds[0].Id, _clock, slotA, slotB, pairKey);
        var match = Match.Create(stage.CompetitionId, stage.Id, home, away, _clock);
        stage.AttachMatch(fixture.Id, match.Id, legIndex: 1, _clock);
        match.Start(_clock);
        match.Finish(new MatchResult(ResultType.Played, new Score(1, 0)), _clock);
        return (fixture.Id, match, home);
    }

    private Stage CreateKnockoutStage(CompetitionId competitionId, string name, string[] slotKeys)
    {
        var stage = Stage.Create(competitionId, new StageName(name), SampleRegulations.Standard(), _clock);
        stage.AddRound("R1", new TieFormat(TieFormat.SingleLeg, aggregateScoring: false), _clock);
        foreach (var key in slotKeys)
        {
            stage.AddSlot(key);
        }

        return stage;
    }
}
