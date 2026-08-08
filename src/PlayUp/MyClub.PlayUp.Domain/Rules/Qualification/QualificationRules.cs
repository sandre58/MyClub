// -----------------------------------------------------------------------
// <copyright file="QualificationRules.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Rules;

/// <summary>
/// Describes how participants leave a stage and are routed to other stages.
/// Immutable configuration only — no qualification application or results.
/// </summary>
public sealed record QualificationRules
{
    private readonly QualificationPath[] _paths;

    /// <summary>
    /// Initializes a new instance of the <see cref="QualificationRules"/> class.
    /// </summary>
    /// <param name="paths">Ordered qualification paths (non-empty, unique orders).</param>
    public QualificationRules(IReadOnlyList<QualificationPath> paths)
    {
        ArgumentNullException.ThrowIfNull(paths);

        if (paths.Count == 0)
        {
            throw new DomainException(
                "Qualification rules require at least one path.",
                RulesErrorCodes.QualificationRulesInvalid);
        }

        if (paths.Select(p => p.Order).Distinct().Count() != paths.Count)
        {
            throw new DomainException(
                "Qualification path orders must be unique.",
                RulesErrorCodes.QualificationRulesInvalid);
        }

        _paths = [..paths.OrderBy(p => p.Order)];
    }

    /// <summary>
    /// Gets the qualification paths ordered by <see cref="QualificationPath.Order"/>.
    /// </summary>
    public IReadOnlyList<QualificationPath> Paths => _paths;

    /// <summary>
    /// Returns an independent copy (new nested path instances).
    /// </summary>
    /// <returns>A deep copy of these qualification rules.</returns>
    public QualificationRules Copy() =>
        new(_paths.Select(p => p.Copy()).ToArray());

    /// <inheritdoc />
    public bool Equals(QualificationRules? other) =>
        other is not null && _paths.SequenceEqual(other._paths);

    /// <inheritdoc />
    public override int GetHashCode() =>
        _paths.Aggregate(0, HashCode.Combine);
}
