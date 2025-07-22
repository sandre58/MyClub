// -----------------------------------------------------------------------
// <copyright file="DisplayName.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using MyNet.Utilities;

namespace MyClub.Shared.Domain.ValueObjects;

public class DisplayName(string name, string? shortName = null) : ValueObject, ISimilar<DisplayName>, IComparable<DisplayName>, IComparable<string>, ISimilar<string>
{
    public string Name { get; } = name.IsRequiredOrThrow();

    public string ShortName { get; } = shortName ?? name.GetInitials();

    public override bool Equals(object? obj) => base.Equals(obj);

    public override int GetHashCode() => Name.GetHashCode(StringComparison.OrdinalIgnoreCase);

    public override string ToString() => Name;

    public static implicit operator string(DisplayName displayName) => displayName.Name;

    public static implicit operator DisplayName(string name) => ToDisplayName(name);

    public static DisplayName ToDisplayName(string name) => new(name);

    #region ISimilar

    public virtual bool IsSimilar(string? obj) => Name.Equals(obj, StringComparison.OrdinalIgnoreCase);

    public virtual bool IsSimilar(DisplayName? obj) => IsSimilar(obj?.Name);

    #endregion

    #region IComparable

    public int CompareTo(string? other) => string.Compare(Name, other, StringComparison.OrdinalIgnoreCase);

    public int CompareTo(DisplayName? other) => CompareTo(other?.Name);

    public static bool operator ==(DisplayName? left, DisplayName? right) => Equals(left, right);

    public static bool operator !=(DisplayName? left, DisplayName? right) => !Equals(left, right);

    public static bool operator >(DisplayName left, DisplayName right) => left.CompareTo(right) > 0;

    public static bool operator <(DisplayName left, DisplayName right) => left.CompareTo(right) < 0;

    public static bool operator >=(DisplayName left, DisplayName right) => left.CompareTo(right) >= 0;

    public static bool operator <=(DisplayName left, DisplayName right) => left.CompareTo(right) <= 0;

    #endregion
}
