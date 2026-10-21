// -----------------------------------------------------------------------
// <copyright file="MatchRecordedSubstitutionChanged.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Matches.Events;

/// <summary>
/// Raised when a substitution fact is corrected on a match.
/// </summary>
public sealed record MatchRecordedSubstitutionChanged : DomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MatchRecordedSubstitutionChanged"/> class.
    /// </summary>
    public MatchRecordedSubstitutionChanged(
        MatchId matchId,
        SubstitutionId substitutionId,
        Side side,
        MemberId outMemberId,
        MemberId inMemberId,
        IClock clock)
        : base(clock)
    {
        MatchId = matchId;
        SubstitutionId = substitutionId;
        Side = side;
        OutMemberId = outMemberId;
        InMemberId = inMemberId;
    }

    /// <summary>Gets the match identity.</summary>
    public MatchId MatchId { get; }

    /// <summary>Gets the substitution identity.</summary>
    public SubstitutionId SubstitutionId { get; }

    /// <summary>Gets the match side.</summary>
    public Side Side { get; }

    /// <summary>Gets the member leaving the field.</summary>
    public MemberId OutMemberId { get; }

    /// <summary>Gets the member entering the field.</summary>
    public MemberId InMemberId { get; }
}
