// -----------------------------------------------------------------------
// <copyright file="DrawResolutionJsonConverter.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Infrastructure.Persistence.Converters;

/// <summary>
/// Converts Domain <see cref="DrawResolution"/> to JSON for a PostgreSQL jsonb column.
/// </summary>
public sealed class DrawResolutionJsonConverter : ValueConverter<DrawResolution, string>
{
    /// <summary>
    /// Gets a structural comparer (Domain record equality is reference-based for array backings).
    /// </summary>
    public static ValueComparer<DrawResolution> Comparer { get; } = new(
        static (left, right) => StructuralEquals(left, right),
        static resolution => StructuralHash(resolution),
        static resolution => resolution.Copy());

    /// <summary>
    /// Initializes a new instance of the <see cref="DrawResolutionJsonConverter"/> class.
    /// </summary>
    public DrawResolutionJsonConverter()
        : base(
            static resolution => DrawResolutionJson.Serialize(resolution),
            static json => DrawResolutionJson.Deserialize(json))
    {
    }

    private static bool StructuralEquals(DrawResolution? left, DrawResolution? right) =>
        ReferenceEquals(left, right)
            || (left is not null && right is not null && (left.State == right.State
                                                          && left.ResolvedKind == right.ResolvedKind
                                                          && left.SlotResults.SequenceEqual(right.SlotResults)
                                                          && left.GroupResults.SequenceEqual(right.GroupResults)));

    private static int StructuralHash(DrawResolution resolution)
    {
        var hash = default(HashCode);
        hash.Add(resolution.State);
        hash.Add(resolution.ResolvedKind);
        foreach (var item in resolution.SlotResults)
        {
            hash.Add(item);
        }

        foreach (var item in resolution.GroupResults)
        {
            hash.Add(item);
        }

        return hash.ToHashCode();
    }
}
