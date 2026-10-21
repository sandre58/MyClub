// -----------------------------------------------------------------------
// <copyright file="MatchRecordedDisciplinaryEventChanged.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;

namespace MyClub.PlayUp.Domain.Matches.Events;

/// <summary>
/// Raised when a disciplinary fact is corrected on a match. Implies no Domain consequence.
/// </summary>
public sealed record MatchRecordedDisciplinaryEventChanged : DomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MatchRecordedDisciplinaryEventChanged"/> class.
    /// </summary>
    public MatchRecordedDisciplinaryEventChanged(
        MatchId matchId,
        DisciplinaryEventId disciplinaryEventId,
        MemberId memberId,
        DisciplinaryType type,
        IClock clock)
        : base(clock)
    {
        MatchId = matchId;
        DisciplinaryEventId = disciplinaryEventId;
        MemberId = memberId;
        Type = type;
    }

    /// <summary>Gets the match identity.</summary>
    public MatchId MatchId { get; }

    /// <summary>Gets the disciplinary event identity.</summary>
    public DisciplinaryEventId DisciplinaryEventId { get; }

    /// <summary>Gets the targeted member identity.</summary>
    public MemberId MemberId { get; }

    /// <summary>Gets the disciplinary type.</summary>
    public DisciplinaryType Type { get; }
}
