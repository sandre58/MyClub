// -----------------------------------------------------------------------
// <copyright file="Reference.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using MyNet.Utilities;

namespace MyClub.Shared.Domain.ValueObjects;

public class Reference(string label, string? code = null, string? description = null, int? order = null) : ValueObject, ISimilar<Reference>, IComparable<Reference>, IComparable<string>, ISimilar<string>
{
    public string Code { get; } = code ?? label.GetInitials();

    public string Label { get; private set; } = label;

    public string? Description { get; } = description;

    public int Order { get; set; } = order ?? 0;

    public override bool Equals(object? obj) => base.Equals(obj);

    public override int GetHashCode() => Label.GetHashCode(StringComparison.OrdinalIgnoreCase);

    public override string ToString() => Label;

    public static implicit operator string(Reference reference) => reference.Label;

    public static implicit operator Reference(string label) => ToReference(label);

    public static Reference ToReference(string label) => new(label);

    #region ISimilar

    public virtual bool IsSimilar(string? obj) => Label.Equals(obj, StringComparison.OrdinalIgnoreCase);

    public virtual bool IsSimilar(Reference? obj) => IsSimilar(obj?.Label);

    #endregion

    #region IComparable

    public int CompareTo(string? other) => string.Compare(Label, other, StringComparison.OrdinalIgnoreCase);

    public int CompareTo(Reference? other) => CompareTo(other?.Label);

    public static bool operator ==(Reference? left, Reference? right) => Equals(left, right);

    public static bool operator !=(Reference? left, Reference? right) => !Equals(left, right);

    public static bool operator >(Reference left, Reference right) => left.CompareTo(right) > 0;

    public static bool operator <(Reference left, Reference right) => left.CompareTo(right) < 0;

    public static bool operator >=(Reference left, Reference right) => left.CompareTo(right) >= 0;

    public static bool operator <=(Reference left, Reference right) => left.CompareTo(right) <= 0;

    #endregion
}
