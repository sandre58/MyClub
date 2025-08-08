// -----------------------------------------------------------------------
// <copyright file="Stadium.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using JetBrains.Annotations;
using MyClub.Shared.Domain.Enums;
using MyClub.Shared.Domain.Stadiums;
using MyClub.Shared.Kernel.Primitives;

namespace MyClub.Scorer.Domain.CompetitionAggregate.Stadiums;

/// <summary>
/// Represents a football stadium entity within the Scorer domain.
/// A stadium serves as a venue for matches and can be associated with teams as their home ground
/// or used as neutral venues for competition matches.
/// </summary>
public class Stadium : StadiumBase<StadiumId>, IAggregateRoot
{
    [UsedImplicitly(Reason = "Used by EF Core.")]
    private Stadium() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="Stadium"/> class with the specified parameters.
    /// </summary>
    /// <param name="id">The unique identifier for the stadium.</param>
    /// <param name="name">The name of the stadium.</param>
    /// <param name="ground">The type of playing surface (grass, artificial, etc.).</param>
    private Stadium(StadiumId id, string name, Ground ground)
        : base(id, name, ground) { }

    /// <summary>
    /// Creates a new stadium with the specified name and ground type.
    /// </summary>
    /// <param name="name">The name of the stadium.</param>
    /// <param name="ground">The type of playing surface for the stadium.</param>
    /// <returns>A new <see cref="Stadium"/> instance.</returns>
    public static Stadium Create(string name, Ground ground) => new(StadiumId.New(), name, ground);
}
