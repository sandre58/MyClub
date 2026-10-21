// -----------------------------------------------------------------------
// <copyright file="RenameStage.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Application use case: rename a stage (locale, non-destructive).
/// Allowed at any lifecycle status — display name is not structure-critical.
/// </summary>
public static class RenameStage
{
    /// <summary>
    /// Renames the stage (validates <see cref="StageName"/> only).
    /// </summary>
    public static void Execute(Stage stage, string name)
    {
        ArgumentNullException.ThrowIfNull(stage);
        stage.Rename(new StageName(name));
    }
}
