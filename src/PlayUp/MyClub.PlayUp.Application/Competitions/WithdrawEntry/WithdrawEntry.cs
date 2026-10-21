// -----------------------------------------------------------------------
// <copyright file="WithdrawEntry.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Competitions;

/// <summary>
/// Application use case: forfait (withdraw) an entry during Running/Suspended.
/// Finishes unfinished matches as administrative forfeit, then marks the entry Withdrawn.
/// </summary>
public static class WithdrawEntry
{
    /// <summary>
    /// Finishes remaining matches involving the entry as forfeit, then withdraws the entry.
    /// </summary>
    public static void Execute(
        Competition competition,
        EntryId entryId,
        IReadOnlyList<Stage> competitionStages,
        IReadOnlyList<Match> competitionMatches,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(competitionStages);
        ArgumentNullException.ThrowIfNull(competitionMatches);
        ArgumentNullException.ThrowIfNull(clock);

        _ = competition.GetEntry(entryId);
        FinishRemainingMatchesAsForfeit(entryId, competitionStages, competitionMatches, clock);
        competition.WithdrawEntry(entryId, clock);
    }

    internal static void FinishRemainingMatchesAsForfeit(
        EntryId entryId,
        IReadOnlyList<Stage> competitionStages,
        IReadOnlyList<Match> competitionMatches,
        IClock clock)
    {
        var stagesById = competitionStages.ToDictionary(stage => stage.Id);
        foreach (var match in competitionMatches)
        {
            if (!match.HomeEntryId.Equals(entryId) && !match.AwayEntryId.Equals(entryId))
            {
                continue;
            }

            if (match.Status is MatchStatus.Finished or MatchStatus.Cancelled)
            {
                continue;
            }

            if (!stagesById.TryGetValue(match.StageId, out var stage))
            {
                throw new ApplicationFailureException(
                    $"Stage '{match.StageId}' was not loaded for match '{match.Id}'.",
                    ApplicationErrorCodes.StageNotFound);
            }

            var policy = stage.Regulation.MatchRules.AdministrativeResultPolicy;
            var result = ForfeitMatchResult.ForWithdrawnSide(match, entryId, policy);
            match.Finish(result, clock);
        }
    }
}
