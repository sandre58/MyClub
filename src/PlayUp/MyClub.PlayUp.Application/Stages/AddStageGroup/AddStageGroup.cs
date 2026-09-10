// -----------------------------------------------------------------------
// <copyright file="AddStageGroup.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Application use case: add one group (locale Groups skeleton growth).
/// </summary>
public static class AddStageGroup
{
    /// <summary>
    /// Adds a group. When <paramref name="name"/> is null/blank, uses A, B, … labels.
    /// </summary>
    public static Group Execute(Stage stage, string? name, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(clock);

        if (stage.Status is StageStatus.Running or StageStatus.Suspended or StageStatus.Completed)
        {
            throw new ApplicationFailureException(
                $"Groups cannot be added while stage status is '{stage.Status}'.",
                ApplicationErrorCodes.StructureNotMutable);
        }

        var resolved = string.IsNullOrWhiteSpace(name)
            ? DefaultGroupLabel(stage.Groups.Count)
            : name.Trim();
        return stage.AddGroup(resolved, clock);
    }

    private static string DefaultGroupLabel(int index) =>
        index < 26 ? ((char)('A' + index)).ToString() : $"G{index + 1}";
}
