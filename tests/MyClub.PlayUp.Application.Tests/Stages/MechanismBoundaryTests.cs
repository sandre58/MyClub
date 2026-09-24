// -----------------------------------------------------------------------
// <copyright file="MechanismBoundaryTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Standings;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using MyNet.Primitives;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Stages;

/// <summary>
/// Anti-confusion boundaries (7.0.8.4): Qualification ≠ Draw ≠ Progression ≠ DirectAssignment.
/// </summary>
public sealed class MechanismBoundaryTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 11, 18, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Qualification_does_not_create_Draw_SeedMap_or_DirectAssignment()
    {
        var competitionId = CompetitionId.New();
        var league = CreateLeague(competitionId, "League");
        var terminal = CreateSlots(competitionId, "Terminal", ["Champ"]);
        var entries = CreateEntries(2);
        var matches = BuildRoundRobin(league, entries);
        var standing = CalculateStanding.Execute(entries, matches, league.Regulation.StandingRules.OrThrow());
        league.ReplaceQualificationRules(
            new QualificationRules(
            [
                new QualificationPath(
                    1,
                    QualificationSource.Overall(),
                    new QualificationSelection(SelectionMode.Position, 1),
                    QualificationDestination.ForPopulation(terminal.Id))
            ]),
            _clock);

        ApplyQualification.Execute(league, standing, [league, terminal], _clock);

        league.Draws.Should().BeEmpty();
        terminal.Draws.Should().BeEmpty();
        terminal.DirectAssignments.Should().BeEmpty();
        terminal.CompositionEntries.Select(e => e.EntryId).Should().Equal(standing.EntryAt(1)!.Value);
        terminal.FindSlot("Champ")!.EntryId.Should().BeNull();
    }

    [Fact]
    public void Progression_does_not_create_Draw_or_DirectAssignment()
    {
        var competitionId = CompetitionId.New();
        var source = CreateKnockout(competitionId, "QF", ["QF1-A", "QF1-B"]);
        var destination = CreateKnockout(competitionId, "SF", ["SF1-A"]);
        var home = EntryId.New();
        var away = EntryId.New();
        var (fixtureId, match) = AttachFinishedMatch(source, home, away, 2, 0);
        source.ReplaceProgressionRules(
            new ProgressionRules(
            [
                new ProgressionPath(fixtureId.Value.ToString("N"),
                    ProgressionOutcome.Winner,
                    ProgressionDestination.ForPopulation(destination.Id))
            ]),
            _clock);

        ApplyProgressionOutcome.Execute(source, fixtureId, [match], [source, destination], _clock);

        source.Draws.Should().BeEmpty();
        destination.Draws.Should().BeEmpty();
        destination.DirectAssignments.Should().BeEmpty();
        destination.CompositionEntries.Select(e => e.EntryId).Should().Equal(home);
        destination.FindSlot("SF1-A")!.EntryId.Should().BeNull();
    }

    [Fact]
    public void SeedMap_alone_does_not_fill_slots_without_Draw_Apply()
    {
        var stage = CreateSlots(CompetitionId.New(), "KO", ["SF1-A", "SF1-B"]);
        var a = EntryId.New();
        var b = EntryId.New();
        var seedMap = new SeedMap(new Dictionary<EntryId, int> { [a] = 1, [b] = 2 });

        var draw = stage.CreateDraw(DrawResolutionKind.Slot, _clock);
        stage.ConfigureDrawInputs(draw.Id, DrawInputs.ForSlot([a, b], seedMap));

        stage.FindSlot("SF1-A")!.EntryId.Should().BeNull();
        stage.FindSlot("SF1-B")!.EntryId.Should().BeNull();
        stage.DirectAssignments.Should().BeEmpty();
        draw.Status.Should().Be(DrawStatus.Draft);
        draw.Inputs!.SeedMap!.Seeds[a].Should().Be(1);
    }

    [Fact]
    public void Qualification_population_and_Draw_remain_separate()
    {
        var competitionId = CompetitionId.New();
        var league = CreateLeague(competitionId, "League");
        var qualified = CreateSlots(competitionId, "Q", ["Q1", "Q2"]);
        var knockout = CreateSlots(competitionId, "KO", ["SF1-A", "SF1-B"]);
        var entries = CreateEntries(2);
        var matches = BuildRoundRobin(league, entries);
        var standing = CalculateStanding.Execute(entries, matches, league.Regulation.StandingRules.OrThrow());
        league.ReplaceQualificationRules(
            new QualificationRules(
            [
                new QualificationPath(
                    1,
                    QualificationSource.Overall(),
                    new QualificationSelection(SelectionMode.Position, 1),
                    QualificationDestination.ForPopulation(qualified.Id)),
                new QualificationPath(
                    2,
                    QualificationSource.Overall(),
                    new QualificationSelection(SelectionMode.Position, 2),
                    QualificationDestination.ForPopulation(qualified.Id))
            ]),
            _clock);

        ApplyQualification.Execute(league, standing, [league, qualified], _clock);
        var pool = qualified.CompositionEntries.Select(e => e.EntryId).ToArray();
        pool.Should().HaveCount(2);
        qualified.FindSlot("Q1")!.EntryId.Should().BeNull();
        qualified.FindSlot("Q2")!.EntryId.Should().BeNull();

        var draw = knockout.CreateDraw(DrawResolutionKind.Slot, _clock);
        knockout.ConfigureDrawInputs(draw.Id, DrawInputs.ForSlot(pool));
        knockout.RecordDrawResolution(
            draw.Id,
            DrawResolution.ResolvedSlots(
            [
                new SlotDrawPlacement(pool[0], "SF1-A"),
                new SlotDrawPlacement(pool[1], "SF1-B")
            ]),
            _clock);
        knockout.PublishDraw(draw.Id, _clock);
        ApplyDraw.Execute(knockout, draw.Id, _clock);

        qualified.CompositionEntries.Select(e => e.EntryId).Should().BeEquivalentTo(pool);
        qualified.Draws.Should().BeEmpty();
        knockout.Draws.Should().ContainSingle();
        knockout.FindSlot("SF1-A")!.EntryId.Should().Be(pool[0]);
        knockout.DirectAssignments.Should().BeEmpty();
        draw.Status.Should().Be(DrawStatus.Published);
    }

    private static EntryId[] CreateEntries(int count) =>
        [..Enumerable.Range(0, count).Select(_ => EntryId.New())];

    private Stage CreateLeague(CompetitionId competitionId, string name) =>
        Stage.Create(competitionId, new StageName(name), SampleRegulations.Standard(), _clock);

    private Stage CreateSlots(CompetitionId competitionId, string name, string[] slotKeys)
    {
        var stage = Stage.Create(competitionId, new StageName(name), SampleRegulations.Standard(), _clock);
        foreach (var key in slotKeys)
        {
            stage.AddSlot(key);
        }

        return stage;
    }

    private Stage CreateKnockout(CompetitionId competitionId, string name, string[] slotKeys)
    {
        var stage = CreateSlots(competitionId, name, slotKeys);
        stage.AddRound("R1", new TieFormat(TieFormat.SingleLeg, aggregateScoring: false), _clock);
        return stage;
    }

    private List<Match> BuildRoundRobin(Stage stage, EntryId[] entries)
    {
        var matches = new List<Match>();
        for (var i = 0; i < entries.Length; i++)
        {
            for (var j = i + 1; j < entries.Length; j++)
            {
                var match = Match.Create(stage.CompetitionId, stage.Id, entries[i], entries[j], _clock);
                match.Start(_clock);
                match.Finish(new MatchResult(ResultType.Played, new Score(entries.Length - i, 0)), _clock);
                matches.Add(match);
            }
        }

        return matches;
    }

    private (FixtureId FixtureId, Match Match) AttachFinishedMatch(
        Stage stage,
        EntryId home,
        EntryId away,
        int homeGoals,
        int awayGoals)
    {
        var fixture = stage.AddFixture(stage.Rounds[0].Id, _clock);
        var match = Match.Create(stage.CompetitionId, stage.Id, home, away, _clock);
        stage.AttachMatch(fixture.Id, match.Id, legIndex: 1, _clock);
        match.Start(_clock);
        match.Finish(new MatchResult(ResultType.Played, new Score(homeGoals, awayGoals)), _clock);
        return (fixture.Id, match);
    }
}
