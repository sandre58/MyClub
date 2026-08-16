// -----------------------------------------------------------------------
// <copyright file="CreateDraw.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Application use case: create a Draft Draw on a Stage.
/// </summary>
public static class CreateDraw
{
    /// <summary>
    /// Creates a Draft draw of the given resolution kind.
    /// </summary>
    /// <param name="stage">Owning stage.</param>
    /// <param name="kind">Resolution kind (Slot, Group, Pairing).</param>
    /// <param name="clock">Clock for domain events.</param>
    /// <returns>The created draw.</returns>
    public static Draw Execute(Stage stage, DrawResolutionKind kind, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(clock);
        EnsureOrganisationMutable(stage);
        return stage.CreateDraw(kind, clock);
    }

    private static void EnsureOrganisationMutable(Stage stage)
    {
        if (stage.Status is StageStatus.Running or StageStatus.Suspended or StageStatus.Completed)
        {
            throw new ApplicationFailureException(
                $"Draw cannot be created when stage status is '{stage.Status}'.",
                ApplicationErrorCodes.OrganisationNotMutable);
        }
    }
}
