// -----------------------------------------------------------------------
// <copyright file="EfModelPendingChangesTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MyClub.PlayUp.Infrastructure.Persistence;
using Xunit;

namespace MyClub.PlayUp.Infrastructure.Tests.Persistence;

public sealed class EfModelPendingChangesTests
{
    [Fact]
    public void PlayUpDbContext_model_matches_applied_migrations()
    {
        var options = new DbContextOptionsBuilder<PlayUpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Database=myclub_unconnected;Username=x;Password=x")
            .Options;

        using var context = new PlayUpDbContext(options);

        context.Database.HasPendingModelChanges().Should().BeFalse(
            "run `dotnet ef migrations has-pending-model-changes --context PlayUpDbContext` before merging schema changes");
    }
}
