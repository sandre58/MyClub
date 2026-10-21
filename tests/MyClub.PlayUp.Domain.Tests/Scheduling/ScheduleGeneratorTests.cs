// -----------------------------------------------------------------------
// <copyright file="ScheduleGeneratorTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Scheduling;
using Xunit;
using static MyClub.PlayUp.Domain.Tests.Scheduling.SchedulingTestHelpers;

namespace MyClub.PlayUp.Domain.Tests.Scheduling;

public sealed class ScheduleGeneratorTests
{
    [Fact]
    public void Generate_single_target_assigns_first_resource_and_start()
    {
        var matchId = MatchId.New();
        var r1 = ResourceId.New();
        var r2 = ResourceId.New();
        var request = Request(
            [Match(matchId)],
            [Resource(r1), Resource(r2)],
            [matchId]);

        var result = ScheduleGenerator.Generate(request);

        result.IsSuccess.Should().BeTrue();
        var assignment = result.Schedule!.Assignments.Should().ContainSingle().Subject;
        assignment.MatchId.Should().Be(matchId);
        assignment.ResourceId.Should().Be(r1);
        assignment.Start.Should().Be(H0);
    }

    [Fact]
    public void Generate_empty_targets_returns_success_with_fixed_only()
    {
        var fixedId = MatchId.New();
        var resource = ResourceId.New();
        var existing = ScheduleOf(Assignment(fixedId, resource, H0));
        var request = Request(
            [Match(fixedId)],
            [Resource(resource)],
            [],
            existing);

        var result = ScheduleGenerator.Generate(request);

        result.IsSuccess.Should().BeTrue();
        result.Schedule!.Assignments.Should().ContainSingle()
            .Which.MatchId.Should().Be(fixedId);
    }

    [Fact]
    public void Validation_horizon_start_equals_end_yields_no_solution_for_target()
    {
        var matchId = MatchId.New();
        var resource = ResourceId.New();
        var request = Request(
            [Match(matchId)],
            [Resource(resource)],
            [matchId],
            horizon: new Horizon(H0, H0));

        var result = ScheduleGenerator.Generate(request);

        result.IsNoSolution.Should().BeTrue();
    }

    [Fact]
    public void Validation_horizon_start_greater_than_end_throws_on_vo_outside_result_trichotomy()
    {
        // A17 domain: H0 > H1 → InvalidRequest. Architecture: Horizon VO rejects construction
        // (DomainException), so Generate never sees an inverted horizon — same pattern as Duration/Granularity.
        var act = () => new Horizon(H0.AddHours(1), H0);

        act.Should().Throw<DomainException>()
            .Which.Code.Should().Be(SchedulingErrorCodes.HorizonInvalid);
    }

    [Fact]
    public void Validation_precedence_successor_dangling_is_invalid()
    {
        var a = MatchId.New();
        var b = MatchId.New();
        var resource = ResourceId.New();
        var request = Request(
            [Match(a), Match(b)],
            [Resource(resource)],
            [a],
            precedences: [new Precedence(a, b, 0)]);

        var result = ScheduleGenerator.Generate(request);

        result.IsInvalidRequest.Should().BeTrue();
        result.Errors.Should().Contain(e =>
            e.Code == SchedulingErrorCodes.InvalidRequest
            && e.Message.Contains("Fixed ∪ Targets", StringComparison.Ordinal));
    }

    [Fact]
    public void Validation_precedence_predecessor_dangling_is_invalid()
    {
        var a = MatchId.New();
        var b = MatchId.New();
        var resource = ResourceId.New();
        var request = Request(
            [Match(a), Match(b)],
            [Resource(resource)],
            [b],
            precedences: [new Precedence(a, b, 0)]);

        var result = ScheduleGenerator.Generate(request);

        result.IsInvalidRequest.Should().BeTrue();
        result.Errors.Should().Contain(e =>
            e.Code == SchedulingErrorCodes.InvalidRequest
            && e.Message.Contains("Fixed ∪ Targets", StringComparison.Ordinal));
    }

    [Fact]
    public void Validation_same_start_dangling_endpoint_is_invalid()
    {
        var a = MatchId.New();
        var b = MatchId.New();
        var resource = ResourceId.New();
        var request = Request(
            [Match(a), Match(b)],
            [Resource(resource)],
            [a],
            sameStarts: [new SameStart(a, b)]);

        var result = ScheduleGenerator.Generate(request);

        result.IsInvalidRequest.Should().BeTrue();
        result.Errors.Should().Contain(e =>
            e.Code == SchedulingErrorCodes.InvalidRequest
            && e.Message.Contains("SameStart", StringComparison.Ordinal)
            && e.Message.Contains("Fixed ∪ Targets", StringComparison.Ordinal));
    }

