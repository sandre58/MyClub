// -----------------------------------------------------------------------
// <copyright file="DrawGenerationResult.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Domain.Stages;

/// <summary>
/// Outcome of <see cref="DrawResolutionGenerator"/> — Resolved or NoSolution (never an exception for feasibility).
/// Soft Preferred feedback lives here only (not persisted on Draw).
/// </summary>
public sealed class DrawGenerationResult
{
    private readonly PreferredViolation[] _preferredViolations;

    private DrawGenerationResult(
        bool isNoSolution,
        DrawResolution? resolution,
        IReadOnlyList<PreferredViolation>? preferredViolations)
    {
        IsNoSolution = isNoSolution;
        Resolution = resolution;
        _preferredViolations = preferredViolations is null ? [] : [.. preferredViolations];
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
    /// Gets Preferred constraint violations for a Resolved outcome (empty when none / NoSolution).
    /// Cost = <see cref="PreferredViolationsCount"/>.
    /// </summary>
    public IReadOnlyList<PreferredViolation> PreferredViolations => _preferredViolations;

    /// <summary>
    /// Gets the soft cost (number of Preferred violations). Zero when NoSolution.
    /// </summary>
    public int PreferredViolationsCount => _preferredViolations.Length;

    /// <summary>
    /// Creates a resolved outcome.
    /// </summary>
    /// <param name="resolution">Admissible resolution (must be Resolved).</param>
    /// <param name="preferredViolations">Optional Preferred violations (empty when none).</param>
    /// <returns>A resolved generation result.</returns>
    public static DrawGenerationResult Resolved(
        DrawResolution resolution,
        IReadOnlyList<PreferredViolation>? preferredViolations = null)
    {
        ArgumentNullException.ThrowIfNull(resolution);
        return resolution.State != DrawResolutionState.Resolved
            ? throw new ArgumentException("Resolution must be in Resolved state.", nameof(resolution))
            : new DrawGenerationResult(false, resolution, preferredViolations);
    }

    /// <summary>
    /// Creates a no-solution outcome (valid request, no admissible combination).
    /// </summary>
    /// <returns>A no-solution generation result.</returns>
    public static DrawGenerationResult NoSolution() => new(true, null, null);
}
