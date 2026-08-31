// -----------------------------------------------------------------------
// <copyright file="DeclaredMemberUseCaseTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Competitions;

public sealed class DeclaredMemberUseCaseTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 31, 8, 0, 0, TimeSpan.Zero));

    [Fact]
    public void AddDeclaredMember_adds_player_and_staff_to_entry_roster()
    {
        var competition = CreateCompetition.Execute("Roster Cup", _clock);
        var entry = AddEntry.Execute(competition, "FC Local", _clock);

        var player = AddDeclaredMember.Execute(competition, entry.Id, "Dupont", DeclaredMemberRole.Player, _clock);
        var staff = AddDeclaredMember.Execute(competition, entry.Id, "Coach", DeclaredMemberRole.Staff, _clock);

        player.Role.Should().Be(DeclaredMemberRole.Player);
        staff.Role.Should().Be(DeclaredMemberRole.Staff);
        competition.GetEntry(entry.Id).DeclaredMembers.Should().HaveCount(2);
    }

    [Fact]
    public void AddDeclaredMember_rejects_unknown_role()
    {
        var competition = CreateCompetition.Execute("Roster Cup", _clock);
        var entry = AddEntry.Execute(competition, "FC Local", _clock);

        var act = () => AddDeclaredMember.Execute(
            competition,
            entry.Id,
            "Dupont",
            (DeclaredMemberRole)99,
            _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.InvalidDeclaredMemberRole);
    }

    [Fact]
    public void RemoveDeclaredMember_removes_member_when_not_on_match_sheet()
    {
        var competition = CreateCompetition.Execute("Roster Cup", _clock);
        var entry = AddEntry.Execute(competition, "FC Local", _clock);
        var member = AddDeclaredMember.Execute(competition, entry.Id, "Dupont", DeclaredMemberRole.Player, _clock);

        RemoveDeclaredMember.Execute(competition, entry.Id, member.Id, [], _clock);

        competition.GetEntry(entry.Id).DeclaredMembers.Should().BeEmpty();
    }

    [Fact]
    public void RemoveDeclaredMember_rejects_when_member_still_on_match_sheet()
    {
        var competition = CreateCompetition.Execute("Roster Cup", _clock);
        var home = AddEntry.Execute(competition, "Home", _clock);
        var away = AddEntry.Execute(competition, "Away", _clock);
        var member = AddDeclaredMember.Execute(competition, home.Id, "Dupont", DeclaredMemberRole.Player, _clock);

        var stage = Stage.Create(competition.Id, new StageName("Stage 1"), competition.Regulation, _clock);
        competition.AddStage(stage.Id, _clock);
        var match = Match.Create(competition.Id, stage.Id, home.Id, away.Id, _clock);
        match.AddDeclaredParticipation(member.Id, Side.Home, CompositionStatus.Starter, _clock);

        var act = () => RemoveDeclaredMember.Execute(competition, home.Id, member.Id, [match], _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.DeclaredMemberReferencedByMatchSheet);
        competition.GetEntry(home.Id).DeclaredMembers.Should().ContainSingle();
    }

    [Fact]
    public void RenameDeclaredMember_and_change_role_work()
    {
        var competition = CreateCompetition.Execute("Roster Cup", _clock);
        var entry = AddEntry.Execute(competition, "FC Local", _clock);
        var member = AddDeclaredMember.Execute(competition, entry.Id, "Dupont", DeclaredMemberRole.Player, _clock);

        RenameDeclaredMember.Execute(competition, entry.Id, member.Id, "Jean Dupont", _clock);
        ChangeDeclaredMemberRole.Execute(competition, entry.Id, member.Id, DeclaredMemberRole.Staff, _clock);

        var updated = competition.GetEntry(entry.Id).DeclaredMembers.Single(m => m.Id.Equals(member.Id));
        updated.DisplayName.Should().Be("Jean Dupont");
        updated.Role.Should().Be(DeclaredMemberRole.Staff);
    }

    [Fact]
    public void ChangeDeclaredMemberRole_rejects_unknown_role()
    {
        var competition = CreateCompetition.Execute("Roster Cup", _clock);
        var entry = AddEntry.Execute(competition, "FC Local", _clock);
        var member = AddDeclaredMember.Execute(competition, entry.Id, "Dupont", DeclaredMemberRole.Player, _clock);

        var act = () => ChangeDeclaredMemberRole.Execute(
            competition,
            entry.Id,
            member.Id,
            (DeclaredMemberRole)99,
            _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.InvalidDeclaredMemberRole);
    }
}
