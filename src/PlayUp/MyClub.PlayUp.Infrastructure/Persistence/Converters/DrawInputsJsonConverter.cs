// -----------------------------------------------------------------------
// <copyright file="DrawInputsJsonConverter.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Infrastructure.Persistence.Converters;

/// <summary>
/// Converts nullable Domain <see cref="DrawInputs"/> to JSON for a PostgreSQL jsonb column.
/// </summary>
public sealed class DrawInputsJsonConverter : ValueConverter<DrawInputs?, string?>
{
    /// <summary>
    /// Gets a structural comparer (Domain record equality is reference-based for array backings).
    /// </summary>
    public static ValueComparer<DrawInputs?> Comparer { get; } = new(
        static (left, right) => StructuralEquals(left, right),
        static inputs => StructuralHash(inputs),
        static inputs => Snapshot(inputs));

    /// <summary>
    /// Initializes a new instance of the <see cref="DrawInputsJsonConverter"/> class.
    /// </summary>
    public DrawInputsJsonConverter()
        : base(
            static inputs => inputs == null ? null : DrawInputsJson.Serialize(inputs),
            static json => string.IsNullOrEmpty(json) ? null : DrawInputsJson.Deserialize(json))
    {
    }

    private static DrawInputs? Snapshot(DrawInputs? inputs) => inputs?.Copy();

    private static bool StructuralEquals(DrawInputs? left, DrawInputs? right) =>
        ReferenceEquals(left, right) || (left is not null && right is not null && (left.Entries.SequenceEqual(right.Entries)
            && MapEquals(left.SeedMap?.Seeds, right.SeedMap?.Seeds)
            && MapEquals(left.PotMembership?.Pots, right.PotMembership?.Pots)
            && left.FixedSlots.SequenceEqual(right.FixedSlots)
            && left.FixedGroups.SequenceEqual(right.FixedGroups)
            && left.FixedPairings.SequenceEqual(right.FixedPairings)));

    private static int StructuralHash(DrawInputs? inputs)
    {
        if (inputs is null)
        {
            return 0;
        }

        var hash = default(HashCode);
        foreach (var entry in inputs.Entries)
        {
            hash.Add(entry);
        }

        AddMap(hash, inputs.SeedMap?.Seeds);
        AddMap(hash, inputs.PotMembership?.Pots);
        foreach (var item in inputs.FixedSlots)
        {
            hash.Add(item);
        }

        foreach (var item in inputs.FixedGroups)
        {
            hash.Add(item);
        }

        foreach (var item in inputs.FixedPairings)
        {
            hash.Add(item);
        }

        return hash.ToHashCode();
    }

    private static bool MapEquals(
        IReadOnlyDictionary<EntryId, int>? left,
        IReadOnlyDictionary<EntryId, int>? right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (left is null || right is null || left.Count != right.Count)
        {
            return false;
        }

        foreach (var pair in left)
        {
            if (!right.TryGetValue(pair.Key, out var value) || value != pair.Value)
            {
                return false;
            }
        }

        return true;
    }

    private static void AddMap(HashCode hash, IReadOnlyDictionary<EntryId, int>? map)
    {
        if (map is null)
        {
            hash.Add(0);
            return;
        }

        foreach (var pair in map.OrderBy(candidate => candidate.Key.Value))
        {
            hash.Add(pair.Key);
            hash.Add(pair.Value);
        }
    }
}
