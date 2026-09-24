// -----------------------------------------------------------------------
// <copyright file="ResolvePlacementAwardsTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Stages;

public sealed class ResolvePlacementAwardsTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 29, 12, 0, 0, TimeSpan.Zero));

    private static string PathKey(Fixture fixture) =>
        fixture.BracketPairKey ?? fixture.Id.Value.ToString("N");

    [Fact]
    public void Execute_final_fixture_awards_ranks_1_and_2()
    {
        var (stage, _, winner, loser, match) = CreateAwardStage(
            winnerRank: 1,
            loserRank: 2,
            homeGoals: 2,
            awayGoals: 0);

        var results = ResolvePlacementAwards.Execute(
            [stage],
            new Dictionary<StageId, IReadOnlyList<Match>> { [stage.Id] = [match] });

        results.Should().HaveCount(2);
        results[0].Should().Be(new Domain.Placement.FinalPlacementInstruction(1, winner));
        results[1].Should().Be(new Domain.Placement.FinalPlacementInstruction(2, loser));
    }

    [Fact]
    public void Execute_skips_undecided_fixture_leaving_partial_ranks()
    {
        var competitionId = CompetitionId.New();
        var stage = Stage.Create(competitionId, new StageName("Cup"), SampleRegulations.Standard(), _clock);
        var round = stage.AddRound("Finals", _clock);
        var final = stage.AddFixture(round.Id, _clock);
        var bronze = stage.AddFixture(round.Id, _clock);

        var home = EntryId.New();
        var away = EntryId.New();
        var finalMatch = Match.Create(competitionId, stage.Id, home, away, _clock);
        finalMatch.Start(_clock);
        finalMatch.Finish(new MatchResult(ResultType.Played, new Score(1, 0)), _clock);
        stage.AttachMatch(final.Id, finalMatch.Id, legIndex: 1, _clock);

        // Bronze fixture has award rules but no finished match → ranks 3/4 absent.
        stage.ReplacePlacementAwardRules(
            new PlacementAwardRules(
            [
                new PlacementAwardPath(PathKey(final), ProgressionOutcome.Winner, 1),
                new PlacementAwardPath(PathKey(final), ProgressionOutcome.Loser, 2),
                new PlacementAwardPath(PathKey(bronze), ProgressionOutcome.Winner, 3),
                new PlacementAwardPath(PathKey(bronze), ProgressionOutcome.Loser, 4)
            ]),
            _clock);

        var results = ResolvePlacementAwards.Execute(
            [stage],
            new Dictionary<StageId, IReadOnlyList<Match>> { [stage.Id] = [finalMatch] });

        results.Should().HaveCount(2);
        results.Select(r => r.Rank).Should().Equal(1, 2);
        results.Should().NotContain(r => r.Rank == 3 || r.Rank == 4);
    }

    [Fact]
    public void Execute_returns_empty_when_no_placement_rules()
    {
        var stage = Stage.Create(
            CompetitionId.New(),
            new StageName("Cup"),
            SampleRegulations.Standard(),
            _clock);
        stage.AddRound("Final", _clock);

        ResolvePlacementAwards.Execute(
            [stage],
            new Dictionary<StageId, IReadOnlyList<Match>>()).Should().BeEmpty();
    }

    [Fact]
    public void ExecuteForFixture_is_pure_no_slot_mutation()
    {
        var (stage, fixtureId, winner, _, match) = CreateAwardStage(1, 2, 3, 1);
        stage.AddSlot("Unused");

        var results = ResolvePlacementAwards.ExecuteForFixture(stage, fixtureId, [match]);

        results.Should().ContainSingle(r => r.Rank == 1 && r.EntryId.Equals(winner));
        stage.FindSlot("Unused")!.EntryId.Should().BeNull();
    }

    private (Stage Stage, FixtureId FixtureId, EntryId Winner, EntryId Loser, Match Match) CreateAwardStage(
        int winnerRank,
        int loserRank,
        int homeGoals,
        int awayGoals)
    {
        var competitionId = CompetitionId.New();
        var stage = Stage.Create(competitionId, new StageName("Final"), SampleRegulations.Standard(), _clock);
        var round = stage.AddRound("Final", _clock);
        var fixture = stage.AddFixture(round.Id, _clock);
        var home = EntryId.New();
        var away = EntryId.New();
        var match = Match.Create(competitionId, stage.Id, home, away, _clock);
        match.Start(_clock);
        match.Finish(new MatchResult(ResultType.Played, new Score(homeGoals, awayGoals)), _clock);
        stage.AttachMatch(fixture.Id, match.Id, legIndex: 1, _clock);
        stage.ReplacePlacementAwardRules(
            new PlacementAwardRules(
            [
                new PlacementAwardPath(PathKey(fixture), ProgressionOutcome.Winner, winnerRank),
                new PlacementAwardPath(PathKey(fixture), ProgressionOutcome.Loser, loserRank)
            ]),
            _clock);

        var winner = homeGoals > awayGoals ? home : away;
        var loser = homeGoals > awayGoals ? away : home;
        return (stage, fixture.Id, winner, loser, match);
    }
}
