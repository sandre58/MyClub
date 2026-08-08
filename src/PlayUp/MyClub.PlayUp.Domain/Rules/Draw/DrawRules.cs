// -----------------------------------------------------------------------
// <copyright file="DrawRules.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Rules;

/// <summary>
/// Parameters describing how a draw should be organized.
/// Immutable; no draw execution, publication, or result history.
/// Distinct from a future Draw entity.
/// </summary>
public sealed record DrawRules
{
    private readonly DrawConstraint[] _constraints;

    /// <summary>
    /// Initializes a new instance of the <see cref="DrawRules"/> class.
    /// </summary>
    /// <param name="mode">How pairings are generated.</param>
    /// <param name="seedingRules">Seeding parameters when used; <see langword="null"/> when unused.</param>
    /// <param name="potRules">Pot organization when used; <see langword="null"/> when unused.</param>
    /// <param name="constraints">Draw constraints (may be empty).</param>
    public DrawRules(
        DrawMode mode,
        SeedingRules? seedingRules = null,
        PotRules? potRules = null,
        IReadOnlyList<DrawConstraint>? constraints = null)
    {
        if (!Enum.IsDefined(mode))
        {
            throw new DomainException(
                "Draw mode is unknown.",
                RulesErrorCodes.DrawRulesInvalid);
        }

        Mode = mode;
        SeedingRules = seedingRules;
        PotRules = potRules;
        _constraints = constraints is null ? [] : [..constraints];
    }

    /// <summary>
    /// Gets the draw mode.
    /// </summary>
    public DrawMode Mode { get; }

    /// <summary>
    /// Gets seeding rules when present; otherwise <see langword="null"/>.
    /// </summary>
    public SeedingRules? SeedingRules { get; }

    /// <summary>
    /// Gets pot rules when present; otherwise <see langword="null"/>.
    /// </summary>
    public PotRules? PotRules { get; }

    /// <summary>
    /// Gets the draw constraints.
    /// </summary>
    public IReadOnlyList<DrawConstraint> Constraints => _constraints;

    /// <summary>
    /// Returns an independent copy (new nested value-object instances).
    /// </summary>
    /// <returns>A deep copy of these draw rules.</returns>
    public DrawRules Copy() =>
        new(
            Mode,
            SeedingRules is null ? null : new SeedingRules(SeedingRules.NumberOfSeeds),
            PotRules is null ? null : new PotRules(PotRules.NumberOfPots),
            _constraints.Select(c => new DrawConstraint(c.ConstraintType, c.Enforcement)).ToArray());

    /// <inheritdoc />
    public bool Equals(DrawRules? other) =>
        other is not null
        && Mode == other.Mode
        && Equals(SeedingRules, other.SeedingRules)
        && Equals(PotRules, other.PotRules)
        && _constraints.SequenceEqual(other._constraints);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = HashCode.Combine(Mode, SeedingRules, PotRules);
        return _constraints.Aggregate(hash, HashCode.Combine);
    }
}
