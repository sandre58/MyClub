// -----------------------------------------------------------------------
// <copyright file="Reference.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using MyNet.Utilities;

namespace MyClub.Shared.Domain.ValueObjects;

/// <summary>
/// Value object representing a reference item with a label, code, description, and ordering.
/// This is commonly used for lookup tables, categories, and other reference data where
/// you need both a display label and an internal code for identification.
/// </summary>
/// <param name="label">The display label for the reference. This parameter is required.</param>
/// <param name="code">The internal code for the reference. If not provided, it will be automatically generated from the label's initials.</param>
/// <param name="description">An optional description providing additional details about the reference.</param>
/// <param name="order">The sort order for this reference. If not provided, defaults to 0.</param>
public class Reference(string label, string? code = null, string? description = null, int? order = null) : ValueObject, ISimilar<Reference>, IComparable<Reference>, IComparable<string>
{
    /// <summary>
    /// Gets the internal code for this reference.
    /// If no code was provided during construction, this is automatically generated from the label's initials.
    /// </summary>
    public string Code { get; } = code ?? label.GetInitials();

    /// <summary>
    /// Gets the display label for this reference.
    /// This is the primary text shown to users.
    /// </summary>
    public string Label { get; private set; } = label;

    /// <summary>
    /// Gets the optional description providing additional details about this reference.
    /// This can be null if no description was provided.
    /// </summary>
    public string? Description { get; } = description;

    /// <summary>
    /// Gets or sets the sort order for this reference.
    /// Lower values appear first when sorting. Defaults to 0 if not specified.
    /// </summary>
    public int Order { get; set; } = order ?? 0;

    /// <summary>
    /// Determines whether the specified object is equal to the current Reference.
    /// Uses the base ValueObject equality comparison.
    /// </summary>
    /// <param name="obj">The object to compare with the current Reference.</param>
    /// <returns>true if the specified object is equal to the current Reference; otherwise, false.</returns>
    public override bool Equals(object? obj) => base.Equals(obj);

    /// <summary>
    /// Returns the hash code for this Reference based on the Label property.
    /// Uses case-insensitive hashing for consistency.
    /// </summary>
    /// <returns>A hash code for the current Reference.</returns>
    public override int GetHashCode() => Label.GetHashCode(StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Returns the label as the string representation of this Reference.
    /// </summary>
    /// <returns>The display label.</returns>
    public override string ToString() => Label;

    /// <summary>
    /// Implicitly converts a Reference to its string representation (label).
    /// </summary>
    /// <param name="reference">The Reference to convert.</param>
    /// <returns>The label as a string.</returns>
    public static implicit operator string(Reference reference) => reference.Label;

    /// <summary>
    /// Implicitly converts a string to a Reference.
    /// The code will be automatically generated from the string's initials.
    /// </summary>
    /// <param name="label">The label to convert to a Reference.</param>
    /// <returns>A new Reference instance.</returns>
    public static implicit operator Reference(string label) => ToReference(label);

    /// <summary>
    /// Converts a string to a Reference.
    /// The code will be automatically generated from the string's initials.
    /// </summary>
    /// <param name="label">The label to convert.</param>
    /// <returns>A new Reference instance.</returns>
    public static Reference ToReference(string label) => new(label);

    #region ISimilar

    /// <summary>
    /// Determines whether this Reference is similar to the specified string.
    /// Similarity is based on case-insensitive comparison of the label.
    /// </summary>
    /// <param name="obj">The string to compare for similarity.</param>
    /// <returns>true if this Reference is similar to the specified string; otherwise, false.</returns>
    public virtual bool IsSimilar(string? obj) => Label.Equals(obj, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Determines whether this Reference is similar to another Reference.
    /// Similarity is based on case-insensitive comparison of the labels.
    /// </summary>
    /// <param name="obj">The Reference to compare for similarity.</param>
    /// <returns>true if this Reference is similar to the specified Reference; otherwise, false.</returns>
    public virtual bool IsSimilar(Reference? obj) => IsSimilar(obj?.Label);

    #endregion

    #region IComparable

    /// <summary>
    /// Compares this Reference with a string using case-insensitive comparison of the label.
    /// </summary>
    /// <param name="other">The string to compare with this Reference.</param>
    /// <returns>A value indicating the relative order of the objects being compared.</returns>
    public int CompareTo(string? other) => string.Compare(Label, other, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Compares this Reference with another Reference using case-insensitive comparison of their labels.
    /// </summary>
    /// <param name="other">The Reference to compare with this Reference.</param>
    /// <returns>A value indicating the relative order of the objects being compared.</returns>
    public int CompareTo(Reference? other) => CompareTo(other?.Label);

    /// <summary>
    /// Determines whether two Reference instances are equal.
    /// </summary>
    /// <param name="left">The first Reference to compare.</param>
    /// <param name="right">The second Reference to compare.</param>
    /// <returns>true if the References are equal; otherwise, false.</returns>
    public static bool operator ==(Reference? left, Reference? right) => Equals(left, right);

    /// <summary>
    /// Determines whether two Reference instances are not equal.
    /// </summary>
    /// <param name="left">The first Reference to compare.</param>
    /// <param name="right">The second Reference to compare.</param>
    /// <returns>true if the References are not equal; otherwise, false.</returns>
    public static bool operator !=(Reference? left, Reference? right) => !Equals(left, right);

    /// <summary>
    /// Determines whether the first Reference is greater than the second Reference.
    /// </summary>
    /// <param name="left">The first Reference to compare.</param>
    /// <param name="right">The second Reference to compare.</param>
    /// <returns>true if the first Reference is greater than the second; otherwise, false.</returns>
    public static bool operator >(Reference left, Reference right) => left.CompareTo(right) > 0;

    /// <summary>
    /// Determines whether the first Reference is less than the second Reference.
    /// </summary>
    /// <param name="left">The first Reference to compare.</param>
    /// <param name="right">The second Reference to compare.</param>
    /// <returns>true if the first Reference is less than the second; otherwise, false.</returns>
    public static bool operator <(Reference left, Reference right) => left.CompareTo(right) < 0;

    /// <summary>
    /// Determines whether the first Reference is greater than or equal to the second Reference.
    /// </summary>
    /// <param name="left">The first Reference to compare.</param>
    /// <param name="right">The second Reference to compare.</param>
    /// <returns>true if the first Reference is greater than or equal to the second; otherwise, false.</returns>
    public static bool operator >=(Reference left, Reference right) => left.CompareTo(right) >= 0;

    /// <summary>
    /// Determines whether the first Reference is less than or equal to the second Reference.
    /// </summary>
    /// <param name="left">The first Reference to compare.</param>
    /// <param name="right">The second Reference to compare.</param>
    /// <returns>true if the first Reference is less than or equal to the second; otherwise, false.</returns>
    public static bool operator <=(Reference left, Reference right) => left.CompareTo(right) <= 0;

    #endregion
}
