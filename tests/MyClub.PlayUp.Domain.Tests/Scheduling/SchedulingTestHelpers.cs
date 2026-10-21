// -----------------------------------------------------------------------
// <copyright file="SchedulingTestHelpers.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Scheduling;

namespace MyClub.PlayUp.Domain.Tests.Scheduling;

internal static class SchedulingTestHelpers
{
    internal static readonly DateTimeOffset H0 = new(2026, 8, 13, 10, 0, 0, TimeSpan.Zero);

    internal static Horizon HorizonMinutes(int minutes) =>
        new(H0, H0.AddMinutes(minutes));

    internal static TimeGranularity Granularity(int minutes = 30) => new(minutes);

    internal static SchedulingDuration Duration(int minutes = 60) => new(minutes);

    internal static TimeWindow Window(DateTimeOffset start, int minutes) =>
        new(start, start.AddMinutes(minutes));

    internal static ResourceSchedulingContext Resource(
        ResourceId id,
        DateTimeOffset? availabilityStart = null,
        int availabilityMinutes = 240) =>
        new(id, [Window(availabilityStart ?? H0, availabilityMinutes)]);

    internal static MatchSchedulingContext Match(
        MatchId id,
        int durationMinutes = 60,
        IReadOnlyList<TimeWindow>? allowedStartWindows = null,
        IReadOnlyList<ResourceId>? allowedResourceIds = null,
        DateTimeOffset? imposedStart = null,
        MatchParticipantRef? home = null,
        MatchParticipantRef? away = null) =>
        new(
            id,
            Duration(durationMinutes),
            allowedStartWindows,
            allowedResourceIds,
            imposedStart,
            home,
            away);

    internal static SchedulingRequest Request(
        IReadOnlyList<MatchSchedulingContext> matches,
        IReadOnlyList<ResourceSchedulingContext> resources,
        IReadOnlyList<MatchId> targets,
        Schedule? existing = null,
        Horizon? horizon = null,
        TimeGranularity? granularity = null,
        MinimumGapBetweenMatches? gap = null,
        IReadOnlyList<Precedence>? precedences = null,
        IReadOnlyList<SameStart>? sameStarts = null,
        IReadOnlyList<MinimumSeparation>? separations = null) =>
        new(
            horizon ?? HorizonMinutes(240),
            granularity ?? Granularity(),
            "UTC",
            matches,
            resources,
            existing ?? Schedule.Empty,
            targets,
            gap,
            precedences,
            sameStarts,
            separations);

    internal static ScheduleAssignment Assignment(MatchId matchId, ResourceId resourceId, DateTimeOffset start) =>
        new(matchId, resourceId, start);

    internal static Schedule ScheduleOf(params ScheduleAssignment[] assignments) => new(assignments);
}
