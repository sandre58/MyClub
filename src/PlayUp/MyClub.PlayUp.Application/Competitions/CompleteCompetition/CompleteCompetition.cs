// -----------------------------------------------------------------------
// <copyright file="CompleteCompetition.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;

namespace MyClub.PlayUp.Application.Competitions;

/// <summary>
/// Application use case: close a competition via <see cref="Competition.Complete"/>.
/// </summary>
/// <remarks>
/// <see cref="CompletionMode.Normal"/> requires sporting completeness (Application gate).
/// Administrative / Abandoned may close an incomplete competition.
/// Does not apply Standing, Qualification, Progression, Finish, Draw, or Schedule.
/// </remarks>
public static class CompleteCompetition
{
    /// <summary>
    /// Completes the competition when allowed.
    /// </summary>
    /// <param name="competition">Target competition.</param>
    /// <param name="mode">Completion manner.</param>
    /// <param name="analysis">Derived sporting-completeness analysis.</param>
    /// <param name="clock">Clock for domain events.</param>
    /// <exception cref="ApplicationFailureException">
    /// Thrown when Normal completion is refused because the competition is incomplete.
    /// </exception>
    public static void Execute(
        Competition competition,
        CompletionMode mode,
        CompletionAnalysis analysis,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(analysis);
        ArgumentNullException.ThrowIfNull(clock);

        if (mode == CompletionMode.Normal && !analysis.IsSportivelyComplete)
        {
            var reasonCodes = analysis.Reasons.Select(reason => reason.Code).ToArray();
            var detail = reasonCodes.Length == 0
                ? "Competition is not sportively complete."
                : $"Competition is not sportively complete: {string.Join(", ", reasonCodes)}.";
            throw new ApplicationFailureException(
                detail,
                ApplicationErrorCodes.CompletionNotAllowed,
                reasonCodes);
        }

        competition.Complete(mode, clock);
    }
}
