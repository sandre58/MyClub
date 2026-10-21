// -----------------------------------------------------------------------
// <copyright file="DevelopmentTestsArchitectureIsolationTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Xml.Linq;
using FluentAssertions;
using Xunit;

namespace MyClub.PlayUp.Development.Tests;

/// <summary>
/// Integration test project composition — Infrastructure allowed; Host/DevRunner forbidden (AUD-0011 / lot C8).
/// </summary>
public sealed class DevelopmentTestsArchitectureIsolationTests
{
    [Fact]
    public void DevelopmentTests_csproj_references_Infrastructure_for_postgres_recette()
    {
        var references = LoadProjectReferences(LocateDevelopmentTestsCsproj());

        references.Should().Contain(static path =>
            path.Contains("MyClub.PlayUp.Infrastructure", StringComparison.OrdinalIgnoreCase));
        references.Should().Contain(static path =>
            path.Contains("MyClub.PlayUp.Development", StringComparison.OrdinalIgnoreCase));
        references.Should().NotContain(static path =>
            path.Contains("MyClub.PlayUp.Host", StringComparison.OrdinalIgnoreCase));
        references.Should().NotContain(static path =>
            path.Contains("MyClub.PlayUp.DevRunner", StringComparison.OrdinalIgnoreCase));
    }

    private static string[] LoadProjectReferences(string csprojPath) =>
        [.. XDocument.Load(csprojPath)
            .Descendants("ProjectReference")
            .Select(static e => (string?)e.Attribute("Include") ?? string.Empty)];

    private static string LocateDevelopmentTestsCsproj()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(
                dir.FullName,
                "tests",
                "MyClub.PlayUp.Development.Tests",
                "MyClub.PlayUp.Development.Tests.csproj");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not locate MyClub.PlayUp.Development.Tests.csproj from test output.");
    }
}
