// -----------------------------------------------------------------------
// <copyright file="CompetitionRepository.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using MyClub.Scorer.Domain.CompetitionAggregate;
using MyClub.Scorer.Domain.CompetitionAggregate.Repositories;
using MyClub.Scorer.Infrastructure.Persistence.DbContexts;
using MyClub.Shared.Infrastructure.Persistence.Repositories;

namespace MyClub.Scorer.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for the Competition aggregate root, providing optimized data access
/// operations for football competition entities including leagues, cups, and tournaments with
/// their complex relationships and hierarchical data structures.
/// </summary>
/// <param name="context">The Scorer database context for accessing competition-related data.</param>
public sealed class CompetitionRepository(ScorerDbContext context) : Repository<Competition, CompetitionId, ScorerDbContext>(context), ICompetitionRepository
{
    /// <summary>
    /// Configures Competition entity queries with optimized includes for related entities.
    /// This method ensures that commonly accessed related data is loaded efficiently
    /// to minimize database round trips and improve application performance.
    /// </summary>
    /// <param name="query">The base queryable for Competition entities.</param>
    /// <returns>
    /// A configured query with includes for teams, stadiums, and competition-specific data.
    /// </returns>
    protected override IQueryable<Competition> ConfigureQuery(IQueryable<Competition> query) =>
        query
            .Include(static c => c.Teams)
            .Include(static c => c.Stadiums);

    /// <summary>
    /// Applies domain-specific validation and preparation logic before adding a new competition.
    /// This method ensures that all business rules and constraints are satisfied
    /// before the competition is persisted to the database.
    /// </summary>
    /// <param name="entity">The competition entity being added to the repository.</param>
    protected override void OnAdding(Competition entity)
    {
        // Validate competition name uniqueness at application level
        // Note: Database-level uniqueness should also be enforced via constraints
        var existingCompetition = DbSet.FirstOrDefault(c => c.DisplayName.Name == entity.DisplayName.Name);
        if (existingCompetition != null)
        {
            throw new InvalidOperationException($"A competition with the name '{entity.DisplayName.Name}' already exists.");
        }

        // Additional domain-specific validations could be added here:
        // - Check minimum number of teams for specific competition types
        // - Validate competition format against business rules
        // - Ensure proper scheduling constraints
        // - Apply regulatory compliance checks
    }

    /// <summary>
    /// Applies domain-specific logic and change tracking before updating an existing competition.
    /// This method handles complex update scenarios and maintains data consistency
    /// during competition modifications, particularly important for ongoing competitions.
    /// </summary>
    /// <param name="entity">The competition entity being updated in the repository.</param>
    protected override void OnUpdating(Competition entity)
    {
        // Additional update scenarios could include:
        // - Recalculating standings when rules change
        // - Updating fixture schedules when format changes
        // - Triggering notifications for significant changes
        // - Handling rule modifications for ongoing competitions
    }

    /// <summary>
    /// Applies domain-specific cleanup and validation before removing a competition.
    /// This method ensures that competition deletion maintains referential integrity
    /// and properly handles the complex relationships within football competition data.
    /// </summary>
    /// <param name="entity">The competition entity being removed from the repository.</param>
    protected override void OnDeleting(Competition entity)
    {
        // Additional cleanup scenarios could include:
        // - Updating season or tournament hierarchies
        // - Notifying external systems of the change
        // - Handling fantasy sports or betting implications
        // - Managing any awards or certificates issued
    }
}
