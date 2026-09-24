// -----------------------------------------------------------------------
// <copyright file="PlacementAwardRules.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Rules;

/// <summary>
/// Ordered set of <see cref="PlacementAwardPath"/> (final competition ranks from confrontations).
/// </summary>
public sealed record PlacementAwardRules
{
    private readonly PlacementAwardPath[] _paths;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlacementAwardRules"/> class.
    /// </summary>
    /// <param name="paths">Award paths (non-empty).</param>
    public PlacementAwardRules(IReadOnlyList<PlacementAwardPath> paths)
    {
        ArgumentNullException.ThrowIfNull(paths);

        if (paths.Count == 0)
        {
            throw new DomainException(
                "Placement award rules require at least one path.",
                RulesErrorCodes.PlacementAwardRulesInvalid);
        }

        if (paths.Select(p => (p.SourcePairKey, p.Outcome)).Distinct().Count() != paths.Count)
        {
            throw new DomainException(
                "Placement award (source, outcome) pairs must be unique.",
                RulesErrorCodes.PlacementAwardRulesInvalid);
        }

        if (paths.Select(p => p.Rank).Distinct().Count() != paths.Count)
        {
            throw new DomainException(
                "Placement award ranks must be unique.",
                RulesErrorCodes.PlacementAwardRulesInvalid);
        }

        _paths =
        [
            .. paths.OrderBy(p => p.SourcePairKey, StringComparer.Ordinal)
                .ThenBy(p => p.Outcome)
                .ThenBy(p => p.Rank)
        ];
    }

    /// <summary>
    /// Gets the award paths.
    /// </summary>
    public IReadOnlyList<PlacementAwardPath> Paths => _paths;

    /// <summary>
    /// Returns a deep copy.
    /// </summary>
    public PlacementAwardRules Copy() =>
        new(_paths.Select(p => p.Copy()).ToArray());

    /// <inheritdoc />
    public bool Equals(PlacementAwardRules? other) =>
        other is not null && _paths.SequenceEqual(other._paths);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        HashCode hash = default;
        foreach (var path in _paths)
        {
            hash.Add(path);
        }

        return hash.ToHashCode();
    }
}
