// -----------------------------------------------------------------------
// <copyright file="PlacementAwardRules.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Rules;

/// <summary>
/// Describes how confrontation outcomes award final competition ranks.
/// Immutable configuration only — no application or TieFormat resolution.
/// Distinct from <see cref="ProgressionRules"/> (slot routing).
/// </summary>
public sealed record PlacementAwardRules
{
    private readonly PlacementAwardPath[] _paths;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlacementAwardRules"/> class.
    /// </summary>
    /// <param name="paths">Placement award paths (non-empty; unique sources and ranks).</param>
    public PlacementAwardRules(IReadOnlyList<PlacementAwardPath> paths)
    {
        ArgumentNullException.ThrowIfNull(paths);

        if (paths.Count == 0)
        {
            throw new DomainException(
                "Placement award rules require at least one path.",
                RulesErrorCodes.PlacementAwardRulesInvalid);
        }

        var sources = paths
            .Select(p => (p.SourceFixtureId, p.Outcome))
            .ToArray();
        if (sources.Distinct().Count() != sources.Length)
        {
            throw new DomainException(
                "Placement award paths must have unique source fixture and outcome pairs.",
                RulesErrorCodes.PlacementAwardRulesInvalid);
        }

        var ranks = paths.Select(p => p.Rank).ToArray();
        if (ranks.Distinct().Count() != ranks.Length)
        {
            throw new DomainException(
                "Placement award paths must target unique ranks.",
                RulesErrorCodes.PlacementAwardRulesInvalid);
        }

        // Stable storage order for deterministic equality (not a business Order property).
        _paths =
        [
            ..paths.OrderBy(p => p.SourceFixtureId.Value)
                .ThenBy(p => p.Outcome)
                .ThenBy(p => p.Rank)
        ];
    }

    /// <summary>
    /// Gets the placement award paths in stable storage order.
    /// </summary>
    public IReadOnlyList<PlacementAwardPath> Paths => _paths;

    /// <summary>
    /// Returns an independent copy (new nested path instances).
    /// </summary>
    /// <returns>A deep copy of these placement award rules.</returns>
    public PlacementAwardRules Copy() =>
        new([.._paths.Select(p => p.Copy())]);

    /// <inheritdoc />
    public bool Equals(PlacementAwardRules? other) =>
        other is not null && _paths.SequenceEqual(other._paths);

    /// <inheritdoc />
    public override int GetHashCode() =>
        _paths.Aggregate(0, HashCode.Combine);
}
