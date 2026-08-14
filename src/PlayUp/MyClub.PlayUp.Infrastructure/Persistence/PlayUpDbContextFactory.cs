// -----------------------------------------------------------------------
// <copyright file="PlayUpDbContextFactory.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MyClub.PlayUp.Infrastructure.Persistence;

/// <summary>
/// Design-time factory used by <c>dotnet ef</c> to generate and apply migrations.
/// </summary>
public sealed class PlayUpDbContextFactory : IDesignTimeDbContextFactory<PlayUpDbContext>
{
    /// <inheritdoc />
    public PlayUpDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("PLAYUP_CONNECTION_STRING")
            ?? "Host=127.0.0.1;Database=playup;Username=playup;Password=playup";

        var options = new DbContextOptionsBuilder<PlayUpDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new PlayUpDbContext(options);
    }
}
