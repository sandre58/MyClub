// -----------------------------------------------------------------------
// <copyright file="SeedMap.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Stage;

/// <summary>
/// Concrete Entry→Seed assignments for a Draw instance.
/// Distinct from <see cref="Rules.SeedingRules"/> (regulation count) and from Standing.Position.
/// </summary>
public sealed record SeedMap
{
    private readonly Dictionary<EntryId, int> _seeds;

    /// <summary>
    /// Initializes a new instance of the <see cref="SeedMap"/> class.
    /// </summary>
    /// <param name="seeds">Entry to seed number (≥ 1) map.</param>
    public SeedMap(IReadOnlyDictionary<EntryId, int> seeds)
    {
        ArgumentNullException.ThrowIfNull(seeds);

        if (seeds.Any(pair => pair.Value < 1))
        {
            throw new DomainException(
                "Seed numbers must be at least 1.",
                StageErrorCodes.DrawInputsInvalid);
        }

        _seeds = new Dictionary<EntryId, int>(seeds);
    }

    /// <summary>
    /// Gets the seed assignments.
    /// </summary>
    public IReadOnlyDictionary<EntryId, int> Seeds => _seeds;

    /// <summary>
    /// Returns an independent copy.
    /// </summary>
    public SeedMap Copy() => new(_seeds);
}
