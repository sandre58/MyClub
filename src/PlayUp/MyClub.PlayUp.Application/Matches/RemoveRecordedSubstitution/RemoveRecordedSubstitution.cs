// -----------------------------------------------------------------------
// <copyright file="RemoveRecordedSubstitution.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;

namespace MyClub.PlayUp.Application.Matches;

/// <summary>
/// Application use case: remove an ordered substitution fact.
/// </summary>
public static class RemoveRecordedSubstitution
{
    /// <summary>
    /// Removes a recorded substitution from the match journal.
    /// </summary>
    public static void Execute(Match match, SubstitutionId substitutionId, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(match);
        ArgumentNullException.ThrowIfNull(clock);
        match.RemoveRecordedSubstitution(substitutionId, clock);
    }
}
