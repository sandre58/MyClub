// -----------------------------------------------------------------------
// <copyright file="CancelDraw.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Application use case: cancel a draw on an already-loaded Stage.
/// </summary>
/// <remarks>
/// Domain owns cancel invariants (<see cref="Stage.CancelDraw"/>). Persistence: caller loads
/// the Stage, invokes this use case, then calls <c>IUnitOfWork.SaveChangesAsync</c> once.
/// Already-Cancelled is a Domain no-op (no event).
/// </remarks>
public static class CancelDraw
{
    /// <summary>
    /// Cancels the draw identified by <paramref name="drawId"/> on <paramref name="stage"/>.
    /// </summary>
    /// <param name="stage">Stage that owns the draw.</param>
    /// <param name="drawId">Draw identity.</param>
    /// <param name="clock">Clock for domain events.</param>
    public static void Execute(Stage stage, DrawId drawId, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(clock);

        stage.CancelDraw(drawId, clock);
    }
}
