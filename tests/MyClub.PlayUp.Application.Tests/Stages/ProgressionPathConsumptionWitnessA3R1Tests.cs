// -----------------------------------------------------------------------
// <copyright file="ProgressionPathConsumptionWitnessA3R1Tests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Progression;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Stages;

/// <summary>
/// Per-path satisfaction attribution (no production gate).
/// </summary>
public sealed class ProgressionPathConsumptionWitnessA3R1Tests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 16, 18, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Population_path_is_satisfied_independently_after_apply_progression()
    {
        var (source, destination, path, home, away, fixtureId, match) = CreatePopulationProgressionScenario();

        ApplyProgressionOutcome.Execute(source, fixtureId, [match], [source, destination], _clock);

        var outcome = new FixtureOutcome(home, away);
        var expected = ProgressionApplier.Apply(path, path.SourcePairKey, outcome);

        DestinationSatisfaction.EvaluateProgression(
                destination,
                source.Id,
                path,
                expected.EntryId)
            .Should()
            .Be(DestinationSatisfaction.Kind.Satisfied);
    }

    [Fact]
    public void Multi_path_same_population_sibling_does_not_clear_consumption_of_removed_path()
    {
        var competitionId = CompetitionId.New();
        var source = CreateKnockoutStage(competitionId, "QF", ["P1-A", "P1-B", "P2-A", "P2-B"]);
        source.ReplaceBracketPairs(
        [
            new BracketPair("P1", "P1-A", "P1-B"),
            new BracketPair("P2", "P2-A", "P2-B")
        ]);
        var destination = Stage.Create(competitionId, new StageName("Finale"), SampleRegulations.Standard(), _clock);

        var pathP1 = new ProgressionPath(
            "P1",
            ProgressionOutcome.Winner,
            ProgressionDestination.ForPopulation(destination.Id));
        var pathP2 = new ProgressionPath(
            "P2",
            ProgressionOutcome.Winner,
            ProgressionDestination.ForPopulation(destination.Id));
        source.ReplaceProgressionRules(new ProgressionRules([pathP1, pathP2]), _clock);

        var (fixtureP1, matchP1, homeP1) = AttachFinishedMatchForPair(source, "P1", "P1-A", "P1-B");
        var (fixtureP2, matchP2, homeP2) = AttachFinishedMatchForPair(source, "P2", "P2-A", "P2-B");

        ApplyProgressionOutcome.Execute(source, fixtureP1, [matchP1], [source, destination], _clock);
        ApplyProgressionOutcome.Execute(source, fixtureP2, [matchP2], [source, destination], _clock);

        destination.CompositionEntries.Should().HaveCount(2);

        var outcomeP1 = new FixtureOutcome(homeP1, EntryId.New());
        DestinationSatisfaction.EvaluateProgression(
                destination,
                source.Id,
                pathP1,
                ProgressionApplier.Apply(pathP1, "P1", outcomeP1).EntryId)
            .Should()
            .Be(DestinationSatisfaction.Kind.Satisfied);

        var outcomeP2 = new FixtureOutcome(homeP2, EntryId.New());
        DestinationSatisfaction.EvaluateProgression(
                destination,
                source.Id,
                pathP2,
                ProgressionApplier.Apply(pathP2, "P2", outcomeP2).EntryId)
            .Should()
            .Be(DestinationSatisfaction.Kind.Satisfied);

        var afterOnlyP2 = new ProgressionRules([pathP2]);
        afterOnlyP2.Paths.Should().NotContain(p =>
            p.SourcePairKey == pathP1.SourcePairKey && p.Outcome == pathP1.Outcome);
    }

    [Fact]
    public void Undecided_fixture_yields_null_projection_and_pending_satisfaction()
    {
        var (source, destination, path, home, _, fixtureId, match) = CreatePopulationProgressionScenario();
        _ = (fixtureId, match);
        var summaries = new Dictionary<StageId, IReadOnlyList<MatchSummaryRow>>
        {
            [source.Id] = []
        };

        InboundFeedSeatResolver.ResolveProgressionEntry(
                path,
                destination,
                source,
                summaries)
            .Should()
            .BeNull();

        DestinationSatisfaction.EvaluateProgression(
                destination,
                source.Id,
                path,
                home)
            .Should()
            .Be(DestinationSatisfaction.Kind.Pending);
    }

    [Fact]
    public void Form_path_uses_fingerprint_attribution()
    {
        var competitionId = CompetitionId.New();
        var source = CreateKnockoutStage(competitionId, "QF", ["QF-A", "QF-B"]);
        source.ReplaceBracketPairs([new BracketPair("P1", "QF-A", "QF-B")]);
        var champ = Stage.Create(competitionId, new StageName("Champ"), SampleRegulations.Standard(), _clock);
        var path = new ProgressionPath(
            "P1",
            ProgressionOutcome.Winner,
            ProgressionDestination.ForForm(champ.Id));
        source.ReplaceProgressionRules(new ProgressionRules([path]), _clock);

        var home = EntryId.New();
        var away = EntryId.New();
        var (fixtureId, match, _) = AttachFinishedMatchForPair(source, "P1", "QF-A", "QF-B", home, away);

        ApplyProgressionOutcome.Execute(source, fixtureId, [match], [source, champ], _clock);

        var fingerprint = FormPathResolutionKey.FromProgression(source.Id, path);
        champ.FormPathResolutions.Should().ContainSingle(r => r.PathFingerprint == fingerprint);

        var expected = ProgressionApplier.Apply(path, "P1", new FixtureOutcome(home, away));
        DestinationSatisfaction.EvaluateProgression(champ, source.Id, path, expected.EntryId)
            .Should()
            .Be(DestinationSatisfaction.Kind.Satisfied);
    }

    private (Stage Source, Stage Destination, ProgressionPath Path, EntryId Home, EntryId Away, FixtureId FixtureId, Match Match)
        CreatePopulationProgressionScenario()
    {
        var competitionId = CompetitionId.New();
        var source = CreateKnockoutStage(competitionId, "QF", ["QF-A", "QF-B"]);
        source.ReplaceBracketPairs([new BracketPair("P1", "QF-A", "QF-B")]);
        var destination = Stage.Create(competitionId, new StageName("SF"), SampleRegulations.Standard(), _clock);
        var home = EntryId.New();
        var away = EntryId.New();
        var path = new ProgressionPath(
            "P1",
            ProgressionOutcome.Winner,
            ProgressionDestination.ForPopulation(destination.Id));
        source.ReplaceProgressionRules(new ProgressionRules([path]), _clock);
        var (fixtureId, match, _) = AttachFinishedMatchForPair(source, "P1", "QF-A", "QF-B", home, away);
        return (source, destination, path, home, away, fixtureId, match);
    }

    private (FixtureId FixtureId, Match Match, EntryId Home) AttachFinishedMatchForPair(
        Stage stage,
        string pairKey,
        string slotA,
        string slotB,
        EntryId? home = null,
        EntryId? away = null)
    {
        var homeId = home ?? EntryId.New();
        var awayId = away ?? EntryId.New();
        var fixture = stage.AddFixture(stage.Rounds[0].Id, _clock, slotA, slotB, pairKey);
        var match = Match.Create(stage.CompetitionId, stage.Id, homeId, awayId, _clock);
        stage.AttachMatch(fixture.Id, match.Id, legIndex: 1, _clock);
        match.Start(_clock);
        match.Finish(new MatchResult(ResultType.Played, new Score(1, 0)), _clock);
        return (fixture.Id, match, homeId);
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
