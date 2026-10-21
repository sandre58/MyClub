// -----------------------------------------------------------------------
// <copyright file="StartStage.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Application use case: start an already-loaded Stage (Ready → Running).
/// </summary>
/// <remarks>
/// Domain owns lifecycle transitions (<see cref="Stage.Start"/>). Persistence: caller loads
/// the Stage, invokes this use case, then calls <c>IUnitOfWork.SaveChangesAsync</c> once.
/// </remarks>
public static class StartStage
{
    /// <summary>
    /// Starts <paramref name="stage"/>.
    /// </summary>
    /// <param name="stage">Stage aggregate.</param>
    /// <param name="clock">Clock for domain events.</param>
    public static void Execute(Stage stage, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(clock);

        stage.Start(clock);
    }
}
