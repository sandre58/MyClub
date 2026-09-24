// -----------------------------------------------------------------------
// <copyright file="StructureUseCaseTests.cs" company="Stéphane ANDRE">
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
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Competitions;

public sealed class StructureUseCaseTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 16, 10, 0, 0, TimeSpan.Zero));

    [Fact]
    public void AddEntry_then_rename_and_delete_work_in_draft()
    {
        var competition = CreateCompetition.Execute("Org Cup", _clock);
        var entry = AddEntry.Execute(competition, "Alpha", _clock);
        entry.DisplayName.Should().Be("Alpha");

        RenameEntry.Execute(competition, entry.Id, "Alpha FC", _clock);
        competition.GetEntry(entry.Id).DisplayName.Should().Be("Alpha FC");

        var other = AddEntry.Execute(competition, "Beta", _clock);
        DeleteEntry.Execute(competition, other.Id, [], [], new NoOpMatchRepository(), _clock);
        competition.Entries.Should().ContainSingle(e => e.Id.Equals(entry.Id));
    }

    [Fact]
    public void WithdrawEntry_marks_forfait_while_running()
    {
        var competition = CreateCompetition.Execute("Org Cup", _clock);
        var entry = AddEntry.Execute(competition, "Alpha", _clock);
        AddEntry.Execute(competition, "Beta", _clock);
        competition.AddStage(StageId.New(), _clock);
        competition.Prepare(_clock);
        competition.Start(_clock);

        WithdrawEntry.Execute(competition, entry.Id, [], [], _clock);

        competition.GetEntry(entry.Id).Status.Should().Be(EntryStatus.Withdrawn);
    }

    [Fact]
    public void AddEntry_respects_maximum_teams()
    {
        var regulation = new Regulation(
            new EntryRules(1, 1),
            SampleRegulations.Standard().MatchRules,
            SampleRegulations.Standard().StandingRules);
        var competition = CreateCompetition.Execute("Tiny", regulation, _clock);
        AddEntry.Execute(competition, "Only", _clock);

        var act = () => AddEntry.Execute(competition, "TooMany", _clock);
        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.EntryCapacityExceeded);
    }

    [Fact]
    public void ReplaceRegulation_updates_entry_rules()
    {
        var competition = CreateCompetition.Execute("Reg", _clock);
        var replacement = new Regulation(
            new EntryRules(4, 16),
            SampleRegulations.Standard().MatchRules,
            SampleRegulations.Standard().StandingRules);

        ReplaceRegulation.Execute(competition, stages: [], replacement, _clock);

        competition.Regulation.EntryRules.MinimumTeams.Should().Be(4);
        competition.Regulation.EntryRules.MaximumTeams.Should().Be(16);
    }

    [Fact]
    public void ConfigureStructure_championship_builds_matchdays()
    {
        var competition = CreateCompetition.Execute("Champ", _clock);
        var result = ConfigureStructure.Execute(
            competition,
            primaryStage: null,
            StructureIntent.Championship(3),
            _clock);

        result.StageCreated.Should().BeTrue();
        result.Stage.Matchdays.Should().HaveCount(3);
        result.Stage.Groups.Should().BeEmpty();
        result.Stage.Rounds.Should().BeEmpty();
        competition.StageIds.Should().ContainSingle().Which.Should().Be(result.Stage.Id);
    }

    [Fact]
    public void ConfigureStructure_groups_builds_groups_matchday_and_places()
    {
        var competition = CreateCompetition.Execute("Groups", _clock);
        var result = ConfigureStructure.Execute(
            competition,
            primaryStage: null,
            StructureIntent.Groups(4, 4),
            _clock);

        result.Stage.Groups.Should().HaveCount(4);
        result.Stage.Matchdays.Should().HaveCount(1);
        result.Stage.PlacesPerGroup.Should().Be(4);
        result.Stage.Regulation.DrawRules.Should().BeNull();
    }

    [Fact]
    public void ConfigureStructure_cup_builds_round_and_power_of_two_slots()
    {
        var competition = CreateCompetition.Execute("Cup", _clock);
        var result = ConfigureStructure.Execute(
            competition,
            primaryStage: null,
            StructureIntent.Cup(8),
            _clock);

        result.Stage.Rounds.Should().ContainSingle();
        result.Stage.Slots.Should().HaveCount(8);
        result.Stage.BracketPairs.Should().HaveCount(4);
        result.Stage.FindBracketPair("P1")!.SlotAKey.Should().Be("S1");
        result.Stage.FindBracketPair("P4")!.SlotBKey.Should().Be("S8");
    }

    [Fact]
    public void StructureSkeleton_Clear_removes_fixtures_before_bracket_pairs()
    {
        var competition = CreateCompetition.Execute("Cup-Clear", _clock);
        var result = ConfigureStructure.Execute(
            competition,
            primaryStage: null,
            StructureIntent.Cup(4),
            _clock);
        var stage = result.Stage;
        var pair = stage.BracketPairs.First(p => p.PairKey == "P1");
        stage.AddFixture(stage.Rounds[0].Id, _clock, pair.SlotAKey, pair.SlotBKey, pair.PairKey);

        var impact = StructureSkeleton.Clear(stage, _clock);

        impact.ClearedRounds.Should().Be(1);
        stage.Rounds.Should().BeEmpty();
        stage.BracketPairs.Should().BeEmpty();
        stage.Slots.Should().BeEmpty();
    }

    [Fact]
    public void ConfigureStructure_cup_rejects_non_power_of_two()
    {
        var act = () => StructureIntent.Cup(6);
        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.CupBracketNotPowerOfTwo);
    }

    [Fact]
    public void ConfigureStructure_swiss_sets_settings_without_matchdays()
    {
        var competition = CreateCompetition.Execute("Swiss", _clock);
        var result = ConfigureStructure.Execute(
            competition,
            primaryStage: null,
            StructureIntent.Swiss(3),
            _clock);

        result.Stage.IsSwiss.Should().BeTrue();
        result.Stage.SwissSettings!.RoundCount.Should().Be(3);
        result.Stage.Matchdays.Should().BeEmpty();
        result.Stage.Groups.Should().BeEmpty();
        result.Stage.Rounds.Should().BeEmpty();
        result.Stage.Slots.Should().BeEmpty();
    }

    [Fact]
    public void StructureView_swiss_ready_for_schedule_path_not_materialization()
    {
        var competition = CreateCompetition.Execute("Swiss Ready", _clock);
        AddEntry.Execute(competition, "A", _clock);
        AddEntry.Execute(competition, "B", _clock);
        var configured = ConfigureStructure.Execute(
            competition,
            null,
            StructureIntent.Swiss(4),
            _clock);
        var view = StructureViewAssembler.Assemble(competition, [configured.Stage]);

        view.Format.Kind.Should().Be(StructureFormatKind.Swiss);
        view.Structure.SwissRoundCount.Should().Be(4);
        view.Structure.MatchdayCount.Should().Be(0);
        view.Readiness.ReadyForSchedulePath.Should().BeTrue();
        view.Readiness.ReadyForMaterialization.Should().BeFalse();
        view.Readiness.ReadyForDraw.Should().BeFalse();
        view.Readiness.ReadyForNextSlice.Should().BeTrue();
        view.Readiness.Blockers.Should().BeEmpty();
    }

    [Fact]
    public void StructureView_readiness_requires_participants_and_structure()
    {
        var competition = CreateCompetition.Execute("Ready", _clock);
        var empty = StructureViewAssembler.Assemble(competition, []);
        empty.Readiness.ReadyForNextSlice.Should().BeFalse();
        empty.Readiness.Blockers.Should().Contain(StructureViewAssembler.BlockerInsufficientParticipants);
        empty.Readiness.Blockers.Should().Contain(StructureViewAssembler.BlockerMissingStage);

        AddEntry.Execute(competition, "A", _clock);
        AddEntry.Execute(competition, "B", _clock);
        var configured = ConfigureStructure.Execute(
            competition,
            null,
            StructureIntent.Championship(),
            _clock);
        var view = StructureViewAssembler.Assemble(competition, [configured.Stage]);

        view.Format.Kind.Should().Be(StructureFormatKind.Championship);
        view.Readiness.ReadyForNextSlice.Should().BeTrue();
        view.Readiness.ReadyForSchedulePath.Should().BeTrue();
        view.Readiness.ReadyForDraw.Should().BeFalse();
        view.Readiness.Blockers.Should().BeEmpty();
    }

    [Fact]
    public void StructureView_groups_ready_for_draw()
    {
        var competition = CreateCompetition.Execute("G", _clock);
        AddEntry.Execute(competition, "A", _clock);
        AddEntry.Execute(competition, "B", _clock);
        var configured = ConfigureStructure.Execute(
            competition,
            null,
            StructureIntent.Groups(2, 2),
            _clock);
        ReplaceStageDrawRules.Execute(
            configured.Stage,
            new DrawRules(DrawMode.Random, potRules: new PotRules(2)),
            _clock);
        var view = StructureViewAssembler.Assemble(competition, [configured.Stage]);

        view.Readiness.ReadyForDraw.Should().BeTrue();
        view.Readiness.ReadyForNextSlice.Should().BeTrue();
    }

    [Fact]
    public void StructureView_orders_entries_and_declared_members_by_display_name()
    {
        var competition = CreateCompetition.Execute("Sort Cup", _clock);
        AddEntry.Execute(competition, "Zebra", _clock);
        var middle = AddEntry.Execute(competition, "Lyon", _clock);
        AddEntry.Execute(competition, "Alpha", _clock);

        AddDeclaredMember.Execute(competition, middle.Id, "Martin", DeclaredMemberRole.Player, _clock);
        AddDeclaredMember.Execute(competition, middle.Id, "Bernard", DeclaredMemberRole.Player, _clock);
        AddDeclaredMember.Execute(competition, middle.Id, "Dupont", DeclaredMemberRole.Staff, _clock);
        AddDeclaredMember.Execute(competition, middle.Id, "Alain", DeclaredMemberRole.Staff, _clock);

        var view = StructureViewAssembler.Assemble(competition, []);

        view.Participants.Entries.Select(entry => entry.DisplayName)
            .Should()
            .Equal("Alpha", "Lyon", "Zebra");

        var roster = view.Participants.Entries.Single(entry => entry.EntryId == middle.Id.Value)
            .DeclaredMembers!
            .Select(member => member.DisplayName);
        roster.Should().Equal("Alain", "Bernard", "Dupont", "Martin");
    }

    [Fact]
    public void StructureView_places_by_format_and_clear_draw_preserves_groups_places()
    {
        var cupCompetition = CreateCompetition.Execute("Cup N", _clock);
        AddEntry.Execute(cupCompetition, "A", _clock);
        AddEntry.Execute(cupCompetition, "B", _clock);
        var cup = ConfigureStructure.Execute(
            cupCompetition,
            null,
            StructureIntent.Cup(8),
            _clock);
        var cupHub = StructureViewAssembler.Assemble(cupCompetition, [cup.Stage]).Stages.Single();
        cupHub.CompositionCapacity.Should().Be(8);

        // Multi-round Coupe: 14 form units (QF+SF+F) ⇒ Places N = 8 entry places.
        var multiCompetition = CreateCompetition.Execute("Cup multi N", _clock);
        var multi = ConfigureStructure.Execute(
            multiCompetition,
            null,
            StructureIntent.Cup(8),
            _clock);
        multi.Stage.AddRound("Demis", _clock);
        multi.Stage.AddRound("Finale", _clock);
        foreach (var key in new[] { "SF-1-A", "SF-1-B", "SF-2-A", "SF-2-B", "F-A", "F-B" })
        {
            multi.Stage.AddSlot(key);
        }

        multi.Stage.Slots.Count.Should().Be(14);
        multi.Stage.Rounds.Count.Should().Be(3);
        StructureViewAssembler.Assemble(multiCompetition, [multi.Stage])
            .Stages.Single()
            .CompositionCapacity.Should()
            .Be(8);

        var championshipCompetition = CreateCompetition.Execute("Champ N", _clock);
        AddEntry.Execute(championshipCompetition, "A", _clock);
        AddEntry.Execute(championshipCompetition, "B", _clock);
        AddEntry.Execute(championshipCompetition, "C", _clock);
        var championship = ConfigureStructure.Execute(
            championshipCompetition,
            null,
            StructureIntent.Championship(),
            _clock);
        var championshipHub = StructureViewAssembler
            .Assemble(championshipCompetition, [championship.Stage])
            .Stages.Single();
        championshipHub.CompositionCapacity.Should().Be(3);

        AddEntry.Execute(championshipCompetition, "D", _clock);
        StructureViewAssembler
            .Assemble(championshipCompetition, [championship.Stage])
            .Stages.Single()
            .CompositionCapacity.Should()
            .Be(4);

        var swissCompetition = CreateCompetition.Execute("Swiss N", _clock);
        AddEntry.Execute(swissCompetition, "A", _clock);
        AddEntry.Execute(swissCompetition, "B", _clock);
        var swiss = ConfigureStructure.Execute(
            swissCompetition,
            null,
            StructureIntent.Swiss(3),
            _clock);
        StructureViewAssembler.Assemble(swissCompetition, [swiss.Stage])
            .Stages.Single()
            .CompositionCapacity.Should()
            .Be(2);

        var groupsCompetition = CreateCompetition.Execute("Groups N", _clock);
        AddEntry.Execute(groupsCompetition, "A", _clock);
        AddEntry.Execute(groupsCompetition, "B", _clock);
        AddEntry.Execute(groupsCompetition, "C", _clock);
        AddEntry.Execute(groupsCompetition, "D", _clock);
        var groups = ConfigureStructure.Execute(
            groupsCompetition,
            null,
            StructureIntent.Groups(2, 4),
            _clock);
        groups.Stage.PlacesPerGroup.Should().Be(4);
        var groupsHub = StructureViewAssembler.Assemble(groupsCompetition, [groups.Stage]).Stages.Single();
        groupsHub.CompositionCapacity.Should().Be(8);
        groupsHub.PlacesPerGroup.Should().Be(4);

        groups.Stage.ReplaceDrawRules(null, _clock);
        groups.Stage.PlacesPerGroup.Should().Be(4);
        StructureViewAssembler.Assemble(groupsCompetition, [groups.Stage])
            .Stages.Single()
            .CompositionCapacity.Should()
            .Be(8);
    }

    private sealed class NoOpMatchRepository : IMatchRepository
    {
        public Task<Match?> GetByIdForUpdateAsync(MatchId id, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Match?> GetByIdReadOnlyAsync(MatchId id, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<Match>> ListByStageForUpdateAsync(
            StageId stageId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyDictionary<StageId, IReadOnlyList<Match>>> ListByStageIdsReadOnlyAsync(
            IReadOnlyList<StageId> stageIds,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<MatchSummaryRow>> ListSummaryRowsByStageReadOnlyAsync(
            StageId stageId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyDictionary<StageId, IReadOnlyList<MatchSummaryRow>>> ListSummaryRowsByStageIdsReadOnlyAsync(
            IReadOnlyList<StageId> stageIds,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyDictionary<StageId, IReadOnlyList<MatchAttentionSlice>>> ListAttentionSlicesByStageIdsReadOnlyAsync(
            IReadOnlyList<StageId> stageIds,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<MatchSheetMemberRef>> ListSheetMemberRefsByCompetitionReadOnlyAsync(
            CompetitionId competitionId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public void Add(Match match)
        {
        }

        public void Remove(Match match)
        {
        }
    }
}
