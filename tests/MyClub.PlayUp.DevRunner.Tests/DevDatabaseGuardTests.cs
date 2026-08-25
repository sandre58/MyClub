// -----------------------------------------------------------------------
// <copyright file="DevDatabaseGuardTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.DevRunner;
using Xunit;

namespace MyClub.PlayUp.DevRunner.Tests;

public sealed class DevDatabaseGuardTests
{
    [Fact]
    public void Accepts_localhost_dev_database()
    {
        var act = () => DevDatabaseGuard.ValidateForDestructiveUse(
            "Host=localhost;Port=5432;Database=myclub_dev;Username=myclub;Password=x",
            "Development");

        act.Should().NotThrow();
    }

    [Fact]
    public void Rejects_database_without_required_suffix()
    {
        var act = () => DevDatabaseGuard.ValidateForDestructiveUse(
            "Host=localhost;Port=5432;Database=myclub;Username=myclub;Password=x",
            "Development");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*_dev*");
    }

    [Fact]
    public void Rejects_non_local_host()
    {
        var act = () => DevDatabaseGuard.ValidateForDestructiveUse(
            "Host=db.example.com;Port=5432;Database=myclub_dev;Username=myclub;Password=x",
            "Development");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*local*");
    }

    [Fact]
    public void Rejects_production_environment()
    {
        var act = () => DevDatabaseGuard.ValidateForDestructiveUse(
            "Host=localhost;Port=5432;Database=myclub_dev;Username=myclub;Password=x",
            "Production");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Production*");
    }
}
