// -----------------------------------------------------------------------
// <copyright file="EfModelPendingChangesTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MyClub.Media.Infrastructure.Persistence;
using Xunit;

namespace MyClub.Media.Infrastructure.Tests;

public sealed class EfModelPendingChangesTests
{
    [Fact]
    public void MediaDbContext_model_matches_applied_migrations()
    {
        var options = new DbContextOptionsBuilder<MediaDbContext>()
            .UseNpgsql("Host=127.0.0.1;Database=media_unconnected;Username=x;Password=x")
            .Options;

        using var context = new MediaDbContext(options);

        context.Database.HasPendingModelChanges().Should().BeFalse(
            "run `dotnet ef migrations has-pending-model-changes --context MediaDbContext` before merging schema changes");
    }
}
