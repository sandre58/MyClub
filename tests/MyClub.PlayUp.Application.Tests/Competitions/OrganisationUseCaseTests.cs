// -----------------------------------------------------------------------
// <copyright file="OrganisationUseCaseTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Competitions;

public sealed class OrganisationUseCaseTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 16, 10, 0, 0, TimeSpan.Zero));

    [Fact]
    public void AddEntry_then_rename_withdraw_exclude_work()
    {
        var competition = CreateCompetition.Execute("Org Cup", _clock);
        var entry = AddEntry.Execute(competition, "Alpha", _clock);
        entry.DisplayName.Should().Be("Alpha");

        RenameEntry.Execute(competition, entry.Id, "Alpha FC", _clock);
        competition.GetEntry(entry.Id).DisplayName.Should().Be("Alpha FC");

        var other = AddEntry.Execute(competition, "Beta", _clock);
        ExcludeEntry.Execute(competition, other.Id, _clock);
        competition.GetEntry(other.Id).Status.Should().Be(EntryStatus.Excluded);

        WithdrawEntry.Execute(competition, entry.Id, _clock);
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

        ReplaceRegulation.Execute(competition, replacement, _clock);

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
    public void ConfigureStructure_groups_builds_groups_matchday_and_pots()
    {
        var competition = CreateCompetition.Execute("Groups", _clock);
        var result = ConfigureStructure.Execute(
            competition,
            primaryStage: null,
            StructureIntent.Groups(4, 4),
            _clock);

        result.Stage.Groups.Should().HaveCount(4);
        result.Stage.Matchdays.Should().HaveCount(1);
        result.Stage.Regulation.DrawRules.Should().NotBeNull();
        result.Stage.Regulation.DrawRules!.PotRules!.NumberOfPots.Should().Be(4);
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
    }

    [Fact]
    public void ConfigureStructure_cup_rejects_non_power_of_two()
    {
        var act = () => StructureIntent.Cup(6);
        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.CupBracketNotPowerOfTwo);
    }

    [Fact]
    public void OrganisationView_readiness_requires_participants_and_structure()
    {
        var competition = CreateCompetition.Execute("Ready", _clock);
        var empty = OrganisationViewAssembler.Assemble(competition, []);
        empty.Readiness.ReadyForNextSlice.Should().BeFalse();
        empty.Readiness.Blockers.Should().Contain(OrganisationViewAssembler.BlockerInsufficientParticipants);
        empty.Readiness.Blockers.Should().Contain(OrganisationViewAssembler.BlockerMissingStage);

        AddEntry.Execute(competition, "A", _clock);
        AddEntry.Execute(competition, "B", _clock);
        var configured = ConfigureStructure.Execute(
            competition,
            null,
            StructureIntent.Championship(1),
            _clock);
        var view = OrganisationViewAssembler.Assemble(competition, [configured.Stage]);

        view.Format.Kind.Should().Be(StructureFormatKind.Championship);
        view.Readiness.ReadyForNextSlice.Should().BeTrue();
        view.Readiness.ReadyForSchedulePath.Should().BeTrue();
        view.Readiness.ReadyForDraw.Should().BeFalse();
        view.Readiness.Blockers.Should().BeEmpty();
    }

    [Fact]
    public void OrganisationView_groups_ready_for_draw()
    {
        var competition = CreateCompetition.Execute("G", _clock);
        AddEntry.Execute(competition, "A", _clock);
        AddEntry.Execute(competition, "B", _clock);
        var configured = ConfigureStructure.Execute(
            competition,
            null,
            StructureIntent.Groups(2, 2),
            _clock);
        var view = OrganisationViewAssembler.Assemble(competition, [configured.Stage]);

        view.Readiness.ReadyForDraw.Should().BeTrue();
        view.Readiness.ReadyForNextSlice.Should().BeTrue();
    }
}
