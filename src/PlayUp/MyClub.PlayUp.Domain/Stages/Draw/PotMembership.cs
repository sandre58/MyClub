// -----------------------------------------------------------------------
// <copyright file="PotMembership.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Stages;

/// <summary>
/// Concrete Entry→Pot assignments for a Draw instance (Host/orchestration input).
/// Distinct from <see cref="Rules.PotRules"/> (regulation count) and from SeedMap.
/// Optional independently of SeedMap; never a destination by itself.
/// </summary>
public sealed record PotMembership
{
    private readonly Dictionary<EntryId, int> _pots;

    /// <summary>
    /// Initializes a new instance of the <see cref="PotMembership"/> class.
    /// </summary>
    /// <param name="pots">Entry to pot number (≥ 1) map.</param>
    public PotMembership(IReadOnlyDictionary<EntryId, int> pots)
    {
        ArgumentNullException.ThrowIfNull(pots);

        if (pots.Any(pair => pair.Value < 1))
        {
            throw new DomainException(
                "Pot numbers must be at least 1.",
                StageErrorCodes.DrawInputsInvalid);
        }

        // Copy ctor — collection expression cannot spread IDictionary into Dictionary.
#pragma warning disable IDE0028
        _pots = new Dictionary<EntryId, int>(pots);
#pragma warning restore IDE0028
    }

    /// <summary>
    /// Gets the pot memberships.
    /// </summary>
    public IReadOnlyDictionary<EntryId, int> Pots => _pots;

    /// <summary>
    /// Returns an independent copy.
    /// </summary>
    public PotMembership Copy() => new(_pots);
}
