// -----------------------------------------------------------------------
// <copyright file="RevertToReady.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Application use case: revert Running or Suspended stage to Ready (pause exploitation).
/// </summary>
public static class RevertToReady
{
    /// <summary>
    /// Reverts the stage when no match in scope blocks dematerialization-style engagement.
    /// </summary>
    /// <param name="stage">Stage to revert.</param>
    /// <param name="stageMatches">Matches loaded for the stage.</param>
    /// <param name="clock">Clock for domain events.</param>
    public static void Execute(Stage stage, IReadOnlyList<Match> stageMatches, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(stageMatches);
        ArgumentNullException.ThrowIfNull(clock);

        var gates = stageMatches
            .Where(match => stage.HasMatch(match.Id))
            .Select(DematerializationMatchGate.FromMatch)
            .ToArray();

        if (DematerializationCapability.TryMatchLifecycleReason(gates) is not null)
        {
            throw new ApplicationFailureException(
                "RevertToReady is blocked because a match was already played or is live.",
                ApplicationErrorCodes.RevertToReadyBlocked);
        }

        stage.RevertToReady(clock);
    }
}
