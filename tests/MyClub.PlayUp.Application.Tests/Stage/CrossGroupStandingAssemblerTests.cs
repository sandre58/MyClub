// -----------------------------------------------------------------------
// <copyright file="CrossGroupStandingAssemblerTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Stage;
using MyClub.PlayUp.Application.Standing;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Match;
using MyClub.PlayUp.Domain.Stage;
using MyClub.PlayUp.Domain.Standing;
using Xunit;
using StageAggregate = MyClub.PlayUp.Domain.Stage.Stage;
using StandingView = MyClub.PlayUp.Domain.Standing.Standing;

namespace MyClub.PlayUp.Application.Tests.Stage;

public sealed class CrossGroupStandingAssemblerTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 11, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Assembler_extracts_position_P_from_each_group()
    {
        var (stage, groups, thirds, standings, matches) = BuildTwoGroupsWithKnownThirds();

        var derived = CrossGroupStandingAssembler.Build(
            stage.Groups,
            standings,
            position: 3,
            matches,
            stage.Regulation.StandingRules);

        derived.Rows.Should().HaveCount(2);
        derived.Rows.Select(r => r.EntryId).Should().BeEquivalentTo(thirds);
        derived.EntryAt(1).Should().NotBeNull();
        derived.EntryAt(2).Should().NotBeNull();
        groups.Should().HaveCount(2);
    }

    [Fact]
    public void Assembler_skips_missing_position()
    {
        var competitionId = CompetitionId.New();
        var stage = CreateLeagueStage(competitionId);
        var groupA = stage.AddGroup("A", _clock);
        var groupB = stage.AddGroup("B", _clock);
        var a1 = EntryId.New();
        var a2 = EntryId.New();
        var b1 = EntryId.New();
        var b2 = EntryId.New();
        var b3 = EntryId.New();
        Assign(stage, groupA.Id, [a1, a2]);
        Assign(stage, groupB.Id, [b1, b2, b3]);

        var standings = new Dictionary<GroupId, StandingView>
        {
            [groupA.Id] = ManualStanding([(a1, 1), (a2, 2)]),
            [groupB.Id] = ManualStanding([(b1, 1), (b2, 2), (b3, 3)])
        };

        var derived = CrossGroupStandingAssembler.Build(
            stage.Groups,
            standings,
            position: 3,
            matches: [],
            stage.Regulation.StandingRules);

        derived.Rows.Should().ContainSingle();
        derived.EntryAt(1).Should().Be(b3);
    }

    [Fact]
    public void Assembler_rejects_duplicate_candidate()
    {
        var competitionId = CompetitionId.New();
        var stage = CreateLeagueStage(competitionId);
        var groupA = stage.AddGroup("A", _clock);
        var groupB = stage.AddGroup("B", _clock);
        var shared = EntryId.New();

        // Standings alone drive extraction; Groups need not own the candidate entries.
        var standings = new Dictionary<GroupId, StandingView>
        {
            [groupA.Id] = ManualStanding([(shared, 1)]),
            [groupB.Id] = ManualStanding([(shared, 1)])
        };

        var act = () => CrossGroupStandingAssembler.Build(
            stage.Groups,
            standings,
            position: 1,
            matches: [],
            stage.Regulation.StandingRules);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.QualificationCandidateDuplicate);
    }

    [Fact]
    public void Assembler_rejects_empty_candidates()
    {
        var competitionId = CompetitionId.New();
        var stage = CreateLeagueStage(competitionId);
        var groupA = stage.AddGroup("A", _clock);
        var a1 = EntryId.New();
        Assign(stage, groupA.Id, [a1]);

        var standings = new Dictionary<GroupId, StandingView>
        {
            [groupA.Id] = ManualStanding([(a1, 1)])
        };

        var act = () => CrossGroupStandingAssembler.Build(
            stage.Groups,
            standings,
            position: 3,
            matches: [],
            stage.Regulation.StandingRules);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.QualificationCandidatesEmpty);
    }

    [Fact]
    public void Assembler_rejects_missing_group_standing()
    {
        var competitionId = CompetitionId.New();
        var stage = CreateLeagueStage(competitionId);
        stage.AddGroup("A", _clock);
        stage.AddGroup("B", _clock);

        var act = () => CrossGroupStandingAssembler.Build(
            stage.Groups,
            new Dictionary<GroupId, StandingView>(),
            position: 3,
            matches: [],
            stage.Regulation.StandingRules);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.QualificationStandingMissing);
    }

    [Fact]
    public void Assembler_counts_matches_vs_non_candidates()
    {
        var (stage, _, thirds, standings, matches) = BuildTwoGroupsWithKnownThirds();

        var derived = CrossGroupStandingAssembler.Build(
            stage.Groups,
            standings,
            position: 3,
            matches,
            stage.Regulation.StandingRules);

        foreach (var third in thirds)
        {
            var row = derived.Find(third)!;

            // Each third played 3 group matches (vs 1st, 2nd, 4th) — opponents outside the candidate set still count.
            row.Played.Should().Be(3);
            row.Points.Should().BeGreaterThan(0);
        }
    }

    private static StandingView ManualStanding(IReadOnlyList<(EntryId EntryId, int Position)> rows) =>
        new(
        [
            ..rows.Select(r => new StandingRow(
                r.EntryId,
                r.Position,
                played: 0,
                wins: 0,
                draws: 0,
                losses: 0,
                goalsFor: 0,
                goalsAgainst: 0,
                points: 0))
        ]);

    private static EntryId[] CreateEntries(int count) =>
        [..Enumerable.Range(0, count).Select(_ => EntryId.New())];

    private (StageAggregate Stage, Group[] Groups, EntryId[] Thirds, Dictionary<GroupId, StandingView> Standings, List<Match> Matches)
        BuildTwoGroupsWithKnownThirds()
    {
        var competitionId = CompetitionId.New();
        var stage = CreateLeagueStage(competitionId);
        var groupA = stage.AddGroup("A", _clock);
        var groupB = stage.AddGroup("B", _clock);
        var entriesA = CreateEntries(4);
        var entriesB = CreateEntries(4);
        Assign(stage, groupA.Id, entriesA);
        Assign(stage, groupB.Id, entriesB);

        var matches = new List<Match>();
        matches.AddRange(BuildRoundRobin(stage, entriesA));
        matches.AddRange(BuildRoundRobin(stage, entriesB));

        var standingA = CalculateStanding.Execute(
            entriesA, matches, stage.Regulation.StandingRules);
        var standingB = CalculateStanding.Execute(
            entriesB, matches, stage.Regulation.StandingRules);

        var thirds = new[] { standingA.EntryAt(3)!.Value, standingB.EntryAt(3)!.Value };
        var standings = new Dictionary<GroupId, StandingView>
        {
            [groupA.Id] = standingA,
            [groupB.Id] = standingB
        };

        return (stage, [groupA, groupB], thirds, standings, matches);
    }

    private void Assign(StageAggregate stage, GroupId groupId, EntryId[] entries)
    {
        foreach (var entry in entries)
        {
            stage.AssignEntryToGroup(groupId, entry, _clock);
        }
    }

    private StageAggregate CreateLeagueStage(CompetitionId competitionId) =>
        StageAggregate.Create(competitionId, new StageName("Groups"), SampleRegulations.Standard(), _clock);

    private List<Match> BuildRoundRobin(StageAggregate stage, EntryId[] entries)
    {
        var matches = new List<Match>();
        for (var i = 0; i < entries.Length; i++)
        {
            for (var j = i + 1; j < entries.Length; j++)
            {
                var homeGoals = entries.Length - i;
                var match = Match.Create(stage.CompetitionId, stage.Id, entries[i], entries[j], _clock);
                matches.Add(Finish(match, homeGoals, 0));
            }
        }

        return matches;
    }

    private Match Finish(Match match, int homeGoals, int awayGoals)
    {
        match.Start(_clock);
        match.Finish(new MatchResult(ResultType.Played, new Score(homeGoals, awayGoals)), _clock);
        return match;
    }
}
