// -----------------------------------------------------------------------
// <copyright file="CompetitionOutcomePlacementScenariosTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Reads;

/// <summary>
/// Placement-award reference scenarios — PlacementAwardRules → ResolvePlacementAwards → CompetitionOutcome.
/// Proves amateur placement / consolation shapes without Host or SPA.
/// Groups → slots (qualification/progression) are out of scope here; awards are the seam under test.
/// </summary>
public sealed class CompetitionOutcomePlacementScenariosTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 29, 14, 0, 0, TimeSpan.Zero));

    private static string PathKey(Fixture fixture) =>
        fixture.BracketPairKey ?? throw new InvalidOperationException("missing BracketPairKey");

    /// <summary>
    /// F1 — Amateur 2×6 shape: terminal placement fixtures award ranks 1–12.
    /// (Groups feed slots via Qualification/Progression elsewhere; this scenario owns the award→outcome seam.)
    /// </summary>
    [Fact]
    public void F1_two_groups_of_six_shape_projects_full_1_to_12_outcome()
    {
        var scenario = BuildPlacementStage(
            "Amateur 2x6",
            teamCount: 12,
            [
                ("Final", 1, 2),
                ("Bronze", 3, 4),
                ("Place5", 5, 6),
                ("Place7", 7, 8),
                ("Place9", 9, 10),
                ("Place11", 11, 12)
            ]);

        FinishAllPlacementFixtures(scenario, homeAlwaysWins: true);
        CompleteCompetition(scenario);

        var view = OverviewAssembler.Assemble(
            scenario.Competition,
            [scenario.Stage],
            new Dictionary<StageId, IReadOnlyList<Match>> { [scenario.Stage.Id] = scenario.Matches });

        view.CompetitionOutcome.Should().NotBeNull();
        view.CompetitionOutcome.Places.Should().HaveCount(12);
        view.CompetitionOutcome.Places.Select(p => p.Rank).Should().Equal(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12);

        // Home always wins each pair → odd ranks = Team01,03,… even = Team02,04,…
        for (var rank = 1; rank <= 12; rank++)
        {
            view.CompetitionOutcome.Places[rank - 1].DisplayName.Should().Be($"Team{rank:00}");
            view.CompetitionOutcome.Places[rank - 1].EntryId.Should().Be(scenario.Entries[rank - 1].Value);
        }

        // Same truth via pure resolver (no Overview lifecycle gate).
        var resolved = ResolvePlacementAwards.Execute(
            [scenario.Stage],
            new Dictionary<StageId, IReadOnlyList<Match>> { [scenario.Stage.Id] = scenario.Matches });
        resolved.Select(i => i.Rank).Should().Equal(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12);
    }

    /// <summary>
    /// F2 — Cup 16 + consolation: eight terminal fixtures award ranks 1–16 via explicit PlacementAwardPath,
    /// never via SlotKey heuristics (Consolante3 ≠ rank).
    /// </summary>
    [Fact]
    public void F2_cup16_consolantes_projects_full_1_to_16_from_explicit_awards()
    {
        var scenario = BuildPlacementStage(
            "Cup16 consolantes",
            teamCount: 16,
            [
                ("Final", 1, 2),
                ("Bronze", 3, 4),
                ("Place5", 5, 6),
                ("Place7", 7, 8),
                ("Consolante9", 9, 10),
                ("Consolante11", 11, 12),
                ("Consolante13", 13, 14),
                ("Consolante15", 15, 16)
            ]);

        // Opaque slot keys must not participate in outcome derivation.
        scenario.Stage.AddSlot("Consolante3");
        scenario.Stage.AddSlot("ConsolanteMagic");

        FinishAllPlacementFixtures(scenario, homeAlwaysWins: true);
        CompleteCompetition(scenario);

        var view = OverviewAssembler.Assemble(
            scenario.Competition,
            [scenario.Stage],
            new Dictionary<StageId, IReadOnlyList<Match>> { [scenario.Stage.Id] = scenario.Matches });

        view.CompetitionOutcome.Should().NotBeNull();
        view.CompetitionOutcome!.Places.Should().HaveCount(16);
        view.CompetitionOutcome.Places.Select(p => p.Rank).Should().Equal(
            1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16);

        // Explicit award for Consolante13 fixture → ranks 13/14, not slot name.
        view.CompetitionOutcome.Places[12].DisplayName.Should().Be("Team13");
        view.CompetitionOutcome.Places[13].DisplayName.Should().Be("Team14");
        scenario.Stage.FindSlot("Consolante3")!.EntryId.Should().BeNull();
    }

    /// <summary>
    /// F3 — Partial outcome: only Final decided → ranks 1–2; finishing Bronze fills 3–4; never invents 5+.
    /// </summary>
    [Fact]
    public void F3_partial_outcome_grows_only_when_fixtures_are_decided()
    {
        var scenario = BuildPlacementStage(
            "Partial placements",
            teamCount: 8,
            [
                ("Final", 1, 2),
                ("Bronze", 3, 4),
                ("Place5", 5, 6),
                ("Place7", 7, 8)
            ]);

        // Attach all matches while Draft (structure lock); finish progressively while Running.
        MaterializeAllFixturesUnfinished(scenario);
        PrepareAndStart(scenario);

        FinishAttachedMatch(scenario, fixtureIndex: 0, homeWins: true);
        scenario.Competition.Complete(CompletionMode.Normal, _clock);

        var matchesByStage = new Dictionary<StageId, IReadOnlyList<Match>>
        {
            [scenario.Stage.Id] = scenario.Matches
        };

        var partial = OverviewAssembler.Assemble(scenario.Competition, [scenario.Stage], matchesByStage);
        partial.CompetitionOutcome.Should().NotBeNull();
        partial.CompetitionOutcome!.Places.Select(p => p.Rank).Should().Equal(1, 2);
        partial.CompetitionOutcome.Places.Should().NotContain(p => p.Rank >= 3);

        FinishAttachedMatch(scenario, fixtureIndex: 1, homeWins: true);
        matchesByStage[scenario.Stage.Id] = scenario.Matches;

        var afterBronze = OverviewAssembler.Assemble(scenario.Competition, [scenario.Stage], matchesByStage);
        afterBronze.CompetitionOutcome!.Places.Select(p => p.Rank).Should().Equal(1, 2, 3, 4);
        afterBronze.CompetitionOutcome.Places.Should().NotContain(p => p.Rank >= 5);

        FinishAttachedMatch(scenario, fixtureIndex: 2, homeWins: true);
        FinishAttachedMatch(scenario, fixtureIndex: 3, homeWins: true);
        matchesByStage[scenario.Stage.Id] = scenario.Matches;

        var full = OverviewAssembler.Assemble(scenario.Competition, [scenario.Stage], matchesByStage);
        full.CompetitionOutcome!.Places.Select(p => p.Rank).Should().Equal(1, 2, 3, 4, 5, 6, 7, 8);
    }

    private PlacementScenario BuildPlacementStage(
        string competitionName,
        int teamCount,
        (string Label, int WinnerRank, int LoserRank)[] fixtures)
    {
        var competition = Competition.Create(
            new CompetitionName(competitionName),
            SampleRegulations.Standard(),
            _clock);

        var entries = new List<EntryId>(teamCount);
        for (var i = 1; i <= teamCount; i++)
        {
            entries.Add(competition.AddEntry(TeamId.New(), $"Team{i:00}", _clock).Id);
        }

        var stage = Stage.Create(
            competition.Id,
            new StageName("Placements"),
            SampleRegulations.Standard(),
            _clock);
        var round = stage.AddRound("Placement round", _clock);
        competition.AddStage(stage.Id, _clock);

        var awardPaths = new List<PlacementAwardPath>();
        var fixtureIds = new List<FixtureId>();
        var pairs = new List<BracketPair>(fixtures.Length);

        for (var i = 0; i < fixtures.Length; i++)
        {
            var slotA = $"S{i + 1}-A";
            var slotB = $"S{i + 1}-B";
            var pairKey = $"P{i + 1}";
            stage.AddSlot(slotA);
            stage.AddSlot(slotB);
            pairs.Add(new BracketPair(pairKey, slotA, slotB));
        }

        stage.ReplaceBracketPairs(pairs);

        for (var i = 0; i < fixtures.Length; i++)
        {
            var (_, winnerRank, loserRank) = fixtures[i];
            var pair = pairs[i];
            var fixture = stage.AddFixture(round.Id, _clock, pair.SlotAKey, pair.SlotBKey, pair.PairKey);
            fixtureIds.Add(fixture.Id);
            awardPaths.Add(new PlacementAwardPath(PathKey(fixture), ProgressionOutcome.Winner, winnerRank));
            awardPaths.Add(new PlacementAwardPath(PathKey(fixture), ProgressionOutcome.Loser, loserRank));
        }

        stage.ReplacePlacementAwardRules(new PlacementAwardRules(awardPaths), _clock);

        return new PlacementScenario(competition, stage, entries, fixtureIds, []);
    }

    private void FinishAllPlacementFixtures(PlacementScenario scenario, bool homeAlwaysWins)
    {
        for (var i = 0; i < scenario.FixtureIds.Count; i++)
        {
            FinishFixture(scenario, i, homeWins: homeAlwaysWins);
        }
    }

    private void MaterializeAllFixturesUnfinished(PlacementScenario scenario)
    {
        for (var i = 0; i < scenario.FixtureIds.Count; i++)
        {
            var home = scenario.Entries[i * 2];
            var away = scenario.Entries[(i * 2) + 1];
            var fixtureId = scenario.FixtureIds[i];
            OccupyFixtureSlots(scenario.Stage, fixtureId, home, away);
            var match = Match.Create(scenario.Competition.Id, scenario.Stage.Id, home, away, _clock);
            scenario.Stage.AttachMatch(fixtureId, match.Id, legIndex: 1, _clock);
            scenario.Matches.Add(match);
        }
    }

    private void FinishFixture(PlacementScenario scenario, int fixtureIndex, bool homeWins)
    {
        var home = scenario.Entries[fixtureIndex * 2];
        var away = scenario.Entries[(fixtureIndex * 2) + 1];
        var fixtureId = scenario.FixtureIds[fixtureIndex];

        var existing = scenario.Stage.GetFixture(fixtureId);
        if (existing.Attachments.Count > 0)
        {
            return;
        }

        OccupyFixtureSlots(scenario.Stage, fixtureId, home, away);
        var match = Match.Create(scenario.Competition.Id, scenario.Stage.Id, home, away, _clock);
        match.Start(_clock);
        match.Finish(
            new MatchResult(ResultType.Played, homeWins ? new Score(1, 0) : new Score(0, 1)),
            _clock);
        scenario.Stage.AttachMatch(fixtureId, match.Id, legIndex: 1, _clock);
        scenario.Matches.Add(match);
    }

    private void OccupyFixtureSlots(Stage stage, FixtureId fixtureId, EntryId home, EntryId away)
    {
        var fixture = stage.GetFixture(fixtureId);
        if (fixture.SlotAKey is not null)
        {
            stage.ApplyResolvedEntry(fixture.SlotAKey, home, _clock);
        }

        if (fixture.SlotBKey is not null)
        {
            stage.ApplyResolvedEntry(fixture.SlotBKey, away, _clock);
        }
    }

    private void FinishAttachedMatch(PlacementScenario scenario, int fixtureIndex, bool homeWins)
    {
        var fixtureId = scenario.FixtureIds[fixtureIndex];
        var matchId = scenario.Stage.GetFixture(fixtureId).Attachments[0].MatchId;
        var match = scenario.Matches.Single(m => m.Id.Equals(matchId));
        switch (match.Status)
        {
            case MatchStatus.Finished:
                return;
            case MatchStatus.Scheduled:
                match.Start(_clock);
                break;
            case MatchStatus.Live:
            case MatchStatus.Postponed:
            case MatchStatus.Cancelled:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(scenario));
        }

        match.Finish(
            new MatchResult(ResultType.Played, homeWins ? new Score(1, 0) : new Score(0, 1)),
            _clock);
    }

    private void PrepareAndStart(PlacementScenario scenario)
    {
        scenario.Competition.Prepare(_clock);
        scenario.Competition.Start(_clock);
        scenario.Stage.Prepare(_clock);
        scenario.Stage.Start(_clock);
    }

    private void CompleteCompetition(PlacementScenario scenario, bool completeStage = true)
    {
        if (scenario.Competition.Status == CompetitionStatus.Draft)
        {
            scenario.Competition.Prepare(_clock);
            scenario.Competition.Start(_clock);
        }

        if (scenario.Stage.Status == StageStatus.Draft)
        {
            scenario.Stage.Prepare(_clock);
            scenario.Stage.Start(_clock);
        }

        if (completeStage
            && scenario.Stage.Status is StageStatus.Running or StageStatus.Suspended)
        {
            scenario.Stage.Complete(_clock);
        }

        if (scenario.Competition.Status is CompetitionStatus.Running or CompetitionStatus.Suspended)
        {
            scenario.Competition.Complete(CompletionMode.Normal, _clock);
        }
    }

    private sealed class PlacementScenario(
        Competition competition,
        Stage stage,
        List<EntryId> entries,
        List<FixtureId> fixtureIds,
        List<Match> matches)
    {
        public Competition Competition { get; } = competition;

        public Stage Stage { get; } = stage;

        public List<EntryId> Entries { get; } = entries;

        public List<FixtureId> FixtureIds { get; } = fixtureIds;

        public List<Match> Matches { get; } = matches;
    }
}
