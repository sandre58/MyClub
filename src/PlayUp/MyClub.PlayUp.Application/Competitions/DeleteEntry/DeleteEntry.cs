// -----------------------------------------------------------------------
// <copyright file="DeleteEntry.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Competitions;

/// <summary>
/// Application use case: hard-delete an entry during construction (Draft/Ready).
/// Detaches and removes matches that reference the entry; structure holes are left as-is.
/// </summary>
public static class DeleteEntry
{
    /// <summary>
    /// Deletes matches referencing the entry, then deletes the entry.
    /// </summary>
    public static void Execute(
        Competition competition,
        EntryId entryId,
        IReadOnlyList<Stage> competitionStages,
        IReadOnlyList<Match> competitionMatches,
        IMatchRepository matchRepository,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(competitionStages);
        ArgumentNullException.ThrowIfNull(competitionMatches);
        ArgumentNullException.ThrowIfNull(matchRepository);
        ArgumentNullException.ThrowIfNull(clock);

        _ = competition.GetEntry(entryId);
        RemoveMatchesForEntry(entryId, competitionStages, competitionMatches, matchRepository, clock);
        competition.DeleteEntry(entryId, clock);
    }

    internal static void RemoveMatchesForEntry(
        EntryId entryId,
        IReadOnlyList<Stage> competitionStages,
        IReadOnlyList<Match> competitionMatches,
        IMatchRepository matchRepository,
        IClock clock)
    {
        var stagesById = competitionStages.ToDictionary(stage => stage.Id);
        foreach (var match in competitionMatches.Where(candidate =>
                     candidate.HomeEntryId.Equals(entryId) || candidate.AwayEntryId.Equals(entryId)))
        {
            if (stagesById.TryGetValue(match.StageId, out var stage))
            {
                var fixtureId = stage.FindFixtureIdContainingMatch(match.Id);
                if (fixtureId is { } attached)
                {
                    stage.DetachMatch(attached, match.Id, clock);
                }
            }

            matchRepository.Remove(match);
        }
    }
}
