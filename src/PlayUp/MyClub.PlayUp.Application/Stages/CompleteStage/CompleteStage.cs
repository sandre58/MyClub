// -----------------------------------------------------------------------
// <copyright file="CompleteStage.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Application use case: complete a Running (or Suspended) stage when every stage match is closed.
/// </summary>
/// <remarks>
/// Domain owns the lifecycle transition (<see cref="Stage.Complete"/>). Caller loads the stage and
/// its matches, invokes this use case, then calls <c>IUnitOfWork.SaveChangesAsync</c> once.
/// </remarks>
public static class CompleteStage
{
    /// <summary>
    /// Machine reason when one or more matches are not <see cref="MatchStatus.Finished"/> or
    /// <see cref="MatchStatus.Cancelled"/>.
    /// </summary>
    public const string ReasonOpenMatches = "OpenMatches";

    /// <summary>
    /// Completes <paramref name="stage"/> when all <paramref name="stageMatches"/> are finished or cancelled.
    /// </summary>
    /// <param name="stage">Stage aggregate.</param>
    /// <param name="stageMatches">Matches attached to the stage (may be empty).</param>
    /// <param name="clock">Clock for domain events.</param>
    /// <exception cref="ApplicationFailureException">
    /// Thrown when at least one match is still open.
    /// </exception>
    public static void Execute(Stage stage, IReadOnlyList<Match> stageMatches, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(stageMatches);
        ArgumentNullException.ThrowIfNull(clock);

        var hasOpenMatches = stageMatches.Any(match =>
            match.Status is not (MatchStatus.Finished or MatchStatus.Cancelled));
        if (hasOpenMatches)
        {
            throw new ApplicationFailureException(
                "Stage cannot be completed while matches are still open.",
                ApplicationErrorCodes.StageCompletionBlocked,
                [ReasonOpenMatches]);
        }

        stage.Complete(clock);
    }
}
