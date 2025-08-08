// -----------------------------------------------------------------------
// <copyright file="StageRepository.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.Scorer.Domain.StageAggregate;
using MyClub.Scorer.Domain.StageAggregate.Repositories;
using MyClub.Scorer.Infrastructure.Persistence.DbContexts;
using MyClub.Shared.Infrastructure.Persistence.Repositories;

namespace MyClub.Scorer.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for the Stage aggregate root, providing optimized data access
/// operations for tournament stage entities including group stages, knockout stages, and
/// championship stages within complex multiphase tournament competitions.
/// </summary>
/// <param name="context">The Scorer database context for accessing stage-related data.</param>
public sealed class StageRepository(ScorerDbContext context)
    : Repository<Stage, StageId, ScorerDbContext>(context), IStageRepository;
