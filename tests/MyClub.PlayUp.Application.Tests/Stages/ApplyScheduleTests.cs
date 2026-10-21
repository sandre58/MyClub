// -----------------------------------------------------------------------
// <copyright file="ApplyScheduleTests.cs" company="Stéphane ANDRE">
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

public sealed class ApplyScheduleTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 13, 16, 0, 0, TimeSpan.Zero));
    private readonly CompetitionId _competitionId = CompetitionId.New();
    private readonly DateTimeOffset _t0 = new(2026, 9, 1, 10, 0, 0, TimeSpan.Zero);
    private readonly DateTimeOffset _t1 = new(2026, 9, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Execute_success_schedule_upserts_targets_on_stage()
    {
        var (stage, m1, m2) = CreateStageWithTwoMatches();
        var r1 = ResourceId.New();
        var r2 = ResourceId.New();
        stage.ApplyMatchPlacements([new MatchPlacement(m1, _t0, r1)], [m1]);

        var result = SchedulingResult.Success(
            new Schedule(
            [
                new ScheduleAssignment(m1, r1, _t0),
                new ScheduleAssignment(m2, r2, _t1)
            ]));

        ApplySchedule.Execute(stage, result, [m2]);

        stage.TryGetMatchPlacement(m1, out var fixedPlacement).Should().BeTrue();
        fixedPlacement.Should().Be(new MatchPlacement(m1, _t0, r1));
        stage.TryGetMatchPlacement(m2, out var added).Should().BeTrue();
        added.Should().Be(new MatchPlacement(m2, _t1, r2));
    }

    [Fact]
    public void Execute_no_solution_is_rejected_without_mutation()
    {
        var (stage, matchId) = CreateStageWithOneMatch();

        var act = () => ApplySchedule.Execute(stage, SchedulingResult.NoSolution(), [matchId]);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.ScheduleApplyFailure);
        stage.MatchPlacements.Should().BeEmpty();
    }

    [Fact]
    public void Execute_invalid_request_is_rejected_without_mutation()
    {
        var (stage, matchId) = CreateStageWithOneMatch();
        var invalid = SchedulingResult.InvalidRequest(
        [
            new SchedulingValidationError(SchedulingErrorCodes.InvalidRequest, "test")
        ]);

        var act = () => ApplySchedule.Execute(stage, invalid, [matchId]);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.ScheduleApplyFailure);
        stage.MatchPlacements.Should().BeEmpty();
    }

    [Fact]
    public void Execute_propagates_domain_failure()
    {
        var (stage, m1) = CreateStageWithOneMatch();
        var orphan = MatchId.New();
        var resource = ResourceId.New();
        var result = SchedulingResult.Success(
            new Schedule([new ScheduleAssignment(orphan, resource, _t0)]));

        var act = () => ApplySchedule.Execute(stage, result, [orphan]);

        act.Should().Throw<DomainException>()
            .Which.Code.Should().Be(StageErrorCodes.MatchPlacementInvalid);
        stage.MatchPlacements.Should().BeEmpty();
        _ = m1;
    }

    [Fact]
    public void Execute_end_to_end_generate_then_apply()
    {
        var (stage, matchId) = CreateStageWithOneMatch();
        var resource = ResourceId.New();
        var horizon = new Horizon(_t0, _t0.AddHours(8));
        var granularity = new TimeGranularity(60);
        var matches = new[]
        {
            new MatchSchedulingContext(
                matchId,
                new SchedulingDuration(90),
                home: MatchParticipantRef.Known(EntryId.New()),
                away: MatchParticipantRef.Known(EntryId.New()))
        };
        var resources = new[]
        {
            new ResourceSchedulingContext(resource, [new TimeWindow(_t0, _t0.AddHours(8))])
        };

        var result = GenerateSchedule.Execute(
            stage,
            [matchId],
            horizon,
            granularity,
            "Europe/Paris",
            matches,
            resources);

        result.IsSuccess.Should().BeTrue();
        ApplySchedule.Execute(stage, result, [matchId]);

        stage.MatchPlacements.Should().ContainSingle()
            .Which.MatchId.Should().Be(matchId);
        stage.MatchPlacements.Single().ResourceId.Should().Be(resource);
    }

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
