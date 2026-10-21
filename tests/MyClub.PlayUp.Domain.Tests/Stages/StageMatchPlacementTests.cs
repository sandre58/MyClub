// -----------------------------------------------------------------------
// <copyright file="StageMatchPlacementTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Domain.Tests.Common;
using Xunit;

namespace MyClub.PlayUp.Domain.Tests.Stages;

public sealed class StageMatchPlacementTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 13, 15, 0, 0, TimeSpan.Zero));
    private readonly CompetitionId _competitionId = CompetitionId.New();
    private readonly DateTimeOffset _t0 = new(2026, 9, 1, 10, 0, 0, TimeSpan.Zero);
    private readonly DateTimeOffset _t1 = new(2026, 9, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void MatchPlacement_construction_exposes_match_start_and_resource()
    {
        var matchId = MatchId.New();
        var resourceId = ResourceId.New();

        var placement = new MatchPlacement(matchId, _t0, resourceId);

        placement.MatchId.Should().Be(matchId);
        placement.Start.Should().Be(_t0);
        placement.ResourceId.Should().Be(resourceId);
    }

    [Fact]
    public void Apply_attached_match_adds_placement()
    {
        var (stage, matchId) = CreateStageWithAttachedMatch();
        var resourceId = ResourceId.New();
        var placement = new MatchPlacement(matchId, _t0, resourceId);

        stage.ApplyMatchPlacements([placement], [matchId]);

        stage.MatchPlacements.Should().ContainSingle()
            .Which.Should().Be(placement);
        stage.TryGetMatchPlacement(matchId, out var found).Should().BeTrue();
        found.Should().Be(placement);
    }

    [Fact]
    public void Apply_unattached_match_is_rejected_without_mutation()
    {
        var (stage, attachedId) = CreateStageWithAttachedMatch();
        var orphanId = MatchId.New();
        var resourceId = ResourceId.New();
        stage.ApplyMatchPlacements(
            [new MatchPlacement(attachedId, _t0, resourceId)],
            [attachedId]);

        var act = () => stage.ApplyMatchPlacements(
            [new MatchPlacement(orphanId, _t1, resourceId)],
            [orphanId]);

        act.Should().Throw<DomainException>()
            .Which.Code.Should().Be(StageErrorCodes.MatchPlacementInvalid);
        stage.MatchPlacements.Should().ContainSingle().Which.MatchId.Should().Be(attachedId);
    }

    [Fact]
    public void Apply_target_replaces_existing_placement()
    {
        var (stage, matchId) = CreateStageWithAttachedMatch();
        var r1 = ResourceId.New();
        var r2 = ResourceId.New();
        stage.ApplyMatchPlacements([new MatchPlacement(matchId, _t0, r1)], [matchId]);

        stage.ApplyMatchPlacements([new MatchPlacement(matchId, _t1, r2)], [matchId]);

        stage.MatchPlacements.Should().ContainSingle()
            .Which.Should().Be(new MatchPlacement(matchId, _t1, r2));
    }

    [Fact]
    public void Apply_duplicate_match_in_placements_is_rejected()
    {
        var (stage, matchId) = CreateStageWithAttachedMatch();
        var resourceId = ResourceId.New();
        var placements = new[]
        {
            new MatchPlacement(matchId, _t0, resourceId),
            new MatchPlacement(matchId, _t1, resourceId)
        };

        var act = () => stage.ApplyMatchPlacements(placements, [matchId]);

        act.Should().Throw<DomainException>()
            .Which.Code.Should().Be(StageErrorCodes.MatchPlacementInvalid);
        stage.MatchPlacements.Should().BeEmpty();
    }

    [Fact]
    public void Apply_duplicate_targets_is_rejected()
    {
        var (stage, matchId) = CreateStageWithAttachedMatch();
        var resourceId = ResourceId.New();

        var act = () => stage.ApplyMatchPlacements(
            [new MatchPlacement(matchId, _t0, resourceId)],
            [matchId, matchId]);

        act.Should().Throw<DomainException>()
            .Which.Code.Should().Be(StageErrorCodes.MatchPlacementInvalid);
    }

    [Fact]
    public void Apply_fixed_identical_is_accepted()
    {
        var (stage, m1, m2) = CreateStageWithTwoAttachedMatches();
        var r1 = ResourceId.New();
        var r2 = ResourceId.New();
        stage.ApplyMatchPlacements(
            [new MatchPlacement(m1, _t0, r1), new MatchPlacement(m2, _t1, r2)],
            [m1, m2]);

        stage.ApplyMatchPlacements(
            [
                new MatchPlacement(m1, _t0, r1),
                new MatchPlacement(m2, _t1.AddHours(1), r2)
            ],
            [m2]);

        stage.TryGetMatchPlacement(m1, out var fixedPlacement).Should().BeTrue();
        fixedPlacement.Should().Be(new MatchPlacement(m1, _t0, r1));
        stage.TryGetMatchPlacement(m2, out var updated).Should().BeTrue();
        updated.Start.Should().Be(_t1.AddHours(1));
    }

    [Fact]
    public void Apply_fixed_divergent_is_rejected_without_mutation()
    {
        var (stage, m1, m2) = CreateStageWithTwoAttachedMatches();
        var r1 = ResourceId.New();
        var r2 = ResourceId.New();
        stage.ApplyMatchPlacements(
            [new MatchPlacement(m1, _t0, r1), new MatchPlacement(m2, _t1, r2)],
            [m1, m2]);

        var act = () => stage.ApplyMatchPlacements(
            [
                new MatchPlacement(m1, _t0.AddMinutes(30), r1),
                new MatchPlacement(m2, _t1.AddHours(1), r2)
            ],
            [m2]);

        act.Should().Throw<DomainException>()
            .Which.Code.Should().Be(StageErrorCodes.MatchPlacementInvalid);
        stage.TryGetMatchPlacement(m2, out var m2Placement).Should().BeTrue();
        m2Placement.Should().Be(new MatchPlacement(m2, _t1, r2));
    }

    [Fact]
    public void Apply_target_missing_from_schedule_is_rejected()
    {
        var (stage, m1, m2) = CreateStageWithTwoAttachedMatches();
        var r1 = ResourceId.New();
        stage.ApplyMatchPlacements([new MatchPlacement(m1, _t0, r1)], [m1]);

        var act = () => stage.ApplyMatchPlacements(
            [new MatchPlacement(m1, _t0, r1)],
            [m2]);

        act.Should().Throw<DomainException>()
            .Which.Code.Should().Be(StageErrorCodes.MatchPlacementInvalid);
        stage.MatchPlacements.Should().ContainSingle().Which.MatchId.Should().Be(m1);
    }

    [Fact]
    public void Apply_atomicity_rejects_second_invalid_without_mutating_first()
    {
        var (stage, m1, m2) = CreateStageWithTwoAttachedMatches();
        var resourceId = ResourceId.New();
        stage.ApplyMatchPlacements([new MatchPlacement(m1, _t0, resourceId)], [m1]);
        var orphan = MatchId.New();

        var act = () => stage.ApplyMatchPlacements(
            [
                new MatchPlacement(m1, _t1, resourceId),
                new MatchPlacement(orphan, _t1, resourceId)
            ],
            [m1, orphan]);

        act.Should().Throw<DomainException>();
        stage.TryGetMatchPlacement(m1, out var m1Placement).Should().BeTrue();
        m1Placement.Should().Be(new MatchPlacement(m1, _t0, resourceId));
        stage.TryGetMatchPlacement(m2, out _).Should().BeFalse();
    }

    [Fact]
    public void Apply_idempotent_rewrites_same_values()
    {
        var (stage, matchId) = CreateStageWithAttachedMatch();
        var resourceId = ResourceId.New();
        var placement = new MatchPlacement(matchId, _t0, resourceId);

        stage.ApplyMatchPlacements([placement], [matchId]);
        stage.ApplyMatchPlacements([placement], [matchId]);

        stage.MatchPlacements.Should().ContainSingle().Which.Should().Be(placement);
    }

    [Fact]
    public void Apply_preserves_placements_outside_targets()
    {
        var (stage, m1, m2) = CreateStageWithTwoAttachedMatches();
        var r1 = ResourceId.New();
        var r2 = ResourceId.New();
        stage.ApplyMatchPlacements(
            [new MatchPlacement(m1, _t0, r1), new MatchPlacement(m2, _t1, r2)],
            [m1, m2]);

        stage.ApplyMatchPlacements(
            [new MatchPlacement(m2, _t1.AddHours(2), r2)],
            [m2]);

        stage.TryGetMatchPlacement(m1, out var kept).Should().BeTrue();
        kept.Should().Be(new MatchPlacement(m1, _t0, r1));
        stage.TryGetMatchPlacement(m2, out var updated).Should().BeTrue();
        updated.Start.Should().Be(_t1.AddHours(2));
    }

    [Fact]
    public void DetachMatch_clears_placement()
    {
        var (stage, matchId) = CreateStageWithAttachedMatch(out var fixtureId);
        var resourceId = ResourceId.New();
        stage.ApplyMatchPlacements([new MatchPlacement(matchId, _t0, resourceId)], [matchId]);

        stage.DetachMatch(fixtureId, matchId, _clock);

        stage.MatchPlacements.Should().BeEmpty();
        stage.HasMatch(matchId).Should().BeFalse();
    }

    [Fact]
    public void RemoveFixture_clears_placements()
    {
        var (stage, matchId) = CreateStageWithAttachedMatch(out var fixtureId);
        var resourceId = ResourceId.New();
        stage.ApplyMatchPlacements([new MatchPlacement(matchId, _t0, resourceId)], [matchId]);

        stage.RemoveFixture(fixtureId, _clock);

        stage.MatchPlacements.Should().BeEmpty();
        stage.HasMatch(matchId).Should().BeFalse();
        stage.HasFixture(fixtureId).Should().BeFalse();
    }

    private (Stage Stage, MatchId MatchId) CreateStageWithAttachedMatch() =>
        CreateStageWithAttachedMatch(out _);

    private (Stage Stage, MatchId MatchId) CreateStageWithAttachedMatch(out FixtureId fixtureId)
    {
        var stage = Stage.Create(_competitionId, new StageName("League"), SampleRegulations.Standard(), _clock);
        var matchday = stage.AddMatchday(1, _clock);
        var fixture = stage.AddFixture(matchday.Id, _clock);
        fixtureId = fixture.Id;
        var matchId = MatchId.New();
        stage.AttachMatch(fixture.Id, matchId, legIndex: 1, _clock);
        return (stage, matchId);
    }

    private (Stage Stage, MatchId First, MatchId Second) CreateStageWithTwoAttachedMatches()
    {
        var stage = Stage.Create(_competitionId, new StageName("League"), SampleRegulations.Standard(), _clock);
        var matchday = stage.AddMatchday(1, _clock);
        var f1 = stage.AddFixture(matchday.Id, _clock);
        var f2 = stage.AddFixture(matchday.Id, _clock);
        var m1 = MatchId.New();
        var m2 = MatchId.New();
        stage.AttachMatch(f1.Id, m1, legIndex: 1, _clock);
        stage.AttachMatch(f2.Id, m2, legIndex: 1, _clock);
        return (stage, m1, m2);
    }
}
