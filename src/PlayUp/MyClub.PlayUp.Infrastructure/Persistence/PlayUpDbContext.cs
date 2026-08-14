// -----------------------------------------------------------------------
// <copyright file="PlayUpDbContext.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using MyClub.PlayUp.Application.Abstractions;

namespace MyClub.PlayUp.Infrastructure.Persistence;

/// <summary>
/// Play'up EF Core unit of work for Competition persistence.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="PlayUpDbContext"/> class.
/// </remarks>
/// <param name="options">The options for this context.</param>
public sealed class PlayUpDbContext(DbContextOptions<PlayUpDbContext> options) : DbContext(options), IUnitOfWork
{
    /// <inheritdoc />
    Task IUnitOfWork.SaveChangesAsync(CancellationToken cancellationToken) => SaveChangesAsync(cancellationToken);

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.ApplyConfigurationsFromAssembly(typeof(PlayUpDbContext).Assembly);
}
