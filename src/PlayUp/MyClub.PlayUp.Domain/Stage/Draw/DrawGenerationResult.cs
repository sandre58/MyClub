// -----------------------------------------------------------------------
// <copyright file="DrawGenerationResult.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Domain.Stage;

/// <summary>
/// Outcome of <see cref="DrawResolutionGenerator"/> — Resolved or NoSolution (never an exception for feasibility).
/// </summary>
public sealed class DrawGenerationResult
{
    private DrawGenerationResult(bool isNoSolution, DrawResolution? resolution)
    {
        IsNoSolution = isNoSolution;
        Resolution = resolution;
    }

    /// <summary>
    /// Gets a value indicating whether no admissible resolution exists.
    /// </summary>
    public bool IsNoSolution { get; }

    /// <summary>
    /// Gets a value indicating whether a resolution was found.
    /// </summary>
    public bool IsResolved => !IsNoSolution;

    /// <summary>
    /// Gets the resolution when <see cref="IsResolved"/>; otherwise <see langword="null"/>.
    /// </summary>
    public DrawResolution? Resolution { get; }

    /// <summary>
    /// Creates a resolved outcome.
    /// </summary>
    /// <param name="resolution">Admissible resolution (must be Resolved).</param>
    /// <returns>A resolved generation result.</returns>
    public static DrawGenerationResult Resolved(DrawResolution resolution)
    {
        ArgumentNullException.ThrowIfNull(resolution);
        return resolution.State != DrawResolutionState.Resolved
            ? throw new ArgumentException("Resolution must be in Resolved state.", nameof(resolution))
            : new DrawGenerationResult(false, resolution);
    }

    /// <summary>
    /// Creates a no-solution outcome (valid request, no admissible combination).
    /// </summary>
    /// <returns>A no-solution generation result.</returns>
    public static DrawGenerationResult NoSolution() => new(true, null);
}
