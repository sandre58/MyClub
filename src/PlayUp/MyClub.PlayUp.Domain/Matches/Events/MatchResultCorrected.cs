// -----------------------------------------------------------------------
// <copyright file="MatchResultCorrected.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Matches.Events;

/// <summary>
/// Raised when a finished match's official result is replaced (effective mutation only).
/// Does not imply any change to <see cref="RunningScore"/> or recorded goals.
/// </summary>
public sealed record MatchResultCorrected : DomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MatchResultCorrected"/> class.
    /// </summary>
    public MatchResultCorrected(MatchId matchId, MatchResult result, IClock clock)
        : base(clock)
    {
        MatchId = matchId;
        Result = result;
    }

    /// <summary>Gets the match identity.</summary>
    public MatchId MatchId { get; }

    /// <summary>Gets the corrected official result.</summary>
    public MatchResult Result { get; }
}
