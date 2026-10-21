// -----------------------------------------------------------------------
// <copyright file="RecordDisciplinaryEvent.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Rules;

namespace MyClub.PlayUp.Application.Matches;

/// <summary>
/// Application use case: record a disciplinary fact on the match journal.
/// </summary>
public static class RecordDisciplinaryEvent
{
    /// <summary>
    /// Records a disciplinary event when authorized by competition rules.
    /// </summary>
    public static RecordedDisciplinaryEvent Execute(
        Match match,
        Competition competition,
        MemberId memberId,
        DisciplinaryType type,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(match);
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(clock);
        DisciplinaryRulesGate.EnsureAllowed(match, competition, type);
        return match.RecordDisciplinaryEvent(memberId, type, clock);
    }
}
