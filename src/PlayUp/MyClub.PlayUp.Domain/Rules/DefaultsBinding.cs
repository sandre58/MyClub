// -----------------------------------------------------------------------
// <copyright file="DefaultsBinding.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Domain.Rules;

/// <summary>
/// Provenance metadata: which heritable regulation parts on a Stage still follow Competition defaults.
/// Stored on the Stage aggregate — not inside MatchRules / StandingRules value objects.
/// </summary>
public sealed class DefaultsBinding : IEquatable<DefaultsBinding>
{
    private readonly HashSet<HeritableRegulationPart> _bound;

    private DefaultsBinding(IEnumerable<HeritableRegulationPart> bound) => _bound = [..bound];

    /// <summary>
    /// All Match parts bound; Standing parts bound only when <paramref name="isClassifyingPhase"/> is true.
    /// </summary>
    public static DefaultsBinding AllBound(bool isClassifyingPhase)
    {
        HeritableRegulationPart[] match =
        [
            HeritableRegulationPart.MatchDuration,
            HeritableRegulationPart.ExtraTime,
            HeritableRegulationPart.PenaltyShootout,
            HeritableRegulationPart.AdministrativeResult,
        ];

        return !isClassifyingPhase
            ? new DefaultsBinding(match)
            : new DefaultsBinding(
        [
            ..match,
            HeritableRegulationPart.Points,
            HeritableRegulationPart.RankingCriteria,
        ]);
    }

    /// <summary>
    /// No parts follow Competition (migration backfill / fully specialized stage).
    /// </summary>
    public static DefaultsBinding AllUnbound() => new([]);

    /// <summary>
    /// Gets whether the part still follows Competition defaults.
    /// </summary>
    public bool IsBound(HeritableRegulationPart part) => _bound.Contains(part);

    /// <summary>
    /// Returns a copy with the part unbound (idempotent).
    /// </summary>
    public DefaultsBinding Unbind(HeritableRegulationPart part)
    {
        if (!_bound.Contains(part))
        {
            return Copy();
        }

        var next = new HashSet<HeritableRegulationPart>(_bound);
        next.Remove(part);
        return new DefaultsBinding(next);
    }

    /// <summary>
    /// Returns a copy with the part bound (idempotent).
    /// </summary>
    public DefaultsBinding Bind(HeritableRegulationPart part)
    {
        if (_bound.Contains(part))
        {
            return Copy();
        }

        var next = new HashSet<HeritableRegulationPart>(_bound) { part };
        return new DefaultsBinding(next);
    }

    /// <summary>
    /// Returns an independent copy.
    /// </summary>
    public DefaultsBinding Copy() => new(_bound);

    /// <summary>
    /// Gets the bound parts (deterministic order for serialization).
    /// </summary>
    public IReadOnlyList<HeritableRegulationPart> BoundParts =>
        [.. _bound.OrderBy(static p => (int)p)];

    /// <inheritdoc />
    public bool Equals(DefaultsBinding? other) =>
        other is not null && _bound.SetEquals(other._bound);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is DefaultsBinding other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = default(HashCode);
        foreach (var part in BoundParts)
        {
            hash.Add(part);
        }

        return hash.ToHashCode();
    }
}
