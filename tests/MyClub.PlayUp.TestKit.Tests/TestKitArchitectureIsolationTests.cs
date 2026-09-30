// -----------------------------------------------------------------------
// <copyright file="TestKitArchitectureIsolationTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Xml.Linq;
using FluentAssertions;
using Xunit;

namespace MyClub.PlayUp.TestKit.Tests;

/// <summary>
/// Enforce TestKit dependency boundary (Domain + Application only).
/// </summary>
public sealed class TestKitArchitectureIsolationTests
{
    [Fact]
    public void TestKit_csproj_references_only_Domain_and_Application()
    {
        var csproj = LocateTestKitCsproj();
        var references = XDocument.Load(csproj)
            .Descendants("ProjectReference")
            .Select(static e => (string?)e.Attribute("Include") ?? string.Empty)
            .ToArray();

        references.Should().HaveCount(2);
        references.Should().Contain(static path =>
            path.Contains("MyClub.PlayUp.Domain", StringComparison.OrdinalIgnoreCase));
        references.Should().Contain(static path =>
            path.Contains("MyClub.PlayUp.Application", StringComparison.OrdinalIgnoreCase));
        references.Should().NotContain(static path =>
            path.Contains("MyClub.PlayUp.Development", StringComparison.OrdinalIgnoreCase));
        references.Should().NotContain(static path =>
            path.Contains("MyClub.PlayUp.DevRunner", StringComparison.OrdinalIgnoreCase));
        references.Should().NotContain(static path =>
            path.Contains("MyClub.PlayUp.Infrastructure", StringComparison.OrdinalIgnoreCase));
        references.Should().NotContain(static path =>
            path.Contains("MyClub.Media", StringComparison.OrdinalIgnoreCase));
        references.Should().NotContain(static path =>
            path.Contains("MyClub.PlayUp.Host", StringComparison.OrdinalIgnoreCase));
    }

    private static string LocateTestKitCsproj()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(
                dir.FullName,
                "src",
                "PlayUp",
                "MyClub.PlayUp.TestKit",
                "MyClub.PlayUp.TestKit.csproj");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not locate MyClub.PlayUp.TestKit.csproj from test output.");
    }
}
