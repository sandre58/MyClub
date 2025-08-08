// -----------------------------------------------------------------------
// <copyright file="StadiumBase.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using MyClub.Shared.Domain.Enums;
using MyClub.Shared.Domain.ValueObjects;
using MyClub.Shared.Kernel.Primitives;
using MyNet.Utilities;
using MyNet.Utilities.Geography;

namespace MyClub.Shared.Domain.Stadiums;

/// <summary>
/// Abstract base class for stadium entities in the sports management system.
/// Provides common stadium functionality and properties that can be inherited by specific stadium implementations.
/// This class implements auditing capabilities and stadium comparison logic.
/// </summary>
/// <typeparam name="TId">The strongly-typed identifier for the stadium entity, which must inherit from EntityId&lt;TId&gt;.</typeparam>
[SuppressMessage("Design", "CA1036:Override methods on comparable types", Justification = "Domain entity comparison is handled through domain-specific methods")]
public abstract class StadiumBase<TId> : AuditableEntity<TId>, IStadium
    where TId : EntityId<TId>
{
    [UsedImplicitly(Reason = "Used by EF Core.")]
    protected StadiumBase() => DisplayName = null!;

    protected StadiumBase(TId id, string name, Ground ground)
        : base(id)
    {
        DisplayName = new(name);
        Ground = ground;
    }

    /// <summary>
    /// Gets or sets the display name of the stadium, including both full name and short name.
    /// </summary>
    public DisplayName DisplayName { get; set; }

    /// <summary>
    /// Gets or sets the type of playing surface or ground at this stadium.
    /// This indicates whether the stadium has grass, artificial grass, sand, or is an indoor facility.
    /// </summary>
    public Ground Ground { get; set; }

    /// <summary>
    /// Gets or sets the physical address of the stadium.
    /// This can be null if the address information is not available or has not been set.
    /// </summary>
    public Address? Address { get; set; }

    /// <summary>
    /// Returns a string representation of the stadium, including its name and city (if available).
    /// The format is "Stadium Name, City" or just "Stadium Name" if no address is set.
    /// </summary>
    /// <returns>A string representation of the stadium.</returns>
    public override string ToString() => string.Join(", ", new[] { DisplayName.Name, Address?.City }.NotNull());

    /// <summary>
    /// Compares this stadium with another stadium based on their display names.
    /// Comparison is case-insensitive and based on the full name.
    /// </summary>
    /// <param name="other">The stadium to compare with this stadium.</param>
    /// <returns>A value indicating the relative order of the stadiums being compared.</returns>
    public int CompareTo(IStadium? other) => DisplayName.CompareTo(other?.DisplayName);

    /// <summary>
    /// Determines whether this stadium is similar to another stadium.
    /// Similarity is based on case-insensitive comparison of the display names.
    /// </summary>
    /// <param name="obj">The stadium to compare for similarity.</param>
    /// <returns>true if this stadium is similar to the specified stadium; otherwise, false.</returns>
    public bool IsSimilar(IStadium? obj) => DisplayName.IsSimilar(obj?.DisplayName);

    /// <summary>
    /// Determines whether this stadium is similar to the specified object.
    /// The object must implement IStadium for the comparison to be meaningful.
    /// </summary>
    /// <param name="obj">The object to compare for similarity.</param>
    /// <returns>true if this stadium is similar to the specified object; otherwise, false.</returns>
    public bool IsSimilar(object? obj) => obj is IStadium person && IsSimilar(person);
}
