// -----------------------------------------------------------------------
// <copyright file="MatchRecordedSubstitutionRemoved.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Matches.Events;

/// <summary>
/// Raised when a substitution fact is removed from a match.
/// </summary>
public sealed record MatchRecordedSubstitutionRemoved : DomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MatchRecordedSubstitutionRemoved"/> class.
    /// </summary>
    public MatchRecordedSubstitutionRemoved(MatchId matchId, SubstitutionId substitutionId, IClock clock)
        : base(clock)
    {
        MatchId = matchId;
        SubstitutionId = substitutionId;
    }

    /// <summary>Gets the match identity.</summary>
    public MatchId MatchId { get; }

    /// <summary>Gets the substitution identity.</summary>
    public SubstitutionId SubstitutionId { get; }
}
