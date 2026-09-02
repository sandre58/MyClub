// -----------------------------------------------------------------------
// <copyright file="DatasetCatalogTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Development.Datasets;
using MyClub.PlayUp.Development.Generators;
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

    [Fact]
    public void Squad_catalog_covers_every_dataset_team()
    {
        var datasets = DatasetCatalog.LoadFromAssembly(typeof(DatasetCatalog).Assembly);
        var squads = SquadCatalog.LoadFromAssembly(typeof(SquadCatalog).Assembly);
        var missing = datasets.All
            .SelectMany(dataset => dataset.Teams.Select(team => team.DisplayName))
            .Distinct(StringComparer.Ordinal)
            .Where(name => !squads.TryGet(name, out _))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        missing.Should().BeEmpty("every inspired dataset club should have a real squad overlay");
    }
}
