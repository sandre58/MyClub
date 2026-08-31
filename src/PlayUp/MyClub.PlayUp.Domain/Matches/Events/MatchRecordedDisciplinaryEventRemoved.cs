// -----------------------------------------------------------------------
// <copyright file="MatchRecordedDisciplinaryEventRemoved.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Matches.Events;

/// <summary>
/// Raised when a disciplinary fact is removed from a match. Implies no Domain consequence.
/// </summary>
public sealed record MatchRecordedDisciplinaryEventRemoved : DomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MatchRecordedDisciplinaryEventRemoved"/> class.
    /// </summary>
    public MatchRecordedDisciplinaryEventRemoved(
        MatchId matchId,
        DisciplinaryEventId disciplinaryEventId,
        IClock clock)
        : base(clock)
    {
        MatchId = matchId;
        DisciplinaryEventId = disciplinaryEventId;
    }

    /// <summary>Gets the match identity.</summary>
    public MatchId MatchId { get; }

    /// <summary>Gets the disciplinary event identity.</summary>
    public DisciplinaryEventId DisciplinaryEventId { get; }
}
