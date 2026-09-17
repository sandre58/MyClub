// -----------------------------------------------------------------------
// <copyright file="QualificationDrawOrchestrationTests.cs" company="Stéphane ANDRE">
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
/// Orchestration boundaries: Qualification → Population → SeedMap → Draw → ApplyDraw.
/// </summary>
public sealed class QualificationDrawOrchestrationTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 11, 15, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Qualification_population_then_SeedMap_then_Draw_Apply_keeps_mechanisms_separate()
    {
        var competitionId = CompetitionId.New();
        var league = CreateLeagueStage(competitionId, "League");
        var qualified = CreateSlotStage(competitionId, "Qualified", ["Q1", "Q2", "Q3", "Q4"]);
        var knockout = CreateSlotStage(competitionId, "KO", ["SF1-A", "SF1-B", "SF2-A", "SF2-B"]);
        var entries = CreateEntries(4);
        var matches = BuildRoundRobin(league, entries);
        var standing = CalculateStanding.Execute(entries, matches, league.Regulation.StandingRules.OrThrow());

        league.ReplaceQualificationRules(
            new QualificationRules(
            [
                Path(1, SelectionMode.Position, 1, qualified.Id),
                Path(2, SelectionMode.Position, 2, qualified.Id),
                Path(3, SelectionMode.Position, 3, qualified.Id),
                Path(4, SelectionMode.Position, 4, qualified.Id)
            ]),
            _clock);

        ApplyQualification.Execute(league, standing, [league, qualified], _clock);

        qualified.CompositionEntries.Should().HaveCount(4);
        qualified.FindSlot("Q1")!.EntryId.Should().BeNull();
        qualified.FindSlot("Q2")!.EntryId.Should().BeNull();
        qualified.FindSlot("Q3")!.EntryId.Should().BeNull();
        qualified.FindSlot("Q4")!.EntryId.Should().BeNull();

        var pool = qualified.CompositionEntries.Select(e => e.EntryId).ToList();
        var seedMap = new SeedMap(new Dictionary<EntryId, int>
        {
            [pool[0]] = 1, [pool[1]] = 2, [pool[2]] = 3, [pool[3]] = 4
        });

        var draw = knockout.CreateDraw(DrawResolutionKind.Slot, _clock);
        knockout.ConfigureDrawInputs(draw.Id, DrawInputs.ForSlot(pool, seedMap));
        knockout.RecordDrawResolution(
            draw.Id,
            DrawResolution.ResolvedSlots(
            [
                new SlotDrawPlacement(pool[0], "SF1-A"),
                new SlotDrawPlacement(pool[3], "SF1-B"),
                new SlotDrawPlacement(pool[1], "SF2-A"),
                new SlotDrawPlacement(pool[2], "SF2-B")
            ]),
            _clock);
        knockout.PublishDraw(draw.Id, _clock);

        var result = ApplyDraw.Execute(knockout, draw.Id, _clock);

        result.SlotInstructions.Should().HaveCount(4);
        knockout.FindSlot("SF1-A")!.EntryId.Should().Be(pool[0]);
        knockout.FindSlot("SF1-B")!.EntryId.Should().Be(pool[3]);
        knockout.FindSlot("SF2-A")!.EntryId.Should().Be(pool[1]);
        knockout.FindSlot("SF2-B")!.EntryId.Should().Be(pool[2]);

        // Qualification population stage untouched by Draw apply on knockout.
        qualified.CompositionEntries.Select(e => e.EntryId).Should().BeEquivalentTo(pool);
        qualified.DirectAssignments.Should().BeEmpty();
        knockout.DirectAssignments.Should().BeEmpty();

        // SeedMap prepared the Draw; it is not a destination map (Seed 1 ≠ SF1-A automatic).
        draw.Inputs!.SeedMap!.Seeds[pool[0]].Should().Be(1);
        knockout.FindSlot("SF1-A")!.EntryId.Should().Be(pool[0]);
        knockout.FindSlot("SF1-B")!.EntryId.Should().NotBe(pool[1]);
    }

    private static QualificationPath Path(
        int order,
        SelectionMode mode,
        int value,
        StageId stageId) =>
        new(
            order,
            QualificationSource.Overall(),
            new QualificationSelection(mode, value),
            QualificationDestination.ForPopulation(stageId));

    private static EntryId[] CreateEntries(int count) =>
        [..Enumerable.Range(0, count).Select(_ => EntryId.New())];

    private Stage CreateLeagueStage(CompetitionId competitionId, string name) =>
        Stage.Create(competitionId, new StageName(name), SampleRegulations.Standard(), _clock);

    private Stage CreateSlotStage(CompetitionId competitionId, string name, string[] slotKeys)
    {
        var stage = Stage.Create(competitionId, new StageName(name), SampleRegulations.Standard(), _clock);
        foreach (var key in slotKeys)
        {
            stage.AddSlot(key);
        }

        return stage;
    }

    private List<Match> BuildRoundRobin(Stage stage, EntryId[] entries)
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
