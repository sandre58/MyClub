// -----------------------------------------------------------------------
// <copyright file="ApplyQualificationTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Standings;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Qualification;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Domain.Standings;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Stages;

public sealed class ApplyQualificationTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 10, 15, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Championship_positions_fill_destination_slots()
    {
        var competitionId = CompetitionId.New();
        var league = CreateLeagueStage(competitionId, "League");
        var terminal = CreateSlotStage(competitionId, "Terminal", ["Champ", "Europe1", "Europe2"]);
        var entries = CreateEntries(3);
        var matches = BuildRoundRobin(league, entries);
        var standing = CalculateStanding.Execute(entries, matches, league.Regulation.StandingRules);

        league.ReplaceQualificationRules(
            new QualificationRules(
            [
                Path(1, SelectionMode.Position, 1, terminal.Id, "Champ"),
                Path(2, SelectionMode.Position, 2, terminal.Id, "Europe1"),
                Path(3, SelectionMode.Position, 3, terminal.Id, "Europe2")
            ]),
            _clock);

        var results = ApplyQualification.Execute(league, standing, [league, terminal], _clock);

        results.Should().HaveCount(3);
        terminal.FindSlot("Champ")!.EntryId.Should().Be(standing.EntryAt(1));
        terminal.FindSlot("Europe1")!.EntryId.Should().Be(standing.EntryAt(2));
        terminal.FindSlot("Europe2")!.EntryId.Should().Be(standing.EntryAt(3));
    }

    [Fact]
    public void Ligue2_style_positions_split_promotion_and_playoff()
    {
        var competitionId = CompetitionId.New();
        var league = CreateLeagueStage(competitionId, "L2");
        var promo = CreateSlotStage(competitionId, "Promo", ["P1", "P2"]);
        var playoff = CreateSlotStage(competitionId, "Playoff", ["PO1", "PO2", "PO3"]);
        var entries = CreateEntries(5);
        var matches = BuildRoundRobin(league, entries);
        var standing = CalculateStanding.Execute(entries, matches, league.Regulation.StandingRules);

        league.ReplaceQualificationRules(
            new QualificationRules(
            [
                Path(1, SelectionMode.Position, 1, promo.Id, "P1"),
                Path(2, SelectionMode.Position, 2, promo.Id, "P2"),
                Path(3, SelectionMode.Position, 3, playoff.Id, "PO1"),
                Path(4, SelectionMode.Position, 4, playoff.Id, "PO2"),
                Path(5, SelectionMode.Position, 5, playoff.Id, "PO3")
            ]),
            _clock);

        ApplyQualification.Execute(league, standing, [league, promo, playoff], _clock);

        promo.FindSlot("P1")!.EntryId.Should().Be(standing.EntryAt(1));
        promo.FindSlot("P2")!.EntryId.Should().Be(standing.EntryAt(2));
        playoff.FindSlot("PO1")!.EntryId.Should().Be(standing.EntryAt(3));
        playoff.FindSlot("PO2")!.EntryId.Should().Be(standing.EntryAt(4));
        playoff.FindSlot("PO3")!.EntryId.Should().Be(standing.EntryAt(5));
    }

    [Fact]
    public void Ucl_style_positions_fill_ko_and_playoff_slots()
    {
        var competitionId = CompetitionId.New();
        var league = CreateLeagueStage(competitionId, "LeaguePhase");
        var koSlots = Enumerable.Range(1, 8).Select(i => $"KO{i}").ToArray();
        var poSlots = Enumerable.Range(9, 16).Select(i => $"PO{i}").ToArray();
        var ko = CreateSlotStage(competitionId, "KO", koSlots);
        var playoff = CreateSlotStage(competitionId, "PO", poSlots);
        var entries = CreateEntries(24);
        var matches = BuildRoundRobin(league, [..entries.Take(8)]);

        // Lightweight standings: matches among first 8; remaining entries fill bottom positions.
        var standing = CalculateStanding.Execute(entries, matches, league.Regulation.StandingRules);

        var paths = new List<QualificationPath>();
        var order = 1;
        for (var i = 1; i <= 8; i++)
        {
            paths.Add(Path(order++, SelectionMode.Position, i, ko.Id, $"KO{i}"));
        }

        for (var i = 9; i <= 24; i++)
        {
            paths.Add(Path(order++, SelectionMode.Position, i, playoff.Id, $"PO{i}"));
        }

        league.ReplaceQualificationRules(new QualificationRules(paths), _clock);

        ApplyQualification.Execute(league, standing, [league, ko, playoff], _clock);

        for (var i = 1; i <= 8; i++)
        {
            ko.FindSlot($"KO{i}")!.EntryId.Should().Be(standing.EntryAt(i));
        }

        for (var i = 9; i <= 24; i++)
        {
            playoff.FindSlot($"PO{i}")!.EntryId.Should().Be(standing.EntryAt(i));
        }
    }

    [Fact]
    public void Ucl_style_positions_25_to_36_have_no_qualification_path()
    {
        var competitionId = CompetitionId.New();
        var league = CreateLeagueStage(competitionId, "LeaguePhase");
        var ko = CreateSlotStage(competitionId, "KO", ["KO1"]);
        var entries = CreateEntries(36);
        var matches = BuildRoundRobin(league, [..entries.Take(4)]);
        var standing = CalculateStanding.Execute(entries, matches, league.Regulation.StandingRules);

        league.ReplaceQualificationRules(
            new QualificationRules([Path(1, SelectionMode.Position, 1, ko.Id, "KO1")]),
            _clock);

        ApplyQualification.Execute(league, standing, [league, ko], _clock);

        ko.FindSlot("KO1")!.EntryId.Should().Be(standing.EntryAt(1));
        for (var i = 25; i <= 36; i++)
        {
            standing.EntryAt(i).Should().NotBeNull();
        }
    }

    [Fact]
    public void Case1_single_group_positions_fill_slots()
    {
        var competitionId = CompetitionId.New();
        var groups = CreateLeagueStage(competitionId, "Groups");
        var terminal = CreateSlotStage(competitionId, "KO", ["A1", "A2"]);
        var entries = CreateEntries(4);
        var groupA = groups.AddGroup("A", _clock);
        foreach (var entry in entries)
        {
            groups.AssignEntryToGroup(groupA.Id, entry);
        }

        var matches = BuildRoundRobin(groups, entries);
        var standingA = CalculateStanding.Execute(groupA.EntryIds, matches, groups.Regulation.StandingRules);

        groups.ReplaceQualificationRules(
            new QualificationRules(
            [
                GroupPath(1, groupA.Id, SelectionMode.Position, 1, terminal.Id, "A1"),
                GroupPath(2, groupA.Id, SelectionMode.Position, 2, terminal.Id, "A2")
            ]),
            _clock);

        ApplyQualification.Execute(
            groups,
            overallStanding: null,
            new Dictionary<GroupId, Standing> { [groupA.Id] = standingA },
            [groups, terminal],
            _clock);

        terminal.FindSlot("A1")!.EntryId.Should().Be(standingA.EntryAt(1));
        terminal.FindSlot("A2")!.EntryId.Should().Be(standingA.EntryAt(2));
    }

    [Fact]
    public void Case1_top_two_on_single_path_still_rejected_by_applier()
    {
        var competitionId = CompetitionId.New();
        var groups = CreateLeagueStage(competitionId, "Groups");
        var terminal = CreateSlotStage(competitionId, "KO", ["Pool"]);
        var entries = CreateEntries(4);
        var groupA = groups.AddGroup("A", _clock);
        foreach (var entry in entries)
        {
            groups.AssignEntryToGroup(groupA.Id, entry);
        }

        var matches = BuildRoundRobin(groups, entries);
        var standingA = CalculateStanding.Execute(groupA.EntryIds, matches, groups.Regulation.StandingRules);

        groups.ReplaceQualificationRules(
            new QualificationRules(
            [
                GroupPath(1, groupA.Id, SelectionMode.Top, 2, terminal.Id, "Pool")
            ]),
            _clock);

        var act = () => ApplyQualification.Execute(
            groups,
            overallStanding: null,
            new Dictionary<GroupId, Standing> { [groupA.Id] = standingA },
            [groups, terminal],
            _clock);

        act.Should().Throw<DomainException>()
            .Which.Code.Should().Be(QualificationErrorCodes.PathMultiEntry);
    }

    [Fact]
    public void Case2_multi_group_positions_do_not_mix_standings()
    {
        var competitionId = CompetitionId.New();
        var groups = CreateLeagueStage(competitionId, "Groups");
        var terminal = CreateSlotStage(competitionId, "KO", ["A1", "A2", "B1", "B2"]);
        var aEntries = CreateEntries(4);
        var bEntries = CreateEntries(4);
        var groupA = groups.AddGroup("A", _clock);
        var groupB = groups.AddGroup("B", _clock);
        foreach (var entry in aEntries)
        {
            groups.AssignEntryToGroup(groupA.Id, entry);
        }

        foreach (var entry in bEntries)
        {
            groups.AssignEntryToGroup(groupB.Id, entry);
        }

        var matchesA = BuildRoundRobin(groups, aEntries);
        var matchesB = BuildRoundRobin(groups, bEntries);
        var standingA = CalculateStanding.Execute(groupA.EntryIds, matchesA, groups.Regulation.StandingRules);
        var standingB = CalculateStanding.Execute(groupB.EntryIds, matchesB, groups.Regulation.StandingRules);

        groups.ReplaceQualificationRules(
            new QualificationRules(
            [
                GroupPath(1, groupA.Id, SelectionMode.Position, 1, terminal.Id, "A1"),
                GroupPath(2, groupA.Id, SelectionMode.Position, 2, terminal.Id, "A2"),
                GroupPath(3, groupB.Id, SelectionMode.Position, 1, terminal.Id, "B1"),
                GroupPath(4, groupB.Id, SelectionMode.Position, 2, terminal.Id, "B2")
            ]),
            _clock);

        ApplyQualification.Execute(
            groups,
            overallStanding: null,
            new Dictionary<GroupId, Standing>
            {
                [groupA.Id] = standingA,
                [groupB.Id] = standingB
            },
            [groups, terminal],
            _clock);

        terminal.FindSlot("A1")!.EntryId.Should().Be(standingA.EntryAt(1));
        terminal.FindSlot("A2")!.EntryId.Should().Be(standingA.EntryAt(2));
        terminal.FindSlot("B1")!.EntryId.Should().Be(standingB.EntryAt(1));
        terminal.FindSlot("B2")!.EntryId.Should().Be(standingB.EntryAt(2));

        aEntries.Should().Contain(terminal.FindSlot("A1")!.EntryId!.Value);
        aEntries.Should().Contain(terminal.FindSlot("A2")!.EntryId!.Value);
        bEntries.Should().Contain(terminal.FindSlot("B1")!.EntryId!.Value);
        bEntries.Should().Contain(terminal.FindSlot("B2")!.EntryId!.Value);
        aEntries.Should().NotContain(terminal.FindSlot("B1")!.EntryId!.Value);
        bEntries.Should().NotContain(terminal.FindSlot("A1")!.EntryId!.Value);
    }

    [Fact]
    public void Case3_different_quotas_per_group()
    {
        var competitionId = CompetitionId.New();
        var groups = CreateLeagueStage(competitionId, "Groups");
        var terminal = CreateSlotStage(competitionId, "KO", ["A1", "A2", "B1", "C1", "C2", "C3"]);
        var aEntries = CreateEntries(4);
        var bEntries = CreateEntries(4);
        var cEntries = CreateEntries(4);
        var groupA = groups.AddGroup("A", _clock);
        var groupB = groups.AddGroup("B", _clock);
        var groupC = groups.AddGroup("C", _clock);
        foreach (var entry in aEntries)
        {
            groups.AssignEntryToGroup(groupA.Id, entry);
        }

        foreach (var entry in bEntries)
        {
            groups.AssignEntryToGroup(groupB.Id, entry);
        }

        foreach (var entry in cEntries)
        {
            groups.AssignEntryToGroup(groupC.Id, entry);
        }

        var standingA = CalculateStanding.Execute(
            groupA.EntryIds,
            BuildRoundRobin(groups, aEntries),
            groups.Regulation.StandingRules);
        var standingB = CalculateStanding.Execute(
            groupB.EntryIds,
            BuildRoundRobin(groups, bEntries),
            groups.Regulation.StandingRules);
        var standingC = CalculateStanding.Execute(
            groupC.EntryIds,
            BuildRoundRobin(groups, cEntries),
            groups.Regulation.StandingRules);

        groups.ReplaceQualificationRules(
            new QualificationRules(
            [
                GroupPath(1, groupA.Id, SelectionMode.Position, 1, terminal.Id, "A1"),
                GroupPath(2, groupA.Id, SelectionMode.Position, 2, terminal.Id, "A2"),
                GroupPath(3, groupB.Id, SelectionMode.Position, 1, terminal.Id, "B1"),
                GroupPath(4, groupC.Id, SelectionMode.Position, 1, terminal.Id, "C1"),
                GroupPath(5, groupC.Id, SelectionMode.Position, 2, terminal.Id, "C2"),
                GroupPath(6, groupC.Id, SelectionMode.Position, 3, terminal.Id, "C3")
            ]),
            _clock);

        var results = ApplyQualification.Execute(
            groups,
            overallStanding: null,
            new Dictionary<GroupId, Standing>
            {
                [groupA.Id] = standingA,
                [groupB.Id] = standingB,
                [groupC.Id] = standingC
            },
            [groups, terminal],
            _clock);

        results.Should().HaveCount(6);
        terminal.FindSlot("A1")!.EntryId.Should().Be(standingA.EntryAt(1));
        terminal.FindSlot("A2")!.EntryId.Should().Be(standingA.EntryAt(2));
        terminal.FindSlot("B1")!.EntryId.Should().Be(standingB.EntryAt(1));
        terminal.FindSlot("C1")!.EntryId.Should().Be(standingC.EntryAt(1));
        terminal.FindSlot("C2")!.EntryId.Should().Be(standingC.EntryAt(2));
        terminal.FindSlot("C3")!.EntryId.Should().Be(standingC.EntryAt(3));
    }

    [Fact]
    public void Case4_specific_positions_from_group_standing()
    {
        var competitionId = CompetitionId.New();
        var groups = CreateLeagueStage(competitionId, "Groups");
        var terminal = CreateSlotStage(competitionId, "KO", ["X", "Y", "Z"]);
        var entries = CreateEntries(6);
        var groupA = groups.AddGroup("A", _clock);
        foreach (var entry in entries)
        {
            groups.AssignEntryToGroup(groupA.Id, entry);
        }

        var standingA = CalculateStanding.Execute(
            groupA.EntryIds,
            BuildRoundRobin(groups, entries),
            groups.Regulation.StandingRules);

        groups.ReplaceQualificationRules(
            new QualificationRules(
            [
                GroupPath(1, groupA.Id, SelectionMode.Position, 1, terminal.Id, "X"),
                GroupPath(2, groupA.Id, SelectionMode.Position, 3, terminal.Id, "Y"),
                GroupPath(3, groupA.Id, SelectionMode.Position, 5, terminal.Id, "Z")
            ]),
            _clock);

        ApplyQualification.Execute(
            groups,
            overallStanding: null,
            new Dictionary<GroupId, Standing> { [groupA.Id] = standingA },
            [groups, terminal],
            _clock);

        terminal.FindSlot("X")!.EntryId.Should().Be(standingA.EntryAt(1));
        terminal.FindSlot("Y")!.EntryId.Should().Be(standingA.EntryAt(3));
        terminal.FindSlot("Z")!.EntryId.Should().Be(standingA.EntryAt(5));
    }

    [Fact]
    public void Execute_rejects_missing_group_standing()
    {
        var competitionId = CompetitionId.New();
        var groups = CreateLeagueStage(competitionId, "Groups");
        var terminal = CreateSlotStage(competitionId, "Terminal", ["A1"]);
        var entries = CreateEntries(2);
        var groupA = groups.AddGroup("A", _clock);
        foreach (var entry in entries)
        {
            groups.AssignEntryToGroup(groupA.Id, entry);
        }

        groups.ReplaceQualificationRules(
            new QualificationRules(
            [
                GroupPath(1, groupA.Id, SelectionMode.Position, 1, terminal.Id, "A1")
            ]),
            _clock);

        var act = () => ApplyQualification.Execute(
            groups,
            overallStanding: null,
            new Dictionary<GroupId, Standing>(),
            [groups, terminal],
            _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.QualificationStandingMissing);
    }

    [Fact]
    public void Execute_rejects_unknown_group_on_path()
    {
        var competitionId = CompetitionId.New();
        var groups = CreateLeagueStage(competitionId, "Groups");
        var terminal = CreateSlotStage(competitionId, "Terminal", ["A1"]);
        var unknownGroupId = GroupId.New();
        var entries = CreateEntries(2);
        var matches = BuildRoundRobin(groups, entries);
        var standing = CalculateStanding.Execute(entries, matches, groups.Regulation.StandingRules);

        groups.ReplaceQualificationRules(
            new QualificationRules(
            [
                GroupPath(1, unknownGroupId, SelectionMode.Position, 1, terminal.Id, "A1")
            ]),
            _clock);

        var act = () => ApplyQualification.Execute(
            groups,
            overallStanding: null,
            new Dictionary<GroupId, Standing> { [unknownGroupId] = standing },
            [groups, terminal],
            _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.QualificationGroupNotFound);
    }

    [Fact]
    public void Execute_rejects_overall_path_without_overall_standing()
    {
        var competitionId = CompetitionId.New();
        var league = CreateLeagueStage(competitionId, "League");
        var terminal = CreateSlotStage(competitionId, "Terminal", ["Champ"]);

        league.ReplaceQualificationRules(
            new QualificationRules([Path(1, SelectionMode.Position, 1, terminal.Id, "Champ")]),
            _clock);

        var act = () => ApplyQualification.Execute(
            league,
            overallStanding: null,
            new Dictionary<GroupId, Standing>(),
            [league, terminal],
            _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.QualificationStandingMissing);
    }

    [Fact]
    public void CalculateStanding_home_filter_differs_from_all()
    {
        var competitionId = CompetitionId.New();
        var stage = CreateLeagueStage(competitionId, "League");
        var a = EntryId.New();
        var b = EntryId.New();
        var match = Match.Create(competitionId, stage.Id, a, b, _clock);
        match.Start(_clock);
        match.Finish(new MatchResult(ResultType.Played, new Score(2, 0)), _clock);

        var all = CalculateStanding.Execute([a, b], [match], stage.Regulation.StandingRules);
        var home = CalculateStanding.Execute([a, b], [match], stage.Regulation.StandingRules, MatchFilter.Home);

        all.Find(a)!.Played.Should().Be(1);
        all.Find(b)!.Played.Should().Be(1);
        home.Find(a)!.Played.Should().Be(1);
        home.Find(b)!.Played.Should().Be(0);
    }

    [Fact]
    public void Execute_replace_local_on_reapply()
    {
        var competitionId = CompetitionId.New();
        var league = CreateLeagueStage(competitionId, "League");
        var terminal = CreateSlotStage(competitionId, "Terminal", ["Champ"]);
        var entries = CreateEntries(2);
        var matches = BuildRoundRobin(league, entries);
        var standing = CalculateStanding.Execute(entries, matches, league.Regulation.StandingRules);
        league.ReplaceQualificationRules(
            new QualificationRules([Path(1, SelectionMode.Position, 1, terminal.Id, "Champ")]),
            _clock);

        ApplyQualification.Execute(league, standing, [league, terminal], _clock);
        var first = terminal.FindSlot("Champ")!.EntryId;

        var inverted = CalculateStanding.Execute(
            entries,
            [
                Finish(Match.Create(competitionId, league.Id, entries[1], entries[0], _clock), 5, 0)
            ],
            league.Regulation.StandingRules);
        ApplyQualification.Execute(league, inverted, [league, terminal], _clock);

        terminal.FindSlot("Champ")!.EntryId.Should().Be(inverted.EntryAt(1));
        terminal.FindSlot("Champ")!.EntryId.Should().NotBe(first);
    }

    [Fact]
    public void Execute_applies_when_destination_suspended()
    {
        var competitionId = CompetitionId.New();
        var league = CreateLeagueStage(competitionId, "League");
        var terminal = CreateSlotStage(competitionId, "Terminal", ["Champ"]);
        terminal.AddRound("R1", _clock);
        var entries = CreateEntries(2);
        var matches = BuildRoundRobin(league, entries);
        var standing = CalculateStanding.Execute(entries, matches, league.Regulation.StandingRules);
        league.ReplaceQualificationRules(
            new QualificationRules([Path(1, SelectionMode.Position, 1, terminal.Id, "Champ")]),
            _clock);
        terminal.Prepare(_clock);
        terminal.Start(_clock);
        terminal.Suspend(_clock);

        ApplyQualification.Execute(league, standing, [league, terminal], _clock);

        terminal.Status.Should().Be(StageStatus.Suspended);
        terminal.FindSlot("Champ")!.EntryId.Should().Be(standing.EntryAt(1));
        terminal.Draws.Should().BeEmpty();
    }

    [Fact]
    public void Execute_rejects_when_destination_completed()
    {
        var competitionId = CompetitionId.New();
        var league = CreateLeagueStage(competitionId, "League");
        var terminal = CreateSlotStage(competitionId, "Terminal", ["Champ"]);
        terminal.AddRound("R1", _clock);
        var entries = CreateEntries(2);
        var matches = BuildRoundRobin(league, entries);
        var standing = CalculateStanding.Execute(entries, matches, league.Regulation.StandingRules);
        league.ReplaceQualificationRules(
            new QualificationRules([Path(1, SelectionMode.Position, 1, terminal.Id, "Champ")]),
            _clock);
        terminal.Prepare(_clock);
        terminal.Start(_clock);
        terminal.Complete(_clock);

        var act = () => ApplyQualification.Execute(league, standing, [league, terminal], _clock);

        act.Should().Throw<DomainException>()
            .Which.Code.Should().Be(StageErrorCodes.InvalidTransition);
        terminal.FindSlot("Champ")!.EntryId.Should().BeNull();
        terminal.Status.Should().Be(StageStatus.Completed);
    }

    [Fact]
    public void CDM_like_8_thirds_positions_1_to_4()
    {
        var scenario = BuildGroupsScenario(groupCount: 8, teamsPerGroup: 4, strengthSpread: true);
        var slotKeys = Enumerable.Range(1, 4).Select(i => $"R16-{i}").ToArray();
        var terminal = CreateSlotStage(scenario.CompetitionId, "R16", slotKeys);
        scenario.GroupsStage.ReplaceQualificationRules(
            new QualificationRules(AcrossGroupsPaths(3, terminal.Id, slotKeys)),
            _clock);

        ApplyQualification.Execute(
            scenario.GroupsStage,
            overallStanding: null,
            scenario.GroupStandings,
            scenario.Matches,
            [scenario.GroupsStage, terminal],
            _clock);

        var derived = CrossGroupStandingAssembler.Build(
            scenario.GroupsStage.Groups,
            scenario.GroupStandings,
            3,
            scenario.Matches,
            scenario.GroupsStage.Regulation.StandingRules);

        for (var i = 1; i <= 4; i++)
        {
            terminal.FindSlot($"R16-{i}")!.EntryId.Should().Be(derived.EntryAt(i));
        }

        foreach (var groupStanding in scenario.GroupStandings.Values)
        {
            var third = groupStanding.EntryAt(3)!.Value;
            var winner = groupStanding.EntryAt(1)!.Value;
            derived.Rows.Select(r => r.EntryId).Should().Contain(third);
            derived.Rows.Select(r => r.EntryId).Should().NotContain(winner);
        }
    }

    [Fact]
    public void Euro_like_6_thirds_positions_1_to_4()
    {
        var scenario = BuildGroupsScenario(groupCount: 6, teamsPerGroup: 4, strengthSpread: true);
        var slotKeys = Enumerable.Range(1, 4).Select(i => $"R16-{i}").ToArray();
        var terminal = CreateSlotStage(scenario.CompetitionId, "R16", slotKeys);
        scenario.GroupsStage.ReplaceQualificationRules(
            new QualificationRules(AcrossGroupsPaths(3, terminal.Id, slotKeys)),
            _clock);

        ApplyQualification.Execute(
            scenario.GroupsStage,
            overallStanding: null,
            scenario.GroupStandings,
            scenario.Matches,
            [scenario.GroupsStage, terminal],
            _clock);

        var derived = CrossGroupStandingAssembler.Build(
            scenario.GroupsStage.Groups,
            scenario.GroupStandings,
            3,
            scenario.Matches,
            scenario.GroupsStage.Regulation.StandingRules);

        for (var i = 1; i <= 4; i++)
        {
            terminal.FindSlot($"R16-{i}")!.EntryId.Should().Be(derived.EntryAt(i));
        }

        derived.Rows.Should().HaveCount(6);
    }

    [Fact]
    public void Eight_thirds_positions_1_to_2()
    {
        var scenario = BuildGroupsScenario(groupCount: 8, teamsPerGroup: 4, strengthSpread: true);
        var slotKeys = new[] { "Best1", "Best2" };
        var terminal = CreateSlotStage(scenario.CompetitionId, "KO", slotKeys);
        scenario.GroupsStage.ReplaceQualificationRules(
            new QualificationRules(AcrossGroupsPaths(3, terminal.Id, slotKeys)),
            _clock);

        ApplyQualification.Execute(
            scenario.GroupsStage,
            overallStanding: null,
            scenario.GroupStandings,
            scenario.Matches,
            [scenario.GroupsStage, terminal],
            _clock);

        var derived = CrossGroupStandingAssembler.Build(
            scenario.GroupsStage.Groups,
            scenario.GroupStandings,
            3,
            scenario.Matches,
            scenario.GroupsStage.Regulation.StandingRules);

        terminal.FindSlot("Best1")!.EntryId.Should().Be(derived.EntryAt(1));
        terminal.FindSlot("Best2")!.EntryId.Should().Be(derived.EntryAt(2));
        terminal.FindSlot("Best1")!.EntryId.Should().NotBe(derived.EntryAt(3));
    }

    [Fact]
    public void Anti_overall_top4_differs_from_across_groups_thirds()
    {
        var scenario = BuildGroupsScenario(groupCount: 4, teamsPerGroup: 4, strengthSpread: true);
        var allEntries = scenario.GroupStandings.Values
            .SelectMany(s => s.Rows.Select(r => r.EntryId))
            .Distinct()
            .ToArray();
        var overall = CalculateStanding.Execute(
            allEntries, scenario.Matches, scenario.GroupsStage.Regulation.StandingRules);
        var derivedThirds = CrossGroupStandingAssembler.Build(
            scenario.GroupsStage.Groups,
            scenario.GroupStandings,
            3,
            scenario.Matches,
            scenario.GroupsStage.Regulation.StandingRules);

        var overallTop4 = Enumerable.Range(1, 4).Select(i => overall.EntryAt(i)!.Value).ToArray();
        var thirdsTop4 = Enumerable.Range(1, 4).Select(i => derivedThirds.EntryAt(i)!.Value).ToArray();
        overallTop4.Should().NotBeEquivalentTo(thirdsTop4);

        var slotKeys = Enumerable.Range(1, 4).Select(i => $"S{i}").ToArray();
        var terminal = CreateSlotStage(scenario.CompetitionId, "KO", slotKeys);
        scenario.GroupsStage.ReplaceQualificationRules(
            new QualificationRules(AcrossGroupsPaths(3, terminal.Id, slotKeys)),
            _clock);

        ApplyQualification.Execute(
            scenario.GroupsStage,
            overall,
            scenario.GroupStandings,
            scenario.Matches,
            [scenario.GroupsStage, terminal],
            _clock);

        for (var i = 1; i <= 4; i++)
        {
            terminal.FindSlot($"S{i}")!.EntryId.Should().Be(thirdsTop4[i - 1]);
            terminal.FindSlot($"S{i}")!.EntryId.Should().NotBe(overallTop4[i - 1]);
        }
    }

    [Fact]
    public void Derived_standing_tie_falls_back_deterministically()
    {
        var scenario = BuildGroupsScenario(groupCount: 3, teamsPerGroup: 4, strengthSpread: false);
        var derived = CrossGroupStandingAssembler.Build(
            scenario.GroupsStage.Groups,
            scenario.GroupStandings,
            3,
            scenario.Matches,
            scenario.GroupsStage.Regulation.StandingRules);

        var again = CrossGroupStandingAssembler.Build(
            scenario.GroupsStage.Groups,
            scenario.GroupStandings,
            3,
            scenario.Matches,
            scenario.GroupsStage.Regulation.StandingRules);

        derived.Rows.Select(r => r.EntryId).Should().Equal(again.Rows.Select(r => r.EntryId));
        derived.Rows.Select(r => r.Points).Should().OnlyContain(p => p == derived.Rows[0].Points);
    }

    [Fact]
    public void Derived_standing_h2h_absent_does_not_break()
    {
        var scenario = BuildGroupsScenario(groupCount: 4, teamsPerGroup: 4, strengthSpread: false);
        var slotKeys = new[] { "T1", "T2" };
        var terminal = CreateSlotStage(scenario.CompetitionId, "KO", slotKeys);
        scenario.GroupsStage.ReplaceQualificationRules(
            new QualificationRules(AcrossGroupsPaths(3, terminal.Id, slotKeys)),
            _clock);

        var act = () => ApplyQualification.Execute(
            scenario.GroupsStage,
            overallStanding: null,
            scenario.GroupStandings,
            scenario.Matches,
            [scenario.GroupsStage, terminal],
            _clock);

        act.Should().NotThrow();
        terminal.FindSlot("T1")!.EntryId.Should().NotBeNull();
        terminal.FindSlot("T2")!.EntryId.Should().NotBeNull();
    }

    [Fact]
    public void Derived_standing_h2h_present_reuses_calculator()
    {
        var competitionId = CompetitionId.New();
        var groups = CreateLeagueStage(competitionId, "Groups");
        var groupA = groups.AddGroup("A", _clock);
        var groupB = groups.AddGroup("B", _clock);
        var a1 = EntryId.New();
        var a2 = EntryId.New();
        var b1 = EntryId.New();
        var b2 = EntryId.New();
        foreach (var e in new[] { a1, a2 })
        {
            groups.AssignEntryToGroup(groupA.Id, e);
        }

        foreach (var e in new[] { b1, b2 })
        {
            groups.AssignEntryToGroup(groupB.Id, e);
        }

        // Identical group results: each winner 1-0, same points/GD/GF.
        var matches = new List<Match>
        {
            Finish(Match.Create(competitionId, groups.Id, a1, a2, _clock), 1, 0),
            Finish(Match.Create(competitionId, groups.Id, b1, b2, _clock), 1, 0),

            // Direct match between candidates enables HeadToHead among tied winners.
            Finish(Match.Create(competitionId, groups.Id, a1, b1, _clock), 2, 0)
        };

        var standingA = CalculateStanding.Execute([a1, a2], matches, groups.Regulation.StandingRules);
        var standingB = CalculateStanding.Execute([b1, b2], matches, groups.Regulation.StandingRules);
        standingA.EntryAt(1).Should().Be(a1);
        standingB.EntryAt(1).Should().Be(b1);

        var derived = CrossGroupStandingAssembler.Build(
            groups.Groups,
            new Dictionary<GroupId, Standing> { [groupA.Id] = standingA, [groupB.Id] = standingB },
            position: 1,
            matches,
            groups.Regulation.StandingRules);

        // a1 beat b1 head-to-head; both candidates reuse StandingCalculator criteria.
        derived.EntryAt(1).Should().Be(a1);
        derived.EntryAt(2).Should().Be(b1);

        var terminal = CreateSlotStage(competitionId, "KO", ["W1", "W2"]);
        groups.ReplaceQualificationRules(
            new QualificationRules(AcrossGroupsPaths(1, terminal.Id, ["W1", "W2"])),
            _clock);

        ApplyQualification.Execute(
            groups,
            overallStanding: null,
            new Dictionary<GroupId, Standing> { [groupA.Id] = standingA, [groupB.Id] = standingB },
            matches,
            [groups, terminal],
            _clock);

        terminal.FindSlot("W1")!.EntryId.Should().Be(a1);
        terminal.FindSlot("W2")!.EntryId.Should().Be(b1);
    }

    [Fact]
    public void Coexistence_group_paths_and_across_groups()
    {
        var scenario = BuildGroupsScenario(groupCount: 2, teamsPerGroup: 4, strengthSpread: true);
        var groupA = scenario.GroupsStage.Groups[0];
        var groupB = scenario.GroupsStage.Groups[1];
        var terminal = CreateSlotStage(scenario.CompetitionId, "KO", ["A1", "B1", "BestThird"]);
        scenario.GroupsStage.ReplaceQualificationRules(
            new QualificationRules(
            [
                GroupPath(1, groupA.Id, SelectionMode.Position, 1, terminal.Id, "A1"),
                GroupPath(2, groupB.Id, SelectionMode.Position, 1, terminal.Id, "B1"),
                AcrossGroupsPath(3, 3, 1, terminal.Id, "BestThird")
            ]),
            _clock);

        ApplyQualification.Execute(
            scenario.GroupsStage,
            overallStanding: null,
            scenario.GroupStandings,
            scenario.Matches,
            [scenario.GroupsStage, terminal],
            _clock);

        var derived = CrossGroupStandingAssembler.Build(
            scenario.GroupsStage.Groups,
            scenario.GroupStandings,
            3,
            scenario.Matches,
            scenario.GroupsStage.Regulation.StandingRules);

        terminal.FindSlot("A1")!.EntryId.Should().Be(scenario.GroupStandings[groupA.Id].EntryAt(1));
        terminal.FindSlot("B1")!.EntryId.Should().Be(scenario.GroupStandings[groupB.Id].EntryAt(1));
        terminal.FindSlot("BestThird")!.EntryId.Should().Be(derived.EntryAt(1));
    }

    [Fact]
    public void AcrossGroups_requires_matches()
    {
        var scenario = BuildGroupsScenario(groupCount: 2, teamsPerGroup: 4, strengthSpread: false);
        var terminal = CreateSlotStage(scenario.CompetitionId, "KO", ["T1"]);
        scenario.GroupsStage.ReplaceQualificationRules(
            new QualificationRules([AcrossGroupsPath(1, 3, 1, terminal.Id, "T1")]),
            _clock);

        var act = () => ApplyQualification.Execute(
            scenario.GroupsStage,
            overallStanding: null,
            scenario.GroupStandings,
            [scenario.GroupsStage, terminal],
            _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.QualificationMatchesRequired);
    }

    [Fact]
    public void Conditional_position_qualifies_when_points_meet_threshold()
    {
        var competitionId = CompetitionId.New();
        var league = CreateLeagueStage(competitionId, "League");
        var terminal = CreateSlotStage(competitionId, "KO", ["A", "B", "C"]);
        var first = EntryId.New();
        var second = EntryId.New();
        var third = EntryId.New();
        var standing = ManualStanding([(first, 1, 50), (second, 2, 45), (third, 3, 42)]);
        league.ReplaceQualificationRules(
            new QualificationRules(
            [
                PositionPath(1, 1, terminal.Id, "A"),
                PositionPath(2, 2, terminal.Id, "B"),
                ConditionalPositionPath(3, 3, 40, terminal.Id, "C")
            ]),
            _clock);

        var results = ApplyQualification.Execute(league, standing, [league, terminal], _clock);

        results.Should().HaveCount(3);
        terminal.FindSlot("A")!.EntryId.Should().Be(first);
        terminal.FindSlot("B")!.EntryId.Should().Be(second);
        terminal.FindSlot("C")!.EntryId.Should().Be(third);
    }

    [Fact]
    public void Conditional_position_skips_when_points_below_threshold()
    {
        var competitionId = CompetitionId.New();
        var league = CreateLeagueStage(competitionId, "League");
        var terminal = CreateSlotStage(competitionId, "KO", ["A", "B", "C"]);
        var first = EntryId.New();
        var second = EntryId.New();
        var third = EntryId.New();
        var standing = ManualStanding([(first, 1, 50), (second, 2, 45), (third, 3, 39)]);
        league.ReplaceQualificationRules(
            new QualificationRules(
            [
                PositionPath(1, 1, terminal.Id, "A"),
                PositionPath(2, 2, terminal.Id, "B"),
                ConditionalPositionPath(3, 3, 40, terminal.Id, "C")
            ]),
            _clock);

        var results = ApplyQualification.Execute(league, standing, [league, terminal], _clock);

        results.Should().HaveCount(2);
        terminal.FindSlot("A")!.EntryId.Should().Be(first);
        terminal.FindSlot("B")!.EntryId.Should().Be(second);
        terminal.FindSlot("C")!.EntryId.Should().BeNull();
    }

    [Fact]
    public void Conditional_position_reapply_clears_slot_when_condition_fails()
    {
        var competitionId = CompetitionId.New();
        var league = CreateLeagueStage(competitionId, "League");
        var terminal = CreateSlotStage(competitionId, "KO", ["C"]);
        var third = EntryId.New();
        league.ReplaceQualificationRules(
            new QualificationRules([ConditionalPositionPath(1, 1, 40, terminal.Id, "C")]),
            _clock);

        ApplyQualification.Execute(
            league,
            ManualStanding([(third, 1, 42)]),
            [league, terminal],
            _clock);
        terminal.FindSlot("C")!.EntryId.Should().Be(third);

        var results = ApplyQualification.Execute(
            league,
            ManualStanding([(third, 1, 39)]),
            [league, terminal],
            _clock);

        results.Should().BeEmpty();
        terminal.FindSlot("C")!.EntryId.Should().BeNull();
    }

    [Fact]
    public void Conditional_position_on_group_standing()
    {
        var competitionId = CompetitionId.New();
        var groups = CreateLeagueStage(competitionId, "Groups");
        var terminal = CreateSlotStage(competitionId, "KO", ["C"]);
        var groupA = groups.AddGroup("A", _clock);
        var first = EntryId.New();
        var second = EntryId.New();
        var third = EntryId.New();
        foreach (var e in new[] { first, second, third })
        {
            groups.AssignEntryToGroup(groupA.Id, e);
        }

        var standingA = ManualStanding([(first, 1, 50), (second, 2, 45), (third, 3, 42)]);
        groups.ReplaceQualificationRules(
            new QualificationRules(
            [
                new QualificationPath(
                    1,
                    QualificationSource.FromGroup(groupA.Id),
                    new QualificationSelection(SelectionMode.Position, 3),
                    new QualificationDestination(terminal.Id, "C"),
                    QualificationCondition.PointsAtLeast(40))
            ]),
            _clock);

        ApplyQualification.Execute(
            groups,
            overallStanding: null,
            new Dictionary<GroupId, Standing> { [groupA.Id] = standingA },
            [groups, terminal],
            _clock);

        terminal.FindSlot("C")!.EntryId.Should().Be(third);
    }

    [Fact]
    public void Conditional_position_on_across_groups_standing()
    {
        var scenario = BuildGroupsScenario(groupCount: 3, teamsPerGroup: 4, strengthSpread: true);
        var terminal = CreateSlotStage(scenario.CompetitionId, "KO", ["BestThird"]);
        var derived = CrossGroupStandingAssembler.Build(
            scenario.GroupsStage.Groups,
            scenario.GroupStandings,
            3,
            scenario.Matches,
            scenario.GroupsStage.Regulation.StandingRules);
        var bestThird = derived.Rows[0];
        scenario.GroupsStage.ReplaceQualificationRules(
            new QualificationRules(
            [
                new QualificationPath(
                    1,
                    QualificationSource.AcrossGroups(3),
                    new QualificationSelection(SelectionMode.Position, 1),
                    new QualificationDestination(terminal.Id, "BestThird"),
                    QualificationCondition.PointsAtLeast(bestThird.Points))
            ]),
            _clock);

        ApplyQualification.Execute(
            scenario.GroupsStage,
            overallStanding: null,
            scenario.GroupStandings,
            scenario.Matches,
            [scenario.GroupsStage, terminal],
            _clock);

        terminal.FindSlot("BestThird")!.EntryId.Should().Be(bestThird.EntryId);

        scenario.GroupsStage.ReplaceQualificationRules(
            new QualificationRules(
            [
                new QualificationPath(
                    1,
                    QualificationSource.AcrossGroups(3),
                    new QualificationSelection(SelectionMode.Position, 1),
                    new QualificationDestination(terminal.Id, "BestThird"),
                    QualificationCondition.PointsAtLeast(bestThird.Points + 1))
            ]),
            _clock);

        ApplyQualification.Execute(
            scenario.GroupsStage,
            overallStanding: null,
            scenario.GroupStandings,
            scenario.Matches,
            [scenario.GroupsStage, terminal],
            _clock);

        terminal.FindSlot("BestThird")!.EntryId.Should().BeNull();
    }

    [Fact]
    public void AcrossGroups_derived_then_Best_1_fills_slot()
    {
        var scenario = BuildGroupsScenario(groupCount: 3, teamsPerGroup: 4, strengthSpread: true);
        var terminal = CreateSlotStage(scenario.CompetitionId, "KO", ["BestThird"]);
        scenario.GroupsStage.ReplaceQualificationRules(
            new QualificationRules(
            [
                new QualificationPath(
                    1,
                    QualificationSource.AcrossGroups(3),
                    new QualificationSelection(SelectionMode.Best, 1),
                    new QualificationDestination(terminal.Id, "BestThird"))
            ]),
            _clock);

        ApplyQualification.Execute(
            scenario.GroupsStage,
            overallStanding: null,
            scenario.GroupStandings,
            scenario.Matches,
            [scenario.GroupsStage, terminal],
            _clock);

        var derived = CrossGroupStandingAssembler.Build(
            scenario.GroupsStage.Groups,
            scenario.GroupStandings,
            3,
            scenario.Matches,
            scenario.GroupsStage.Regulation.StandingRules);

        terminal.FindSlot("BestThird")!.EntryId.Should().Be(derived.EntryAt(1));
    }

    [Fact]
    public void Anti_overall_Best_is_not_across_groups_thirds()
    {
        var scenario = BuildGroupsScenario(groupCount: 4, teamsPerGroup: 4, strengthSpread: true);
        var allEntries = scenario.GroupStandings.Values
            .SelectMany(s => s.Rows.Select(r => r.EntryId))
            .Distinct()
            .ToArray();
        var overall = CalculateStanding.Execute(
            allEntries, scenario.Matches, scenario.GroupsStage.Regulation.StandingRules);
        var derivedThirds = CrossGroupStandingAssembler.Build(
            scenario.GroupsStage.Groups,
            scenario.GroupStandings,
            3,
            scenario.Matches,
            scenario.GroupsStage.Regulation.StandingRules);

        var overallBest1 = QualificationApplier.SelectEntries(
            overall, new QualificationSelection(SelectionMode.Best, 1))[0];
        var thirdsBest1 = QualificationApplier.SelectEntries(
            derivedThirds, new QualificationSelection(SelectionMode.Best, 1))[0];

        overallBest1.Should().Be(overall.EntryAt(1)!.Value);
        thirdsBest1.Should().Be(derivedThirds.EntryAt(1)!.Value);
        overallBest1.Should().NotBe(thirdsBest1);

        var terminal = CreateSlotStage(scenario.CompetitionId, "KO", ["OverallBest"]);
        scenario.GroupsStage.ReplaceQualificationRules(
            new QualificationRules(
            [
                new QualificationPath(
                    1,
                    QualificationSource.Overall(),
                    new QualificationSelection(SelectionMode.Best, 1),
                    new QualificationDestination(terminal.Id, "OverallBest"))
            ]),
            _clock);

        ApplyQualification.Execute(
            scenario.GroupsStage,
            overall,
            scenario.GroupStandings,
            scenario.Matches,
            [scenario.GroupsStage, terminal],
            _clock);

        terminal.FindSlot("OverallBest")!.EntryId.Should().Be(overallBest1);
        terminal.FindSlot("OverallBest")!.EntryId.Should().NotBe(thirdsBest1);
    }

    private static QualificationPath Path(
        int order,
        SelectionMode mode,
        int value,
        StageId stageId,
        string slotKey) =>
        new(
            order,
            QualificationSource.Overall(),
            new QualificationSelection(mode, value),
            new QualificationDestination(stageId, slotKey));

    private static QualificationPath PositionPath(
        int order,
        int position,
        StageId stageId,
        string slotKey) =>
        Path(order, SelectionMode.Position, position, stageId, slotKey);

    private static QualificationPath ConditionalPositionPath(
        int order,
        int position,
        int minimumPoints,
        StageId stageId,
        string slotKey) =>
        new(
            order,
            QualificationSource.Overall(),
            new QualificationSelection(SelectionMode.Position, position),
            new QualificationDestination(stageId, slotKey),
            QualificationCondition.PointsAtLeast(minimumPoints));

    private static Standing ManualStanding(IReadOnlyList<(EntryId EntryId, int Position, int Points)> rows) =>
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
                points: r.Points))
        ]);

    private static QualificationPath GroupPath(
        int order,
        GroupId groupId,
        SelectionMode mode,
        int value,
        StageId stageId,
        string slotKey) =>
        new(
            order,
            QualificationSource.FromGroup(groupId),
            new QualificationSelection(mode, value),
            new QualificationDestination(stageId, slotKey));

    private static QualificationPath AcrossGroupsPath(
        int order,
        int acrossGroupsPosition,
        int selectionPosition,
        StageId stageId,
        string slotKey) =>
        new(
            order,
            QualificationSource.AcrossGroups(acrossGroupsPosition),
            new QualificationSelection(SelectionMode.Position, selectionPosition),
            new QualificationDestination(stageId, slotKey));

    private static QualificationPath[] AcrossGroupsPaths(
        int acrossGroupsPosition,
        StageId stageId,
        IReadOnlyList<string> slotKeys) =>
        [
            ..slotKeys.Select((key, index) =>
                AcrossGroupsPath(index + 1, acrossGroupsPosition, index + 1, stageId, key))
        ];

    private GroupsScenario BuildGroupsScenario(int groupCount, int teamsPerGroup, bool strengthSpread)
    {
        var competitionId = CompetitionId.New();
        var groupsStage = CreateLeagueStage(competitionId, "Groups");
        var matches = new List<Match>();
        var groupStandings = new Dictionary<GroupId, Standing>();

        for (var g = 0; g < groupCount; g++)
        {
            var group = groupsStage.AddGroup(((char)('A' + g)).ToString(), _clock);
            var entries = CreateEntries(teamsPerGroup);
            foreach (var entry in entries)
            {
                groupsStage.AssignEntryToGroup(group.Id, entry);
            }

            matches.AddRange(BuildRoundRobin(groupsStage, entries, goalOffset: strengthSpread ? g : 0));
            groupStandings[group.Id] = CalculateStanding.Execute(
                entries, matches, groupsStage.Regulation.StandingRules);
        }

        return new GroupsScenario(competitionId, groupsStage, groupStandings, matches);
    }

    private sealed record GroupsScenario(
        CompetitionId CompetitionId,
        Stage GroupsStage,
        Dictionary<GroupId, Standing> GroupStandings,
        List<Match> Matches);

    private static EntryId[] CreateEntries(int count) =>
        [..Enumerable.Range(0, count).Select(_ => EntryId.New())];

    private Stage CreateLeagueStage(CompetitionId competitionId, string name) =>
        Stage.Create(competitionId, new StageName(name), SampleRegulations.Standard(), _clock);

    private Stage CreateSlotStage(CompetitionId competitionId, string name, string[] slotKeys)
    {
        var stage = Stage.Create(competitionId, new StageName(name), SampleRegulations.Standard(), _clock);
        foreach (var key in slotKeys)
        {
            stage.AddSlot(key, _clock);
        }

        return stage;
    }

    private List<Match> BuildRoundRobin(Stage stage, EntryId[] entries, int goalOffset = 0)
    {
        var matches = new List<Match>();
        for (var i = 0; i < entries.Length; i++)
        {
            for (var j = i + 1; j < entries.Length; j++)
            {
                // Deterministic: earlier index wins at home with goal margin based on indices.
                var homeGoals = entries.Length - i + goalOffset;
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
