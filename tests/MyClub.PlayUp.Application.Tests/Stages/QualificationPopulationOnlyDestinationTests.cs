// -----------------------------------------------------------------------
// <copyright file="QualificationPopulationOnlyDestinationTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Domain.Standings;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Stages;

/// <summary>
/// B1 / C1 — Qual → Championship / Swiss = Population only (matrice Placement).
/// No form feeds, no Slot/Group dual-write. Schematic RosterPlace stays FeedOrigin-null.
/// </summary>
public sealed class QualificationPopulationOnlyDestinationTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 9, 22, 14, 0, 0, TimeSpan.Zero));

    [Fact]
    public void B1_qual_top_of_each_group_fills_championship_population_only()
    {
        var competition = Competition.Create(new CompetitionName("B1"), SampleRegulations.Standard(), _clock);
        var (source, groupA, groupB, tops, standings) = CreateGroupsSource(competition);
        var champ = Stage.Create(competition.Id, new StageName("Championnat"), SampleRegulations.Standard(), _clock);
        champ.AddMatchday(1, _clock);

        ReplaceEachGroupTop1Population(source, groupA.Id, groupB.Id, champ.Id);

        var applied = ApplyQualification.Execute(
            source,
            overallStanding: null,
            standings,
            [source, champ],
            _clock);

        applied.Should().HaveCount(2);
        champ.CompositionEntries.Select(e => e.EntryId).Should().BeEquivalentTo(tops);
        champ.Slots.Should().BeEmpty();
        champ.Groups.Should().BeEmpty();

        var schematic = StageSchematicAssembler.Assemble(champ, competition, [source, champ]);
        schematic.FormatKind.Should().Be(StructureFormatKind.Championship);
        schematic.Cases.Should().OnlyContain(c =>
            c.FormPosition.Kind == StageSchematicAssembler.FormKindRosterPlace
            && c.FeedOrigin == null);
        schematic.GroupFeeds.Should().BeNullOrEmpty();
        schematic.FormFeed.Should().BeNull();
        schematic.Cases.Count(c => c.Entry != null).Should().Be(2);
    }

    [Fact]
    public void C1_qual_top_of_each_group_fills_swiss_population_only()
    {
        var competition = Competition.Create(new CompetitionName("C1"), SampleRegulations.Standard(), _clock);
        var (source, groupA, groupB, tops, standings) = CreateGroupsSource(competition);
        var swiss = Stage.Create(competition.Id, new StageName("Suisse"), SampleRegulations.Standard(), _clock);
        swiss.SetSwissSettings(new SwissSettings(3));

        ReplaceEachGroupTop1Population(source, groupA.Id, groupB.Id, swiss.Id);

        var applied = ApplyQualification.Execute(
            source,
            overallStanding: null,
            standings,
            [source, swiss],
            _clock);

        applied.Should().HaveCount(2);
        swiss.CompositionEntries.Select(e => e.EntryId).Should().BeEquivalentTo(tops);
        swiss.Slots.Should().BeEmpty();
        swiss.Groups.Should().BeEmpty();

        var schematic = StageSchematicAssembler.Assemble(swiss, competition, [source, swiss]);
        schematic.FormatKind.Should().Be(StructureFormatKind.Swiss);
        schematic.Cases.Should().OnlyContain(c =>
            c.FormPosition.Kind == StageSchematicAssembler.FormKindRosterPlace
            && c.FeedOrigin == null);
        schematic.GroupFeeds.Should().BeNullOrEmpty();
        schematic.FormFeed.Should().BeNull();
        schematic.Cases.Count(c => c.Entry != null).Should().Be(2);
    }

    private static Standing ManualStanding(IReadOnlyList<(EntryId EntryId, int Position, int Points)> rows) =>
        new(
        [
            .. rows.Select(r => new StandingRow(
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

    private (Stage Source, Group GroupA, Group GroupB, EntryId[] Tops, Dictionary<GroupId, Standing> Standings)
        CreateGroupsSource(Competition competition)
    {
        var source = Stage.Create(competition.Id, new StageName("Poules"), SampleRegulations.Standard(), _clock);
        var groupA = source.AddGroup("A", _clock);
        var groupB = source.AddGroup("B", _clock);
        var eA1 = competition.AddEntry(TeamId.New(), "A1", _clock).Id;
        var eA2 = competition.AddEntry(TeamId.New(), "A2", _clock).Id;
        var eB1 = competition.AddEntry(TeamId.New(), "B1", _clock).Id;
        var eB2 = competition.AddEntry(TeamId.New(), "B2", _clock).Id;
        source.AssignEntryToGroup(groupA.Id, eA1);
        source.AssignEntryToGroup(groupA.Id, eA2);
        source.AssignEntryToGroup(groupB.Id, eB1);
        source.AssignEntryToGroup(groupB.Id, eB2);
        source.AddMatchday(1, _clock);

        var standingA = ManualStanding([(eA1, 1, 6), (eA2, 2, 3)]);
        var standingB = ManualStanding([(eB1, 1, 6), (eB2, 2, 3)]);
        return (
            source,
            groupA,
            groupB,
            [eA1, eB1],
            new Dictionary<GroupId, Standing>
            {
                [groupA.Id] = standingA,
                [groupB.Id] = standingB
            });
    }

    private void ReplaceEachGroupTop1Population(
        Stage source,
        GroupId groupA,
        GroupId groupB,
        StageId destinationStageId)
    {
        var intent = new QualificationIntent(
            IntentId.New(),
            order: 1,
            QualificationIntentSourceKind.EachGroup,
            positionFrom: 1,
            positionTo: 1,
            destinationStageId);
        source.ReplaceQualificationRules(
            QualificationRules.FromIntents([intent], [groupA, groupB]),
            _clock);
    }
}
