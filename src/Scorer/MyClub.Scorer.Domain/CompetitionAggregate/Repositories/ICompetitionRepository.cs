// -----------------------------------------------------------------------
// <copyright file="ICompetitionRepository.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.Shared.Kernel.Repositories;

namespace MyClub.Scorer.Domain.CompetitionAggregate.Repositories;

/// <summary>
/// Repository interface for managing Competition aggregate roots.
/// Provides specialized data access operations for Competition entities including leagues, cups, and tournaments.
/// This interface follows the Repository pattern from Domain-Driven Design and supports the aggregate root boundary.
/// </summary>
public interface ICompetitionRepository : IRepository<Competition, CompetitionId>;
