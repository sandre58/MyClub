// -----------------------------------------------------------------------
// <copyright file="UnitOfWork.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.Scorer.Infrastructure.Persistence.DbContexts;
using MyClub.Shared.Infrastructure.Persistence;
using MyClub.Shared.Kernel.Events;

namespace MyClub.Scorer.Infrastructure.Persistence;

/// <summary>
/// Unit of Work implementation for the Scorer module, managing transactional consistency
/// and domain event dispatching for football competition and match operations.
/// </summary>
/// <param name="context">The Scorer-specific database context managing football entities.</param>
/// <param name="domainEventDispatcher">Optional domain event dispatcher for publishing events after successful transactions.</param>
public sealed class UnitOfWork(ScorerDbContext context, IDomainEventDispatcher? domainEventDispatcher = null)
    : UnitOfWork<ScorerDbContext>(context, domainEventDispatcher);
