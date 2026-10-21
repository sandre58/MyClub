// -----------------------------------------------------------------------
// <copyright file="RemoveRecordedDisciplinaryEvent.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;

namespace MyClub.PlayUp.Application.Matches;

/// <summary>
/// Application use case: remove a disciplinary fact from the match journal.
/// </summary>
public static class RemoveRecordedDisciplinaryEvent
{
    /// <summary>
    /// Removes a recorded disciplinary event.
    /// </summary>
    public static void Execute(Match match, DisciplinaryEventId disciplinaryEventId, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(match);
        ArgumentNullException.ThrowIfNull(clock);
        match.RemoveRecordedDisciplinaryEvent(disciplinaryEventId, clock);
    }
}
