// -----------------------------------------------------------------------
// <copyright file="QualificationForFormDestinationTests.cs" company="Stéphane ANDRE">
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
/// Place → Championship / Swiss = ForForm (Population materialization + Form-grain feed).
/// ExpectedFormParticipants: Resolved = Composition; Pending = ForForm without provenance.
/// </summary>
public sealed class QualificationForFormDestinationTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 9, 22, 14, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Place_to_championship_before_apply_exposes_pending_form_participants()
    {
        var competition = Competition.Create(new CompetitionName("FormChamp"), SampleRegulations.Standard(), _clock);
        var (source, groupA, groupB, _, _) = CreateGroupsSource(competition);
        var champ = Stage.Create(competition.Id, new StageName("Championnat"), SampleRegulations.Standard(), _clock);
        champ.AddMatchday(1, _clock);
        var e1 = competition.AddEntry(TeamId.New(), "E1", _clock).Id;
        var e2 = competition.AddEntry(TeamId.New(), "E2", _clock).Id;
        champ.ReplaceAffectationAuthoring([e1, e2], _clock);

        ReplaceEachGroupTop1Form(source, groupA.Id, groupB.Id, champ.Id);

        var schematic = StageSchematicAssembler.Assemble(champ, competition, [source, champ]);
        schematic.ExpectedFormParticipants.Should().NotBeNull();
        schematic.ExpectedFormParticipants!.Resolved.Should().HaveCount(2);
        schematic.ExpectedFormParticipants.Pending.Should().HaveCount(2);
        schematic.ExpectedFormParticipants.Pending.Should().OnlyContain(f => f.Kind == FeedKind.Qualification);
        schematic.Cases.Count(c => c.Entry != null).Should().Be(2);
        schematic.Cases.Count(c => c.FeedOrigin != null).Should().Be(2);
        champ.FormPathResolutions.Should().BeEmpty();
    }

    [Fact]
    public void Place_to_championship_fills_population_and_clears_pending_after_apply()
    {
        var competition = Competition.Create(new CompetitionName("FormChamp"), SampleRegulations.Standard(), _clock);
        var (source, groupA, groupB, tops, standings) = CreateGroupsSource(competition);
        var champ = Stage.Create(competition.Id, new StageName("Championnat"), SampleRegulations.Standard(), _clock);
        champ.AddMatchday(1, _clock);

        ReplaceEachGroupTop1Form(source, groupA.Id, groupB.Id, champ.Id);

        var applied = ApplyQualification.Execute(
            source,
            overallStanding: null,
            standings,
            [source, champ],
            _clock);

        applied.Should().HaveCount(2);
        champ.CompositionEntries.Select(e => e.EntryId).Should().BeEquivalentTo(tops);
        champ.FormPathResolutions.Should().HaveCount(2);
        champ.Slots.Should().BeEmpty();
        champ.Groups.Should().BeEmpty();
        source.Regulation.QualificationRules!.Paths.Should().OnlyContain(p => p.Destination.TargetsForm);

        var schematic = StageSchematicAssembler.Assemble(champ, competition, [source, champ]);
        schematic.FormatKind.Should().Be(StructureFormatKind.Championship);
        schematic.ExpectedFormParticipants.Should().NotBeNull();
        schematic.ExpectedFormParticipants!.Resolved.Should().HaveCount(2);
        schematic.ExpectedFormParticipants.Pending.Should().BeEmpty();
        schematic.Cases.Should().OnlyContain(c =>
            c.FormPosition.Kind == StageSchematicAssembler.FormKindRosterPlace);
        schematic.Cases.Count(c => c.Entry != null).Should().Be(2);
        schematic.Cases.Should().OnlyContain(c => c.FeedOrigin == null);
    }

    [Fact]
    public void Place_to_championship_partial_provenance_keeps_other_path_pending()
    {
        var competition = Competition.Create(new CompetitionName("FormPartial"), SampleRegulations.Standard(), _clock);
        var (source, groupA, groupB, tops, _) = CreateGroupsSource(competition);
        var champ = Stage.Create(competition.Id, new StageName("Championnat"), SampleRegulations.Standard(), _clock);
        champ.AddMatchday(1, _clock);
        ReplaceEachGroupTop1Form(source, groupA.Id, groupB.Id, champ.Id);

        var pathA = source.Regulation.QualificationRules!.Paths
            .Single(p => p.Source.GroupId!.Equals(groupA.Id));
        champ.AddResolvedPopulationEntry(tops[0], _clock);
        champ.RecordFormPathResolution(
            FormPathResolutionKey.FromQualification(source.Id, pathA),
            tops[0]);

        var schematic = StageSchematicAssembler.Assemble(champ, competition, [source, champ]);
        schematic.ExpectedFormParticipants!.Resolved.Should().HaveCount(1);
        schematic.ExpectedFormParticipants.Pending.Should().HaveCount(1);
        schematic.ExpectedFormParticipants.Pending.Single().GroupId.Should().Be(groupB.Id.Value);
        schematic.Cases.Count(c => c.Entry != null).Should().Be(1);
        schematic.Cases.Count(c => c.FeedOrigin != null).Should().Be(1);
    }

    [Fact]
    public void Place_to_championship_reapply_is_idempotent_on_provenance()
    {
        var competition = Competition.Create(new CompetitionName("FormIdem"), SampleRegulations.Standard(), _clock);
        var (source, groupA, groupB, tops, standings) = CreateGroupsSource(competition);
        var champ = Stage.Create(competition.Id, new StageName("Championnat"), SampleRegulations.Standard(), _clock);
        champ.AddMatchday(1, _clock);
        ReplaceEachGroupTop1Form(source, groupA.Id, groupB.Id, champ.Id);

        ApplyQualification.Execute(source, null, standings, [source, champ], _clock);
        ApplyQualification.Execute(source, null, standings, [source, champ], _clock);

        champ.FormPathResolutions.Should().HaveCount(2);
        champ.CompositionEntries.Select(e => e.EntryId).Should().BeEquivalentTo(tops);
    }

    [Fact]
    public void Place_to_swiss_fills_population_and_clears_pending_after_apply()
    {
        var competition = Competition.Create(new CompetitionName("FormSwiss"), SampleRegulations.Standard(), _clock);
        var (source, groupA, groupB, tops, standings) = CreateGroupsSource(competition);
        var swiss = Stage.Create(competition.Id, new StageName("Suisse"), SampleRegulations.Standard(), _clock);
        swiss.SetSwissSettings(new SwissSettings(3));

        ReplaceEachGroupTop1Form(source, groupA.Id, groupB.Id, swiss.Id);

        var applied = ApplyQualification.Execute(
            source,
            overallStanding: null,
            standings,
            [source, swiss],
            _clock);

        applied.Should().HaveCount(2);
        swiss.CompositionEntries.Select(e => e.EntryId).Should().BeEquivalentTo(tops);
        swiss.FormPathResolutions.Should().HaveCount(2);

        var schematic = StageSchematicAssembler.Assemble(swiss, competition, [source, swiss]);
        schematic.FormatKind.Should().Be(StructureFormatKind.Swiss);
        schematic.ExpectedFormParticipants!.Pending.Should().BeEmpty();
        schematic.ExpectedFormParticipants.Resolved.Should().HaveCount(2);
        schematic.Cases.Should().OnlyContain(c => c.FeedOrigin == null);
    }

    [Fact]
    public void Population_only_still_has_no_pending_form_participants()
    {
        var competition = Competition.Create(new CompetitionName("PopOnly"), SampleRegulations.Standard(), _clock);
        var (source, groupA, groupB, _, standings) = CreateGroupsSource(competition);
        var champ = Stage.Create(competition.Id, new StageName("Championnat"), SampleRegulations.Standard(), _clock);
        champ.AddMatchday(1, _clock);

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

        var schematic = StageSchematicAssembler.Assemble(champ, competition, [source, champ]);
        schematic.ExpectedFormParticipants!.Pending.Should().BeEmpty();
        schematic.ExpectedFormParticipants.Resolved.Should().HaveCount(2);
        champ.FormPathResolutions.Should().BeEmpty();
        schematic.Cases.Should().OnlyContain(c => c.FeedOrigin == null);
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
