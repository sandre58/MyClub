// -----------------------------------------------------------------------
// <copyright file="RemoveDeclaredMembers.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;

namespace MyClub.PlayUp.Application.Competitions;

/// <summary>
/// Named atomic batch: remove several declared members from one entry. One R4 failure refuses the lot.
/// </summary>
public static class RemoveDeclaredMembers
{
    /// <summary>
    /// Removes every listed member after all R4 gates succeed.
    /// </summary>
    public static void Execute(
        Competition competition,
        EntryId entryId,
        IReadOnlyList<MemberId> memberIds,
        IReadOnlyList<Match> competitionMatches,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(memberIds);
        ArgumentNullException.ThrowIfNull(competitionMatches);
        ArgumentNullException.ThrowIfNull(clock);
        EntryBatch.EnsureNonEmptyDistinct(memberIds);

        var entry = competition.GetEntry(entryId);
        foreach (var memberId in memberIds)
        {
            _ = entry.DeclaredMembers.FirstOrDefault(member => member.Id.Equals(memberId))
                ?? throw new ApplicationFailureException(
                    $"Declared member '{memberId}' was not found on entry '{entryId}'.",
                    ApplicationErrorCodes.DeclaredMemberNotFound);
            RemoveDeclaredMember.EnsureNotReferencedOnMatchSheets(entryId, memberId, competitionMatches);
        }

        foreach (var memberId in memberIds)
        {
            competition.RemoveDeclaredMember(entryId, memberId, clock);
        }
    }
}
