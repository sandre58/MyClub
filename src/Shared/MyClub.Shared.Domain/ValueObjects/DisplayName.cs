// -----------------------------------------------------------------------
// <copyright file="DisplayName.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using MyNet.Utilities;

namespace MyClub.Shared.Domain.ValueObjects;

/// <summary>
/// Value object representing a display name with both full and short name variants.
/// This is commonly used for entities like teams, stadiums, and competitions where
/// both a full name and an abbreviated version are needed for different display contexts.
/// </summary>
/// <param name="name">The full name. This parameter is required and cannot be null or empty.</param>
/// <param name="shortName">The short name or abbreviation. If not provided, it will be automatically generated from the initials of the full name.</param>
public sealed class DisplayName(string name, string? shortName = null) : ValueObject, ISimilar<DisplayName>, IComparable<DisplayName>, IComparable<string>
{
    /// <summary>
    /// Gets the full name.
    /// This is the primary display name used in most contexts.
    /// </summary>
    public string Name { get; } = name.IsRequiredOrThrow();

    /// <summary>
    /// Gets the short name or abbreviation.
    /// If not explicitly provided during construction, this is automatically generated from the initials of the full name.
    /// </summary>
    public string ShortName { get; } = shortName ?? name.GetInitials();

    /// <summary>
    /// Determines whether the specified object is equal to the current DisplayName.
    /// Equality is based on case-insensitive comparison of the Name property.
    /// </summary>
    /// <param name="obj">The object to compare with the current DisplayName.</param>
    /// <returns>true if the specified object is equal to the current DisplayName; otherwise, false.</returns>
    public override bool Equals(object? obj) =>
        obj is DisplayName other && Name.Equals(other.Name, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Returns the hash code for this DisplayName based on the Name property.
    /// Uses case-insensitive hashing for consistency with the Equals method.
    /// </summary>
    /// <returns>A hash code for the current DisplayName.</returns>
    public override int GetHashCode() => Name.GetHashCode(StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Returns the full name as the string representation of this DisplayName.
    /// </summary>
    /// <returns>The full name.</returns>
    public override string ToString() => Name;

    /// <summary>
    /// Implicitly converts a DisplayName to its string representation (full name).
    /// </summary>
    /// <param name="displayName">The DisplayName to convert.</param>
    /// <returns>The full name as a string.</returns>
    public static implicit operator string(DisplayName displayName) => displayName.Name;

    /// <summary>
    /// Implicitly converts a string to a DisplayName.
    /// The short name will be automatically generated from the string's initials.
    /// </summary>
    /// <param name="name">The name to convert to a DisplayName.</param>
    /// <returns>A new DisplayName instance.</returns>
    public static implicit operator DisplayName(string name) => ToDisplayName(name);

    /// <summary>
    /// Converts a string to a DisplayName.
    /// The short name will be automatically generated from the string's initials.
    /// </summary>
    /// <param name="name">The name to convert.</param>
    /// <returns>A new DisplayName instance.</returns>
    public static DisplayName ToDisplayName(string name) => new(name);

    #region ISimilar

    /// <summary>
    /// Determines whether this DisplayName is similar to the specified string.
    /// Similarity is based on case-insensitive comparison of the full name.
    /// </summary>
    /// <param name="obj">The string to compare for similarity.</param>
    /// <returns>true if this DisplayName is similar to the specified string; otherwise, false.</returns>
    public bool IsSimilar(string? obj) => Name.Equals(obj, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Determines whether this DisplayName is similar to another DisplayName.
    /// Similarity is based on case-insensitive comparison of the full names.
    /// </summary>
    /// <param name="obj">The DisplayName to compare for similarity.</param>
    /// <returns>true if this DisplayName is similar to the specified DisplayName; otherwise, false.</returns>
    public bool IsSimilar(DisplayName? obj) => IsSimilar(obj?.Name);

    #endregion

    #region IComparable

    /// <summary>
    /// Compares this DisplayName with a string using case-insensitive comparison.
    /// </summary>
    /// <param name="other">The string to compare with this DisplayName.</param>
    /// <returns>A value indicating the relative order of the objects being compared.</returns>
    public int CompareTo(string? other) => string.Compare(Name, other, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Compares this DisplayName with another DisplayName using case-insensitive comparison of their full names.
    /// </summary>
    /// <param name="other">The DisplayName to compare with this DisplayName.</param>
    /// <returns>A value indicating the relative order of the objects being compared.</returns>
    public int CompareTo(DisplayName? other) => CompareTo(other?.Name);

    /// <summary>
    /// Determines whether two DisplayName instances are equal.
    /// </summary>
    /// <param name="left">The first DisplayName to compare.</param>
    /// <param name="right">The second DisplayName to compare.</param>
    /// <returns>true if the DisplayNames are equal; otherwise, false.</returns>
    public static bool operator ==(DisplayName? left, DisplayName? right) => Equals(left, right);

    /// <summary>
    /// Determines whether two DisplayName instances are not equal.
    /// </summary>
    /// <param name="left">The first DisplayName to compare.</param>
    /// <param name="right">The second DisplayName to compare.</param>
    /// <returns>true if the DisplayNames are not equal; otherwise, false.</returns>
    public static bool operator !=(DisplayName? left, DisplayName? right) => !Equals(left, right);

    /// <summary>
    /// Determines whether the first DisplayName is greater than the second DisplayName.
    /// </summary>
    /// <param name="left">The first DisplayName to compare.</param>
    /// <param name="right">The second DisplayName to compare.</param>
    /// <returns>true if the first DisplayName is greater than the second; otherwise, false.</returns>
    public static bool operator >(DisplayName left, DisplayName right) => left.CompareTo(right) > 0;

    /// <summary>
    /// Determines whether the first DisplayName is less than the second DisplayName.
    /// </summary>
    /// <param name="left">The first DisplayName to compare.</param>
    /// <param name="right">The second DisplayName to compare.</param>
    /// <returns>true if the first DisplayName is less than the second; otherwise, false.</returns>
    public static bool operator <(DisplayName left, DisplayName right) => left.CompareTo(right) < 0;

    /// <summary>
    /// Determines whether the first DisplayName is greater than or equal to the second DisplayName.
    /// </summary>
    /// <param name="left">The first DisplayName to compare.</param>
    /// <param name="right">The second DisplayName to compare.</param>
    /// <returns>true if the first DisplayName is greater than or equal to the second; otherwise, false.</returns>
    public static bool operator >=(DisplayName left, DisplayName right) => left.CompareTo(right) >= 0;

    /// <summary>
    /// Determines whether the first DisplayName is less than or equal to the second DisplayName.
    /// </summary>
    /// <param name="left">The first DisplayName to compare.</param>
    /// <param name="right">The second DisplayName to compare.</param>
    /// <returns>true if the first DisplayName is less than or equal to the second; otherwise, false.</returns>
    public static bool operator <=(DisplayName left, DisplayName right) => left.CompareTo(right) <= 0;

    #endregion
}
