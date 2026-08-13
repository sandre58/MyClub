// -----------------------------------------------------------------------
// <copyright file="MatchPlacement.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Stages;

/// <summary>
/// Materialized calendar placement for an attached match: resource and start instant.
/// Value object keyed naturally by <see cref="MatchId"/> (no PlacementId).
/// </summary>
public sealed record MatchPlacement
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MatchPlacement"/> class.
    /// </summary>
    /// <param name="matchId">Attached match identity.</param>
    /// <param name="start">Occupation start instant.</param>
    /// <param name="resourceId">Assigned scheduling resource (external ownership).</param>
    public MatchPlacement(MatchId matchId, DateTimeOffset start, ResourceId resourceId)
    {
        MatchId = matchId;
        Start = start;
        ResourceId = resourceId;
    }

    /// <summary>
    /// Gets the match identity.
    /// </summary>
    public MatchId MatchId { get; }

    /// <summary>
    /// Gets the occupation start instant.
    /// </summary>
    public DateTimeOffset Start { get; }

    /// <summary>
    /// Gets the assigned resource identity.
    /// </summary>
    public ResourceId ResourceId { get; }
}
