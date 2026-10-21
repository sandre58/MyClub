// -----------------------------------------------------------------------
// <copyright file="StructureInboundCompositionAssemblerTests.cs" company="Stéphane ANDRE">
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

namespace MyClub.PlayUp.Application.Tests.Reads;

public sealed class StructureInboundCompositionAssemblerTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 16, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Assemble_projects_qualification_path_per_inbound_rule()
    {
        var competition = Competition.Create(
            new CompetitionName("Qual inbound"),
            SampleRegulations.Standard(),
            _clock);
        var source = Stage.Create(competition.Id, new StageName("Groups"), SampleRegulations.Standard(), _clock);
        var final = Stage.Create(competition.Id, new StageName("Final"), SampleRegulations.Standard(), _clock);
        competition.AddStage(source.Id, _clock);
        competition.AddStage(final.Id, _clock);
        var groupA = source.AddGroup("Poule A", _clock);

        source.ReplaceQualificationRules(
            new QualificationRules(
            [
                new QualificationPath(
                    1,
                    QualificationSource.FromGroup(groupA.Id),
                    new QualificationSelection(SelectionMode.Range, 1, 2),
                    QualificationDestination.ForPopulation(final.Id))
            ]),
            _clock);

        var inbound = StructureInboundCompositionAssembler.Assemble(
            competition,
            final,
            [source, final]);

        inbound.Should().NotBeNull();
        inbound.Rules.Should().ContainSingle();
        var rule = inbound.Rules[0];
        rule.QualificationPath.Should().NotBeNull();
        rule.QualificationPath.Order.Should().Be(1);
        rule.QualificationPath.SelectionMode.Should().Be(SelectionMode.Range);
        rule.QualificationPath.SelectionValue.Should().Be(1);
        rule.QualificationPath.SelectionEndValue.Should().Be(2);
        rule.QualificationPath.GroupName.Should().Be("Poule A");
        rule.QualificationPath.DestinationStageId.Should().Be(final.Id.Value);
        rule.ProgressionPath.Should().BeNull();
    }

    [Fact]
    public void Assemble_resolves_for_form_seats_after_apply_qualification()
    {
        var competition = Competition.Create(
            new CompetitionName("Qual form inbound"),
            SampleRegulations.Standard(),
            _clock);
        var (source, groupA, groupB, tops, standings) = CreateGroupsSource(competition);
        var champ = Stage.Create(competition.Id, new StageName("Championnat"), SampleRegulations.Standard(), _clock);
        champ.AddMatchday(1, _clock);
        competition.AddStage(source.Id, _clock);
        competition.AddStage(champ.Id, _clock);
        ReplaceEachGroupTop1Form(source, groupA.Id, groupB.Id, champ.Id);

        ApplyQualification.Execute(source, null, standings, [source, champ], _clock);
        PrepareStage.Execute(champ, [source, champ], _clock);
        StartStage.Execute(champ, _clock);

        var inbound = StructureInboundCompositionAssembler.Assemble(
            competition,
            champ,
            [source, champ]);

        inbound.Should().NotBeNull();
        inbound.ConfiguredVolume.Should().Be(2);
        inbound.ResolvedCount.Should().Be(2);
        inbound.Rules.Should().HaveCount(2);
        inbound.Rules
            .SelectMany(rule => rule.Seats)
            .Should()
            .AllSatisfy(seat => seat.EntryId.Should().NotBeNull());
        inbound.Rules
            .SelectMany(rule => rule.Seats)
            .Select(seat => seat.EntryId!.Value)
            .Should()
            .BeEquivalentTo(tops.Select(id => id.Value));
    }

    [Fact]
    public void Assemble_resolves_population_target_seats_from_composition_after_apply()
    {
        var competition = Competition.Create(
            new CompetitionName("Qual population inbound"),
            SampleRegulations.Standard(),
            _clock);
        var (source, groupA, groupB, tops, standings) = CreateGroupsSource(competition);
        var champ = Stage.Create(competition.Id, new StageName("Championnat"), SampleRegulations.Standard(), _clock);
        champ.AddMatchday(1, _clock);
        competition.AddStage(source.Id, _clock);
        competition.AddStage(champ.Id, _clock);

        var intent = new QualificationIntent(
            IntentId.New(),
            order: 1,
            QualificationIntentSourceKind.EachGroup,
            positionFrom: 1,
            positionTo: 1,
            champ.Id);
        source.ReplaceQualificationRules(
            QualificationRules.FromIntents([intent], [groupA.Id, groupB.Id]),
            _clock);

        ApplyQualification.Execute(source, null, standings, [source, champ], _clock);
        PrepareStage.Execute(champ, [source, champ], _clock);

        var inbound = StructureInboundCompositionAssembler.Assemble(
            competition,
            champ,
            [source, champ]);

        inbound.Should().NotBeNull();
        inbound.ConfiguredVolume.Should().Be(2);
        inbound.Rules.SelectMany(rule => rule.Seats).Should().HaveCount(2);
        inbound.ResolvedCount.Should().Be(2);
        inbound.Rules
            .SelectMany(rule => rule.Seats)
            .Select(seat => seat.EntryId!.Value)
            .Should()
            .BeEquivalentTo(tops.Select(id => id.Value));
    }

    [Fact]
    public void Assemble_keeps_resolved_count_after_competition_withdraw()
    {
        var competition = Competition.Create(
            new CompetitionName("Qual withdrawn inbound"),
            SampleRegulations.Standard(),
            _clock);
        var (source, groupA, groupB, tops, standings) = CreateGroupsSource(competition);
        var champ = Stage.Create(competition.Id, new StageName("Championnat"), SampleRegulations.Standard(), _clock);
        champ.AddMatchday(1, _clock);
        competition.AddStage(source.Id, _clock);
        competition.AddStage(champ.Id, _clock);
        ReplaceEachGroupTop1Form(source, groupA.Id, groupB.Id, champ.Id);

        ApplyQualification.Execute(source, null, standings, [source, champ], _clock);
        PrepareStage.Execute(champ, [source, champ], _clock);
        competition.Prepare(_clock);
        competition.Start(_clock);
        WithdrawEntry.Execute(competition, tops[0], [source, champ], [], _clock);

        var inbound = StructureInboundCompositionAssembler.Assemble(
            competition,
            champ,
            [source, champ]);

        inbound.Should().NotBeNull();
        inbound.ResolvedCount.Should().Be(2);
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

    private void ReplaceEachGroupTop1Form(
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
            destinationStageId,
            destinationForm: true);
        source.ReplaceQualificationRules(
            QualificationRules.FromIntents([intent], [groupA, groupB]),
            _clock);
    }
}
