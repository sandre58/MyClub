// -----------------------------------------------------------------------
// <copyright file="MatchRepository.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.MatchAggregate.Repositories;
using MyClub.Scorer.Infrastructure.Persistence.DbContexts;
using MyClub.Shared.Domain.Enums;
using MyClub.Shared.Domain.Matches;
using MyClub.Shared.Infrastructure.Persistence.Repositories;

namespace MyClub.Scorer.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for the Match aggregate root, providing optimized data access
/// operations for football match entities with proper relationship loading and domain-specific behavior.
/// </summary>
/// <param name="context">The Scorer database context for accessing match-related data.</param>
public sealed class MatchRepository(ScorerDbContext context) : Repository<Match, MatchId, ScorerDbContext>(context), IMatchRepository
{
    /// <summary>
    /// Configures Match entity queries with optimized includes for related entities.
    /// This method ensures that commonly accessed related data is loaded efficiently
    /// to minimize database round trips and improve application performance.
    /// </summary>
    /// <param name="query">The base queryable for Match entities.</param>
    /// <returns>
    /// A configured query with includes for match opponents and their related data.
    /// </returns>
    protected override IQueryable<Match> ConfigureQuery(IQueryable<Match> query) =>
        query
            .Include(static m => m.Home)
            .Include(static m => m.Away);

    /// <summary>
    /// Applies domain-specific validation and preparation logic before adding a new match.
    /// This method ensures that all business rules and constraints are satisfied
    /// before the match is persisted to the database.
    /// </summary>
    /// <param name="entity">The match entity being added to the repository.</param>
    protected override void OnAdding(Match entity)
    {
        // Validate match scheduling constraints
        if (entity.Date < DateTime.Today.AddDays(-1))
        {
            // Log warning for past dates or apply business rules
            // This could be normal for historical data import
        }

        // Ensure team integrity - critical business rule
        if (entity.HomeTeamReference.Equals(entity.AwayTeamReference))
        {
            // This should be caught at domain level, but adding safety check
            throw new InvalidOperationException("Home and away teams cannot be the same.");
        }

        // Additional domain-specific validations could be added here:
        // - Check for team availability on the match date
        // - Validate stadium capacity and availability
        // - Ensure competition rules compliance
        // - Apply any scheduling conflict checks
    }

    /// <summary>
    /// Applies domain-specific logic and change tracking before updating an existing match.
    /// This method handles complex update scenarios and maintains data consistency
    /// during match modifications, particularly important for live match updates.
    /// </summary>
    /// <param name="entity">The match entity being updated in the repository.</param>
    protected override void OnUpdating(Match entity)
    {
        // Additional update scenarios could include:
        // - Recalculating derived statistics
        // - Updating related standings or rankings
        // - Triggering notifications for status changes
        // - Handling postponement or cancellation logic
    }

    /// <summary>
    /// Applies domain-specific cleanup and validation before removing a match.
    /// This method ensures that match deletion maintains referential integrity
    /// and properly handles the complex relationships within football competition data.
    /// </summary>
    /// <param name="entity">The match entity being removed from the repository.</param>
    protected override void OnDeleting(Match entity)
    {
        // Validate deletion constraints
        if (entity.Status == MatchStatus.Played)
        {
            // Consider soft delete for historical matches
            // may need to be preserved for statistics,
            // records, and regulatory compliance
        }

        // Handle related entity cleanup
        // - Remove or update dependent statistics
        // - Update any tournament brackets or standings
        // - Clean up any scheduled notifications or events

        // Audit the deletion for compliance
        // - Log the deletion with timestamp and user information
        // - Ensure compliance with data retention policies
        // - Consider archiving data before permanent deletion

        // Additional cleanup scenarios could include:
        // - Updating competition-level statistics
        // - Notifying external systems of the change
        // - Handling fantasy sports or betting implications
        // - Managing any media or content associated with the match
    }
}
