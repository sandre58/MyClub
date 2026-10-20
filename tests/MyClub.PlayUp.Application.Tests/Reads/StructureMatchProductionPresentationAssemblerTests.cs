// -----------------------------------------------------------------------
// <copyright file="StructureMatchProductionPresentationAssemblerTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Text.Json;
using FluentAssertions;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Qualification;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.TestKit;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Reads;

public sealed class StructureMatchProductionPresentationAssemblerTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 16, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Championship_ready_before_materialize_uses_prospective_match_slots_and_info_primary()
    {
        var situation = TestCompetition.Create("Champ", _clock)
            .WithTeams("A", "B", "C")
            .WithStructure(StructureIntent.Championship());
        var stage = situation.PreparePrimaryStageForMaterialization();

        var view = StructureViewAssembler.Assemble(situation.Competition, [stage]);
        var hub = view.Stages.Should().ContainSingle().Subject;
        var presentation = hub.MatchProductionPresentation;

        presentation.Should().NotBeNull();
        presentation.ProductionVisible.Should().BeTrue();
        presentation.PrimaryGaugeMode.Should().Be(StructureMatchProductionPresentationAssembler.GaugeModeMaterialized);
        presentation.PrimaryGauge.Current.Should().Be(0);
        presentation.PrimaryGauge.Capacity.Should().Be(3);
        presentation.PrimaryGauge.MessageKind.Should().Be(StructureMatchProductionPresentationAssembler.MessageKindInfo);
        presentation.Materialize.Visible.Should().BeTrue();
        presentation.Materialize.Enabled.Should().BeTrue();
        presentation.PrerequisiteGauge.Should().BeNull();
    }

    [Fact]
    public void Championship_incomplete_roster_primary_info_and_composition_prerequisite_attention()
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

        var view = StructureViewAssembler.Assemble(competition, [source, champ]);
        var hub = view.Stages.Single(stage => stage.StageId == champ.Id.Value);
        var presentation = hub.MatchProductionPresentation;

        presentation.Should().NotBeNull();
        presentation.PrimaryGauge.MessageKind.Should().Be(
            StructureMatchProductionPresentationAssembler.MessageKindInfo);
        presentation.PrerequisiteGauge.Should().NotBeNull();
        presentation.PrerequisiteGauge!.MessageCode.Should().Be(
            StructureMatchProductionMessageCodes.PrerequisiteComposition);
        presentation.PrerequisiteGauge.MessageKind.Should().Be(
            StructureMatchProductionPresentationAssembler.MessageKindAttention);
        presentation.Materialize.Enabled.Should().BeFalse();
        presentation.Materialize.DisabledReason.Should().Be(
            StructureMaterializeDisabledReasons.PhaseRosterIncomplete);
    }

    [Fact]
    public void Swiss_has_no_match_production_presentation()
    {
        var situation = TestCompetition.Create("Swiss", _clock)
            .WithTeams("A", "B", "C", "D")
            .WithStructure(StructureIntent.Swiss(roundCount: 3));
        var stage = situation.PreparePrimaryStageForMaterialization();

        var view = StructureViewAssembler.Assemble(situation.Competition, [stage]);
        view.Stages.Should().ContainSingle().Subject.MatchProductionPresentation.Should().BeNull();
    }

    [Fact]
    public void Groups_draw_to_launch_uses_info_for_placement_prerequisite()
    {
        var competition = CreateCompetition.Execute("Groups draw pending", _clock);
        for (var i = 0; i < 4; i++)
        {
            AddEntry.Execute(competition, $"T{i}", _clock);
        }

        var configured = ConfigureStructure.Execute(
            competition,
            null,
            StructureIntent.Groups(2, 2),
            _clock);
        var stage = configured.Stage;
        ReplaceStageDrawRules.Execute(
            stage,
            new DrawRules(DrawMode.Random, potRules: new PotRules(2)),
            _clock);
        StageMaterializationTestSupport.EnsureReadyForMaterialize(competition, stage, [stage], _clock);

        var presentation = StructureViewAssembler.Assemble(competition, [stage]).Stages[0]
            .MatchProductionPresentation;

        presentation.Should().NotBeNull();
        presentation!.PrerequisiteGauge.Should().NotBeNull();
        presentation.PrerequisiteGauge!.MessageCode.Should().Be(
            StructureMatchProductionMessageCodes.PrerequisiteGroupsPlacement);
        presentation.PrerequisiteGauge.MessageKind.Should().Be(
            StructureMatchProductionPresentationAssembler.MessageKindInfo);
        presentation.PrimaryGauge.MessageKind.Should().Be(
            StructureMatchProductionPresentationAssembler.MessageKindInfo);
    }

    [Fact]
    public void Groups_not_ready_prospective_match_slots_and_groups_prerequisite()
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
        stage.AssignEntryToGroup(stage.Groups[0].Id, competition.Entries[0].Id);
        StageMaterializationTestSupport.EnsureReadyForMaterialize(competition, stage, [stage], _clock);

        var presentation = StructureViewAssembler.Assemble(competition, [stage]).Stages[0]
            .MatchProductionPresentation;

        presentation.Should().NotBeNull();
        presentation!.PrimaryGauge.Current.Should().Be(0);
        presentation.PrimaryGauge.Capacity.Should().Be(2);
        presentation.PrimaryGauge.MessageCode.Should().Be(
            StructureMatchProductionMessageCodes.MatchesBlockedByGroupsPools);
        presentation.PrimaryGauge.MessageKind.Should().Be(
            StructureMatchProductionPresentationAssembler.MessageKindAttention);
        presentation.PrerequisiteGauge.Should().NotBeNull();
        presentation.PrerequisiteGauge!.MessageCode.Should().Be(
            StructureMatchProductionMessageCodes.PrerequisiteGroupsPlacement);
        presentation.PrerequisiteGauge.Current.Should().Be(1);
        presentation.PrerequisiteGauge.Capacity.Should().Be(4);
        presentation.Materialize.Enabled.Should().BeFalse();
        presentation.Materialize.DisabledReason.Should().Be(
            StructureMaterializeDisabledReasons.GroupsNotReady);
    }

    [Fact]
    public void Groups_ready_partial_materialization_uses_attached_match_count()
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
        var (home, away) = MaterializeMatches
            .BuildRoundRobinRounds([.. stage.Groups[0].EntryIds], stage.MatchGenerationFormat)[0][0];
        var fixture = stage.AddFixture(stage.Matchdays[0].Id, _clock);
        var match = Match.Create(competition.Id, stage.Id, home, away, _clock);
        stage.AttachMatch(fixture.Id, match.Id, legIndex: 1, _clock);
        StageMaterializationTestSupport.EnsureReadyForMaterialize(competition, stage, [stage], _clock);

        var hub = StructureViewAssembler.Assemble(
            competition,
            [stage],
            matchSummariesByStage: new Dictionary<StageId, IReadOnlyList<MatchSummaryRow>>
            {
                [stage.Id] = [Summary(match)]
            }).Stages[0];
        var presentation = hub.MatchProductionPresentation;

        presentation.Should().NotBeNull();
        presentation!.PrimaryGauge.Current.Should().Be(1);
        presentation.PrimaryGauge.Capacity.Should().Be(2);
        presentation.PrimaryGauge.MessageKind.Should().Be(
            StructureMatchProductionPresentationAssembler.MessageKindAttention);
        presentation.Materialize.Enabled.Should().BeTrue();
        presentation.PrerequisiteGauge.Should().BeNull();
    }

    [Fact]
    public void Cup_two_leg_regulation_doubles_match_slot_capacity()
    {
        var situation = TestCompetition.Create("Cup-2L", _clock)
            .WithTeams("Alpha", "Bravo")
            .WithStructure(StructureIntent.Cup(2));
        var stage = situation.RequirePrimaryStage();
        stage.ReplaceRegulation(
            stage.Regulation.WithTieFormat(new TieFormat(TieFormat.TwoLegs, aggregateScoring: true)),
            _clock);
        var entries = situation.Competition.Entries.Select(entry => entry.Id).ToArray();
        stage.ApplyResolvedEntry(stage.Slots[0].SlotKey, entries[0], _clock);
        stage.ApplyResolvedEntry(stage.Slots[1].SlotKey, entries[1], _clock);
        stage.Prepare(_clock);

        var presentation = StructureViewAssembler.Assemble(situation.Competition, [stage]).Stages[0]
            .MatchProductionPresentation;

        presentation.Should().NotBeNull();
        presentation!.PrimaryGauge.Capacity.Should().Be(2);
        presentation.PrimaryGauge.Current.Should().Be(0);
        presentation.Materialize.Enabled.Should().BeTrue();
    }

    [Fact]
    public void Cup_mono_round_exhausted_primary_success_without_follow_up()
    {
        var situation = TestCompetition.Create("Cup", _clock)
            .WithTeams("Alpha", "Bravo")
            .WithStructure(StructureIntent.Cup(2));
        var stage = situation.RequirePrimaryStage();
        var entries = situation.Competition.Entries.Select(entry => entry.Id).ToArray();
        stage.ApplyResolvedEntry(stage.Slots[0].SlotKey, entries[0], _clock);
        stage.ApplyResolvedEntry(stage.Slots[1].SlotKey, entries[1], _clock);
        stage.Prepare(_clock);
        MaterializeCupFromOccupiedSlots.Execute(
            situation.Competition,
            stage,
            [],
            [],
            _clock);

        var hub = StructureViewAssembler.Assemble(situation.Competition, [stage]).Stages[0];
        var presentation = hub.MatchProductionPresentation;

        presentation.Should().NotBeNull();
        presentation!.PrimaryGauge.Current.Should().Be(hub.MatchCount);
        presentation.PrimaryGauge.Capacity.Should().Be(hub.MatchCount);
        presentation.PrimaryGauge.MessageKind.Should().Be(
            StructureMatchProductionPresentationAssembler.MessageKindSuccess);
        presentation.FollowUp.Should().BeNull();
        presentation.Materialize.Enabled.Should().BeFalse();
        presentation.PrerequisiteGauge.Should().BeNull();
    }

    [Fact]
    public void Cup_no_determined_shows_entry_round_prerequisite_from_confrontation_rows()
    {
        var situation = TestCompetition.Create("Cup partial pair", _clock)
            .WithTeams("A", "B", "C", "D")
            .WithStructure(StructureIntent.Cup(4));
        var stage = situation.RequirePrimaryStage();
        stage.ApplyResolvedEntry("S1", situation.Competition.Entries.First().Id, _clock);
        situation.PreparePrimaryStage();

        var hub = StructureViewAssembler.Assemble(situation.Competition, [stage]).Stages[0];
        var presentation = hub.MatchProductionPresentation;

        presentation.Should().NotBeNull();
        hub.Materialize!.DisabledReason.Should()
            .Be(StructureMaterializeDisabledReasons.NoDeterminedConfrontations);
        presentation!.PrerequisiteGauge.Should().NotBeNull();
        presentation.PrerequisiteGauge!.MessageCode.Should().Be(
            StructureMatchProductionMessageCodes.PrerequisiteCupEntryRound);
        presentation.PrerequisiteGauge.Current.Should().Be(0);
        presentation.PrerequisiteGauge.Capacity.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Cup_entry_round_prerequisite_follows_materialize_command_enabled_gate()
    {
        var situation = TestCompetition.Create("Cup partial", _clock)
            .WithTeams("A", "B", "C", "D")
            .WithStructure(StructureIntent.Cup(4));
        var stage = situation.RequirePrimaryStage();
        var entries = situation.Competition.Entries.Select(entry => entry.Id).ToArray();
        stage.ApplyResolvedEntry("S1", entries[0], _clock);
        stage.ApplyResolvedEntry("S2", entries[1], _clock);
        stage.Prepare(_clock);

        var hub = StructureViewAssembler.Assemble(situation.Competition, [stage]).Stages[0];
        var materialize = hub.Materialize!;
        var presentation = hub.MatchProductionPresentation;

        presentation.Should().NotBeNull();
        materialize.UndeterminedCanonicalEntryPairCount.Should().BeGreaterThan(0);

        presentation!.PrerequisiteGauge.Should().NotBeNull();
        presentation.PrerequisiteGauge!.MessageCode.Should().Be(
            StructureMatchProductionMessageCodes.PrerequisiteCupEntryRound);
        presentation.PrerequisiteGauge.MessageKind.Should().Be(
            StructureMatchProductionPresentationAssembler.MessageKindDanger);
        if (!materialize.CommandEnabled)
        {
            presentation.PrerequisiteGauge.BlocksPhaseStart.Should().BeTrue();
        }
    }

    [Fact]
    public void Cup_partial_materialized_with_incomplete_pairs_uses_danger_partial_when_materialize_command_disabled()
    {
        var situation = TestCompetition.Create("Cup partial R0 gauge", _clock)
            .WithTeams("A", "B", "C", "D")
            .WithStructure(StructureIntent.Cup(4));
        var stage = situation.PrimaryStage!;
        var entries = situation.Competition.Entries.Select(entry => entry.Id).ToArray();
        stage.ApplyResolvedEntry("S1", entries[0], _clock);
        stage.ApplyResolvedEntry("S2", entries[1], _clock);
        situation.PreparePrimaryStage();

        var materialized = MaterializeCupFromOccupiedSlots.Execute(
            situation.Competition,
            stage,
            ["P1"],
            [],
            _clock);
        var summaries = new Dictionary<StageId, IReadOnlyList<MatchSummaryRow>>
        {
            [stage.Id] = StageStartReadinessGuard.ToSummaryRows(materialized.CreatedMatches)
        };

        var hub = StructureViewAssembler.Assemble(
            situation.Competition,
            situation.Stages,
            matchSummariesByStage: summaries).Stages.Single();
        var presentation = hub.MatchProductionPresentation;

        presentation.Should().NotBeNull();
        hub.Materialize!.CommandEnabled.Should().BeFalse();
        hub.Lifecycle!.Transition.Enabled.Should().BeFalse();
        presentation!.PrimaryGauge.Capacity.Should().BeGreaterThan(presentation.PrimaryGauge.Current);
        presentation.PrimaryGauge.MessageCode.Should().Be(StructureMatchProductionMessageCodes.Partial);
        presentation.PrimaryGauge.MessageKind.Should().Be(
            StructureMatchProductionPresentationAssembler.MessageKindDanger);
    }

    [Fact]
    public void Cup_one_determined_pair_uses_full_round_match_capacity_entry_prerequisite_and_danger_partial()
    {
        var situation = TestCompetition.Create("Cup16 partial", _clock)
            .WithTeams(16)
            .WithStructure(StructureIntent.Cup(16));
        var stage = situation.RequirePrimaryStage();
        var entries = situation.Competition.Entries.Select(entry => entry.Id).ToArray();
        stage.ApplyResolvedEntry("S1", entries[0], _clock);
        stage.ApplyResolvedEntry("S2", entries[1], _clock);
        stage.Prepare(_clock);

        var hub = StructureViewAssembler.Assemble(situation.Competition, [stage]).Stages[0];
        var materialize = hub.Materialize!;
        var presentation = hub.MatchProductionPresentation;

        presentation.Should().NotBeNull();
        materialize.DeterminedCount.Should().BeGreaterThan(0);
        materialize.CommandEnabled.Should().BeTrue();
        presentation!.PrimaryGauge.Current.Should().Be(0);
        presentation.PrimaryGauge.Capacity.Should().Be(8);
        presentation.PrimaryGauge.MessageCode.Should().Be(StructureMatchProductionMessageCodes.Partial);
        presentation.PrimaryGauge.MessageKind.Should().Be(
            StructureMatchProductionPresentationAssembler.MessageKindDanger);
        presentation.PrerequisiteGauge.Should().NotBeNull();
        presentation.PrerequisiteGauge!.MessageCode.Should().Be(
            StructureMatchProductionMessageCodes.PrerequisiteCupEntryRound);
        presentation.PrerequisiteGauge.Current.Should().Be(1);
        presentation.PrerequisiteGauge.Capacity.Should().Be(8);
        presentation.Materialize.Visible.Should().BeTrue();
    }

    [Fact]
    public void Cup_ready_all_entry_pairs_shows_materialize_cta_when_no_matches_attached()
    {
        var situation = TestCompetition.Create("Cup4 ready", _clock)
            .WithTeams("A", "B", "C", "D")
            .WithStructure(StructureIntent.Cup(4));
        var stage = situation.RequirePrimaryStage();
        var entries = situation.Competition.Entries.Select(entry => entry.Id).ToArray();
        for (var i = 0; i < 4; i++)
        {
            stage.ApplyResolvedEntry($"S{i + 1}", entries[i], _clock);
        }

        stage.Prepare(_clock);

        var hub = StructureViewAssembler.Assemble(situation.Competition, [stage]).Stages[0];
        var presentation = hub.MatchProductionPresentation;

        presentation.Should().NotBeNull();
        hub.Materialize!.CommandEnabled.Should().BeTrue();
        presentation!.PrimaryGauge.Current.Should().Be(0);
        presentation.PrimaryGauge.Capacity.Should().Be(2);
        presentation.PrimaryGauge.MessageCode.Should().Be(StructureMatchProductionMessageCodes.Partial);
        presentation.PrerequisiteGauge.Should().BeNull();
        presentation.Materialize.Visible.Should().BeTrue();
    }

    [Fact]
    public void Cup_direct_assignments_count_as_resolved_form_teams_without_prog_resolution()
    {
        var situation = TestCompetition.Create("Cup4 direct", _clock)
            .WithTeams("A", "B", "C", "D")
            .WithStructure(StructureIntent.Cup(4));
        var stage = situation.RequirePrimaryStage();
        var entries = situation.Competition.Entries.Select(entry => entry.Id).ToArray();
        stage.ApplyResolvedEntry("S1", entries[0], _clock);
        stage.ApplyResolvedEntry("S2", entries[1], _clock);

        var resolved = CupEntryRoundResolvedTeamGauge.CountResolvedEntryRoundTeams(
            situation.Competition,
            stage,
            [stage]);

        resolved.Should().Be(2);
    }

    [Fact]
    public void Cup_ready_keeps_materialize_visible_when_fixtures_exist_without_attached_matches()
    {
        var situation = TestCompetition.Create("Cup4 fixtures", _clock)
            .WithTeams("A", "B", "C", "D")
            .WithStructure(StructureIntent.Cup(4));
        var stage = situation.RequirePrimaryStage();
        var entries = situation.Competition.Entries.Select(entry => entry.Id).ToArray();
        for (var i = 0; i < 4; i++)
        {
            stage.ApplyResolvedEntry($"S{i + 1}", entries[i], _clock);
        }

        foreach (var pair in stage.BracketPairs)
        {
            _ = stage.AddFixture(
                stage.Rounds[0].Id,
                _clock,
                pair.SlotAKey,
                pair.SlotBKey,
                pair.PairKey);
        }

        stage.Prepare(_clock);

        var hub = StructureViewAssembler.Assemble(situation.Competition, [stage]).Stages[0];
        var presentation = hub.MatchProductionPresentation;

        presentation.Should().NotBeNull();
        hub.Materialize!.MaterializedCount.Should().Be(0);
        hub.Materialize.EligibleCount.Should().Be(2);
        hub.Materialize.CommandEnabled.Should().BeTrue();
        presentation!.PrimaryGauge.Current.Should().Be(0);
        presentation.PrimaryGauge.Capacity.Should().Be(2);
        presentation.Materialize.Visible.Should().BeTrue();
        presentation.Materialize.Enabled.Should().BeTrue();
    }

    [Fact]
    public void Groups_match_production_awaiting_inbound_feed_uses_info_no_determined_and_inbound_prerequisite()
    {
        var situation = TestCompetition.Create("Groups inbound", _clock)
            .WithTeams("A", "B", "C", "D")
            .WithStructure(StructureIntent.Groups(2, 2));
        var stage = situation.RequirePrimaryStage();
        situation.PreparePrimaryStage();
        var inbound = new StructureInboundCompositionDto(4, 0, []);
        var materialize = new StructureMaterializeDto(
            OverviewAssembler.ActionMaterializeMatches,
            StructureMaterializeAssembler.LabelVariantIdle,
            0,
            0,
            0,
            false,
            [],
            StructureMaterializeDisabledReasons.NoDeterminedConfrontations,
            GroupsPlacementFilledCount: 0,
            GroupsPlacementCapacityCount: 4,
            ProspectiveConfrontationCount: 2);

        var presentation = StructureMatchProductionPresentationAssembler.Assemble(
            new StructureMatchProductionPresentationAssembler.Input(
                situation.Competition,
                stage,
                [stage],
                StructureFormatKind.Groups,
                materialize,
                null,
                0,
                1,
                null,
                4,
                0,
                null,
                new StructureStageLifecycleDto(
                    new StructureStageLifecycleTransitionDto(
                        OverviewAssembler.ActionStartStage,
                        false,
                        StructureStageLifecycleAssembler.DisabledNoDeterminedConfrontations),
                    null),
                false,
                null,
                inbound))!;

        presentation.PrimaryGauge.MessageCode.Should().Be(
            StructureMatchProductionMessageCodes.NoDeterminedConfrontations);
        presentation.PrimaryGauge.MessageKind.Should().Be(
            StructureMatchProductionPresentationAssembler.MessageKindInfo);
        presentation.PrerequisiteGauge.Should().NotBeNull();
        presentation.PrerequisiteGauge!.MessageCode.Should().Be(
            StructureMatchProductionMessageCodes.PrerequisiteInboundFeed);
        presentation.PrerequisiteGauge.Current.Should().Be(0);
        presentation.PrerequisiteGauge.Capacity.Should().Be(4);
        presentation.PrerequisiteGauge.MessageKind.Should().Be(
            StructureMatchProductionPresentationAssembler.MessageKindInfo);
    }

    [Fact]
    public void Cup_explicit_topology_entry_round_gauge_counts_slot_resolved_pairs_not_skeleton_confrontations()
    {
        var competition = CreateCompetition.Execute("SF explicit", _clock);
        for (var i = 0; i < 4; i++)
        {
            competition.AddEntry(TeamId.New(), $"T{i}", _clock);
        }

        var stage = Stage.Create(
            competition.Id,
            new StageName("Demi"),
            SampleRegulations.Standard(),
            _clock);
        stage.AddRound("Tour", new TieFormat(TieFormat.SingleLeg, aggregateScoring: false), _clock);
        stage.AddSlot("SF-A");
        stage.AddSlot("SF-B");
        stage.AddSlot("SF-C");
        stage.AddSlot("SF-D");
        stage.ReplaceBracketPairs(
        [
            new BracketPair("P1", "SF-A", "SF-B"),
            new BracketPair("P2", "SF-C", "SF-D"),
        ]);
        competition.AddStage(stage.Id, _clock);
        _ = stage.AddFixture(stage.Rounds[0].Id, _clock, "SF-A", "SF-B", "P1");
        _ = stage.AddFixture(stage.Rounds[0].Id, _clock, "SF-C", "SF-D", "P2");
        var entries = competition.Entries.Select(entry => entry.Id).ToArray();
        stage.ApplyResolvedEntry("SF-A", entries[0], _clock);
        stage.ApplyResolvedEntry("SF-B", entries[1], _clock);
        stage.Prepare(_clock);

        var hub = StructureViewAssembler.Assemble(competition, [stage]).Stages[0];
        var presentation = hub.MatchProductionPresentation;

        presentation.Should().NotBeNull();
        hub.Materialize!.CanonicalEntryPairCount.Should().Be(0);
        presentation!.PrerequisiteGauge.Should().NotBeNull();
        presentation.PrerequisiteGauge!.MessageCode.Should().Be(
            StructureMatchProductionMessageCodes.PrerequisiteCupEntryRound);
        presentation.PrerequisiteGauge.Current.Should().Be(1);
        presentation.PrerequisiteGauge.Capacity.Should().Be(2);
    }

    [Fact]
    public void Cup_incomplete_entry_round_prerequisite_takes_priority_over_inbound_feed_deferral()
    {
        var situation = TestCompetition.Create("Cup inbound defer", _clock)
            .WithTeams("A", "B", "C", "D")
            .WithStructure(StructureIntent.Cup(4));
        var stage = situation.RequirePrimaryStage();
        var entries = situation.Competition.Entries.Select(entry => entry.Id).ToArray();
        stage.ApplyResolvedEntry("S1", entries[0], _clock);
        stage.ApplyResolvedEntry("S2", entries[1], _clock);
        situation.PreparePrimaryStage();
        var inbound = new StructureInboundCompositionDto(2, 0, []);
        var materialize = new StructureMaterializeDto(
            OverviewAssembler.ActionMaterializeFromOccupiedSlots,
            StructureMaterializeAssembler.LabelVariantIdle,
            0,
            0,
            0,
            false,
            [],
            StructureMaterializeDisabledReasons.NoDeterminedConfrontations,
            CanonicalEntryPairCount: 2,
            UndeterminedCanonicalEntryPairCount: 1);

        var presentation = StructureMatchProductionPresentationAssembler.Assemble(
            new StructureMatchProductionPresentationAssembler.Input(
                situation.Competition,
                stage,
                [stage],
                StructureFormatKind.Cup,
                materialize,
                null,
                0,
                1,
                null,
                3,
                0,
                null,
                new StructureStageLifecycleDto(
                    new StructureStageLifecycleTransitionDto(
                        OverviewAssembler.ActionStartStage,
                        false,
                        StructureStageLifecycleAssembler.DisabledIncompleteEntryRound),
                    null),
                false,
                null,
                inbound))!;

        presentation.PrerequisiteGauge.Should().NotBeNull();
        presentation.PrerequisiteGauge!.MessageCode.Should().Be(
            StructureMatchProductionMessageCodes.PrerequisiteCupEntryRound);
        presentation.PrerequisiteGauge.Current.Should().Be(1);
        presentation.PrerequisiteGauge.Capacity.Should().Be(2);
    }

    [Fact]
    public void Championship_partial_materialization_reflects_attached_matches_not_confrontation_count_only()
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

        var hub = StructureViewAssembler.Assemble(
            situation.Competition,
            [stage],
            matchSummariesByStage: new Dictionary<StageId, IReadOnlyList<MatchSummaryRow>>
            {
                [stage.Id] = [Summary(match)]
            }).Stages[0];
        var presentation = hub.MatchProductionPresentation;

        presentation.Should().NotBeNull();
        presentation!.PrimaryGauge.Current.Should().Be(1);
        presentation.PrimaryGauge.Capacity.Should().Be(3);
        presentation.Materialize.LabelVariant.Should().Be(OverviewAssembler.LabelVariantComplete);
    }

    [Fact]
    public void Groups_running_frozen_partial_keeps_production_projection_with_match_counts()
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

        var hub = StructureViewAssembler.Assemble(
            competition,
            [stage],
            matchSummariesByStage: new Dictionary<StageId, IReadOnlyList<MatchSummaryRow>>
            {
                [stage.Id] = [Summary(match)]
            }).Stages[0];
        var presentation = hub.MatchProductionPresentation;

        presentation.Should().NotBeNull();
        presentation!.ProductionVisible.Should().BeTrue();
        presentation.ShowPrimaryGauge.Should().BeFalse();
        presentation.FrozenPartial.Should().NotBeNull();
        presentation.FrozenPartial!.Current.Should().Be(1);
        presentation.FrozenPartial.Capacity.Should().Be(2);
        presentation.FrozenPartial.MessageCode.Should().Be(
            StructureMatchProductionMessageCodes.FrozenPartial);
        presentation.PrimaryGaugeMode.Should().Be(
            StructureMatchProductionPresentationAssembler.GaugeModeMaterialized);
        presentation.PrimaryGauge.Current.Should().Be(1);
        presentation.PrimaryGauge.Capacity.Should().Be(2);
        presentation.Materialize.Visible.Should().BeFalse();
        presentation.Materialize.Enabled.Should().BeFalse();
        presentation.Materialize.DisabledReason.Should().Be(
            StructureMaterializeDisabledReasons.StructureNotMutable);
        presentation.Dematerialize.Visible.Should().BeFalse();
        presentation.Dematerialize.Enabled.Should().BeFalse();
    }

    [Fact]
    public void Groups_suspended_frozen_partial_uses_compact_signal_without_production_ctas()
    {
        var competition = CreateCompetition.Execute("Suspended", _clock);
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
        stage.Suspend(_clock);

        var presentation = StructureViewAssembler.Assemble(
            competition,
            [stage],
            matchSummariesByStage: new Dictionary<StageId, IReadOnlyList<MatchSummaryRow>>
            {
                [stage.Id] = [Summary(match)]
            }).Stages[0].MatchProductionPresentation;

        presentation.Should().NotBeNull();
        presentation!.ShowPrimaryGauge.Should().BeFalse();
        presentation.FrozenPartial.Should().NotBeNull();
        presentation.FrozenPartial!.Current.Should().Be(1);
        presentation.Materialize.Visible.Should().BeFalse();
        presentation.Dematerialize.Visible.Should().BeFalse();
    }

    [Fact]
    public void Championship_terminal_exhausted_has_no_follow_up()
    {
        var situation = TestCompetition.Create("Champ", _clock)
            .WithTeams("A", "B", "C")
            .WithStructure(StructureIntent.Championship());
        var stage = situation.RequirePrimaryStage();
        situation.PreparePrimaryStageForMaterialization();
        MaterializeMatches.Execute(situation.Competition, stage, [], _clock);

        var presentation = StructureViewAssembler.Assemble(situation.Competition, [stage]).Stages[0]
            .MatchProductionPresentation;

        presentation.Should().NotBeNull();
        presentation!.FollowUp.Should().BeNull();
        presentation.PrimaryGauge.MessageKind.Should().Be(
            StructureMatchProductionPresentationAssembler.MessageKindSuccess);
    }

    [Fact]
    public void Partial_with_draw_applied_and_unmaterialized_start_gate_uses_attention()
    {
        var presentation = AssemblePartialPresentation(
            StructureDrawExecutionBadge.Applied,
            CreateStartBlockedLifecycle(StructureStageLifecycleAssembler.DisabledUnmaterializedConfrontations),
            current: 0,
            capacity: 8);

        presentation.PrimaryGauge.MessageCode.Should().Be(
            StructureMatchProductionMessageCodes.Partial);
        presentation.PrimaryGauge.MessageKind.Should().Be(
            StructureMatchProductionPresentationAssembler.MessageKindDanger);
    }

    [Fact]
    public void Partial_with_draw_to_launch_uses_info_while_matches_remain()
    {
        var presentation = AssemblePartialPresentation(
            StructureDrawExecutionBadge.ToLaunch,
            lifecycle: null,
            current: 0,
            capacity: 8);

        presentation.PrimaryGauge.MessageCode.Should().Be(
            StructureMatchProductionMessageCodes.Partial);
        presentation.PrimaryGauge.MessageKind.Should().Be(
            StructureMatchProductionPresentationAssembler.MessageKindInfo);
    }

    [Fact]
    public void No_determined_with_draw_to_launch_uses_info_severity()
    {
        var presentation = AssembleNoDeterminedPresentation(
            StructureDrawExecutionBadge.ToLaunch,
            hasDrawRules: true);

        presentation.PrimaryGauge.MessageCode.Should().Be(
            StructureMatchProductionMessageCodes.NoDeterminedConfrontations);
        presentation.PrimaryGauge.MessageKind.Should().Be(
            StructureMatchProductionPresentationAssembler.MessageKindInfo);
    }

    [Fact]
    public void No_determined_with_draw_applied_uses_attention_severity()
    {
        var presentation = AssembleNoDeterminedPresentation(
            StructureDrawExecutionBadge.Applied,
            hasDrawRules: true);

        presentation.PrimaryGauge.MessageCode.Should().Be(
            StructureMatchProductionMessageCodes.NoDeterminedConfrontations);
        presentation.PrimaryGauge.MessageKind.Should().Be(
            StructureMatchProductionPresentationAssembler.MessageKindAttention);
    }

    [Fact]
    public void Draw_obsolete_for_consumption_keeps_danger_severity()
    {
        var presentation = AssembleBlockedPresentation(
            StructureMaterializeDisabledReasons.DrawObsoleteForConsumption);

        presentation.PrimaryGauge.MessageCode.Should().Be(
            StructureMaterializeDisabledReasons.DrawObsoleteForConsumption);
        presentation.PrimaryGauge.MessageKind.Should().Be(
            StructureMatchProductionPresentationAssembler.MessageKindDanger);
    }

    [Fact]
    public void Structure_get_json_includes_match_production_presentation_property()
    {
        var situation = TestCompetition.Create("Champ", _clock)
            .WithTeams("A", "B", "C")
            .WithStructure(StructureIntent.Championship());
        var stage = situation.PreparePrimaryStageForMaterialization();

        var hub = StructureViewAssembler.Assemble(situation.Competition, [stage]).Stages[0];
        var json = JsonSerializer.Serialize(
            hub,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        json.Should().Contain("matchProductionPresentation");
        json.Should().Contain("productionVisible");
        json.Should().Contain("showPrimaryGauge");
        json.Should().Contain("frozenPartial");
    }

    [Fact]
    public void Suspended_with_matches_keeps_dematerialize_visible_but_disabled()
    {
        var situation = TestCompetition.Create("Cup", _clock)
            .WithTeams("Alpha", "Bravo")
            .WithStructure(StructureIntent.Cup(2));
        var stage = situation.RequirePrimaryStage();
        var entries = situation.Competition.Entries.Select(entry => entry.Id).ToArray();
        stage.ApplyResolvedEntry(stage.Slots[0].SlotKey, entries[0], _clock);
        stage.ApplyResolvedEntry(stage.Slots[1].SlotKey, entries[1], _clock);
        stage.Prepare(_clock);
        MaterializeCupFromOccupiedSlots.Execute(
            situation.Competition,
            stage,
            [],
            [],
            _clock);
        stage.Start(_clock);
        stage.Suspend(_clock);

        var hub = StructureViewAssembler.Assemble(situation.Competition, [stage]).Stages[0];
        var presentation = hub.MatchProductionPresentation;

        presentation.Should().NotBeNull();
        presentation!.ProductionVisible.Should().BeTrue();
        presentation.ShowPrimaryGauge.Should().BeFalse();
        presentation.FrozenPartial.Should().BeNull();
        presentation.Dematerialize.Visible.Should().BeFalse();
        presentation.Dematerialize.Enabled.Should().BeFalse();
    }

    private static StructureStageLifecycleDto CreateStartBlockedLifecycle(string disabledReason) =>
        new(
            new StructureStageLifecycleTransitionDto(
                OverviewAssembler.ActionStartStage,
                false,
                disabledReason),
            null);

    private StructureMatchProductionPresentationDto AssemblePartialPresentation(
        StructureDrawExecutionBadge? drawBadge,
        StructureStageLifecycleDto? lifecycle,
        int current,
        int capacity)
    {
        var situation = TestCompetition.Create("Groups", _clock)
            .WithTeams("A", "B", "C", "D")
            .WithStructure(StructureIntent.Groups(2, 2));
        var stage = situation.RequirePrimaryStage();
        ReplaceStageDrawRules.Execute(
            stage,
            new DrawRules(DrawMode.Random, potRules: new PotRules(2)),
            _clock);
        stage.Prepare(_clock);

        var materialize = new StructureMaterializeDto(
            OverviewAssembler.ActionMaterializeMatches,
            OverviewAssembler.LabelVariantCreate,
            capacity,
            capacity - current,
            current,
            true,
            []);

        return StructureMatchProductionPresentationAssembler.Assemble(
            new StructureMatchProductionPresentationAssembler.Input(
                situation.Competition,
                stage,
                [stage],
                StructureFormatKind.Groups,
                materialize,
                null,
                current,
                1,
                null,
                null,
                0,
                null,
                lifecycle,
                true,
                drawBadge,
                null))!;
    }

    private StructureMatchProductionPresentationDto AssembleNoDeterminedPresentation(
        StructureDrawExecutionBadge? drawBadge,
        bool hasDrawRules)
    {
        var situation = TestCompetition.Create("Cup draw", _clock)
            .WithTeams("A", "B", "C", "D")
            .WithStructure(StructureIntent.Cup(4));
        var stage = situation.RequirePrimaryStage();
        ReplaceStageDrawRules.Execute(
            stage,
            new DrawRules(DrawMode.Random, potRules: new PotRules(2)),
            _clock);
        stage.Prepare(_clock);

        var materialize = new StructureMaterializeDto(
            OverviewAssembler.ActionMaterializeFromOccupiedSlots,
            StructureMaterializeAssembler.LabelVariantIdle,
            0,
            0,
            0,
            false,
            [],
            StructureMaterializeDisabledReasons.NoDeterminedConfrontations);

        return StructureMatchProductionPresentationAssembler.Assemble(
            new StructureMatchProductionPresentationAssembler.Input(
                situation.Competition,
                stage,
                [stage],
                StructureFormatKind.Cup,
                materialize,
                null,
                0,
                1,
                null,
                null,
                0,
                null,
                null,
                hasDrawRules,
                drawBadge,
                null))!;
    }

    private StructureMatchProductionPresentationDto AssembleBlockedPresentation(string disabledReason)
    {
        var situation = TestCompetition.Create("Cup", _clock)
            .WithTeams("A", "B")
            .WithStructure(StructureIntent.Cup(2));
        var stage = situation.RequirePrimaryStage();
        stage.Prepare(_clock);

        var materialize = new StructureMaterializeDto(
            OverviewAssembler.ActionMaterializeFromOccupiedSlots,
            StructureMaterializeAssembler.LabelVariantIdle,
            0,
            0,
            0,
            false,
            [],
            disabledReason);

        return StructureMatchProductionPresentationAssembler.Assemble(
            new StructureMatchProductionPresentationAssembler.Input(
                situation.Competition,
                stage,
                [stage],
                StructureFormatKind.Cup,
                materialize,
                null,
                0,
                1,
                null,
                null,
                0,
                null,
                null,
                false,
                null,
                null))!;
    }

    private static MatchSummaryRow Summary(Match match) =>
        new(match.Id, match.StageId, match.Status, match.HomeEntryId, match.AwayEntryId, match.Result);
}
