// -----------------------------------------------------------------------
// <copyright file="MatchSchedulingContext.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Scheduling;

/// <summary>
/// Scheduling context for one match (duration, windows, resources, participants).
/// </summary>
public sealed class MatchSchedulingContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MatchSchedulingContext"/> class.
    /// </summary>
    /// <param name="matchId">Match identity.</param>
    /// <param name="duration">Occupation duration.</param>
    /// <param name="allowedStartWindows">
    /// Specific start windows; <see langword="null"/> = no extra restriction; empty = InvalidRequest.
    /// </param>
    /// <param name="allowedResourceIds">
    /// Candidate resources; <see langword="null"/> = all resources in the request; empty = no candidate.
    /// </param>
    /// <param name="imposedStart">Optional imposed start (singleton candidate).</param>
    /// <param name="home">Home participant knowledge.</param>
    /// <param name="away">Away participant knowledge.</param>
    public MatchSchedulingContext(
        MatchId matchId,
        SchedulingDuration duration,
        IReadOnlyList<TimeWindow>? allowedStartWindows = null,
        IReadOnlyList<ResourceId>? allowedResourceIds = null,
        DateTimeOffset? imposedStart = null,
        MatchParticipantRef? home = null,
        MatchParticipantRef? away = null)
    {
        ArgumentNullException.ThrowIfNull(duration);

        if (allowedResourceIds is not null
            && allowedResourceIds.Distinct().Count() != allowedResourceIds.Count)
        {
            throw new DomainException(
                "Allowed resource ids cannot contain duplicates.",
                SchedulingErrorCodes.MatchContextInvalid);
        }

        MatchId = matchId;
        Duration = duration;
        AllowedStartWindows = allowedStartWindows;
        AllowedResourceIds = allowedResourceIds;
        ImposedStart = imposedStart;
        Home = home ?? MatchParticipantRef.UnknownStructural();
        Away = away ?? MatchParticipantRef.UnknownStructural();
    }

    /// <summary>
    /// Gets the match identity.
    /// </summary>
    public MatchId MatchId { get; }

    /// <summary>
    /// Gets the occupation duration.
    /// </summary>
    public SchedulingDuration Duration { get; }

    /// <summary>
    /// Gets allowed start windows, or <see langword="null"/> when unrestricted.
    /// </summary>
    public IReadOnlyList<TimeWindow>? AllowedStartWindows { get; }

    /// <summary>
    /// Gets allowed resource candidates, or <see langword="null"/> when all request resources apply.
    /// </summary>
    public IReadOnlyList<ResourceId>? AllowedResourceIds { get; }

    /// <summary>
    /// Gets an imposed start when set.
    /// </summary>
    public DateTimeOffset? ImposedStart { get; }

    /// <summary>
    /// Gets the home participant reference.
    /// </summary>
    public MatchParticipantRef Home { get; }

    /// <summary>
    /// Gets the away participant reference.
    /// </summary>
    public MatchParticipantRef Away { get; }
}
