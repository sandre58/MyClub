// -----------------------------------------------------------------------
// <copyright file="MatchSheetEligibility.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;

namespace MyClub.PlayUp.Application.Matches;

/// <summary>
/// Application orchestration rules for match composition eligibility (C1 / players only).
/// </summary>
internal static class MatchSheetEligibility
{
    /// <summary>
    /// Ensures the member may be added to the match sheet on the given side.
    /// </summary>
    public static void EnsurePlayerEligible(
        Match match,
        Competition competition,
        MemberId memberId,
        Side side)
    {
        ArgumentNullException.ThrowIfNull(match);
        ArgumentNullException.ThrowIfNull(competition);

        if (!match.CompetitionId.Equals(competition.Id))
        {
            throw new ApplicationFailureException(
                $"Match '{match.Id}' does not belong to competition '{competition.Id}'.",
                ApplicationErrorCodes.MatchNotInCompetition);
        }

        var entryId = ResolveEntryId(match, side);
        var entry = competition.GetEntry(entryId);
        if (entry.Status != EntryStatus.Active)
        {
            throw new ApplicationFailureException(
                $"Entry '{entryId}' is not active for match sheet selection.",
                ApplicationErrorCodes.ParticipationNotEligible);
        }

        var member = entry.DeclaredMembers.FirstOrDefault(declared => declared.Id.Equals(memberId)) ?? throw new ApplicationFailureException(
                $"Member '{memberId}' is not declared on entry '{entryId}'.",
                ApplicationErrorCodes.ParticipationNotEligible);
        if (member.Role != DeclaredMemberRole.Player)
        {
            throw new ApplicationFailureException(
                $"Member '{memberId}' is not a player and cannot be added to the match sheet.",
                ApplicationErrorCodes.ParticipationNotEligible);
        }
    }

    internal static void EnsureDefinedSide(Side side)
    {
        if (!Enum.IsDefined(side))
        {
            throw new ApplicationFailureException(
                $"Unknown match side '{side}'.",
                ApplicationErrorCodes.InvalidSide);
        }
    }

    internal static void EnsureDefinedCompositionStatus(CompositionStatus compositionStatus)
    {
        if (!Enum.IsDefined(compositionStatus))
        {
            throw new ApplicationFailureException(
                $"Unknown composition status '{compositionStatus}'.",
                ApplicationErrorCodes.InvalidCompositionStatus);
        }
    }

    private static EntryId ResolveEntryId(Match match, Side side) =>
        side switch
        {
            Side.Home => match.HomeEntryId,
            Side.Away => match.AwayEntryId,
            _ => throw new ApplicationFailureException(
                $"Unknown match side '{side}'.",
                ApplicationErrorCodes.InvalidSide)
        };
}
