// -----------------------------------------------------------------------
// <copyright file="ProgressionPathProductionParticipationWitnessTests.cs" company="Stéphane ANDRE">
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

public sealed class ProgressionPathProductionParticipationWitnessTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 16, 19, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Wc2_no_attached_production_is_not_applicable()
    {
        var (source, destination, path, _, _, fixtureId, match, summaries) = CreatePopulationScenario();
        ApplyProgressionOutcome.Execute(source, fixtureId, [match], [source, destination], _clock);

        var verdict = ProgressionPathProductionParticipationWitness.EvaluateTouchedPath(
            path,
            source,
            destination,
            [source, destination],
            MaterializedParticipantSnapshots.FromMatches([]),
            summaries);

        verdict.Should().Be(ProgressionPathProductionParticipationWitness.Verdict.NotApplicable);
    }

    [Fact]
    public void Wc2_progressed_entry_on_attached_downstream_match_is_participating()
    {
        var (source, destination, path, home, _, fixtureId, match, summaries) = CreatePopulationScenario();
        ApplyProgressionOutcome.Execute(source, fixtureId, [match], [source, destination], _clock);

        var other = EntryId.New();
        var downstream = Match.Create(destination.CompetitionId, destination.Id, home, other, _clock);
        var sfFixture = destination.AddFixture(destination.Rounds[0].Id, _clock, "SF-A", "SF-B", "P1");
        destination.AttachMatch(sfFixture.Id, downstream.Id, legIndex: 1, _clock);
        downstream.Start(_clock);

        var verdict = ProgressionPathProductionParticipationWitness.EvaluateTouchedPath(
            path,
            source,
            destination,
            [source, destination],
            MaterializedParticipantSnapshots.FromMatches([downstream]),
            summaries);

        verdict.Should().Be(ProgressionPathProductionParticipationWitness.Verdict.Participating);
    }

    [Fact]
    public void Wc2_attached_production_without_path_entry_is_not_participating()
    {
        var (source, destination, path, _, _, _, _, summaries) = CreatePopulationScenario();
        var alienA = EntryId.New();
        var alienB = EntryId.New();
        var downstream = Match.Create(destination.CompetitionId, destination.Id, alienA, alienB, _clock);
        var sfFixture = destination.AddFixture(destination.Rounds[0].Id, _clock, "SF-A", "SF-B", "P1");
        destination.AttachMatch(sfFixture.Id, downstream.Id, legIndex: 1, _clock);

        var verdict = ProgressionPathProductionParticipationWitness.EvaluateTouchedPath(
            path,
            source,
            destination,
            [source, destination],
            MaterializedParticipantSnapshots.FromMatches([downstream]),
            summaries);

        verdict.Should().Be(ProgressionPathProductionParticipationWitness.Verdict.NotParticipating);
    }

    [Fact]
    public void Wc2_attached_production_with_undecidable_replay_is_ambiguous()
    {
        var (source, destination, path, _, _, _, _, _) = CreatePopulationScenario();
        var alienA = EntryId.New();
        var alienB = EntryId.New();
        var downstream = Match.Create(destination.CompetitionId, destination.Id, alienA, alienB, _clock);
        var sfFixture = destination.AddFixture(destination.Rounds[0].Id, _clock, "SF-A", "SF-B", "P1");
        destination.AttachMatch(sfFixture.Id, downstream.Id, legIndex: 1, _clock);

        var verdict = ProgressionPathProductionParticipationWitness.EvaluateTouchedPath(
            path,
            source,
            destination,
            [source, destination],
            MaterializedParticipantSnapshots.FromMatches([downstream]),
            new Dictionary<StageId, IReadOnlyList<MatchSummaryRow>>());

        verdict.Should().Be(ProgressionPathProductionParticipationWitness.Verdict.ParticipationAmbiguous);
    }

    [Fact]
    public void Wc2_form_resolution_aligned_with_replay_and_attached_match_is_participating()
    {
        var (source, destination, path, home, _, fixtureId, match, summaries) = CreateFormScenario();
        ApplyProgressionOutcome.Execute(source, fixtureId, [match], [source, destination], _clock);

        var other = EntryId.New();
        var downstream = Match.Create(destination.CompetitionId, destination.Id, home, other, _clock);
        var finalFixture = destination.AddFixture(destination.Rounds[0].Id, _clock, "F-A", "F-B", "P1");
        destination.AttachMatch(finalFixture.Id, downstream.Id, legIndex: 1, _clock);

        ProgressionPathProductionParticipationWitness.EvaluateTouchedPath(
                path,
                source,
                destination,
                [source, destination],
                MaterializedParticipantSnapshots.FromMatches([downstream]),
                summaries)
            .Should()
            .Be(ProgressionPathProductionParticipationWitness.Verdict.Participating);
    }

    [Fact]
    public void Wc2_form_resolution_not_aligned_with_replay_is_not_participating()
    {
        var (source, destination, path, _, _, fixtureId, match, _) = CreateFormScenario();
        ApplyProgressionOutcome.Execute(source, fixtureId, [match], [source, destination], _clock);

        var summariesAwayWins = new Dictionary<StageId, IReadOnlyList<MatchSummaryRow>>
        {
            [source.Id] =
            [
                new MatchSummaryRow(
                    match.Id,
                    source.Id,
                    match.Status,
                    match.HomeEntryId,
                    match.AwayEntryId,
                    new MatchResult(ResultType.Played, new Score(0, 1)))
            ]
        };

        var alienA = EntryId.New();
        var alienB = EntryId.New();
        var downstream = Match.Create(destination.CompetitionId, destination.Id, alienA, alienB, _clock);
        var finalFixture = destination.AddFixture(destination.Rounds[0].Id, _clock, "F-A", "F-B", "P1");
        destination.AttachMatch(finalFixture.Id, downstream.Id, legIndex: 1, _clock);

        ProgressionPathProductionParticipationWitness.EvaluateTouchedPath(
                path,
                source,
                destination,
                [source, destination],
                MaterializedParticipantSnapshots.FromMatches([downstream]),
                summariesAwayWins)
            .Should()
            .Be(ProgressionPathProductionParticipationWitness.Verdict.NotParticipating);
    }

    [Fact]
    public void Wc2_form_without_resolution_with_attached_production_is_ambiguous()
    {
        var (source, destination, path, _, _, _, _, summaries) = CreateFormScenario();
        var downstream = Match.Create(destination.CompetitionId, destination.Id, EntryId.New(), EntryId.New(), _clock);
        var finalFixture = destination.AddFixture(destination.Rounds[0].Id, _clock, "F-A", "F-B", "P1");
        destination.AttachMatch(finalFixture.Id, downstream.Id, legIndex: 1, _clock);

        ProgressionPathProductionParticipationWitness.EvaluateTouchedPath(
                path,
                source,
                destination,
                [source, destination],
                MaterializedParticipantSnapshots.FromMatches([downstream]),
                summaries)
            .Should()
            .Be(ProgressionPathProductionParticipationWitness.Verdict.ParticipationAmbiguous);
    }

    [Fact]
    public void Wc2_slot_explicit_feed_and_entry_on_attached_match_is_participating()
    {
        var (source, destination, path, home, _, fixtureId, match, summaries) = CreateSlotScenario();
        ApplyProgressionOutcome.Execute(source, fixtureId, [match], [source, destination], _clock);

        var other = EntryId.New();
        var downstream = Match.Create(destination.CompetitionId, destination.Id, home, other, _clock);
        var sfFixture = destination.AddFixture(destination.Rounds[0].Id, _clock, "SF1-A", "SF1-B", "P1");
        destination.AttachMatch(sfFixture.Id, downstream.Id, legIndex: 1, _clock);

        ProgressionPathProductionParticipationWitness.EvaluateTouchedPath(
                path,
                source,
                destination,
                [source, destination],
                MaterializedParticipantSnapshots.FromMatches([downstream]),
                summaries)
            .Should()
            .Be(ProgressionPathProductionParticipationWitness.Verdict.Participating);
    }

    [Fact]
    public void Wc2_slot_feed_matches_but_entry_not_on_attached_production_is_not_participating()
    {
        var (source, destination, path, _, _, fixtureId, match, summaries) = CreateSlotScenario();
        ApplyProgressionOutcome.Execute(source, fixtureId, [match], [source, destination], _clock);

        var alienA = EntryId.New();
        var alienB = EntryId.New();
        var downstream = Match.Create(destination.CompetitionId, destination.Id, alienA, alienB, _clock);
        var sfFixture = destination.AddFixture(destination.Rounds[0].Id, _clock, "SF1-A", "SF1-B", "P1");
        destination.AttachMatch(sfFixture.Id, downstream.Id, legIndex: 1, _clock);

        ProgressionPathProductionParticipationWitness.EvaluateTouchedPath(
                path,
                source,
                destination,
                [source, destination],
                MaterializedParticipantSnapshots.FromMatches([downstream]),
                summaries)
            .Should()
            .Be(ProgressionPathProductionParticipationWitness.Verdict.NotParticipating);
    }

    [Fact]
    public void Wc2_slot_multiple_feeds_with_attached_production_is_ambiguous()
    {
        var (source, destination, path, _, _, _, _, summaries) = CreateSlotScenario();
        var direct = EntryId.New();
        destination.ReplaceAffectationAuthoring([direct], _clock);
        destination.AssignEntryToSlot("SF1-A", direct);

        var downstream = Match.Create(destination.CompetitionId, destination.Id, direct, EntryId.New(), _clock);
        var sfFixture = destination.AddFixture(destination.Rounds[0].Id, _clock, "SF1-A", "SF1-B", "P1");
        destination.AttachMatch(sfFixture.Id, downstream.Id, legIndex: 1, _clock);

        ProgressionPathProductionParticipationWitness.EvaluateTouchedPath(
                path,
                source,
                destination,
                [source, destination],
                MaterializedParticipantSnapshots.FromMatches([downstream]),
                summaries)
            .Should()
            .Be(ProgressionPathProductionParticipationWitness.Verdict.ParticipationAmbiguous);
    }

    [Fact]
    public void Wc2_add_only_diff_has_empty_touched_scope()
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

        var after = new ProgressionRules([pathP1, pathP2, new ProgressionPath("P3", ProgressionOutcome.Winner, ProgressionDestination.ForPopulation(destination.Id))]);
        var diff = ProgressionRulesPathDiff.Compare(source.Regulation.ProgressionRules, after);

        diff.Touched.Should().BeEmpty();
        diff.Added.Should().ContainSingle(p => p.SourcePairKey == "P3");
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

    private (
        Stage Source,
        Stage Destination,
        ProgressionPath Path,
        EntryId Home,
        EntryId Away,
        FixtureId FixtureId,
        Match Match,
        Dictionary<StageId, IReadOnlyList<MatchSummaryRow>> Summaries)
        CreateFormScenario()
    {
        var competitionId = CompetitionId.New();
        var source = CreateKnockoutStage(competitionId, "QF", ["QF-A", "QF-B"]);
        source.ReplaceBracketPairs([new BracketPair("P1", "QF-A", "QF-B")]);
        var destination = CreateKnockoutStage(competitionId, "Final", ["F-A", "F-B"]);
        destination.ReplaceBracketPairs([new BracketPair("P1", "F-A", "F-B")]);
        var path = new ProgressionPath(
            "P1",
            ProgressionOutcome.Winner,
            ProgressionDestination.ForForm(destination.Id));
        source.ReplaceProgressionRules(new ProgressionRules([path]), _clock);
        var home = EntryId.New();
        var away = EntryId.New();
        var fixture = source.AddFixture(source.Rounds[0].Id, _clock, "QF-A", "QF-B", "P1");
        var match = Match.Create(source.CompetitionId, source.Id, home, away, _clock);
        source.AttachMatch(fixture.Id, match.Id, legIndex: 1, _clock);
        match.Start(_clock);
        match.Finish(new MatchResult(ResultType.Played, new Score(1, 0)), _clock);
        var summaries = BuildSummaries(source, match);
        return (source, destination, path, home, away, fixture.Id, match, summaries);
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
        CreateSlotScenario()
    {
        var competitionId = CompetitionId.New();
        var source = CreateKnockoutStage(competitionId, "QF", ["QF-A", "QF-B"]);
        source.ReplaceBracketPairs([new BracketPair("P1", "QF-A", "QF-B")]);
        var destination = CreateKnockoutStage(competitionId, "SF", ["SF1-A", "SF1-B"]);
        destination.ReplaceBracketPairs([new BracketPair("P1", "SF1-A", "SF1-B")]);
        var path = new ProgressionPath(
            "P1",
            ProgressionOutcome.Winner,
            ProgressionDestination.ForSlot(destination.Id, "SF1-A"));
        source.ReplaceProgressionRules(new ProgressionRules([path]), _clock);
        var home = EntryId.New();
        var away = EntryId.New();
        var fixture = source.AddFixture(source.Rounds[0].Id, _clock, "QF-A", "QF-B", "P1");
        var match = Match.Create(source.CompetitionId, source.Id, home, away, _clock);
        source.AttachMatch(fixture.Id, match.Id, legIndex: 1, _clock);
        match.Start(_clock);
        match.Finish(new MatchResult(ResultType.Played, new Score(1, 0)), _clock);
        var summaries = BuildSummaries(source, match);
        return (source, destination, path, home, away, fixture.Id, match, summaries);
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
        CreatePopulationScenario()
    {
        var competitionId = CompetitionId.New();
        var source = CreateKnockoutStage(competitionId, "QF", ["QF-A", "QF-B"]);
        source.ReplaceBracketPairs([new BracketPair("P1", "QF-A", "QF-B")]);
        var destination = CreateKnockoutStage(competitionId, "SF", ["SF-A", "SF-B"]);
        destination.ReplaceBracketPairs([new BracketPair("P1", "SF-A", "SF-B")]);
        var path = new ProgressionPath(
            "P1",
            ProgressionOutcome.Winner,
            ProgressionDestination.ForPopulation(destination.Id));
        source.ReplaceProgressionRules(new ProgressionRules([path]), _clock);
        var home = EntryId.New();
        var away = EntryId.New();
        var fixture = source.AddFixture(source.Rounds[0].Id, _clock, "QF-A", "QF-B", "P1");
        var match = Match.Create(source.CompetitionId, source.Id, home, away, _clock);
        source.AttachMatch(fixture.Id, match.Id, legIndex: 1, _clock);
        match.Start(_clock);
        match.Finish(new MatchResult(ResultType.Played, new Score(1, 0)), _clock);
        var summaries = new Dictionary<StageId, IReadOnlyList<MatchSummaryRow>>
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
        return (source, destination, path, home, away, fixture.Id, match, summaries);
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
