// -----------------------------------------------------------------------
// <copyright file="GenerateScheduleTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Scheduling;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Stages;

public sealed class GenerateScheduleTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 13, 16, 0, 0, TimeSpan.Zero));
    private readonly CompetitionId _competitionId = CompetitionId.New();
    private readonly DateTimeOffset _h0 = new(2026, 9, 1, 10, 0, 0, TimeSpan.Zero);
    private readonly DateTimeOffset _h1 = new(2026, 9, 1, 18, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Execute_success_builds_existing_from_stage_placements_and_does_not_mutate()
    {
        var (stage, m1, m2) = CreateStageWithTwoMatches();
        var r1 = ResourceId.New();
        var r2 = ResourceId.New();
        stage.ApplyMatchPlacements([new MatchPlacement(m1, _h0, r1)], [m1]);
        var placementsBefore = stage.MatchPlacements.ToArray();

        var result = GenerateSchedule.Execute(
            stage,
            [m2],
            new Horizon(_h0, _h1),
            new TimeGranularity(60),
            "Europe/Paris",
            [MatchContext(m1), MatchContext(m2)],
            [ResourceContext(r1), ResourceContext(r2)]);

        result.IsSuccess.Should().BeTrue();
        result.Schedule!.Assignments.Should().Contain(a => a.MatchId.Equals(m1) && a.Start == _h0 && a.ResourceId.Equals(r1));
        result.Schedule.Assignments.Should().Contain(a => a.MatchId.Equals(m2));
        stage.MatchPlacements.Should().BeEquivalentTo(placementsBefore);
    }

    [Fact]
    public void Execute_propagates_no_solution()
    {
        var (stage, matchId) = CreateStageWithOneMatch();
        var resource = ResourceId.New();

        // Empty availability → no candidates → NoSolution.
        var result = GenerateSchedule.Execute(
            stage,
            [matchId],
            new Horizon(_h0, _h1),
            new TimeGranularity(60),
            "Europe/Paris",
            [MatchContext(matchId)],
            [new ResourceSchedulingContext(resource, [])]);

        result.IsNoSolution.Should().BeTrue();
        stage.MatchPlacements.Should().BeEmpty();
    }

    [Fact]
    public void Execute_propagates_invalid_request()
    {
        var (stage, matchId) = CreateStageWithOneMatch();
        var resource = ResourceId.New();

        var result = GenerateSchedule.Execute(
            stage,
            [matchId],
            new Horizon(_h0, _h1),
            new TimeGranularity(60),
            "Europe/Paris",
            [MatchContext(matchId, allowedStartWindows: [])],
            [ResourceContext(resource)]);

        result.IsInvalidRequest.Should().BeTrue();
        stage.MatchPlacements.Should().BeEmpty();
    }

    [Fact]
    public void Execute_unattached_target_throws_application_failure()
    {
        var (stage, _) = CreateStageWithOneMatch();
        var orphan = MatchId.New();
        var resource = ResourceId.New();

        var act = () => GenerateSchedule.Execute(
            stage,
            [orphan],
            new Horizon(_h0, _h1),
            new TimeGranularity(60),
            "Europe/Paris",
            [MatchContext(orphan)],
            [ResourceContext(resource)]);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.ScheduleGenerationFailure);
    }

    [Fact]
    public void Execute_transmits_targets_in_request_order_via_success_schedule()
    {
        var (stage, m1, m2) = CreateStageWithTwoMatches();
        var r1 = ResourceId.New();
        var r2 = ResourceId.New();

        var result = GenerateSchedule.Execute(
            stage,
            [m2, m1],
            new Horizon(_h0, _h1),
            new TimeGranularity(60),
            "Europe/Paris",
            [MatchContext(m1), MatchContext(m2)],
            [ResourceContext(r1), ResourceContext(r2)]);

        result.IsSuccess.Should().BeTrue();
        result.Schedule!.Assignments.Select(a => a.MatchId).Should().BeEquivalentTo([m1, m2]);
    }

    private static MatchSchedulingContext MatchContext(
        MatchId matchId,
        IReadOnlyList<TimeWindow>? allowedStartWindows = null) =>
        new(
            matchId,
            new SchedulingDuration(90),
            allowedStartWindows,
            allowedResourceIds: null,
            imposedStart: null,
            home: MatchParticipantRef.Known(EntryId.New()),
            away: MatchParticipantRef.Known(EntryId.New()));

    private static ResourceSchedulingContext ResourceContext(ResourceId resourceId) =>
        new(
            resourceId,
            [new TimeWindow(new DateTimeOffset(2026, 9, 1, 10, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 9, 1, 18, 0, 0, TimeSpan.Zero))]);

    private (Stage Stage, MatchId MatchId) CreateStageWithOneMatch()
    {
        var stage = Stage.Create(_competitionId, new StageName("League"), SampleRegulations.Standard(), _clock);
        var matchday = stage.AddMatchday(1, _clock);
        var fixture = stage.AddFixture(matchday.Id, _clock);
        var matchId = MatchId.New();
        stage.AttachMatch(fixture.Id, matchId, legIndex: 1, _clock);
        return (stage, matchId);
    }

    private (Stage Stage, MatchId First, MatchId Second) CreateStageWithTwoMatches()
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
