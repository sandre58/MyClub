// -----------------------------------------------------------------------
// <copyright file="ProgressionRules.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Rules;

/// <summary>
/// Describes how confrontation outcomes route participants to slots.
/// Immutable configuration only — no application or TieFormat resolution.
/// </summary>
public sealed record ProgressionRules
{
    private readonly ProgressionPath[] _paths;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProgressionRules"/> class.
    /// </summary>
    /// <param name="paths">Progression paths (non-empty; unique destinations).</param>
    public ProgressionRules(IReadOnlyList<ProgressionPath> paths)
    {
        ArgumentNullException.ThrowIfNull(paths);

        if (paths.Count == 0)
        {
            throw new DomainException(
                "Progression rules require at least one path.",
                RulesErrorCodes.ProgressionRulesInvalid);
        }

        var sources = paths
            .Select(p => (p.SourceFixtureId, p.Outcome))
            .ToArray();
        if (sources.Distinct().Count() != sources.Length)
        {
            throw new DomainException(
                "Progression paths must have unique source fixture and outcome pairs.",
                RulesErrorCodes.ProgressionRulesInvalid);
        }

        var destinationKeys = paths
            .Select(p => (p.Destination.StageId, p.Destination.SlotKey))
            .ToArray();
        if (destinationKeys.Distinct().Count() != destinationKeys.Length)
        {
            throw new DomainException(
                "Progression paths must target unique destinations.",
                RulesErrorCodes.ProgressionRulesInvalid);
        }

        // Stable storage order for deterministic equality (not a business Order property).
        _paths =
        [
            ..paths.OrderBy(p => p.SourceFixtureId.Value)
                .ThenBy(p => p.Outcome)
                .ThenBy(p => p.Destination.StageId.Value)
                .ThenBy(p => p.Destination.SlotKey, StringComparer.Ordinal)
        ];
    }

    /// <summary>
    /// Gets the progression paths in stable storage order.
    /// </summary>
    public IReadOnlyList<ProgressionPath> Paths => _paths;

    /// <summary>
    /// Returns an independent copy (new nested path instances).
    /// </summary>
    /// <returns>A deep copy of these progression rules.</returns>
    public ProgressionRules Copy() =>
        new([.._paths.Select(p => p.Copy())]);

    /// <inheritdoc />
    public bool Equals(ProgressionRules? other) =>
        other is not null && _paths.SequenceEqual(other._paths);

    /// <inheritdoc />
    public override int GetHashCode() =>
        _paths.Aggregate(0, HashCode.Combine);
}
