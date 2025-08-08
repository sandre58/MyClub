// -----------------------------------------------------------------------
// <copyright file="IRoundRepository.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.Shared.Kernel.Repositories;

namespace MyClub.Scorer.Domain.RoundAggregate.Repositories;

/// <summary>
/// Repository interface for managing Round aggregate roots.
/// Provides specialized data access operations for Round entities in knockout tournaments,
/// including their fixtures, stages, and team management within elimination formats.
/// </summary>
public interface IRoundRepository : IRepository<Round, RoundId>;
