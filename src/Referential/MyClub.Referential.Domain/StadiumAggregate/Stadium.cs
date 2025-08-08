// -----------------------------------------------------------------------
// <copyright file="Stadium.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using JetBrains.Annotations;
using MyClub.Shared.Domain.Enums;
using MyClub.Shared.Domain.Stadiums;
using MyClub.Shared.Kernel.Primitives;

namespace MyClub.Referential.Domain.StadiumAggregate;

/// <summary>
/// Represents a football stadium as a reference entity within the MyClub ecosystem,
/// providing core venue identification and ground surface information for use across multiple modules.
/// This aggregate root maintains stadium reference data that can be shared between different
/// functional areas such as competition management, match organization, and team administration.
/// </summary>
public class Stadium : StadiumBase<StadiumId>, IAggregateRoot
{
    [UsedImplicitly(Reason = "Used by EF Core.")]
    private Stadium() { }

    private Stadium(StadiumId id, string name, Ground ground)
        : base(id, name, ground) { }

    /// <summary>
    /// Creates a new Stadium instance with the specified name and ground surface,
    /// generating a new unique identifier and ensuring proper entity initialization.
    /// </summary>
    /// <param name="name">The name of the stadium. Cannot be null or empty.</param>
    /// <param name="ground">The type of playing surface at this stadium.</param>
    /// <returns>A new Stadium instance with a generated unique identifier.</returns>
    public static Stadium Create(string name, Ground ground) => new(StadiumId.New(), name, ground);
}
