// -----------------------------------------------------------------------
// <copyright file="ResourceSchedulingContext.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Scheduling;

/// <summary>
/// Scheduling context for one resource (availability windows only).
/// </summary>
public sealed class ResourceSchedulingContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceSchedulingContext"/> class.
    /// </summary>
    /// <param name="resourceId">Resource identity.</param>
    /// <param name="availabilityWindows">
    /// Allowed availability windows (never <see langword="null"/>; empty = never available).
    /// Must be strictly disjoint and non-adjacent.
    /// </param>
    public ResourceSchedulingContext(ResourceId resourceId, IReadOnlyList<TimeWindow> availabilityWindows)
    {
        ArgumentNullException.ThrowIfNull(availabilityWindows);

        ResourceId = resourceId;
        AvailabilityWindows = availabilityWindows;
    }

    /// <summary>
    /// Gets the resource identity.
    /// </summary>
    public ResourceId ResourceId { get; }

    /// <summary>
    /// Gets availability windows (never <see langword="null"/>).
    /// </summary>
    public IReadOnlyList<TimeWindow> AvailabilityWindows { get; }
}
