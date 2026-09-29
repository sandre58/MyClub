// -----------------------------------------------------------------------
// <copyright file="QualPopulationVerticalSliceTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Standings;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Domain.Standings;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Stages;

/// <summary>
/// Qual V2 vertical slice: Groups standing → Qual ForPopulation(phaseB) → population → Draw pool → Placement.
/// </summary>
public sealed class QualPopulationVerticalSliceTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 9, 16, 9, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Groups_qualification_to_population_to_draw_pool_to_slot_placement()
    {
        var competitionId = CompetitionId.New();
        var groups = Stage.Create(competitionId, new StageName("Groups"), SampleRegulations.Standard(), _clock);
        var phaseB = CreateKnockout(competitionId, "Tour2", ["T2-A", "T2-B", "T2-C", "T2-D"]);

        var groupA = groups.AddGroup("A", _clock);
        var groupB = groups.AddGroup("B", _clock);
        var a1 = EntryId.New();
        var a2 = EntryId.New();
        var b1 = EntryId.New();
        var b2 = EntryId.New();
        foreach (var entry in new[] { a1, a2 })
        {
            groups.AssignEntryToGroup(groupA.Id, entry);
        }

        foreach (var entry in new[] { b1, b2 })
        {
            groups.AssignEntryToGroup(groupB.Id, entry);
        }

        var matches = new List<Match>
        {
            Finish(Match.Create(competitionId, groups.Id, a1, a2, _clock), 2, 0),
            Finish(Match.Create(competitionId, groups.Id, b1, b2, _clock), 3, 1)
        };
        var standingA = CalculateStanding.Execute(
            groupA.EntryIds, matches, groups.Regulation.StandingRules.OrThrow());
        var standingB = CalculateStanding.Execute(
            groupB.EntryIds, matches, groups.Regulation.StandingRules.OrThrow());

        groups.ReplaceQualificationRules(
            new QualificationRules(
            [
                new QualificationPath(
                    1,
                    QualificationSource.FromGroup(groupA.Id),
                    new QualificationSelection(SelectionMode.Position, 1),
                    QualificationDestination.ForPopulation(phaseB.Id)),
                new QualificationPath(
                    2,
                    QualificationSource.FromGroup(groupB.Id),
                    new QualificationSelection(SelectionMode.Position, 1),
                    QualificationDestination.ForPopulation(phaseB.Id)),
                new QualificationPath(
                    3,
                    QualificationSource.FromGroup(groupA.Id),
                    new QualificationSelection(SelectionMode.Position, 2),
                    QualificationDestination.ForPopulation(phaseB.Id)),
                new QualificationPath(
                    4,
                    QualificationSource.FromGroup(groupB.Id),
                    new QualificationSelection(SelectionMode.Position, 2),
                    QualificationDestination.ForPopulation(phaseB.Id))
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
            [groups, phaseB],
            _clock);

        phaseB.CompositionEntries.Select(e => e.EntryId)
            .Should().BeEquivalentTo([a1, b1, a2, b2]);
        phaseB.FindSlot("T2-A")!.EntryId.Should().BeNull();
        phaseB.FindSlot("T2-B")!.EntryId.Should().BeNull();
        phaseB.FindSlot("T2-C")!.EntryId.Should().BeNull();
        phaseB.FindSlot("T2-D")!.EntryId.Should().BeNull();
        phaseB.DirectAssignments.Should().BeEmpty();

        var competition = Competition.Create(new CompetitionName("Qual-Pop"), SampleRegulations.Standard(), _clock);
        competition.AddEntry(TeamId.New(), "DecoyActive", EntryId.New(), _clock);
        competition.AddEntry(TeamId.New(), "A1", a1, _clock);
        competition.AddEntry(TeamId.New(), "B1", b1, _clock);
        competition.AddEntry(TeamId.New(), "A2", a2, _clock);
        competition.AddEntry(TeamId.New(), "B2", b2, _clock);

        var inputs = DrawInputsFactory.CreateDefault(phaseB, DrawResolutionKind.Slot);
        inputs.Entries.Should().BeEquivalentTo([a1, b1, a2, b2]);
        inputs.Entries.Should().NotContain(competition.Entries[0].Id);

        var pool = phaseB.CompositionEntries.Select(e => e.EntryId).ToArray();
        var draw = phaseB.CreateDraw(DrawResolutionKind.Slot, _clock);
        phaseB.ConfigureDrawInputs(draw.Id, inputs);
        phaseB.RecordDrawResolution(
            draw.Id,
            DrawResolution.ResolvedSlots(
            [
                new SlotDrawPlacement(pool[0], "T2-A"),
                new SlotDrawPlacement(pool[1], "T2-B"),
                new SlotDrawPlacement(pool[2], "T2-C"),
                new SlotDrawPlacement(pool[3], "T2-D")
            ]),
            _clock);
        phaseB.PublishDraw(draw.Id, _clock);
        ApplyDraw.Execute(phaseB, draw.Id, _clock);

        phaseB.FindSlot("T2-A")!.EntryId.Should().Be(pool[0]);
        phaseB.FindSlot("T2-B")!.EntryId.Should().Be(pool[1]);
        phaseB.FindSlot("T2-C")!.EntryId.Should().Be(pool[2]);
        phaseB.FindSlot("T2-D")!.EntryId.Should().Be(pool[3]);
        phaseB.CompositionEntries.Select(e => e.EntryId).Should().BeEquivalentTo(pool);
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

    private Match Finish(Match match, int homeGoals, int awayGoals)
    {
        match.Start(_clock);
        match.Finish(new MatchResult(ResultType.Played, new Score(homeGoals, awayGoals)), _clock);
        return match;
    }
}
