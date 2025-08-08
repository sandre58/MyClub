// -----------------------------------------------------------------------
// <copyright file="StageTeam.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using JetBrains.Annotations;
using MyClub.Scorer.Domain.StageAggregate;
using MyClub.Shared.Domain.Teams;

namespace MyClub.Scorer.Infrastructure.Persistence.JoinEntities;

/// <summary>
/// Join entity representing the many-to-many relationship between Stage entities and team references.
/// This entity enables tournament stages to manage participating teams while supporting both
/// concrete teams and virtual team references for multi-stage tournament progression.
/// </summary>
/// <param name="entityId">The identifier of the stage that contains the teams.</param>
/// <param name="team">The team reference participating in the stage.</param>
/// <remarks>
/// The StageTeam join entity facilitates team management across different tournament stages,
/// supporting complex tournament structures where teams progress from one stage to another
/// based on results and qualification criteria.
/// </remarks>
internal sealed class StageTeam(StageId entityId, TeamReference team) : EntityTeam<StageId>(entityId, team)
{
    [UsedImplicitly(Reason = "Used by EF Core.")]
    private StageTeam()
        : this(null!, null!)
    {
    }
}
