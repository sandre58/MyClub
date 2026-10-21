// -----------------------------------------------------------------------
// <copyright file="CorrectRecordedDisciplinaryEvent.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Rules;

namespace MyClub.PlayUp.Application.Matches;

/// <summary>
/// Application use case: correct a disciplinary fact on the match journal.
/// </summary>
public static class CorrectRecordedDisciplinaryEvent
{
    /// <summary>
    /// Corrects an existing disciplinary event when the new type is authorized.
    /// </summary>
    public static void Execute(
        Match match,
        Competition competition,
        DisciplinaryEventId disciplinaryEventId,
        MemberId memberId,
        DisciplinaryType type,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(match);
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(clock);
        DisciplinaryRulesGate.EnsureAllowed(match, competition, type);
        match.CorrectRecordedDisciplinaryEvent(disciplinaryEventId, memberId, type, clock);
    }
}
