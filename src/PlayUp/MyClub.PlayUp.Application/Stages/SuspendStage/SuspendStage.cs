// -----------------------------------------------------------------------
// <copyright file="SuspendStage.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Application use case: suspend a running stage (Running → Suspended).
/// </summary>
public static class SuspendStage
{
    /// <summary>
    /// Suspends the stage when no match in scope is live.
    /// </summary>
    /// <param name="stage">Stage to suspend.</param>
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

        if (DematerializationCapability.TrySuspendBlockedByLiveReason(gates) is not null)
        {
            throw new ApplicationFailureException(
                "SuspendStage is blocked because a match is live.",
                ApplicationErrorCodes.RevertToReadyBlocked);
        }

        stage.Suspend(clock);
    }
}
