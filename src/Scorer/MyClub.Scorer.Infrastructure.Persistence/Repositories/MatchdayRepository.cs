// -----------------------------------------------------------------------
// <copyright file="MatchdayRepository.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.Scorer.Domain.MatchdayAggregate;
using MyClub.Scorer.Domain.MatchdayAggregate.Repositories;
using MyClub.Scorer.Infrastructure.Persistence.DbContexts;
using MyClub.Shared.Infrastructure.Persistence.Repositories;

namespace MyClub.Scorer.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for the Matchday aggregate root, providing optimized data access
/// operations for matchday entities that organize collections of matches into structured rounds
/// and fixture schedules within football competitions.
/// </summary>
/// <param name="context">The Scorer database context for accessing matchday-related data.</param>
public sealed class MatchdayRepository(ScorerDbContext context) : Repository<Matchday, MatchdayId, ScorerDbContext>(context), IMatchdayRepository;
