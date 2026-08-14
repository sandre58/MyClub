// -----------------------------------------------------------------------
// <copyright file="PlayUpInMemory.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using MyClub.PlayUp.Infrastructure.Persistence;
using MyClub.PlayUp.Infrastructure.Persistence.SaveInterceptors;

namespace MyClub.PlayUp.Infrastructure.Tests.Persistence;

internal static class PlayUpInMemory
{
    internal static PlayUpDbContext CreateContext(string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<PlayUpDbContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
            .AddInterceptors(new CompetitionOrderedCollectionsInterceptor())
            .Options;

        return new PlayUpDbContext(options);
    }
}
