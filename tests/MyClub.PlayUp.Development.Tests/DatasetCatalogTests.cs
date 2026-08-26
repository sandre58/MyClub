// -----------------------------------------------------------------------
// <copyright file="DatasetCatalogTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Development.Datasets;
using Xunit;

namespace MyClub.PlayUp.Development.Tests;

public sealed class DatasetCatalogTests
{
    [Fact]
    public void Embedded_datasets_load_expected_keys_and_counts()
    {
        var catalog = DatasetCatalog.LoadFromAssembly(typeof(DatasetCatalog).Assembly);
        catalog.Get("ligue-1").Teams.Should().HaveCount(18);
        catalog.Get("champions-league").Teams.Should().HaveCount(32);
        catalog.Get("world-cup").Teams.Should().HaveCount(32);
        catalog.Get("coupe-de-france").Teams.Should().HaveCount(32);
    }
}
