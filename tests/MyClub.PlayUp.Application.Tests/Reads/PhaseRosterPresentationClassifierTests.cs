// -----------------------------------------------------------------------
// <copyright file="PhaseRosterPresentationClassifierTests.cs" company="Stéphane ANDRE">
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
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Domain.Standings;
using MyClub.PlayUp.TestKit;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Reads;

/// <summary>
/// Reference scenarios R-A…R-F for phase roster presentation (step 2 — classifier only).
/// </summary>
public sealed class PhaseRosterPresentationClassifierTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 19, 10, 0, 0, TimeSpan.Zero));

    [Fact]
    public void R_A_qual_form_champ_ready_partial_is_awaiting_inbound_qualification()
    {
        var (competition, _, champ, stages) = BuildQualFormChampReadyPartial();

        PhaseRosterPresentationClassifier.Classify(competition, champ, stages)
            .Should()
            .Be(PhaseRosterPresentationKind.AwaitingInboundQualification);

        StageStartReadinessGuard.TryGetDisabledReasonCode(
                champ,
                competition,
                [],
                stages)
            .Should()
            .Be(StructureMaterializeDisabledReasons.PhaseRosterIncomplete);
    }

    [Fact]
    public void R_B_source_running_without_determined_qualification_is_awaiting_inbound_qualification()
    {
        var (competition, groups, champ, stages) = BuildQualFormChampReadyPartial();
        StageMaterializationTestSupport.EnsureReadyForMaterialize(competition, groups, stages, _clock);
        var materialized = MaterializeMatches.Execute(competition, groups, [], _clock);
        StartStage.Execute(groups, _clock);
        groups.Status.Should().Be(StageStatus.Running);

        var summaries = new Dictionary<StageId, IReadOnlyList<MatchSummaryRow>>
        {
            [groups.Id] = StageStartReadinessGuard.ToSummaryRows(materialized.CreatedMatches)
        };

        PhaseRosterPresentationClassifier.Classify(competition, champ, stages, summaries)
            .Should()
            .Be(PhaseRosterPresentationKind.AwaitingInboundQualification);
    }

    [Fact]
    public void R_C_qual_producible_but_not_consumed_is_composition_incomplete()
    {
        var (competition, groups, champ, stages) = BuildQualFormChampReadyPartial();
        StageMaterializationTestSupport.EnsureReadyForMaterialize(competition, groups, stages, _clock);
        var materialized = MaterializeMatches.Execute(competition, groups, [], _clock);
        foreach (var match in materialized.CreatedMatches)
        {
            match.Finish(new MatchResult(ResultType.Played, new Score(1, 0)), _clock);
        }

        StartStage.Execute(groups, _clock);
        var summaries = new Dictionary<StageId, IReadOnlyList<MatchSummaryRow>>
        {
            [groups.Id] = StageStartReadinessGuard.ToSummaryRows(materialized.CreatedMatches)
        };

        PhaseRosterPresentationClassifier.Classify(competition, champ, stages, summaries)
            .Should()
            .Be(PhaseRosterPresentationKind.CompositionIncomplete);
    }

    [Fact]
    public void R_D_roster_complete_is_complete()
    {
        var (competition, groups, champ, stages, standings) = BuildQualFormGroupsWithStandings();
        ReplaceEachGroupTop1Form(groups, standings.GroupA, standings.GroupB, champ.Id);
        ApplyQualification.Execute(groups, null, standings.StandingsByGroup, stages, _clock);

        PhaseRosterPresentationClassifier.Classify(competition, champ, stages)
            .Should()
            .Be(PhaseRosterPresentationKind.Complete);
    }

    [Fact]
    public void R_E_path_conflict_with_qual_wait_is_composition_incomplete()
    {
        var (competition, groups, champ, stages) = BuildQualFormChampReadyPartial();
        var groupA = groups.Groups[0].Id;
        var pathA = groups.Regulation.QualificationRules!.Paths
            .First(path => path.Source.GroupId!.Equals(groupA));
        var conflictingEntry = competition.AddEntry(TeamId.New(), "Conflict", _clock).Id;
        champ.RecordFormPathResolution(
            FormPathResolutionKey.FromQualification(groups.Id, pathA),
            conflictingEntry);

        PhaseRosterPresentationClassifier.Classify(competition, champ, stages)
            .Should()
            .Be(PhaseRosterPresentationKind.CompositionIncomplete);
    }

    [Fact]
    public void R_F_withdrawn_direct_is_composition_incomplete_not_simple_qual_wait()
    {
        var (competition, _, champ, stages) = BuildQualFormChampReadyPartial();
        competition.Prepare(_clock);
        competition.Start(_clock);
        var withdrawn = competition.Entries
            .First(entry => champ.CompositionEntries.Any(ce => ce.EntryId.Equals(entry.Id)))
            .Id;
        WithdrawEntry.Execute(competition, withdrawn, stages, [], _clock);

        PhaseRosterPresentationClassifier.Classify(competition, champ, stages)
            .Should()
            .Be(PhaseRosterPresentationKind.CompositionIncomplete);
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

    private (Competition Competition, Stage Groups, Stage Champ, List<Stage> Stages) BuildQualFormChampReadyPartial(
        int directCount = 2)
    {
        var competition = CreateCompetition.Execute("qual-form-champ-ready-partial", _clock);
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

        var directIds = new List<EntryId>();
        for (var i = 0; i < directCount; i++)
        {
            directIds.Add(competition.AddEntry(TeamId.New(), $"Direct{i}", _clock).Id);
        }

        champ.ReplaceAffectationAuthoring(directIds, _clock);

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
        champ.Status.Should().Be(StageStatus.Ready);

        return (competition, groups, champ, stages);
    }

    private (
        Competition Competition,
        Stage Groups,
        Stage Champ,
        List<Stage> Stages,
        (GroupId GroupA, GroupId GroupB, Dictionary<GroupId, Standing> StandingsByGroup) Standings)
        BuildQualFormGroupsWithStandings()
    {
        var competition = CreateCompetition.Execute("qual-form-champ-ready-full", _clock);
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

        var stages = new List<Stage> { groups, champ };
        return (competition, groups, champ, stages, (groupA.Id, groupB.Id, standings));
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
