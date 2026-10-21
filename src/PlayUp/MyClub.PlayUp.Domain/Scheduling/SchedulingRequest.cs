// -----------------------------------------------------------------------
// <copyright file="SchedulingRequest.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Scheduling;

/// <summary>
/// Flat immutable input for <see cref="ScheduleGenerator"/> (never an aggregate).
/// </summary>
public sealed class SchedulingRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SchedulingRequest"/> class.
    /// </summary>
    public SchedulingRequest(
        Horizon horizon,
        TimeGranularity timeGranularity,
        string timeZoneId,
        IReadOnlyList<MatchSchedulingContext> matches,
        IReadOnlyList<ResourceSchedulingContext> resources,
        Schedule existing,
        IReadOnlyList<MatchId> targetMatchIds,
        MinimumGapBetweenMatches? minimumGapBetweenMatches = null,
        IReadOnlyList<Precedence>? precedences = null,
        IReadOnlyList<SameStart>? sameStarts = null,
        IReadOnlyList<MinimumSeparation>? minimumSeparations = null)
    {
        ArgumentNullException.ThrowIfNull(horizon);
        ArgumentNullException.ThrowIfNull(timeGranularity);
        ArgumentException.ThrowIfNullOrWhiteSpace(timeZoneId);
        ArgumentNullException.ThrowIfNull(matches);
        ArgumentNullException.ThrowIfNull(resources);
        ArgumentNullException.ThrowIfNull(existing);
        ArgumentNullException.ThrowIfNull(targetMatchIds);

        Horizon = horizon;
        TimeGranularity = timeGranularity;
        TimeZoneId = timeZoneId;
        Matches = matches;
        Resources = resources;
        Existing = existing;
        TargetMatchIds = targetMatchIds;
        MinimumGapBetweenMatches = minimumGapBetweenMatches;
        Precedences = precedences ?? [];
        SameStarts = sameStarts ?? [];
        MinimumSeparations = minimumSeparations ?? [];
    }

    /// <summary>
    /// Gets the global horizon.
    /// </summary>
    public Horizon Horizon { get; }

    /// <summary>
    /// Gets the global start granularity.
    /// </summary>
    public TimeGranularity TimeGranularity { get; }

    /// <summary>
    /// Gets the IANA time-zone id (civil-day rules only; not used for the start grid).
    /// </summary>
    public string TimeZoneId { get; }

    /// <summary>
    /// Gets match scheduling contexts.
    /// </summary>
    public IReadOnlyList<MatchSchedulingContext> Matches { get; }

    /// <summary>
    /// Gets resource scheduling contexts (order is the default resource exploration order).
    /// </summary>
    public IReadOnlyList<ResourceSchedulingContext> Resources { get; }

    /// <summary>
    /// Gets the existing schedule (Fixed = Existing ∖ Targets).
    /// </summary>
    public Schedule Existing { get; }

    /// <summary>
    /// Gets target match identities in exploration order.
    /// </summary>
    public IReadOnlyList<MatchId> TargetMatchIds { get; }

    /// <summary>
    /// Gets the optional global minimum gap on the same resource.
    /// </summary>
    public MinimumGapBetweenMatches? MinimumGapBetweenMatches { get; }

    /// <summary>
    /// Gets precedence constraints.
    /// </summary>
    public IReadOnlyList<Precedence> Precedences { get; }

    /// <summary>
    /// Gets same-start constraints.
    /// </summary>
    public IReadOnlyList<SameStart> SameStarts { get; }

    /// <summary>
    /// Gets minimum-separation constraints.
    /// </summary>
    public IReadOnlyList<MinimumSeparation> MinimumSeparations { get; }
}
