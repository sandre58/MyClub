// -----------------------------------------------------------------------
// <copyright file="MediaDbContext.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;

namespace MyClub.Media.Infrastructure.Persistence;

/// <summary>
/// EF Core unit of work for Media metadata (PostgreSQL schema <c>media</c>).
/// </summary>
/// <param name="options">DbContext options.</param>
public sealed class MediaDbContext(DbContextOptions<MediaDbContext> options) : DbContext(options)
{
    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("media");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MediaDbContext).Assembly);
    }
}
