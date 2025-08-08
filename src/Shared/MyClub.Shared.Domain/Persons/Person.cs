// -----------------------------------------------------------------------
// <copyright file="Person.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using MyClub.Shared.Domain.Extensions;
using MyClub.Shared.Kernel.Primitives;
using MyNet.Utilities;
using MyNet.Utilities.Geography;

namespace MyClub.Shared.Domain.Persons;

/// <summary>
/// Abstract base class for person entities in the sports management system.
/// Provides common person functionality and properties that can be inherited by specific person implementations
/// such as players, coaches, staff members, etc. This class implements auditing capabilities and person comparison logic.
/// </summary>
/// <typeparam name="TId">The strongly-typed identifier for the person entity, which must inherit from EntityId&lt;TId&gt;.</typeparam>
[SuppressMessage("Design", "CA1036:Override methods on comparable types", Justification = "Domain entity comparison is handled through domain-specific methods")]
public abstract class Person<TId> : AuditableEntity<TId>, IPerson
    where TId : EntityId<TId>
{
    [UsedImplicitly(Reason = "Used by EF Core.")]
    protected Person() { }

    protected Person(TId id, string firstName, string lastName)
        : base(id) => Rename(firstName, lastName);

    /// <summary>
    /// Gets the last name (family name) of the person.
    /// This property is set during construction or through the Rename method.
    /// </summary>
    public string LastName { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the first name of the person.
    /// This property is set during construction or through the Rename method.
    /// </summary>
    public string FirstName { get; private set; } = string.Empty;

    /// <summary>
    /// Gets or sets the country that this person is from or represents.
    /// This can be null for persons that don't have a specific country association.
    /// </summary>
    public Country? Country { get; set; }

    /// <summary>
    /// Gets or sets the person's photo as a byte array.
    /// This can be null if no photo has been assigned to the person.
    /// </summary>
    public byte[]? Photo { get; set; }

    /// <summary>
    /// Gets or sets the gender of the person.
    /// This is typically used for categorization in sports contexts (men's/women's teams, etc.).
    /// Defaults to Male if not explicitly set.
    /// </summary>
    public GenderType Gender { get; set; } = GenderType.Male;

    /// <summary>
    /// Gets or sets the license number for the person.
    /// This is typically used for official sports licenses or registration numbers.
    /// </summary>
    public string? LicenseNumber { get; set; }

    /// <summary>
    /// Gets or sets the email address of the person.
    /// This can be null if no email address has been provided.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Changes the first and last name of the person.
    /// Both parameters are required and cannot be null or empty.
    /// </summary>
    /// <param name="firstName">The new first name of the person.</param>
    /// <param name="lastName">The new last name of the person.</param>
    /// <exception cref="ArgumentException">Thrown when firstName or lastName is null or empty.</exception>
    public void Rename(string firstName, string lastName)
    {
        FirstName = firstName.IsRequiredOrThrow();
        LastName = lastName.IsRequiredOrThrow();
    }

    /// <summary>
    /// Returns the person's name in "Last, First" format.
    /// </summary>
    /// <returns>The person's inverse name format.</returns>
    public override string ToString() => this.GetInverseName();

    /// <summary>
    /// Compares this person with another person based on their inverse names (Last, First format).
    /// Comparison is case-insensitive.
    /// </summary>
    /// <param name="other">The person to compare with this person.</param>
    /// <returns>A value indicating the relative order of the persons being compared.</returns>
    public int CompareTo(IPerson? other) => string.Compare(this.GetInverseName(), other?.GetInverseName(), StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Determines whether this person is similar to another person.
    /// Similarity is based on case-insensitive comparison of the inverse names (Last, First format).
    /// </summary>
    /// <param name="obj">The person to compare for similarity.</param>
    /// <returns>true if this person is similar to the specified person; otherwise, false.</returns>
    public bool IsSimilar(IPerson? obj) => this.GetInverseName().Equals(obj?.GetInverseName(), StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Determines whether this person is similar to the specified object.
    /// The object must implement IPerson for the comparison to be meaningful.
    /// </summary>
    /// <param name="obj">The object to compare for similarity.</param>
    /// <returns>true if this person is similar to the specified object; otherwise, false.</returns>
    public bool IsSimilar(object? obj) => obj is IPerson person && IsSimilar(person);
}