    [Fact]
    public void Validation_minimum_separation_dangling_endpoint_is_invalid()
    {
        var a = MatchId.New();
        var b = MatchId.New();
        var resource = ResourceId.New();
        var request = Request(
            [Match(a), Match(b)],
            [Resource(resource)],
            [a],
            separations: [new MinimumSeparation(a, b, 0)]);

        var result = ScheduleGenerator.Generate(request);

        result.IsInvalidRequest.Should().BeTrue();
        result.Errors.Should().Contain(e =>
            e.Code == SchedulingErrorCodes.InvalidRequest
            && e.Message.Contains("MinimumSeparation", StringComparison.Ordinal)
            && e.Message.Contains("Fixed ∪ Targets", StringComparison.Ordinal));
    }

    [Fact]
    public void Validation_constraint_endpoint_in_matches_but_neither_target_nor_existing_is_invalid()
    {
        var a = MatchId.New();
        var b = MatchId.New();
        var resource = ResourceId.New();

        // B is in Matches (not ghost) but neither Target nor Existing — classic dangling SameStart partner.
        var request = Request(
            [Match(a), Match(b)],
            [Resource(resource)],
            [a],
            sameStarts: [new SameStart(a, b)]);

        var result = ScheduleGenerator.Generate(request);

        result.IsInvalidRequest.Should().BeTrue();
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Validation_unknown_allowed_resource_is_invalid()
    {
        var matchId = MatchId.New();
        var known = ResourceId.New();
        var unknown = ResourceId.New();
        var request = Request(
            [Match(matchId, allowedResourceIds: [unknown])],
            [Resource(known)],
            [matchId]);

        var result = ScheduleGenerator.Generate(request);

        result.IsInvalidRequest.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == SchedulingErrorCodes.InvalidRequest);
    }

