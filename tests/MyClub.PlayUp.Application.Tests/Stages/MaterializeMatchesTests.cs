// -----------------------------------------------------------------------
// <copyright file="MaterializeMatchesTests.cs" company="Stéphane ANDRE">
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
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Stages;

public sealed class MaterializeMatchesTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 16, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Championship_round_robin_creates_expected_matches_and_is_idempotent()
    {
        var competition = CreateCompetition.Execute("Champ", _clock);
        AddEntry.Execute(competition, "A", _clock);
        AddEntry.Execute(competition, "B", _clock);
        AddEntry.Execute(competition, "C", _clock);
        AddEntry.Execute(competition, "D", _clock);
        var configured = ConfigureStructure.Execute(
            competition,
            null,
            StructureIntent.Championship(),
            _clock);

        configured.Stage.MatchGenerationFormat.Should().Be(MatchGenerationFormat.SingleRoundRobin);

        var first = MaterializeMatches.Execute(competition, configured.Stage, [], _clock);
        first.CreatedMatches.Should().HaveCount(6); // 4*3/2
        first.AlreadyComplete.Should().BeFalse();

        var second = MaterializeMatches.Execute(
            competition,
            configured.Stage,
            first.CreatedMatches,
            _clock);
        second.CreatedMatches.Should().BeEmpty();
        second.AlreadyComplete.Should().BeTrue();
        second.AttachedMatchIds.Should().HaveCount(6);

        var view = StructureViewAssembler.Assemble(competition, [configured.Stage]);
        view.Readiness.ReadyForMatchOperation.Should().BeTrue();
        view.Readiness.AttachedMatchCount.Should().Be(6);
        view.Readiness.ReadyForSchedule.Should().BeTrue();
        view.Structure.MatchGenerationFormat.Should().Be(MatchGenerationFormat.SingleRoundRobin);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public void Championship_double_round_robin_satisfies_pair_mirror_invariants(int teamCount)
    {
        var competition = CreateCompetition.Execute($"Double-{teamCount}", _clock);
        for (var i = 0; i < teamCount; i++)
        {
            AddEntry.Execute(competition, $"T{i}", _clock);
        }

        var configured = ConfigureStructure.Execute(
            competition,
            null,
            StructureIntent.Championship(matchGenerationFormat: MatchGenerationFormat.DoubleRoundRobin),
            _clock);

        configured.Stage.MatchGenerationFormat.Should().Be(MatchGenerationFormat.DoubleRoundRobin);

        var result = MaterializeMatches.Execute(competition, configured.Stage, [], _clock);
        var matches = result.CreatedMatches;
        matches.Should().HaveCount(teamCount * (teamCount - 1));

        var expectedMatchdays = teamCount % 2 == 0 ? 2 * (teamCount - 1) : 2 * teamCount;
        configured.Stage.Matchdays.Should().HaveCount(expectedMatchdays);

        AssertPairMirrorInvariants(matches, teamCount);

        var again = MaterializeMatches.Execute(competition, configured.Stage, matches, _clock);
        again.CreatedMatches.Should().BeEmpty();
        again.AlreadyComplete.Should().BeTrue();
        again.AttachedMatchIds.Should().HaveCount(teamCount * (teamCount - 1));
    }

    [Fact]
    public void Championship_double_round_robin_is_deterministic()
    {
        var entries = Enumerable.Range(0, 5)
            .Select(i => new EntryId(Guid.Parse($"00000000-0000-0000-0000-{i + 1:D12}")))
            .ToArray();

        var first = MaterializeMatches.BuildRoundRobinRounds(entries, MatchGenerationFormat.DoubleRoundRobin);
        var second = MaterializeMatches.BuildRoundRobinRounds(entries, MatchGenerationFormat.DoubleRoundRobin);

        first.SelectMany(round => round.Select(pair => $"{pair.Home.Value:N}>{pair.Away.Value:N}"))
            .Should()
            .Equal(second.SelectMany(round => round.Select(pair => $"{pair.Home.Value:N}>{pair.Away.Value:N}")));
        first.Should().HaveCount(10); // 2N for odd N
    }

    [Fact]
    public void Championship_double_round_robin_completes_missing_return_leg()
    {
        var competition = CreateCompetition.Execute("Partial", _clock);
        AddEntry.Execute(competition, "A", _clock);
        AddEntry.Execute(competition, "B", _clock);
        var configured = ConfigureStructure.Execute(
            competition,
            null,
            StructureIntent.Championship(matchGenerationFormat: MatchGenerationFormat.DoubleRoundRobin),
            _clock);

        var full = MaterializeMatches.Execute(competition, configured.Stage, [], _clock);
        full.CreatedMatches.Should().HaveCount(2);

        var keep = full.CreatedMatches[0];
        var drop = full.CreatedMatches[1];
        var dropFixtureId = configured.Stage.Matchdays
            .SelectMany(matchday => matchday.Fixtures)
            .First(fixture => fixture.MatchIds.Contains(drop.Id))
            .Id;
        configured.Stage.DetachMatch(dropFixtureId, drop.Id, _clock);

        var partial = MaterializeMatches.Execute(competition, configured.Stage, [keep], _clock);
        partial.CreatedMatches.Should().HaveCount(1);
        partial.AlreadyComplete.Should().BeFalse();

        var all = new[] { keep }.Concat(partial.CreatedMatches).ToList();
        AssertPairMirrorInvariants(all, teamCount: 2);
    }

    [Fact]
    public void Groups_double_round_robin_is_scoped_per_group()
    {
        var competition = CreateCompetition.Execute("Groups-Double", _clock);
        var e1 = AddEntry.Execute(competition, "A", _clock);
        var e2 = AddEntry.Execute(competition, "B", _clock);
        var e3 = AddEntry.Execute(competition, "C", _clock);
        var e4 = AddEntry.Execute(competition, "D", _clock);
        var configured = ConfigureStructure.Execute(
            competition,
            null,
            StructureIntent.Groups(2, 2, matchGenerationFormat: MatchGenerationFormat.DoubleRoundRobin),
            _clock);
        var stage = configured.Stage;
        stage.AssignEntryToGroup(stage.Groups[0].Id, e1.Id);
        stage.AssignEntryToGroup(stage.Groups[0].Id, e2.Id);
        stage.AssignEntryToGroup(stage.Groups[1].Id, e3.Id);
        stage.AssignEntryToGroup(stage.Groups[1].Id, e4.Id);

        var result = MaterializeMatches.Execute(competition, stage, [], _clock);

        // 2 groups × 2 teams → 2 directed matches per group = 4
        result.CreatedMatches.Should().HaveCount(4);
        stage.Matchdays.Should().HaveCount(2); // 2*(2-1)
    }

    [Fact]
    public void Groups_materialize_after_manual_assignment()
    {
        var competition = CreateCompetition.Execute("Groups", _clock);
        var e1 = AddEntry.Execute(competition, "A", _clock);
        var e2 = AddEntry.Execute(competition, "B", _clock);
        var e3 = AddEntry.Execute(competition, "C", _clock);
        var e4 = AddEntry.Execute(competition, "D", _clock);
        var configured = ConfigureStructure.Execute(
            competition,
            null,
            StructureIntent.Groups(2, 2),
            _clock);
        var stage = configured.Stage;
        stage.AssignEntryToGroup(stage.Groups[0].Id, e1.Id);
        stage.AssignEntryToGroup(stage.Groups[0].Id, e2.Id);
        stage.AssignEntryToGroup(stage.Groups[1].Id, e3.Id);
        stage.AssignEntryToGroup(stage.Groups[1].Id, e4.Id);

        var result = MaterializeMatches.Execute(competition, stage, [], _clock);
        result.CreatedMatches.Should().HaveCount(2); // 1 pair per group of 2
    }

    [Fact]
    public void Cup_materialize_prepares_fixtures_without_matches_until_pairing_apply()
    {
        var competition = CreateCompetition.Execute("Cup", _clock);
        AddEntry.Execute(competition, "A", _clock);
        AddEntry.Execute(competition, "B", _clock);
        AddEntry.Execute(competition, "C", _clock);
        AddEntry.Execute(competition, "D", _clock);
        var configured = ConfigureStructure.Execute(
            competition,
            null,
            StructureIntent.Cup(4),
            _clock);

        var result = MaterializeMatches.Execute(competition, configured.Stage, [], _clock);
        result.CreatedMatches.Should().BeEmpty();
        configured.Stage.Rounds[0].Fixtures.Should().HaveCount(2);

        var view = StructureViewAssembler.Assemble(competition, [configured.Stage]);
        view.Readiness.ReadyForMatchOperation.Should().BeFalse();
    }

    [Fact]
    public void Swiss_materialize_is_rejected()
    {
        var competition = CreateCompetition.Execute("Swiss", _clock);
        AddEntry.Execute(competition, "A", _clock);
        AddEntry.Execute(competition, "B", _clock);
        var configured = ConfigureStructure.Execute(
            competition,
            null,
            StructureIntent.Swiss(3),
            _clock);

        var act = () => MaterializeMatches.Execute(competition, configured.Stage, [], _clock);
        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.MaterializationFailure);
    }

    [Fact]
    public void BuildRoundRobinPairs_count_is_n_times_n_minus_1_over_2()
    {
        var entries = Enumerable.Range(0, 5).Select(_ => EntryId.New()).ToArray();
        MaterializeMatches.BuildRoundRobinPairs(entries).Should().HaveCount(10);
    }

    [Fact]
    public void BuildRoundRobinRounds_double_mirrors_home_away()
    {
        var entries = Enumerable.Range(0, 4).Select(_ => EntryId.New()).OrderBy(id => id.Value).ToArray();
        var rounds = MaterializeMatches.BuildRoundRobinRounds(entries, MatchGenerationFormat.DoubleRoundRobin);
        rounds.Should().HaveCount(6); // 2*(4-1)

        var directed = rounds.SelectMany(round => round).ToList();
        directed.Should().HaveCount(12);
        foreach (var (home, away) in directed.Take(6))
        {
            directed.Should().Contain((away, home));
        }
    }

    private static void AssertPairMirrorInvariants(IReadOnlyList<Match> matches, int teamCount)
    {
        var directed = matches
            .Select(match => (match.HomeEntryId, match.AwayEntryId))
            .ToList();
        directed.Should().OnlyHaveUniqueItems();

        var unordered = directed
            .Select(pair => pair.HomeEntryId.Value.CompareTo(pair.AwayEntryId.Value) <= 0
                ? (pair.HomeEntryId, pair.AwayEntryId)
                : (pair.AwayEntryId, pair.HomeEntryId))
            .GroupBy(pair => pair)
            .ToDictionary(group => group.Key, group => group.Count());

        unordered.Values.Should().OnlyContain(count => count == 2);

        foreach (var (home, away) in directed)
        {
            directed.Should().Contain((away, home));
        }

        var entries = directed
            .SelectMany(pair => new[] { pair.HomeEntryId, pair.AwayEntryId })
            .Distinct()
            .ToList();
        entries.Should().HaveCount(teamCount);

        foreach (var entry in entries)
        {
            var homeCount = directed.Count(pair => pair.HomeEntryId.Equals(entry));
            var awayCount = directed.Count(pair => pair.AwayEntryId.Equals(entry));
            homeCount.Should().Be(teamCount - 1);
            awayCount.Should().Be(teamCount - 1);
            (homeCount + awayCount).Should().Be(2 * (teamCount - 1));
        }
    }
}
