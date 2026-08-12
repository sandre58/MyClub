// -----------------------------------------------------------------------
// <copyright file="StageFixtureTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stage;
using MyClub.PlayUp.Domain.Stage.Events;
using MyClub.PlayUp.Domain.Tests.Common;
using Xunit;
using StageAggregate = MyClub.PlayUp.Domain.Stage.Stage;

namespace MyClub.PlayUp.Domain.Tests.Stage.Fixtures;

public sealed class StageFixtureTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 6, 16, 0, 0, TimeSpan.Zero));
    private readonly CompetitionId _competitionId = CompetitionId.New();

    [Fact]
    public void AddFixture_under_Round_raises_StageFixtureAdded()
    {
        var stage = CreateCupWithRound(out var round);
        stage.ClearDomainEvents();

        var fixture = stage.AddFixture(round.Id, _clock);

        fixture.MatchIds.Should().BeEmpty();
        round.Fixtures.Should().ContainSingle().Which.Id.Should().Be(fixture.Id);
        stage.HasFixture(fixture.Id).Should().BeTrue();
        var added = stage.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<StageFixtureAdded>().Subject;
        added.StageId.Should().Be(stage.Id);
        added.FixtureId.Should().Be(fixture.Id);
        added.OccurredOn.Should().Be(_clock.UtcNow);
    }

    [Fact]
    public void AddFixture_under_Matchday_raises_StageFixtureAdded()
    {
        var stage = CreateChampionshipWithMatchday(out var matchday);
        stage.ClearDomainEvents();

        var fixture = stage.AddFixture(matchday.Id, _clock);

        matchday.Fixtures.Should().ContainSingle().Which.Id.Should().Be(fixture.Id);
        stage.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<StageFixtureAdded>();
    }

    [Fact]
    public void AddFixture_unknown_round_is_rejected()
    {
        var stage = CreateCupWithRound(out _);

        var act = () => stage.AddFixture(RoundId.New(), _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.RoundNotFound);
    }

    [Fact]
    public void RemoveFixture_raises_StageFixtureRemoved()
    {
        var stage = CreateCupWithRound(out var round);
        var fixture = stage.AddFixture(round.Id, _clock);
        stage.ClearDomainEvents();

        stage.RemoveFixture(fixture.Id, _clock);

        stage.HasFixture(fixture.Id).Should().BeFalse();
        round.Fixtures.Should().BeEmpty();
        var removed = stage.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<StageFixtureRemoved>().Subject;
        removed.FixtureId.Should().Be(fixture.Id);
    }

    [Fact]
    public void RemoveFixture_unknown_is_rejected()
    {
        var stage = CreateCupWithRound(out _);

        var act = () => stage.RemoveFixture(FixtureId.New(), _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.FixtureNotFound);
    }

    [Fact]
    public void AttachMatch_raises_StageMatchAttached()
    {
        var stage = CreateCupWithRound(out var round);
        var fixture = stage.AddFixture(round.Id, _clock);
        var matchId = MatchId.New();
        stage.ClearDomainEvents();

        stage.AttachMatch(fixture.Id, matchId, legIndex: 1, _clock);

        fixture.MatchIds.Should().Equal(matchId);
        fixture.Attachments.Should().ContainSingle().Which.LegIndex.Should().Be(1);
        stage.HasMatch(matchId).Should().BeTrue();
        var attached = stage.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<StageMatchAttached>().Subject;
        attached.StageId.Should().Be(stage.Id);
        attached.FixtureId.Should().Be(fixture.Id);
        attached.MatchId.Should().Be(matchId);
        attached.LegIndex.Should().Be(1);
        attached.OccurredOn.Should().Be(_clock.UtcNow);
    }

    [Fact]
    public void AttachMatch_same_fixture_twice_is_noop()
    {
        var stage = CreateCupWithRound(out var round);
        var fixture = stage.AddFixture(round.Id, _clock);
        var matchId = MatchId.New();
        stage.AttachMatch(fixture.Id, matchId, legIndex: 1, _clock);
        stage.Prepare(_clock);
        stage.ClearDomainEvents();

        stage.AttachMatch(fixture.Id, matchId, legIndex: 1, _clock);

        stage.Status.Should().Be(StageStatus.Ready);
        fixture.MatchIds.Should().Equal(matchId);
        stage.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void AttachMatch_duplicate_leg_index_is_rejected()
    {
        var stage = CreateCupWithRound(out var round);
        var fixture = stage.AddFixture(round.Id, _clock);
        stage.AttachMatch(fixture.Id, MatchId.New(), legIndex: 1, _clock);

        var act = () => stage.AttachMatch(fixture.Id, MatchId.New(), legIndex: 1, _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.InvalidConfiguration);
    }

    [Fact]
    public void AttachMatch_to_other_fixture_is_rejected()
    {
        var stage = CreateCupWithRound(out var round);
        var first = stage.AddFixture(round.Id, _clock);
        var second = stage.AddFixture(round.Id, _clock);
        var matchId = MatchId.New();
        stage.AttachMatch(first.Id, matchId, legIndex: 1, _clock);

        var act = () => stage.AttachMatch(second.Id, matchId, legIndex: 1, _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.MatchAlreadyAttached);
    }

    [Fact]
    public void DetachMatch_raises_StageMatchDetached()
    {
        var stage = CreateCupWithRound(out var round);
        var fixture = stage.AddFixture(round.Id, _clock);
        var matchId = MatchId.New();
        stage.AttachMatch(fixture.Id, matchId, legIndex: 1, _clock);
        stage.ClearDomainEvents();

        stage.DetachMatch(fixture.Id, matchId, _clock);

        fixture.MatchIds.Should().BeEmpty();
        stage.HasMatch(matchId).Should().BeFalse();
        var detached = stage.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<StageMatchDetached>().Subject;
        detached.MatchId.Should().Be(matchId);
    }

    [Fact]
    public void DetachMatch_when_not_attached_is_rejected()
    {
        var stage = CreateCupWithRound(out var round);
        var fixture = stage.AddFixture(round.Id, _clock);

        var act = () => stage.DetachMatch(fixture.Id, MatchId.New(), _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.MatchNotAttached);
    }

    [Fact]
    public void MatchId_is_unique_across_Round_and_Matchday_fixtures()
    {
        var stage = CreateChampionshipWithMatchday(out var matchday);
        var a = stage.AddFixture(matchday.Id, _clock);
        var b = stage.AddFixture(matchday.Id, _clock);
        var matchId = MatchId.New();
        stage.AttachMatch(a.Id, matchId, legIndex: 1, _clock);

        var act = () => stage.AttachMatch(b.Id, matchId, legIndex: 1, _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.MatchAlreadyAttached);
    }

    [Fact]
    public void AddFixture_demotes_Ready_to_Draft()
    {
        var stage = CreateCupWithRound(out var round);
        stage.Prepare(_clock);
        stage.ClearDomainEvents();

        stage.AddFixture(round.Id, _clock);

        stage.Status.Should().Be(StageStatus.Draft);
        stage.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<StageFixtureAdded>();
    }

    [Fact]
    public void RemoveFixture_demotes_Ready_to_Draft()
    {
        var stage = CreateCupWithRound(out var round);
        var fixture = stage.AddFixture(round.Id, _clock);
        stage.Prepare(_clock);
        stage.ClearDomainEvents();

        stage.RemoveFixture(fixture.Id, _clock);

        stage.Status.Should().Be(StageStatus.Draft);
        stage.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<StageFixtureRemoved>();
    }

    [Fact]
    public void AttachMatch_demotes_Ready_to_Draft()
    {
        var stage = CreateCupWithRound(out var round);
        var fixture = stage.AddFixture(round.Id, _clock);
        stage.Prepare(_clock);
        stage.ClearDomainEvents();

        stage.AttachMatch(fixture.Id, MatchId.New(), legIndex: 1, _clock);

        stage.Status.Should().Be(StageStatus.Draft);
        stage.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<StageMatchAttached>();
    }

    [Fact]
    public void DetachMatch_demotes_Ready_to_Draft()
    {
        var stage = CreateCupWithRound(out var round);
        var fixture = stage.AddFixture(round.Id, _clock);
        var matchId = MatchId.New();
        stage.AttachMatch(fixture.Id, matchId, legIndex: 1, _clock);
        stage.Prepare(_clock);
        stage.ClearDomainEvents();

        stage.DetachMatch(fixture.Id, matchId, _clock);

        stage.Status.Should().Be(StageStatus.Draft);
        stage.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<StageMatchDetached>();
    }

    [Theory]
    [InlineData(StageStatus.Running)]
    [InlineData(StageStatus.Suspended)]
    [InlineData(StageStatus.Completed)]
    public void Fixture_mutations_are_rejected_when_structure_locked(StageStatus locked)
    {
        var stage = CreateCupWithRound(out var round);
        var fixture = stage.AddFixture(round.Id, _clock);
        var matchId = MatchId.New();
        stage.AttachMatch(fixture.Id, matchId, legIndex: 1, _clock);
        stage.Prepare(_clock);
        stage.Start(_clock);
        switch (locked)
        {
            case StageStatus.Suspended:
                stage.Suspend(_clock);
                break;
            case StageStatus.Completed:
                stage.Complete(_clock);
                break;
            case StageStatus.Draft:
            case StageStatus.Ready:
            case StageStatus.Running:
            default:
                break;
        }

        ((Action)(() => stage.AddFixture(round.Id, _clock))).Should().Throw<DomainException>()
            .Which.Code.Should().Be(StageErrorCodes.StructureLocked);
        ((Action)(() => stage.RemoveFixture(fixture.Id, _clock))).Should().Throw<DomainException>()
            .Which.Code.Should().Be(StageErrorCodes.StructureLocked);
        ((Action)(() => stage.AttachMatch(fixture.Id, MatchId.New(), legIndex: 2, _clock))).Should().Throw<DomainException>()
            .Which.Code.Should().Be(StageErrorCodes.StructureLocked);
        ((Action)(() => stage.DetachMatch(fixture.Id, matchId, _clock))).Should().Throw<DomainException>()
            .Which.Code.Should().Be(StageErrorCodes.StructureLocked);
    }

    [Fact]
    public void AttachMatch_two_legs_with_distinct_indexes()
    {
        var stage = CreateCupWithRound(out var round);
        var fixture = stage.AddFixture(round.Id, _clock);
        var first = MatchId.New();
        var second = MatchId.New();

        stage.AttachMatch(fixture.Id, first, legIndex: 1, _clock);
        stage.AttachMatch(fixture.Id, second, legIndex: 2, _clock);

        fixture.MatchIds.Should().Equal(first, second);
        fixture.Attachments.Select(a => a.LegIndex).Should().Equal(1, 2);
    }

    [Fact]
    public void RemoveFixture_with_attached_matches_drops_MatchIds_without_Detach_events()
    {
        var stage = CreateCupWithRound(out var round);
        var fixture = stage.AddFixture(round.Id, _clock);
        var matchId = MatchId.New();
        stage.AttachMatch(fixture.Id, matchId, legIndex: 1, _clock);
        stage.ClearDomainEvents();

        stage.RemoveFixture(fixture.Id, _clock);

        stage.HasMatch(matchId).Should().BeFalse();
        stage.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<StageFixtureRemoved>();
        stage.DomainEvents.Should().NotContain(e => e is StageMatchDetached);
    }

    [Fact]
    public void AddFixture_unknown_matchday_is_rejected()
    {
        var stage = CreateChampionshipWithMatchday(out _);

        var act = () => stage.AddFixture(MatchdayId.New(), _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.MatchdayNotFound);
    }

    [Fact]
    public void AttachMatch_and_DetachMatch_unknown_fixture_are_rejected()
    {
        var stage = CreateCupWithRound(out _);
        var unknown = FixtureId.New();

        ((Action)(() => stage.AttachMatch(unknown, MatchId.New(), legIndex: 1, _clock))).Should().Throw<DomainException>()
            .Which.Code.Should().Be(StageErrorCodes.FixtureNotFound);
        ((Action)(() => stage.DetachMatch(unknown, MatchId.New(), _clock))).Should().Throw<DomainException>()
            .Which.Code.Should().Be(StageErrorCodes.FixtureNotFound);
    }

    [Fact]
    public void RemoveFixture_under_Matchday_raises_StageFixtureRemoved()
    {
        var stage = CreateChampionshipWithMatchday(out var matchday);
        var fixture = stage.AddFixture(matchday.Id, _clock);
        stage.ClearDomainEvents();

        stage.RemoveFixture(fixture.Id, _clock);

        matchday.Fixtures.Should().BeEmpty();
        stage.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<StageFixtureRemoved>();
    }

    private StageAggregate CreateCupWithRound(out Round round)
    {
        var stage = StageAggregate.Create(_competitionId, new StageName("Cup"), SampleRegulations.Standard(), _clock);
        round = stage.AddRound("QF", _clock);
        return stage;
    }

    private StageAggregate CreateChampionshipWithMatchday(out Matchday matchday)
    {
        var stage = StageAggregate.Create(_competitionId, new StageName("League"), SampleRegulations.Standard(), _clock);
        matchday = stage.AddMatchday(1, _clock);
        return stage;
    }
}
