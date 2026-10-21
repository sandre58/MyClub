// -----------------------------------------------------------------------
// <copyright file="MatchRecordedSubstitutionAdded.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Matches.Events;

/// <summary>
/// Raised when a substitution fact is recorded on a match.
/// Does not imply any change to declared composition or scores.
/// </summary>
public sealed record MatchRecordedSubstitutionAdded : DomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MatchRecordedSubstitutionAdded"/> class.
    /// </summary>
    public MatchRecordedSubstitutionAdded(
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
