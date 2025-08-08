// -----------------------------------------------------------------------
// <copyright file="TeamBase.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using MyClub.Shared.Domain.Stadiums;
using MyClub.Shared.Domain.ValueObjects;
using MyClub.Shared.Kernel.Primitives;
using MyNet.Utilities.Geography;

namespace MyClub.Shared.Domain.Teams;

/// <summary>
/// Abstract base class for team entities in the sports management system.
/// Provides common team functionality and properties that can be inherited by specific team implementations.
/// This class implements auditing capabilities and team comparison logic.
/// </summary>
/// <typeparam name="TId">The strongly-typed identifier for the team entity, which must inherit from EntityId&lt;TId&gt;.</typeparam>
[SuppressMessage("Design", "CA1036:Override methods on comparable types", Justification = "Domain entity comparison is handled through domain-specific methods")]
public abstract class TeamBase<TId> : AuditableEntity<TId>, ITeam
    where TId : EntityId<TId>
{
    [UsedImplicitly(Reason = "Used by EF Core.")]
    protected TeamBase() => DisplayName = null!;

    protected TeamBase(TId id, string name, string? shortName = null)
        : base(id) => DisplayName = new(name, shortName);

    /// <summary>
    /// Gets or sets the display name of the team, including both full name and short name.
    /// </summary>
    public DisplayName DisplayName { get; set; }

    /// <summary>
    /// Gets or sets the team's logo as a byte array.
    /// This can be null if no logo has been assigned to the team.
    /// </summary>
    public byte[]? Logo { get; set; }

    /// <summary>
    /// Gets or sets the country that this team represents or is based in.
    /// This can be null for teams that don't have a specific country association.
    /// </summary>
    public Country? Country { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the team's home stadium.
    /// This can be null if the team doesn't have a designated home stadium.
    /// </summary>
    public StadiumId? StadiumId { get; set; }

    /// <summary>
    /// Gets or sets the primary home color of the team (typically for jerseys/uniforms).
    /// This is usually represented as a hex color code or color name.
    /// </summary>
    public string? HomeColor { get; set; }

    /// <summary>
    /// Gets or sets the away color of the team (typically for away jerseys/uniforms).
    /// This is usually represented as a hex color code or color name.
    /// </summary>
    public string? AwayColor { get; set; }

    /// <summary>
    /// Returns the team's display name as the string representation.
    /// </summary>
    /// <returns>The team's full name.</returns>
    public override string ToString() => DisplayName;

    /// <summary>
    /// Compares this team with another team based on their display names.
    /// Comparison is case-insensitive and based on the full name.
    /// </summary>
    /// <param name="other">The team to compare with this team.</param>
    /// <returns>A value indicating the relative order of the teams being compared.</returns>
    public int CompareTo(ITeam? other) => DisplayName.CompareTo(other?.DisplayName);

    /// <summary>
    /// Determines whether this team is similar to another team.
    /// Similarity is based on case-insensitive comparison of the display names.
    /// </summary>
    /// <param name="obj">The team to compare for similarity.</param>
    /// <returns>true if this team is similar to the specified team; otherwise, false.</returns>
    public bool IsSimilar(ITeam? obj) => DisplayName.IsSimilar(obj?.DisplayName);

    /// <summary>
    /// Determines whether this team is similar to the specified object.
    /// The object must implement ITeam for the comparison to be meaningful.
    /// </summary>
    /// <param name="obj">The object to compare for similarity.</param>
    /// <returns>true if this team is similar to the specified object; otherwise, false.</returns>
    public bool IsSimilar(object? obj) => obj is ITeam person && IsSimilar(person);
}
