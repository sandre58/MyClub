// -----------------------------------------------------------------------
// <copyright file="StructureMaterializeAssemblerTests.cs" company="Stéphane ANDRE">
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
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.TestKit;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Reads;

public sealed class StructureMaterializeAssemblerTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 16, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Championship_materialize_command_disabled_while_stage_is_Draft()
    {
        var situation = TestCompetition.Create("Champ draft", _clock)
            .WithTeams("A", "B", "C")
            .WithStructure(StructureIntent.Championship());
        var stage = situation.RequirePrimaryStage();
        stage.Status.Should().Be(StageStatus.Draft);

        var view = StructureViewAssembler.Assemble(situation.Competition, [stage]);
        var hub = view.Stages.Should().ContainSingle().Subject;

        var materialize = hub.Materialize;
        materialize.Should().NotBeNull();
        materialize.CommandEnabled.Should().BeFalse();
        materialize.EligibleCount.Should().Be(0);
    }

    [Fact]
    public void Championship_ready_incomplete_roster_projects_PhaseRosterIncomplete()
    {
        var competition = CreateCompetition.Execute("Form partial", _clock);
        var source = Stage.Create(
            competition.Id,
            new StageName("Poules"),
            StageRegulation.MaterializeFrom(competition.Regulation, isClassifyingPhase: true),
            _clock);
        competition.AddStage(source.Id, _clock);
        _ = source.AddGroup("A", _clock);
        _ = source.AddGroup("B", _clock);
        source.AddMatchday(1, _clock);

        var champ = Stage.Create(competition.Id, new StageName("Championnat"), SampleRegulations.Standard(), _clock);
        champ.AddMatchday(1, _clock);
        competition.AddStage(champ.Id, _clock);
        var e1 = competition.AddEntry(TeamId.New(), "E1", _clock).Id;
        var e2 = competition.AddEntry(TeamId.New(), "E2", _clock).Id;
        champ.ReplaceAffectationAuthoring([e1, e2], _clock);

        ReplaceStageQualificationRules.Execute(
            source,
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

        PrepareStage.Execute(champ, [source, champ], _clock);
        champ.Status.Should().Be(StageStatus.Ready);

        var materialize = StructureMaterializeAssembler.Assemble(
            competition,
            champ,
            [source, champ],
            [])!;

        materialize.CommandEnabled.Should().BeFalse();
        materialize.DeterminedCount.Should().Be(0);
        materialize.DisabledReason.Should()
            .Be(StructureMaterializeDisabledReasons.PhaseRosterIncomplete);
    }

    [Fact]
    public void Hub_championship_ready_before_materialize_exposes_zero_matchdays()
    {
        var situation = TestCompetition.Create("Champ", _clock)
            .WithTeams("A", "B", "C")
            .WithStructure(StructureIntent.Championship());
        var stage = situation.PreparePrimaryStageForMaterialization();

        var hub = StructureViewAssembler.Assemble(situation.Competition, [stage]).Stages.Single();

        hub.MatchCount.Should().Be(0);
        hub.MatchdayCount.Should().Be(0);
        stage.Matchdays.Count.Should().Be(1);
        hub.ConfiguredMatchdayCount.Should().Be(0);
    }

    [Fact]
    public void Championship_create_is_counts_only_without_pair_rows()
    {
        var situation = TestCompetition.Create("Champ", _clock)
            .WithTeams("A", "B", "C")
            .WithStructure(StructureIntent.Championship());
        var stage = situation.PreparePrimaryStageForMaterialization();

        var view = StructureViewAssembler.Assemble(situation.Competition, [stage]);
        var materialize = view.Stages.Should().ContainSingle().Subject.Materialize;

        materialize.Should().NotBeNull();
        materialize.CommandCode.Should().Be(OverviewAssembler.ActionMaterializeMatches);
        materialize.LabelVariant.Should().Be(OverviewAssembler.LabelVariantCreate);
        materialize.DeterminedCount.Should().Be(3);
        materialize.EligibleCount.Should().Be(3);
        materialize.MaterializedCount.Should().Be(0);
        materialize.CommandEnabled.Should().BeTrue();
        materialize.Confrontations.Should().BeEmpty();
    }

    [Fact]
    public void Championship_complete_when_some_matches_exist()
    {
        var situation = TestCompetition.Create("Champ", _clock)
            .WithTeams("A", "B", "C")
            .WithStructure(StructureIntent.Championship());
        var stage = situation.RequirePrimaryStage();
        var ids = situation.Competition.Entries.Select(entry => entry.Id).OrderBy(id => id.Value).ToArray();
        var fixture = stage.AddFixture(stage.Matchdays[0].Id, _clock);
        var match = Match.Create(situation.Competition.Id, stage.Id, ids[0], ids[1], _clock);
        stage.AttachMatch(fixture.Id, match.Id, legIndex: 1, _clock);
        situation.PreparePrimaryStageForMaterialization();

        var view = StructureViewAssembler.Assemble(
            situation.Competition,
            [stage],
            matchSummariesByStage: new Dictionary<StageId, IReadOnlyList<MatchSummaryRow>>
            {
                [stage.Id] = [Summary(match)]
            });
        var materialize = view.Stages[0].Materialize!;

        materialize.LabelVariant.Should().Be(OverviewAssembler.LabelVariantComplete);
        materialize.DeterminedCount.Should().Be(3);
        materialize.MaterializedCount.Should().Be(1);
        materialize.EligibleCount.Should().Be(2);
        materialize.CommandEnabled.Should().BeTrue();
        materialize.Confrontations.Should().BeEmpty();
    }

    [Fact]
    public void Groups_not_ready_exposes_placement_progress_for_matchs_gauge()
    {
        var competition = CreateCompetition.Execute("Groups partial", _clock);
        AddEntry.Execute(competition, "A", _clock);
        AddEntry.Execute(competition, "B", _clock);
        var configured = ConfigureStructure.Execute(
            competition,
            null,
            StructureIntent.Groups(2, 2),
            _clock);
        var stage = configured.Stage;
        var entry = competition.Entries[0].Id;
        stage.AssignEntryToGroup(stage.Groups[0].Id, entry);
        StageMaterializationTestSupport.EnsureReadyForMaterialize(competition, stage, [stage], _clock);

        var view = StructureViewAssembler.Assemble(competition, [stage]);
        var materialize = view.Stages[0].Materialize!;

        materialize.DisabledReason.Should().Be(StructureMaterializeDisabledReasons.GroupsNotReady);
        materialize.GroupsPlacementFilledCount.Should().Be(1);
        materialize.GroupsPlacementCapacityCount.Should().Be(4);
        materialize.ProspectiveConfrontationCount.Should().Be(2);
        materialize.Confrontations.Should().BeEmpty();
    }

    [Fact]
    public void Groups_lists_determined_pairs_and_keeps_command_when_uncovered()
    {
        var competition = CreateCompetition.Execute("Groups", _clock);
        var e1 = AddEntry.Execute(competition, "A", _clock);
        var e2 = AddEntry.Execute(competition, "B", _clock);
        var e3 = AddEntry.Execute(competition, "C", _clock);
        var e4 = AddEntry.Execute(competition, "D", _clock);
        var configured = ConfigureStructure.Execute(
            competition,
            null,
            StructureIntent.Groups(2, 2),
            _clock);
        var stage = configured.Stage;
        stage.AssignEntryToGroup(stage.Groups[0].Id, e1.Id);
        stage.AssignEntryToGroup(stage.Groups[0].Id, e2.Id);
        stage.AssignEntryToGroup(stage.Groups[1].Id, e3.Id);
        stage.AssignEntryToGroup(stage.Groups[1].Id, e4.Id);
        StageMaterializationTestSupport.EnsureReadyForMaterialize(competition, stage, [stage], _clock);

        var view = StructureViewAssembler.Assemble(competition, [stage]);
        var materialize = view.Stages[0].Materialize!;

        materialize.CommandCode.Should().Be(OverviewAssembler.ActionMaterializeMatches);
        materialize.Confrontations.Should().HaveCount(2);
        materialize.CommandEnabled.Should().BeTrue();
        materialize.LabelVariant.Should().Be(OverviewAssembler.LabelVariantCreate);
        materialize.Confrontations.Should().OnlyContain(row => row.Determined && row.Eligible && !row.Selectable);
    }

    [Fact]
    public void Ready_cup_keeps_slot_actions_when_matches_attached_but_entry_round_incomplete()
    {
        var situation = TestCompetition.Create("Cup partial mat", _clock)
            .WithTeams("A", "B", "C", "D")
            .WithStructure(StructureIntent.Cup(4));
        var stage = situation.RequirePrimaryStage();
        var entries = situation.Competition.Entries.Select(entry => entry.Id).ToArray();
        stage.ApplyResolvedEntry("S1", entries[0], _clock);
        stage.ApplyResolvedEntry("S2", entries[1], _clock);
        situation.PreparePrimaryStage();
        MaterializeCupFromOccupiedSlots.Execute(
            situation.Competition,
            stage,
            ["P1"],
            [],
            _clock);

        var view = StructureViewAssembler.Assemble(situation.Competition, [stage]);
        var hub = view.Stages[0];

        hub.Actions.Should().Contain(StructureViewAssembler.ActionAssignEntryToSlot);
        hub.Actions.Should().Contain(StructureViewAssembler.ActionReplaceAffectationAuthoring);
    }

    [Fact]
    public void Cup_reports_canonical_entry_round_progress_when_incomplete()
    {
        var situation = TestCompetition.Create("Cup partial", _clock)
            .WithTeams("A", "B", "C", "D")
            .WithStructure(StructureIntent.Cup(4));
        var stage = situation.RequirePrimaryStage();
        var entries = situation.Competition.Entries.Select(entry => entry.Id).ToArray();
        stage.ApplyResolvedEntry("S1", entries[0], _clock);
        stage.ApplyResolvedEntry("S2", entries[1], _clock);
        stage.Prepare(_clock);

        var view = StructureViewAssembler.Assemble(situation.Competition, [stage]);
        var materialize = view.Stages[0].Materialize!;

        materialize.CanonicalEntryPairCount.Should().Be(2);
        materialize.UndeterminedCanonicalEntryPairCount.Should().Be(1);
    }

    [Fact]
    public void Cup_lists_pair_keys_and_selection_flags()
    {
        var situation = TestCompetition.Create("Cup", _clock)
            .WithTeams("Alpha", "Bravo")
            .WithStructure(StructureIntent.Cup(2));
        var stage = situation.RequirePrimaryStage();
        var entries = situation.Competition.Entries.Select(entry => entry.Id).ToArray();
        stage.ApplyResolvedEntry(stage.Slots[0].SlotKey, entries[0], _clock);
        stage.ApplyResolvedEntry(stage.Slots[1].SlotKey, entries[1], _clock);
        stage.Prepare(_clock);

        var view = StructureViewAssembler.Assemble(situation.Competition, [stage]);
        var materialize = view.Stages[0].Materialize!;

        materialize.CommandCode.Should().Be(OverviewAssembler.ActionMaterializeFromOccupiedSlots);
        materialize.CommandEnabled.Should().BeTrue();
        materialize.Confrontations.Should().ContainSingle();
        var row = materialize.Confrontations[0];
        row.Key.Should().Be(stage.BracketPairs[0].PairKey);
        row.Selectable.Should().BeTrue();
        row.Eligible.Should().BeTrue();
        row.Determined.Should().BeTrue();
        row.SideA.Should().Be("Alpha");
        row.SideB.Should().Be("Bravo");
    }

    [Fact]
    public void Cup_exhausted_when_fixture_already_bound()
    {
        var situation = TestCompetition.Create("Cup", _clock)
            .WithTeams("Alpha", "Bravo")
            .WithStructure(StructureIntent.Cup(2));
        var stage = situation.RequirePrimaryStage();
        var entries = situation.Competition.Entries.Select(entry => entry.Id).ToArray();
        stage.ApplyResolvedEntry(stage.Slots[0].SlotKey, entries[0], _clock);
        stage.ApplyResolvedEntry(stage.Slots[1].SlotKey, entries[1], _clock);
        stage.Prepare(_clock);
        var result = MaterializeCupFromOccupiedSlots.Execute(
            situation.Competition,
            stage,
            [],
            [],
            _clock);
        result.CreatedMatches.Should().NotBeEmpty();

        var view = StructureViewAssembler.Assemble(situation.Competition, [stage]);
        var materialize = view.Stages[0].Materialize!;

        materialize.LabelVariant.Should().Be(StructureMaterializeAssembler.LabelVariantExhausted);
        materialize.CommandEnabled.Should().BeFalse();
        materialize.Confrontations[0].Materialized.Should().BeTrue();
        materialize.Confrontations[0].Selectable.Should().BeFalse();
        materialize.DisabledReason.Should().BeNull();
    }

    [Fact]
    public void Groups_running_partial_materialization_sets_structure_not_mutable_reason()
    {
        var competition = CreateCompetition.Execute("Running", _clock);
        var e1 = AddEntry.Execute(competition, "A", _clock);
        var e2 = AddEntry.Execute(competition, "B", _clock);
        var e3 = AddEntry.Execute(competition, "C", _clock);
        var e4 = AddEntry.Execute(competition, "D", _clock);
        var configured = ConfigureStructure.Execute(
            competition,
            null,
            StructureIntent.Groups(2, 2),
            _clock);
        var stage = configured.Stage;
        stage.AssignEntryToGroup(stage.Groups[0].Id, e1.Id);
        stage.AssignEntryToGroup(stage.Groups[0].Id, e2.Id);
        stage.AssignEntryToGroup(stage.Groups[1].Id, e3.Id);
        stage.AssignEntryToGroup(stage.Groups[1].Id, e4.Id);
        var (home, away) = MaterializeMatches
            .BuildRoundRobinRounds([.. stage.Groups[0].EntryIds], stage.MatchGenerationFormat)[0][0];
        var fixture = stage.AddFixture(stage.Matchdays[0].Id, _clock);
        var match = Match.Create(competition.Id, stage.Id, home, away, _clock);
        stage.AttachMatch(fixture.Id, match.Id, legIndex: 1, _clock);
        stage.Prepare(_clock);
        stage.Start(_clock);

        var view = StructureViewAssembler.Assemble(
            competition,
            [stage],
            matchSummariesByStage: new Dictionary<StageId, IReadOnlyList<MatchSummaryRow>>
            {
                [stage.Id] = [Summary(match)]
            });
        var materialize = view.Stages[0].Materialize!;

        materialize.CommandEnabled.Should().BeFalse();
        materialize.MaterializedCount.Should().Be(1);
        materialize.DeterminedCount.Should().Be(2);
        materialize.DisabledReason.Should().Be(StructureMaterializeDisabledReasons.StructureNotMutable);
    }

    [Fact]
    public void Groups_idle_when_no_determined_pairs_sets_no_determined_reason()
    {
        var competition = CreateCompetition.Execute("Idle", _clock);
        _ = AddEntry.Execute(competition, "A", _clock);
        _ = AddEntry.Execute(competition, "B", _clock);
        var configured = ConfigureStructure.Execute(
            competition,
            null,
            StructureIntent.Groups(2, 2),
            _clock);
        var stage = configured.Stage;

        var view = StructureViewAssembler.Assemble(competition, [stage]);
        var materialize = view.Stages[0].Materialize!;

        materialize.CommandEnabled.Should().BeFalse();
        materialize.DeterminedCount.Should().Be(0);
        materialize.DisabledReason.Should().Be(StructureMaterializeDisabledReasons.StructureNotMutable);
    }

    [Fact]
    public void Championship_create_does_not_set_disabled_reason()
    {
        var situation = TestCompetition.Create("Champ", _clock)
            .WithTeams("A", "B", "C")
            .WithStructure(StructureIntent.Championship());
        var stage = situation.PreparePrimaryStageForMaterialization();

        var view = StructureViewAssembler.Assemble(situation.Competition, [stage]);
        var materialize = view.Stages[0].Materialize!;

        materialize.CommandEnabled.Should().BeTrue();
        materialize.DisabledReason.Should().BeNull();
    }

    private static MatchSummaryRow Summary(Match match) =>
        new(match.Id, match.StageId, match.Status, match.HomeEntryId, match.AwayEntryId, match.Result);
}
