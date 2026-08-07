// -----------------------------------------------------------------------
// <copyright file="PointsPolicy.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Domain.Rules;

/// <summary>
/// Points awarded for a win, draw, or loss when computing standings.
/// </summary>
public sealed record PointsPolicy
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PointsPolicy"/> class.
    /// </summary>
    /// <param name="winPoints">Points for a win.</param>
    /// <param name="drawPoints">Points for a draw.</param>
    /// <param name="lossPoints">Points for a loss (may be 0).</param>
    public PointsPolicy(int winPoints, int drawPoints, int lossPoints)
    {
        WinPoints = winPoints;
        DrawPoints = drawPoints;
        LossPoints = lossPoints;
    }

    /// <summary>
    /// Gets the points awarded for a win.
    /// </summary>
    public int WinPoints { get; }

    /// <summary>
    /// Gets the points awarded for a draw.
    /// </summary>
    public int DrawPoints { get; }

    /// <summary>
    /// Gets the points awarded for a loss.
    /// </summary>
    public int LossPoints { get; }
}
