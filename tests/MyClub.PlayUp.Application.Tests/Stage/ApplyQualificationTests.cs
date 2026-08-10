// -----------------------------------------------------------------------
// <copyright file="ApplyQualificationTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Stage;
using MyClub.PlayUp.Application.Standing;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Match;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stage;
using MyClub.PlayUp.Domain.Standing;
using Xunit;
using StageAggregate = MyClub.PlayUp.Domain.Stage.Stage;

namespace MyClub.PlayUp.Application.Tests.Stage;

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
    public void Execute_rejects_group_scoped_paths_until_multi_standing_orchestration()
    {
        var competitionId = CompetitionId.New();
        var groupId = GroupId.New();
        var league = CreateLeagueStage(competitionId, "Groups");
        var terminal = CreateSlotStage(competitionId, "Terminal", ["A1"]);
        var entries = CreateEntries(2);
        var matches = BuildRoundRobin(league, entries);
        var standing = CalculateStanding.Execute(entries, matches, league.Regulation.StandingRules);

        league.ReplaceQualificationRules(
            new QualificationRules(
            [
                new QualificationPath(
                    1,
                    QualificationSource.FromGroup(groupId),
                    new QualificationSelection(SelectionMode.Position, 1),
                    new QualificationDestination(terminal.Id, "A1"))
            ]),
            _clock);

        var act = () => ApplyQualification.Execute(league, standing, [league, terminal], _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.QualificationSourceNotSupported);
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

    private static EntryId[] CreateEntries(int count) =>
    [..Enumerable.Range(0, count).Select(_ => EntryId.New())];

    private StageAggregate CreateLeagueStage(CompetitionId competitionId, string name) =>
        StageAggregate.Create(competitionId, new StageName(name), SampleRegulations.Standard(), _clock);

    private StageAggregate CreateSlotStage(CompetitionId competitionId, string name, string[] slotKeys)
    {
        var stage = StageAggregate.Create(competitionId, new StageName(name), SampleRegulations.Standard(), _clock);
        foreach (var key in slotKeys)
        {
            stage.AddSlot(key, _clock);
        }

        return stage;
    }

    private List<Match> BuildRoundRobin(StageAggregate stage, EntryId[] entries)
    {
        var matches = new List<Match>();
        for (var i = 0; i < entries.Length; i++)
        {
            for (var j = i + 1; j < entries.Length; j++)
            {
                // Deterministic: earlier index wins at home with goal margin based on indices.
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