    [Fact]
    public void Validation_empty_allowed_start_windows_is_invalid()
    {
        var matchId = MatchId.New();
        var resource = ResourceId.New();
        var request = Request(
            [Match(matchId, allowedStartWindows: [])],
            [Resource(resource)],
            [matchId]);

        var result = ScheduleGenerator.Generate(request);

        result.IsInvalidRequest.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Message.Contains("AllowedStartWindows", StringComparison.Ordinal));
    }

    [Fact]
    public void Validation_precedence_cycle_is_invalid()
    {
        var a = MatchId.New();
        var b = MatchId.New();
        var resource = ResourceId.New();
        var request = Request(
            [Match(a), Match(b)],
            [Resource(resource)],
            [a, b],
            precedences: [new Precedence(a, b, 0), new Precedence(b, a, 0)]);

        var result = ScheduleGenerator.Generate(request);

        result.IsInvalidRequest.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Message.Contains("cycle", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validation_same_start_fixed_conflict_is_invalid()
    {
        var a = MatchId.New();
        var b = MatchId.New();
        var r1 = ResourceId.New();
        var r2 = ResourceId.New();
        var existing = ScheduleOf(
            Assignment(a, r1, H0),
            Assignment(b, r2, H0.AddMinutes(30)));
        var request = Request(
            [Match(a), Match(b)],
            [Resource(r1), Resource(r2)],
            [],
            existing,
            sameStarts: [new SameStart(a, b)]);

        var result = ScheduleGenerator.Generate(request);

        result.IsInvalidRequest.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Message.Contains("SameStart", StringComparison.Ordinal));
    }

    [Fact]
    public void Validation_missing_expected_participant_is_invalid()
    {
        var matchId = MatchId.New();
        var resource = ResourceId.New();
        var request = Request(
            [Match(matchId, home: MatchParticipantRef.MissingExpected())],
            [Resource(resource)],
            [matchId]);

        var result = ScheduleGenerator.Generate(request);

        result.IsInvalidRequest.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Message.Contains("missing", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Domain_imposed_start_is_singleton_when_feasible()
    {
        var matchId = MatchId.New();
        var resource = ResourceId.New();
        var imposed = H0.AddMinutes(45);
        var request = Request(
            [Match(matchId, durationMinutes: 60, imposedStart: imposed)],
            [Resource(resource)],
            [matchId],
            horizon: HorizonMinutes(180),
            granularity: Granularity());

        var result = ScheduleGenerator.Generate(request);

        result.IsSuccess.Should().BeTrue();
        result.Schedule!.Assignments.Single().Start.Should().Be(imposed);
    }

    [Fact]
    public void Domain_start_equals_horizon_end_impossible()
    {
        var matchId = MatchId.New();
        var resource = ResourceId.New();
        var h1 = H0.AddMinutes(120);
        var request = Request(
            [Match(matchId, durationMinutes: 60, imposedStart: h1)],
            [Resource(resource, availabilityMinutes: 180)],
            [matchId],
            horizon: new Horizon(H0, h1));

        var result = ScheduleGenerator.Generate(request);

        result.IsNoSolution.Should().BeTrue();
    }

    [Fact]
    public void Domain_end_equals_horizon_end_is_allowed()
    {
        var matchId = MatchId.New();
        var resource = ResourceId.New();
        var request = Request(
            [Match(matchId, durationMinutes: 60, imposedStart: H0)],
            [Resource(resource, availabilityMinutes: 60)],
            [matchId],
            horizon: new Horizon(H0, H0.AddMinutes(60)));

        var result = ScheduleGenerator.Generate(request);

        result.IsSuccess.Should().BeTrue();
        result.Schedule!.Assignments.Single().Start.Should().Be(H0);
    }

    [Fact]
    public void Domain_availability_filters_candidates()
    {
        var matchId = MatchId.New();
        var resource = ResourceId.New();
        var late = H0.AddMinutes(90);
        var request = Request(
            [Match(matchId, durationMinutes: 60)],
            [new ResourceSchedulingContext(resource, [Window(late, 60)])],
            [matchId],
            horizon: HorizonMinutes(180),
            granularity: Granularity());

        var result = ScheduleGenerator.Generate(request);

        result.IsSuccess.Should().BeTrue();
        result.Schedule!.Assignments.Single().Start.Should().Be(late);
    }

    [Fact]
    public void Domain_allowed_resources_order_is_respected()
    {
        var matchId = MatchId.New();
        var r1 = ResourceId.New();
        var r2 = ResourceId.New();
        var request = Request(
            [Match(matchId, allowedResourceIds: [r2, r1])],
            [Resource(r1), Resource(r2)],
            [matchId]);

        var result = ScheduleGenerator.Generate(request);

        result.IsSuccess.Should().BeTrue();
        result.Schedule!.Assignments.Single().ResourceId.Should().Be(r2);
    }

    [Fact]
    public void Search_first_resource_blocked_uses_second()
    {
        var fixedId = MatchId.New();
        var targetId = MatchId.New();
        var r1 = ResourceId.New();
        var r2 = ResourceId.New();
        var existing = ScheduleOf(Assignment(fixedId, r1, H0));
        var request = Request(
            [Match(fixedId), Match(targetId)],
            [Resource(r1), Resource(r2)],
            [targetId],
            existing,
            horizon: HorizonMinutes(60));

        var result = ScheduleGenerator.Generate(request);

        result.IsSuccess.Should().BeTrue();
        result.Schedule!.TryGet(targetId, out var assignment).Should().BeTrue();
        assignment.ResourceId.Should().Be(r2);
        assignment.Start.Should().Be(H0);
    }

    [Fact]
    public void Search_first_start_blocked_uses_later_start()
    {
        var fixedId = MatchId.New();
        var targetId = MatchId.New();
        var resource = ResourceId.New();
        var existing = ScheduleOf(Assignment(fixedId, resource, H0));
        var request = Request(
            [Match(fixedId), Match(targetId)],
            [Resource(resource)],
            [targetId],
            existing,
            horizon: HorizonMinutes(180),
            granularity: Granularity(60));

        var result = ScheduleGenerator.Generate(request);

        result.IsSuccess.Should().BeTrue();
        result.Schedule!.TryGet(targetId, out var assignment).Should().BeTrue();
        assignment.Start.Should().Be(H0.AddMinutes(60));
    }

    [Fact]
    public void Search_target_in_existing_is_reassigned()
    {
        var matchId = MatchId.New();
        var r1 = ResourceId.New();
        var r2 = ResourceId.New();
        var existing = ScheduleOf(Assignment(matchId, r1, H0));
        var request = Request(
            [Match(matchId, allowedResourceIds: [r2])],
            [Resource(r1), Resource(r2)],
            [matchId],
            existing);

        var result = ScheduleGenerator.Generate(request);

        result.IsSuccess.Should().BeTrue();
        result.Schedule!.Assignments.Should().ContainSingle()
            .Which.ResourceId.Should().Be(r2);
    }

    [Fact]
    public void Search_last_candidate_succeeds_after_backtrack()
    {
        var a = MatchId.New();
        var b = MatchId.New();
        var resource = ResourceId.New();
        var request = Request(
            [
                Match(a, durationMinutes: 60, allowedResourceIds: [resource]),
                Match(b, durationMinutes: 60, allowedResourceIds: [resource])
            ],
            [Resource(resource, availabilityMinutes: 120)],
            [a, b],
            horizon: HorizonMinutes(120),
            granularity: Granularity(60));

        var result = ScheduleGenerator.Generate(request);

        result.IsSuccess.Should().BeTrue();
        result.Schedule!.TryGet(a, out var aa).Should().BeTrue();
        result.Schedule.TryGet(b, out var bb).Should().BeTrue();
        aa.Start.Should().Be(H0);
        bb.Start.Should().Be(H0.AddMinutes(60));
    }

    [Fact]
    public void SameStart_forces_identical_starts_on_distinct_resources()
    {
        var a = MatchId.New();
        var b = MatchId.New();
        var r1 = ResourceId.New();
        var r2 = ResourceId.New();
        var request = Request(
            [Match(a), Match(b)],
            [Resource(r1), Resource(r2)],
            [a, b],
            horizon: HorizonMinutes(120),
            granularity: Granularity(),
            sameStarts: [new SameStart(a, b)]);

        var result = ScheduleGenerator.Generate(request);

        result.IsSuccess.Should().BeTrue();
        result.Schedule!.TryGet(a, out var aa).Should().BeTrue();
        result.Schedule.TryGet(b, out var bb).Should().BeTrue();
        aa.Start.Should().Be(bb.Start);
        aa.ResourceId.Should().NotBe(bb.ResourceId);
    }

    [Fact]
    public void SameStart_impossible_when_only_one_resource()
    {
        var a = MatchId.New();
        var b = MatchId.New();
        var resource = ResourceId.New();
        var request = Request(
            [Match(a), Match(b)],
            [Resource(resource)],
            [a, b],
            horizon: HorizonMinutes(120),
            sameStarts: [new SameStart(a, b)]);

        var result = ScheduleGenerator.Generate(request);

        result.IsNoSolution.Should().BeTrue();
    }

    [Fact]
    public void Precedence_enforces_successor_after_predecessor_plus_delay()
    {
        var pred = MatchId.New();
        var succ = MatchId.New();
        var r1 = ResourceId.New();
        var r2 = ResourceId.New();
        var request = Request(
            [Match(pred), Match(succ)],
            [Resource(r1), Resource(r2)],
            [pred, succ],
            horizon: HorizonMinutes(180),
            granularity: Granularity(),
            precedences: [new Precedence(pred, succ, 30)]);

        var result = ScheduleGenerator.Generate(request);

        result.IsSuccess.Should().BeTrue();
        result.Schedule!.TryGet(pred, out var p).Should().BeTrue();
        result.Schedule.TryGet(succ, out var s).Should().BeTrue();
        s.Start.Should().BeOnOrAfter(p.Start.AddMinutes(60 + 30));
    }

    [Fact]
    public void Gap_on_same_resource_blocks_adjacent_when_positive()
    {
        var a = MatchId.New();
        var b = MatchId.New();
        var resource = ResourceId.New();
        var request = Request(
            [Match(a), Match(b)],
            [Resource(resource, availabilityMinutes: 150)],
            [a, b],
            horizon: HorizonMinutes(150),
            granularity: Granularity(),
            gap: new MinimumGapBetweenMatches(30));

        var result = ScheduleGenerator.Generate(request);

        result.IsSuccess.Should().BeTrue();
        result.Schedule!.TryGet(a, out var aa).Should().BeTrue();
        result.Schedule.TryGet(b, out var bb).Should().BeTrue();
        var earlier = aa.Start <= bb.Start ? aa : bb;
        var later = aa.Start <= bb.Start ? bb : aa;
        later.Start.Should().BeOnOrAfter(earlier.Start.AddMinutes(90));
    }

    [Fact]
    public void MinimumSeparation_blocks_overlap_even_on_distinct_resources()
    {
        var a = MatchId.New();
        var b = MatchId.New();
        var r1 = ResourceId.New();
        var r2 = ResourceId.New();
        var request = Request(
            [Match(a), Match(b)],
            [Resource(r1), Resource(r2)],
            [a, b],
            horizon: HorizonMinutes(120),
            granularity: Granularity(60),
            separations: [new MinimumSeparation(a, b, 0)]);

        var result = ScheduleGenerator.Generate(request);

        result.IsSuccess.Should().BeTrue();
        result.Schedule!.TryGet(a, out var aa).Should().BeTrue();
        result.Schedule.TryGet(b, out var bb).Should().BeTrue();
        (aa.Start + TimeSpan.FromMinutes(60) <= bb.Start || bb.Start + TimeSpan.FromMinutes(60) <= aa.Start)
            .Should().BeTrue();
    }

    [Fact]
    public void Participant_same_entry_without_constraint_allows_overlap()
    {
        var entry = EntryId.New();
        var a = MatchId.New();
        var b = MatchId.New();
        var r1 = ResourceId.New();
        var r2 = ResourceId.New();
        var request = Request(
            [
                Match(a, home: MatchParticipantRef.Known(entry)),
                Match(b, home: MatchParticipantRef.Known(entry))
            ],
            [Resource(r1), Resource(r2)],
            [a, b],
            horizon: HorizonMinutes(60));

        var result = ScheduleGenerator.Generate(request);

        result.IsSuccess.Should().BeTrue();
        result.Schedule!.TryGet(a, out var aa).Should().BeTrue();
        result.Schedule.TryGet(b, out var bb).Should().BeTrue();
        aa.Start.Should().Be(bb.Start);
    }

    [Fact]
    public void Participant_known_with_explicit_separation_is_enforced()
    {
        var entry = EntryId.New();
        var a = MatchId.New();
        var b = MatchId.New();
        var r1 = ResourceId.New();
        var r2 = ResourceId.New();
        var request = Request(
            [
                Match(a, home: MatchParticipantRef.Known(entry)),
                Match(b, home: MatchParticipantRef.Known(entry))
            ],
            [Resource(r1), Resource(r2)],
            [a, b],
            horizon: HorizonMinutes(180),
            granularity: Granularity(),
            separations: [new MinimumSeparation(a, b, 30)]);

        var result = ScheduleGenerator.Generate(request);

        result.IsSuccess.Should().BeTrue();
        result.Schedule!.TryGet(a, out var aa).Should().BeTrue();
        result.Schedule.TryGet(b, out var bb).Should().BeTrue();
        var earlier = aa.Start <= bb.Start ? aa : bb;
        var later = aa.Start <= bb.Start ? bb : aa;
        later.Start.Should().BeOnOrAfter(earlier.Start.AddMinutes(90));
    }

    [Fact]
    public void Participant_unknown_structural_does_not_invalidate()
    {
        var matchId = MatchId.New();
        var resource = ResourceId.New();
        var request = Request(
            [Match(matchId, home: MatchParticipantRef.UnknownStructural())],
            [Resource(resource)],
            [matchId]);

        var result = ScheduleGenerator.Generate(request);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Determinism_double_generate_yields_identical_schedule()
    {
        var a = MatchId.New();
        var b = MatchId.New();
        var r1 = ResourceId.New();
        var r2 = ResourceId.New();
        var request = Request(
            [Match(a), Match(b)],
            [Resource(r1), Resource(r2)],
            [a, b],
            horizon: HorizonMinutes(120),
            granularity: Granularity(),
            gap: new MinimumGapBetweenMatches(0),
            sameStarts: [new SameStart(a, b)]);

        var first = ScheduleGenerator.Generate(request);
        var second = ScheduleGenerator.Generate(request);

        first.IsSuccess.Should().BeTrue();
        second.IsSuccess.Should().BeTrue();
        second.Schedule!.Assignments.Should().BeEquivalentTo(first.Schedule!.Assignments);
    }

    [Fact]
    public void Immutability_existing_and_request_are_unchanged()
    {
        var matchId = MatchId.New();
        var resource = ResourceId.New();
        var existingAssignment = Assignment(matchId, resource, H0);
        var existing = ScheduleOf(existingAssignment);
        var request = Request(
            [Match(matchId)],
            [Resource(resource)],
            [matchId],
            existing);

        _ = ScheduleGenerator.Generate(request);

        request.Existing.Assignments.Should().ContainSingle()
            .Which.Should().Be(existingAssignment);
        request.TargetMatchIds.Should().Equal(matchId);
    }

    [Fact]
    public void SameStart_first_start_impossible_second_succeeds_after_undo()
    {
        // Fixed blocks R2 at H0; A@R1/H0 binds SameStart → B has no free resource at H0;
        // Undo then A@R1/H0+60 → B@R2/H0+60 succeeds.
        var a = MatchId.New();
        var b = MatchId.New();
        var blocker = MatchId.New();
        var r1 = ResourceId.New();
        var r2 = ResourceId.New();
        var existing = ScheduleOf(Assignment(blocker, r2, H0));
        var request = Request(
            [Match(a), Match(b), Match(blocker)],
            [Resource(r1, availabilityMinutes: 120), Resource(r2, availabilityMinutes: 120)],
            [a, b],
            existing,
            horizon: HorizonMinutes(120),
            granularity: Granularity(60),
            sameStarts: [new SameStart(a, b)]);

        var result = ScheduleGenerator.Generate(request);

        result.IsSuccess.Should().BeTrue();
        result.Schedule!.TryGet(a, out var aa).Should().BeTrue();
        result.Schedule.TryGet(b, out var bb).Should().BeTrue();
        aa.Start.Should().Be(H0.AddMinutes(60));
        bb.Start.Should().Be(H0.AddMinutes(60));
        aa.ResourceId.Should().Be(r1);
        bb.ResourceId.Should().Be(r2);
    }

    [Fact]
    public void MinimumSeparation_exact_15_minutes_is_satisfied()
    {
        var a = MatchId.New();
        var b = MatchId.New();
        var r1 = ResourceId.New();
        var r2 = ResourceId.New();
        var request = Request(
            [
                Match(a, durationMinutes: 60, imposedStart: H0),
                Match(b, durationMinutes: 60, imposedStart: H0.AddMinutes(75))
            ],
            [Resource(r1), Resource(r2)],
            [a, b],
            horizon: HorizonMinutes(180),
            separations: [new MinimumSeparation(a, b, 15)]);

        var result = ScheduleGenerator.Generate(request);

        result.IsSuccess.Should().BeTrue();

        // A.End + 15 == B.Start (interval semantics, not |ΔStart|)
        result.Schedule!.TryGet(a, out var aa).Should().BeTrue();
        result.Schedule.TryGet(b, out var bb).Should().BeTrue();
        bb.Start.Should().Be(aa.Start.AddMinutes(75));
    }

    [Fact]
    public void MinimumSeparation_14_minutes_is_violated_yielding_no_solution()
    {
        var a = MatchId.New();
        var b = MatchId.New();
        var r1 = ResourceId.New();
        var r2 = ResourceId.New();
        var request = Request(
            [
                Match(a, durationMinutes: 60, imposedStart: H0),
                Match(b, durationMinutes: 60, imposedStart: H0.AddMinutes(74))
            ],
            [Resource(r1), Resource(r2)],
            [a, b],
            horizon: HorizonMinutes(180),
            separations: [new MinimumSeparation(a, b, 15)]);

        var result = ScheduleGenerator.Generate(request);

        result.IsNoSolution.Should().BeTrue();
    }

    [Fact]
    public void Search_only_last_resource_is_valid()
    {
        var target = MatchId.New();
        var f1 = MatchId.New();
        var f2 = MatchId.New();
        var r1 = ResourceId.New();
        var r2 = ResourceId.New();
        var r3 = ResourceId.New();
        var existing = ScheduleOf(
            Assignment(f1, r1, H0),
            Assignment(f2, r2, H0));
        var request = Request(
            [Match(target), Match(f1), Match(f2)],
            [Resource(r1), Resource(r2), Resource(r3)],
            [target],
            existing,
            horizon: HorizonMinutes(60));

        var result = ScheduleGenerator.Generate(request);

        result.IsSuccess.Should().BeTrue();
        result.Schedule!.TryGet(target, out var assignment).Should().BeTrue();
        assignment.ResourceId.Should().Be(r3);
        assignment.Start.Should().Be(H0);
    }

    [Fact]
    public void Boundary_scheduling_duration_is_distinct_from_match_duration_type()
    {
        var scheduling = new SchedulingDuration(90);
        scheduling.Minutes.Should().Be(90);
        scheduling.GetType().Name.Should().Be("SchedulingDuration");
        typeof(SchedulingDuration).Assembly.Should().BeSameAs(typeof(ScheduleGenerator).Assembly);
    }
}
