// -----------------------------------------------------------------------
// <copyright file="RevertToDraft.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Application use case: revert a Ready stage to Draft when no matches are attached.
/// </summary>
public static class RevertToDraft
{
    /// <summary>
    /// Reverts the stage when preflight passes.
    /// </summary>
    /// <param name="stage">Stage to revert.</param>
    /// <param name="stageMatches">Matches loaded for the stage.</param>
    /// <param name="clock">Clock for domain events.</param>
    public static void Execute(Stage stage, IReadOnlyList<Match> stageMatches, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(stageMatches);
        ArgumentNullException.ThrowIfNull(clock);

        if (stageMatches.Any(match => stage.HasMatch(match.Id)))
        {
            throw new ApplicationFailureException(
                "RevertToDraft is blocked because matches are attached to the stage.",
                ApplicationErrorCodes.RevertToDraftBlocked);
        }

        stage.RevertToDraft(clock);
    }
}
