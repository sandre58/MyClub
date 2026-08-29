// -----------------------------------------------------------------------
// <copyright file="MatchDeclaredParticipationsTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Matches.Events;
using MyClub.PlayUp.Domain.Tests.Common;
using Xunit;

namespace MyClub.PlayUp.Domain.Tests.Matches;

public sealed class MatchDeclaredParticipationsTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 29, 22, 0, 0, TimeSpan.Zero));
    private readonly CompetitionId _competitionId = CompetitionId.New();
    private readonly StageId _stageId = StageId.New();
    private readonly EntryId _home = EntryId.New();
    private readonly EntryId _away = EntryId.New();

    [Fact]
    public void Case1_add_declared_participation_on_scheduled_match()
    {
        var match = CreateMatch();
        var memberId = MemberId.New();

        var participation = match.AddDeclaredParticipation(
            memberId, Side.Home, CompositionStatus.Starter, _clock, jerseyNumber: 10);

        participation.Id.Should().Be(memberId);
        participation.Side.Should().Be(Side.Home);
        participation.CompositionStatus.Should().Be(CompositionStatus.Starter);
        participation.JerseyNumber.Should().Be(10);
        match.DeclaredParticipations.Should().ContainSingle();
        match.DomainEvents.OfType<MatchDeclaredParticipationAdded>().Should().ContainSingle();
    }

    [Fact]
    public void Case2_match_accepts_member_id_without_loading_competition()
    {
        // C1: eligibility is Application — Match only enforces local invariants.
        var match = CreateMatch();
        var unknownMemberId = MemberId.New();

        var act = () => match.AddDeclaredParticipation(
            unknownMemberId, Side.Away, CompositionStatus.Bench, _clock);

        act.Should().NotThrow();
        match.HasDeclaredParticipation(unknownMemberId).Should().BeTrue();
    }

    [Fact]
    public void Case3_scheduled_allows_remove_from_sheet_before_roster_removal()
    {
        var match = CreateMatch();
        var memberId = MemberId.New();
        match.AddDeclaredParticipation(memberId, Side.Home, CompositionStatus.Starter, _clock);

        match.RemoveDeclaredParticipation(memberId, _clock);

        match.DeclaredParticipations.Should().BeEmpty();
        match.HasDeclaredParticipation(memberId).Should().BeFalse();

        // R4 refuse on Competition.RemoveDeclaredMember while still on sheet = Application orchestration.
    }

    [Fact]
    public void Case4_after_start_composition_is_immutable_and_participation_remains()
    {
        var match = CreateMatch();
        var memberId = MemberId.New();
        match.AddDeclaredParticipation(memberId, Side.Home, CompositionStatus.Starter, _clock);
        match.Start(_clock);

        var removeFromSheet = () => match.RemoveDeclaredParticipation(memberId, _clock);
        var addOther = () => match.AddDeclaredParticipation(
            MemberId.New(), Side.Away, CompositionStatus.Bench, _clock);

        removeFromSheet.Should().Throw<DomainException>().Which.Code.Should().Be(MatchErrorCodes.CompositionImmutable);
        addOther.Should().Throw<DomainException>().Which.Code.Should().Be(MatchErrorCodes.CompositionImmutable);
        match.HasDeclaredParticipation(memberId).Should().BeTrue();
    }

    [Fact]
    public void Case5_participation_keeps_member_id_only_no_display_name_snapshot()
    {
        var match = CreateMatch();
        var memberId = MemberId.New();
        var participation = match.AddDeclaredParticipation(
            memberId, Side.Home, CompositionStatus.Bench, _clock);

        participation.Id.Should().Be(memberId);
        typeof(DeclaredParticipation).GetProperty("DisplayName").Should().BeNull();
        typeof(DeclaredParticipation).GetProperty("Role").Should().BeNull();
    }

    [Fact]
    public void Case6_finish_keeps_composition_immutable()
    {
        var match = CreateMatch();
        var memberId = MemberId.New();
        match.AddDeclaredParticipation(memberId, Side.Home, CompositionStatus.Starter, _clock, 9);
        match.Start(_clock);
        match.Finish(new MatchResult(ResultType.Played, new Score(1, 0)), _clock);

        var mutate = () => match.ChangeDeclaredParticipationCompositionStatus(
            memberId, CompositionStatus.Bench, _clock);

        mutate.Should().Throw<DomainException>().Which.Code.Should().Be(MatchErrorCodes.CompositionImmutable);
        match.DeclaredParticipations.Should().ContainSingle()
            .Which.JerseyNumber.Should().Be(9);
    }

    [Fact]
    public void Case6b_cancelled_keeps_composition_immutable()
    {
        var match = CreateMatch();
        var memberId = MemberId.New();
        match.AddDeclaredParticipation(memberId, Side.Away, CompositionStatus.Bench, _clock);
        match.Cancel(_clock);

        var mutate = () => match.RemoveDeclaredParticipation(memberId, _clock);

        mutate.Should().Throw<DomainException>().Which.Code.Should().Be(MatchErrorCodes.CompositionImmutable);
        match.HasDeclaredParticipation(memberId).Should().BeTrue();
    }

    [Fact]
    public void Case7_start_allowed_without_declared_participations()
    {
        var match = CreateMatch();

        var act = () => match.Start(_clock);

        act.Should().NotThrow();
        match.Status.Should().Be(MatchStatus.Live);
        match.DeclaredParticipations.Should().BeEmpty();
    }

    [Fact]
    public void Postponed_composition_remains_mutable_like_scheduled()
    {
        var match = CreateMatch();
        match.Postpone(_clock);
        var memberId = MemberId.New();

        match.AddDeclaredParticipation(memberId, Side.Home, CompositionStatus.Starter, _clock);
        match.ChangeDeclaredParticipationCompositionStatus(memberId, CompositionStatus.Bench, _clock);
        match.SetDeclaredParticipationJerseyNumber(memberId, 7, _clock);
        match.RemoveDeclaredParticipation(memberId, _clock);

        match.DeclaredParticipations.Should().BeEmpty();
    }

    [Fact]
    public void Rejects_duplicate_member_id_on_same_match()
    {
        var match = CreateMatch();
        var memberId = MemberId.New();
        match.AddDeclaredParticipation(memberId, Side.Home, CompositionStatus.Starter, _clock);

        var act = () => match.AddDeclaredParticipation(
            memberId, Side.Away, CompositionStatus.Bench, _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(MatchErrorCodes.DuplicateParticipation);
    }

    [Fact]
    public void Rejects_duplicate_jersey_number_on_same_side_allows_across_sides()
    {
        var match = CreateMatch();
        match.AddDeclaredParticipation(MemberId.New(), Side.Home, CompositionStatus.Starter, _clock, 10);

        var sameSide = () => match.AddDeclaredParticipation(
            MemberId.New(), Side.Home, CompositionStatus.Bench, _clock, 10);
        var otherSide = () => match.AddDeclaredParticipation(
            MemberId.New(), Side.Away, CompositionStatus.Starter, _clock, 10);

        sameSide.Should().Throw<DomainException>().Which.Code.Should().Be(MatchErrorCodes.DuplicateJerseyNumber);
        otherSide.Should().NotThrow();
    }

    [Fact]
    public void Allows_missing_jersey_number_and_multiple_nulls_on_same_side()
    {
        var match = CreateMatch();

        match.AddDeclaredParticipation(MemberId.New(), Side.Home, CompositionStatus.Starter, _clock);
        match.AddDeclaredParticipation(MemberId.New(), Side.Home, CompositionStatus.Bench, _clock);

        match.DeclaredParticipations.Should().HaveCount(2);
        match.DeclaredParticipations.Should().OnlyContain(p => p.JerseyNumber == null);
    }

    private Match CreateMatch() =>
        Match.Create(_competitionId, _stageId, _home, _away, _clock);
}
