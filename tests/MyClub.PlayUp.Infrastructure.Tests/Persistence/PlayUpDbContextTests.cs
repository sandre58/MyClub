// -----------------------------------------------------------------------
// <copyright file="PlayUpDbContextTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MyClub.PlayUp.Infrastructure.Persistence;
using Xunit;

namespace MyClub.PlayUp.Infrastructure.Tests.Persistence;

public sealed class PlayUpDbContextTests
{
    [Fact]
    public void Model_is_empty_without_opening_a_connection()
    {
        var options = new DbContextOptionsBuilder<PlayUpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Database=playup_unconnected;Username=x;Password=x")
            .Options;

        using var context = new PlayUpDbContext(options);

        context.Model.GetEntityTypes().Should().BeEmpty();
        context.Database.ProviderName.Should().Be("Npgsql.EntityFrameworkCore.PostgreSQL");
    }
}
