// -----------------------------------------------------------------------
// <copyright file="EntityMatch.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Shared.Domain.Matches;
using MyClub.Shared.Infrastructure.Persistence.JoinEntities;
using MyClub.Shared.Kernel.Primitives;

namespace MyClub.Scorer.Infrastructure.Persistence.JoinEntities;

/// <summary>
/// Abstract base class for join entities representing relationships between domain entities and match entities.
/// This class provides a standardized pattern for associating matches with various container entities
/// such as matchdays, rounds, stages, and competitions.
/// </summary>
/// <typeparam name="T">The entity type that contains or organizes matches.</typeparam>
/// <typeparam name="TId">The strongly-typed identifier for the container entity.</typeparam>
/// <param name="entity">The container entity that organizes matches.</param>
/// <param name="match">The match entity being associated with the container.</param>
internal abstract class EntityMatch<T, TId>(T entity, Match match) : EntityLink<T, Match, TId, MatchId>(entity, match, entity.Id, match.Id)
    where T : Entity<TId>
    where TId : EntityId<TId>;
