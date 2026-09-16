// -----------------------------------------------------------------------
// <copyright file="B1M2VerticalSliceTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Stages;

/// <summary>
/// B1-M2 vertical slice: Winner Phase A → Prog inter → Population B → Draw pool → Placement.
/// SoT: Open minimaux B1-M2 (O1-a, O2-a, O4-a).
/// </summary>
public sealed class B1M2VerticalSliceTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 9, 16, 8, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Winner_to_population_to_draw_pool_to_slot_placement_without_slot_as_population_write_target()
    {
        var competitionId = CompetitionId.New();
        var phaseA = CreateKnockout(competitionId, "Tour1", ["T1-A", "T1-B", "T1-C", "T1-D"]);
        var phaseB = CreateKnockout(competitionId, "Tour2", ["T2-A", "T2-B"]);

        var home = EntryId.New();
        var away = EntryId.New();
        var home2 = EntryId.New();
        var away2 = EntryId.New();

        var (fixture1, match1) = AttachFinishedMatch(phaseA, home, away, homeGoals: 2, awayGoals: 0);
        var (fixture2, match2) = AttachFinishedMatch(phaseA, home2, away2, homeGoals: 1, awayGoals: 0);

        phaseA.ReplaceProgressionRules(
            new ProgressionRules(
            [
                new ProgressionPath(
                    fixture1,
                    ProgressionOutcome.Winner,
                    ProgressionDestination.ForPopulation(phaseB.Id)),
                new ProgressionPath(
                    fixture2,
                    ProgressionOutcome.Winner,
                    ProgressionDestination.ForPopulation(phaseB.Id))
            ]),
            _clock);

        ApplyProgressionOutcome.Execute(phaseA, fixture1, [match1], [phaseA, phaseB], _clock);
        ApplyProgressionOutcome.Execute(phaseA, fixture2, [match2], [phaseA, phaseB], _clock);

        phaseB.CompositionEntries.Select(e => e.EntryId).Should().BeEquivalentTo([home, home2]);
        phaseB.FindSlot("T2-A")!.EntryId.Should().BeNull();
        phaseB.FindSlot("T2-B")!.EntryId.Should().BeNull();
        phaseB.DirectAssignments.Should().BeEmpty();

        var competition = Competition.Create(new CompetitionName("B1-M2"), SampleRegulations.Standard(), _clock);
        competition.AddEntry(TeamId.New(), "DecoyActive", EntryId.New(), _clock);
        competition.AddEntry(TeamId.New(), "Winner1", home, _clock);
        competition.AddEntry(TeamId.New(), "Winner2", home2, _clock);

        var inputs = DrawInputsFactory.CreateDefault(competition, phaseB, DrawResolutionKind.Slot);
        inputs.Entries.Should().BeEquivalentTo([home, home2]);
        inputs.Entries.Should().NotContain(competition.Entries[0].Id);

        var draw = phaseB.CreateDraw(DrawResolutionKind.Slot, _clock);
        phaseB.ConfigureDrawInputs(draw.Id, inputs);
        phaseB.RecordDrawResolution(
            draw.Id,
            DrawResolution.ResolvedSlots(
            [
                new SlotDrawPlacement(home, "T2-A"),
                new SlotDrawPlacement(home2, "T2-B")
            ]),
            _clock);
        phaseB.PublishDraw(draw.Id, _clock);
        ApplyDraw.Execute(phaseB, draw.Id, _clock);

        phaseB.FindSlot("T2-A")!.EntryId.Should().Be(home);
        phaseB.FindSlot("T2-B")!.EntryId.Should().Be(home2);
        phaseB.CompositionEntries.Select(e => e.EntryId).Should().BeEquivalentTo([home, home2]);
        phaseB.DirectAssignments.Should().BeEmpty();
    }

    private Stage CreateKnockout(CompetitionId competitionId, string name, string[] slotKeys)
    {
        var stage = Stage.Create(competitionId, new StageName(name), SampleRegulations.Standard(), _clock);
        stage.AddRound("R1", new TieFormat(TieFormat.SingleLeg, aggregateScoring: false), _clock);
        foreach (var key in slotKeys)
        {
            stage.AddSlot(key);
        }

        return stage;
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
