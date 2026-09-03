// -----------------------------------------------------------------------
// <copyright file="RemoveDeclaredMember.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;

namespace MyClub.PlayUp.Application.Competitions;

/// <summary>
/// Application use case: remove a declared member from an entry roster.
/// </summary>
/// <remarks>
/// Enforces R4 (Décision Feuille): refuse removal while the member is still on any match composition sheet.
/// Explicit sequence: remove from sheet(s), then remove from roster.
/// </remarks>
public static class RemoveDeclaredMember
{
    /// <summary>
    /// Removes a declared member when not referenced by any match composition sheet.
    /// </summary>
    /// <param name="competition">Target competition.</param>
    /// <param name="entryId">Entry identity.</param>
    /// <param name="memberId">Member identity within the entry roster.</param>
    /// <param name="competitionMatches">All matches loaded for the competition (cross-stage).</param>
    /// <param name="clock">Clock for domain events.</param>
    public static void Execute(
        Competition competition,
        EntryId entryId,
        MemberId memberId,
        IReadOnlyList<Match> competitionMatches,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(competitionMatches);
        ArgumentNullException.ThrowIfNull(clock);
        EnsureNotReferencedOnMatchSheets(entryId, memberId, competitionMatches);
        competition.RemoveDeclaredMember(entryId, memberId, clock);
    }

    internal static void EnsureNotReferencedOnMatchSheets(
        EntryId entryId,
        MemberId memberId,
        IReadOnlyList<Match> competitionMatches)
    {
        foreach (var match in competitionMatches)
        {
            if (!match.HomeEntryId.Equals(entryId) && !match.AwayEntryId.Equals(entryId))
            {
                continue;
            }

            if (match.HasDeclaredParticipation(memberId))
            {
                throw new ApplicationFailureException(
                    $"Declared member '{memberId}' is still referenced on match '{match.Id}' composition sheet.",
                    ApplicationErrorCodes.DeclaredMemberReferencedByMatchSheet);
            }
        }
    }
}
