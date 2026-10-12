// -----------------------------------------------------------------------
// <copyright file="StageStructureEditGuard.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Application guard: structural stage edits are allowed in <see cref="StageStatus.Draft"/> only.
/// </summary>
internal static class StageStructureEditGuard
{
    internal static void EnsureEditable(Stage stage)
    {
        ArgumentNullException.ThrowIfNull(stage);

        if (stage.Status is not StageStatus.Draft)
        {
            throw new ApplicationFailureException(
                $"Structure cannot be modified while stage status is '{stage.Status}'.",
                ApplicationErrorCodes.StructureNotMutable);
        }
    }
}
