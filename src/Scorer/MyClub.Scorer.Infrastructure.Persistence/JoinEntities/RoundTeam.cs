// -----------------------------------------------------------------------
// <copyright file="RoundTeam.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using JetBrains.Annotations;
using MyClub.Scorer.Domain.RoundAggregate;
using MyClub.Shared.Domain.Teams;

namespace MyClub.Scorer.Infrastructure.Persistence.JoinEntities;

/// <summary>
/// Join entity representing the many-to-many relationship between Round entities and team references.
/// This entity enables tournament rounds to track participating teams while supporting both
/// concrete teams and virtual team references for knockout tournament progression.
/// </summary>
/// <param name="entityId">The identifier of the round that contains the teams.</param>
/// <param name="team">The team reference participating in the round.</param>
/// <remarks>
/// The RoundTeam join entity is essential for knockout tournament management, allowing rounds
/// to maintain lists of participating teams while supporting virtual references that enable
/// tournament brackets to be completely defined before all teams are determined.
/// </remarks>
internal sealed class RoundTeam(RoundId entityId, TeamReference team) : EntityTeam<RoundId>(entityId, team)
{
    [UsedImplicitly(Reason = "Used by EF Core.")]
    private RoundTeam()
        : this(null!, null!)
    {
    }
}
