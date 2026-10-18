// -----------------------------------------------------------------------
// <copyright file="StructurePresentedPhaseRosterDiagnosticsTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Qualification;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Domain.Standings;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Reads;

public sealed class StructurePresentedPhaseRosterDiagnosticsTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 19, 14, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Qual_form_champ_ready_partial_presents_awaiting_qual_not_phase_roster_incomplete()
    {
        var (competition, groups, champ, stages) = BuildQualFormChampReadyPartial();
        var view = StructureViewAssembler.Assemble(competition, stages);
        var hub = view.Stages.Single(stage => stage.StageId == champ.Id.Value);

        hub.PhaseRosterPresentation.Should()
            .Be(PhaseRosterPresentationKind.AwaitingInboundQualification);
        hub.Lifecycle!.Transition.DisabledReasonCode.Should()
            .Be(StructureStageLifecycleAssembler.DisabledPhaseRosterIncomplete);
        hub.PresentedIssueCodes.Should().Contain(
            StructurePresentedStageDiagnostics.PresentedAwaitingInboundQualification);
        hub.PresentedIssueCodes.Should().NotContain(
            StructureStageLifecycleAssembler.DisabledPhaseRosterIncomplete);

        var attention = new List<NeedsAttentionItemDto>();
        StructurePresentedAttentionCollector.Collect(view, attention);
        attention.Should().NotContain(item =>
            item.Source == StructurePresentedStageDiagnostics.PresentedAwaitingInboundQualification);
    }

    [Fact]
    public void Qual_form_champ_ready_full_has_no_roster_presentation_issue()
    {
        var competition = CreateCompetition.Execute("qual-form-full-presented", _clock);
        var groups = Stage.Create(competition.Id, new StageName("Poules"), SampleRegulations.Standard(), _clock);
        var groupA = groups.AddGroup("A", _clock);
        var groupB = groups.AddGroup("B", _clock);
        var eA1 = competition.AddEntry(TeamId.New(), "A1", _clock).Id;
        var eA2 = competition.AddEntry(TeamId.New(), "A2", _clock).Id;
        var eB1 = competition.AddEntry(TeamId.New(), "B1", _clock).Id;
        var eB2 = competition.AddEntry(TeamId.New(), "B2", _clock).Id;
        groups.AssignEntryToGroup(groupA.Id, eA1);
        groups.AssignEntryToGroup(groupA.Id, eA2);
        groups.AssignEntryToGroup(groupB.Id, eB1);
        groups.AssignEntryToGroup(groupB.Id, eB2);
        groups.AddMatchday(1, _clock);
        competition.AddStage(groups.Id, _clock);

        var champ = Stage.Create(competition.Id, new StageName("Championnat"), SampleRegulations.Standard(), _clock);
        champ.AddMatchday(1, _clock);
        competition.AddStage(champ.Id, _clock);

        var standingA = ManualStanding([(eA1, 1, 6), (eA2, 2, 3)]);
        var standingB = ManualStanding([(eB1, 1, 6), (eB2, 2, 3)]);
        var standings = new Dictionary<GroupId, Standing>
        {
            [groupA.Id] = standingA,
            [groupB.Id] = standingB
        };

        var intent = new QualificationIntent(
            IntentId.New(),
            order: 1,
            QualificationIntentSourceKind.EachGroup,
            positionFrom: 1,
            positionTo: 1,
            champ.Id,
            destinationForm: true);
        groups.ReplaceQualificationRules(
            QualificationRules.FromIntents([intent], [groupA.Id, groupB.Id]),
            _clock);
        ApplyQualification.Execute(groups, null, standings, [groups, champ], _clock);
        PrepareStage.Execute(champ, [groups, champ], _clock);

        var view = StructureViewAssembler.Assemble(competition, [groups, champ]);
        var hub = view.Stages.Single(stage => stage.StageId == champ.Id.Value);

        hub.PhaseRosterPresentation.Should().Be(PhaseRosterPresentationKind.Complete);
        hub.PresentedIssueCodes.Should().NotContain(
            StructurePresentedStageDiagnostics.PresentedAwaitingInboundQualification);
        hub.PresentedIssueCodes.Should().NotContain(
            StructureStageLifecycleAssembler.DisabledPhaseRosterIncomplete);
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

    private (Competition Competition, Stage Groups, Stage Champ, List<Stage> Stages) BuildQualFormChampReadyPartial()
    {
        var competition = CreateCompetition.Execute("qual-form-champ-ready-partial-presented", _clock);
        var groups = Stage.Create(
            competition.Id,
            new StageName("Poules"),
            StageRegulation.MaterializeFrom(competition.Regulation, isClassifyingPhase: true),
            _clock);
        competition.AddStage(groups.Id, _clock);
        _ = groups.AddGroup("A", _clock);
        _ = groups.AddGroup("B", _clock);
        groups.AddMatchday(1, _clock);

        var champ = Stage.Create(competition.Id, new StageName("Championnat"), SampleRegulations.Standard(), _clock);
        champ.AddMatchday(1, _clock);
        competition.AddStage(champ.Id, _clock);

        for (var i = 0; i < 4; i++)
        {
            competition.AddEntry(TeamId.New(), $"Pool{i}", _clock);
        }

        var poolEntries = competition.Entries.Take(4).Select(entry => entry.Id).ToArray();
        groups.AssignEntryToGroup(groups.Groups[0].Id, poolEntries[0]);
        groups.AssignEntryToGroup(groups.Groups[0].Id, poolEntries[1]);
        groups.AssignEntryToGroup(groups.Groups[1].Id, poolEntries[2]);
        groups.AssignEntryToGroup(groups.Groups[1].Id, poolEntries[3]);

        var e1 = competition.AddEntry(TeamId.New(), "Direct0", _clock).Id;
        var e2 = competition.AddEntry(TeamId.New(), "Direct1", _clock).Id;
        champ.ReplaceAffectationAuthoring([e1, e2], _clock);

        ReplaceStageQualificationRules.Execute(
            groups,
            [
                new QualificationIntentSpec(
                    Guid.NewGuid(),
                    1,
                    QualificationIntentSourceKind.EachGroup,
                    1,
                    2,
                    champ.Id.Value,
                    DestinationForm: true)
            ],
            _clock);

        var stages = new List<Stage> { groups, champ };
        PrepareStage.Execute(champ, stages, _clock);
        return (competition, groups, champ, stages);
    }
}
