// -----------------------------------------------------------------------
// <copyright file="EntityTeam.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.Shared.Domain.Teams;
using MyClub.Shared.Infrastructure.Persistence.JoinEntities;
using MyClub.Shared.Kernel.Primitives;

namespace MyClub.Scorer.Infrastructure.Persistence.JoinEntities;

/// <summary>
/// Abstract base class for join entities representing relationships between domain entities and team references.
/// This class specializes the Link pattern for team assignments, supporting both concrete teams
/// and virtual team references in complex tournament scenarios.
/// </summary>
/// <typeparam name="TId">The strongly-typed identifier for the entity that has team relationships.</typeparam>
/// <param name="entityId">The identifier of the entity that contains or references teams.</param>
/// <param name="team">The team reference, which can be concrete or virtual (e.g., "Winner of Match A").</param>
internal abstract class EntityTeam<TId>(TId entityId, TeamReference team) : Link<TId, TeamReference>(entityId, team)
    where TId : EntityId<TId>;
