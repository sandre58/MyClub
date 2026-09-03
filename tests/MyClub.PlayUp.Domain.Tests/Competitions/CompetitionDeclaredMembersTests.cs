// -----------------------------------------------------------------------
// <copyright file="CompetitionDeclaredMembersTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Competitions.Events;
using MyClub.PlayUp.Domain.Tests.Common;
using Xunit;

namespace MyClub.PlayUp.Domain.Tests.Competitions;

public sealed class CompetitionDeclaredMembersTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 29, 18, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Case1_amateur_tournament_declares_subset_of_players_and_staff()
    {
        var competition = Competition.Create(new CompetitionName("Cup"), SampleRegulations.Standard(), _clock);
        var entry = competition.AddEntry(TeamId.New(), "FC Local", _clock);

        for (var i = 1; i <= 15; i++)
        {
            competition.AddDeclaredMember(entry.Id, $"Player {i}", DeclaredMemberRole.Player, _clock);
        }

        competition.AddDeclaredMember(entry.Id, "Coach", DeclaredMemberRole.Staff, _clock);
        competition.AddDeclaredMember(entry.Id, "Delegate", DeclaredMemberRole.Staff, _clock);

        entry.DeclaredMembers.Should().HaveCount(17);
        entry.DeclaredMembers.Count(m => m.Role == DeclaredMemberRole.Player).Should().Be(15);
        entry.DeclaredMembers.Count(m => m.Role == DeclaredMemberRole.Staff).Should().Be(2);
    }

    [Fact]
    public void Case2_same_display_name_on_two_entries_yields_independent_member_ids()
    {
        var competition = Competition.Create(new CompetitionName("Season"), SampleRegulations.Standard(), _clock);
        var a = competition.AddEntry(TeamId.New(), "Team A", _clock);
        var b = competition.AddEntry(TeamId.New(), "Team B", _clock);

        var memberA = competition.AddDeclaredMember(a.Id, "Dupont", DeclaredMemberRole.Player, _clock);
        var memberB = competition.AddDeclaredMember(b.Id, "Dupont", DeclaredMemberRole.Player, _clock);

        memberA.Id.Should().NotBe(memberB.Id);
        memberA.DisplayName.Should().Be(memberB.DisplayName);
    }

    [Fact]
    public void Case3_reentry_starts_with_empty_roster_while_old_entry_keeps_members_read_only()
    {
        var competition = Competition.Create(new CompetitionName("League"), SampleRegulations.Standard(), _clock);
        var teamId = TeamId.New();
        var first = competition.AddEntry(teamId, "Team A", _clock);
        var kept = competition.AddDeclaredMember(first.Id, "Dupont", DeclaredMemberRole.Player, _clock);

        competition.WithdrawEntry(first.Id, _clock);
        var second = competition.AddEntry(teamId, "Team A return", _clock);

        second.DeclaredMembers.Should().BeEmpty();
        first.DeclaredMembers.Should().ContainSingle(m => m.Id.Equals(kept.Id));

        var mutateWithdrawn = () => competition.AddDeclaredMember(first.Id, "Other", DeclaredMemberRole.Player, _clock);
        mutateWithdrawn.Should().Throw<DomainException>().Which.Code.Should().Be(CompetitionErrorCodes.InvalidTransition);
    }

    [Fact]
    public void Case4_mutations_allowed_while_running_or_suspended_when_entry_active()
    {
        var competition = CreateRunning();
        var entry = competition.Entries[0];
        competition.ClearDomainEvents();

        var member = competition.AddDeclaredMember(entry.Id, "Dupont", DeclaredMemberRole.Player, _clock);
        competition.RenameDeclaredMember(entry.Id, member.Id, "Jean Dupont", _clock);
        competition.ChangeDeclaredMemberRole(entry.Id, member.Id, DeclaredMemberRole.Staff, _clock);

        member.DisplayName.Should().Be("Jean Dupont");
        member.Role.Should().Be(DeclaredMemberRole.Staff);

        competition.Suspend(_clock);
        competition.ClearDomainEvents();
        competition.RemoveDeclaredMember(entry.Id, member.Id, _clock);
        entry.DeclaredMembers.Should().BeEmpty();
        competition.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<CompetitionDeclaredMemberRemoved>();
    }

    [Fact]
    public void Case4_mutations_allowed_when_completed_or_archived()
    {
        var competition = CreateRunning();
        var entry = competition.Entries[0];
        competition.Complete(CompletionMode.Normal, _clock);

        var member = competition.AddDeclaredMember(entry.Id, "Late", DeclaredMemberRole.Player, _clock);
        member.DisplayName.Should().Be("Late");

        competition.Archive(_clock);
        competition.RenameDeclaredMember(entry.Id, member.Id, "Jean Late", _clock);
        member.DisplayName.Should().Be("Jean Late");
    }

    [Fact]
    public void Case4_mutations_rejected_when_entry_withdrawn()
    {
        var competition = Competition.Create(new CompetitionName("League"), SampleRegulations.Standard(), _clock);
        var entry = competition.AddEntry(TeamId.New(), "Team A", _clock);
        competition.WithdrawEntry(entry.Id, _clock);

        var act = () => competition.AddDeclaredMember(entry.Id, "Late", DeclaredMemberRole.Player, _clock);
        act.Should().Throw<DomainException>().Which.Code.Should().Be(CompetitionErrorCodes.InvalidTransition);
    }

    [Fact]
    public void Case5_and_6_member_id_stable_across_rename()
    {
        var competition = Competition.Create(new CompetitionName("Cup"), SampleRegulations.Standard(), _clock);
        var entry = competition.AddEntry(TeamId.New(), "Team A", _clock);
        var member = competition.AddDeclaredMember(entry.Id, "J. Dupont", DeclaredMemberRole.Player, _clock);
        var id = member.Id;

        competition.RenameDeclaredMember(entry.Id, id, "Jean Dupont", _clock);

        member.Id.Should().Be(id);
        member.DisplayName.Should().Be("Jean Dupont");
        competition.DomainEvents[^1].Should().BeOfType<CompetitionDeclaredMemberRenamed>();
    }

    [Fact]
    public void Case7_staff_role_is_distinct_from_player()
    {
        var competition = Competition.Create(new CompetitionName("Cup"), SampleRegulations.Standard(), _clock);
        var entry = competition.AddEntry(TeamId.New(), "Team A", _clock);

        var player = competition.AddDeclaredMember(entry.Id, "Dupont", DeclaredMemberRole.Player, _clock);
        var staff = competition.AddDeclaredMember(entry.Id, "Coach", DeclaredMemberRole.Staff, _clock);

        player.Role.Should().Be(DeclaredMemberRole.Player);
        staff.Role.Should().Be(DeclaredMemberRole.Staff);
    }

    [Fact]
    public void AddDeclaredMember_rejects_empty_display_name()
    {
        var competition = Competition.Create(new CompetitionName("Cup"), SampleRegulations.Standard(), _clock);
        var entry = competition.AddEntry(TeamId.New(), "Team A", _clock);

        var act = () => competition.AddDeclaredMember(entry.Id, "   ", DeclaredMemberRole.Player, _clock);
        act.Should().Throw<DomainException>().Which.Code.Should().Be(CompetitionErrorCodes.InvalidMemberDisplayName);
    }

    [Fact]
    public void RemoveDeclaredMember_rejects_unknown_member()
    {
        var competition = Competition.Create(new CompetitionName("Cup"), SampleRegulations.Standard(), _clock);
        var entry = competition.AddEntry(TeamId.New(), "Team A", _clock);

        var act = () => competition.RemoveDeclaredMember(entry.Id, MemberId.New(), _clock);
        act.Should().Throw<DomainException>().Which.Code.Should().Be(CompetitionErrorCodes.MemberNotFound);
    }

    [Fact]
    public void AddDeclaredMember_with_explicit_member_id_uses_that_identity()
    {
        var competition = Competition.Create(new CompetitionName("Cup"), SampleRegulations.Standard(), _clock);
        var entry = competition.AddEntry(TeamId.New(), "Team A", _clock);
        var memberId = new MemberId(Guid.Parse("33333333-3333-5333-8333-333333333333"));

        var member = competition.AddDeclaredMember(
            entry.Id, "Dupont", DeclaredMemberRole.Player, memberId, _clock);

        member.Id.Should().Be(memberId);
        competition.DomainEvents[^1].Should().BeOfType<CompetitionDeclaredMemberAdded>();
    }

    private Competition CreateRunning()
    {
        var competition = Competition.Create(new CompetitionName("League"), SampleRegulations.Standard(), _clock);
        competition.AddEntry(TeamId.New(), "Team A", _clock);
        competition.AddStage(StageId.New(), _clock);
        competition.Prepare(_clock);
        competition.Start(_clock);
        return competition;
    }
}
