// -----------------------------------------------------------------------
// <copyright file="RoundRepository.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.Scorer.Domain.RoundAggregate;
using MyClub.Scorer.Domain.RoundAggregate.Repositories;
using MyClub.Scorer.Infrastructure.Persistence.DbContexts;
using MyClub.Shared.Infrastructure.Persistence.Repositories;

namespace MyClub.Scorer.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for the Round aggregate root, providing optimized data access
/// operations for tournament round entities including elimination rounds, fixtures, and
/// team progression management within knockout tournament structures.
/// </summary>
/// <param name="context">The Scorer database context for accessing round-related data.</param>
public sealed class RoundRepository(ScorerDbContext context)
    : Repository<Round, RoundId, ScorerDbContext>(context), IRoundRepository;
