// -----------------------------------------------------------------------
// <copyright file="DeclaredParticipationUseCaseTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Matches;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Matches;

public sealed class DeclaredParticipationUseCaseTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 31, 8, 30, 0, TimeSpan.Zero));

    [Fact]
    public void AddDeclaredParticipation_adds_eligible_player_to_sheet()
    {
        var (match, competition, member) = CreateScheduledMatchWithHomePlayer();

        var participation = AddDeclaredParticipation.Execute(
            match,
            competition,
            member.Id,
            Side.Home,
            CompositionStatus.Starter,
            _clock,
            jerseyNumber: 9);

        participation.CompositionStatus.Should().Be(CompositionStatus.Starter);
        participation.JerseyNumber.Should().Be(9);
        match.DeclaredParticipations.Should().ContainSingle();
    }

    [Fact]
    public void AddDeclaredParticipation_rejects_member_not_on_roster()
    {
        var (match, competition, _) = CreateScheduledMatchWithHomePlayer();

        var act = () => AddDeclaredParticipation.Execute(
            match,
            competition,
            MemberId.New(),
            Side.Home,
            CompositionStatus.Starter,
            _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.ParticipationNotEligible);
    }

    [Fact]
    public void AddDeclaredParticipation_rejects_staff_on_sheet()
    {
        var (match, competition, _) = CreateScheduledMatchWithHomePlayer();
        var coach = AddDeclaredMember.Execute(competition, match.HomeEntryId, "Coach", DeclaredMemberRole.Staff, _clock);

        var act = () => AddDeclaredParticipation.Execute(
            match,
            competition,
            coach.Id,
            Side.Home,
            CompositionStatus.Bench,
            _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.ParticipationNotEligible);
    }

    [Fact]
    public void AddDeclaredParticipation_rejects_member_from_other_side_entry()
    {
        var (match, competition, _) = CreateScheduledMatchWithHomePlayer();
        var awayPlayer = AddDeclaredMember.Execute(
            competition,
            match.AwayEntryId,
            "Away Player",
            DeclaredMemberRole.Player,
            _clock);

        var act = () => AddDeclaredParticipation.Execute(
            match,
            competition,
            awayPlayer.Id,
            Side.Home,
            CompositionStatus.Starter,
            _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.ParticipationNotEligible);
    }

    [Fact]
    public void AddDeclaredParticipation_rejects_inactive_entry()
    {
        var (match, competition, member) = CreateScheduledMatchWithHomePlayer();
        competition.WithdrawEntry(match.HomeEntryId, _clock);

        var act = () => AddDeclaredParticipation.Execute(
            match,
            competition,
            member.Id,
            Side.Home,
            CompositionStatus.Starter,
            _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.ParticipationNotEligible);
    }

    [Fact]
    public void AddDeclaredParticipation_rejects_unknown_side_and_composition_status()
    {
        var (match, competition, member) = CreateScheduledMatchWithHomePlayer();

        var invalidSide = () => AddDeclaredParticipation.Execute(
            match,
            competition,
            member.Id,
            (Side)99,
            CompositionStatus.Starter,
            _clock);
        invalidSide.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.InvalidSide);

        var invalidStatus = () => AddDeclaredParticipation.Execute(
            match,
            competition,
            member.Id,
            Side.Home,
            (CompositionStatus)99,
            _clock);
        invalidStatus.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.InvalidCompositionStatus);
    }

    [Fact]
    public void RemoveDeclaredParticipation_removes_sheet_line()
    {
        var (match, competition, member) = CreateScheduledMatchWithHomePlayer();
        AddDeclaredParticipation.Execute(
            match, competition, member.Id, Side.Home, CompositionStatus.Starter, _clock);

        RemoveDeclaredParticipation.Execute(match, member.Id, _clock);

        match.DeclaredParticipations.Should().BeEmpty();
    }

    [Fact]
    public void ChangeDeclaredParticipationCompositionStatus_and_jersey_work()
    {
        var (match, competition, member) = CreateScheduledMatchWithHomePlayer();
        AddDeclaredParticipation.Execute(
            match, competition, member.Id, Side.Home, CompositionStatus.Starter, _clock);

        ChangeDeclaredParticipationCompositionStatus.Execute(
            match, member.Id, CompositionStatus.Bench, _clock);
        SetDeclaredParticipationJerseyNumber.Execute(match, member.Id, 11, _clock);

        var participation = match.DeclaredParticipations.Single();
        participation.CompositionStatus.Should().Be(CompositionStatus.Bench);
        participation.JerseyNumber.Should().Be(11);
    }

    private (Match Match, Competition Competition, DeclaredMember HomePlayer) CreateScheduledMatchWithHomePlayer()
    {
        var competition = CreateCompetition.Execute("Sheet Cup", _clock);
        var home = AddEntry.Execute(competition, "Home", _clock);
        var away = AddEntry.Execute(competition, "Away", _clock);
        var player = AddDeclaredMember.Execute(competition, home.Id, "Dupont", DeclaredMemberRole.Player, _clock);

        var stage = Stage.Create(competition.Id, new StageName("Stage 1"), competition.Regulation, _clock);
        competition.AddStage(stage.Id, _clock);
        var match = Match.Create(competition.Id, stage.Id, home.Id, away.Id, _clock);

        return (match, competition, player);
    }
}
