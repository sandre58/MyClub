// -----------------------------------------------------------------------
// <copyright file="AddStageMatchday.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Application use case: add one matchday (locale Championship / Groups skeleton growth).
/// </summary>
public static class AddStageMatchday
{
    /// <summary>
    /// Adds a matchday with the next available 1-based number when <paramref name="number"/> is null.
    /// </summary>
    public static Matchday Execute(Stage stage, int? number, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(clock);

        if (stage.Status is StageStatus.Running or StageStatus.Suspended or StageStatus.Completed)
        {
            throw new ApplicationFailureException(
                $"Matchdays cannot be added while stage status is '{stage.Status}'.",
                ApplicationErrorCodes.OrganisationNotMutable);
        }

        var resolved = number ?? (stage.Matchdays.Count == 0
            ? 1
            : stage.Matchdays.Max(matchday => matchday.Number) + 1);
        return stage.AddMatchday(resolved, clock);
    }
}
