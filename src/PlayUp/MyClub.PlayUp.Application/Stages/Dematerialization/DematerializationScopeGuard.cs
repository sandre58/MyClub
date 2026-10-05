// -----------------------------------------------------------------------
// <copyright file="DematerializationScopeGuard.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Validates dematerialization scope before mutating fixtures (avoids partial deletes and persistence failures).
/// </summary>
internal static class DematerializationScopeGuard
{
    public static void EnsureAllAttachedMatchesLoaded(
        IReadOnlyCollection<MatchId> attachedMatchIds,
        IReadOnlyList<Match> loadedMatches)
    {
        ArgumentNullException.ThrowIfNull(attachedMatchIds);
        ArgumentNullException.ThrowIfNull(loadedMatches);

        if (attachedMatchIds.Count == loadedMatches.Count)
        {
            return;
        }

        throw new ApplicationFailureException(
            "Materialization references matches that could not be loaded for dematerialization.",
            ApplicationErrorCodes.MaterializationStateInconsistent);
    }
}
