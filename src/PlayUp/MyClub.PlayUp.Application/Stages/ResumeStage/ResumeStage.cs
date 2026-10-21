// -----------------------------------------------------------------------
// <copyright file="ResumeStage.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Application use case: resume a suspended stage (Suspended → Running).
/// </summary>
public static class ResumeStage
{
    /// <summary>
    /// Resumes <paramref name="stage"/>.
    /// </summary>
    /// <param name="stage">Stage aggregate.</param>
    /// <param name="clock">Clock for domain events.</param>
    public static void Execute(Stage stage, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(clock);

        stage.Resume(clock);
    }
}
