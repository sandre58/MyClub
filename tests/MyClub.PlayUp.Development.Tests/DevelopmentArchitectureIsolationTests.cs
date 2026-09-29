// -----------------------------------------------------------------------
// <copyright file="DevelopmentArchitectureIsolationTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Xml.Linq;
using FluentAssertions;
using Xunit;

namespace MyClub.PlayUp.Development.Tests;

/// <summary>
/// Lot D: Development consumes TestKit; never the reverse.
/// </summary>
public sealed class DevelopmentArchitectureIsolationTests
{
    [Fact]
    public void Development_csproj_references_TestKit_and_not_Infrastructure_or_Host()
    {
        var csproj = LocateDevelopmentCsproj();
        var references = XDocument.Load(csproj)
            .Descendants("ProjectReference")
            .Select(static e => (string?)e.Attribute("Include") ?? string.Empty)
            .ToArray();

        references.Should().Contain(static path =>
            path.Contains("MyClub.PlayUp.TestKit", StringComparison.OrdinalIgnoreCase));
        references.Should().Contain(static path =>
            path.Contains("MyClub.PlayUp.Domain", StringComparison.OrdinalIgnoreCase));
        references.Should().Contain(static path =>
            path.Contains("MyClub.PlayUp.Application", StringComparison.OrdinalIgnoreCase));
        references.Should().NotContain(static path =>
            path.Contains("MyClub.PlayUp.Infrastructure", StringComparison.OrdinalIgnoreCase));
        references.Should().NotContain(static path =>
            path.Contains("MyClub.PlayUp.Host", StringComparison.OrdinalIgnoreCase));
        references.Should().NotContain(static path =>
            path.Contains("MyClub.PlayUp.DevRunner", StringComparison.OrdinalIgnoreCase));
    }

    private static string LocateDevelopmentCsproj()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(
                dir.FullName,
                "src",
                "PlayUp",
                "MyClub.PlayUp.Development",
                "MyClub.PlayUp.Development.csproj");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not locate MyClub.PlayUp.Development.csproj from test output.");
    }
}
