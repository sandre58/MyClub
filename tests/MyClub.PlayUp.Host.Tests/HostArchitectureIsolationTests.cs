// -----------------------------------------------------------------------
// <copyright file="HostArchitectureIsolationTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Xml.Linq;
using FluentAssertions;
using Xunit;

namespace MyClub.PlayUp.Host.Tests;

public sealed class HostArchitectureIsolationTests
{
    [Fact]
    public void Host_csproj_does_not_reference_Development_DevRunner_or_TestKit()
    {
        var hostCsproj = LocateHostCsproj();
        var document = XDocument.Load(hostCsproj);
        var references = document
            .Descendants("ProjectReference")
            .Select(static e => (string?)e.Attribute("Include") ?? string.Empty)
            .ToArray();

        references.Should().NotBeEmpty();
        references.Should().NotContain(static path =>
            path.Contains("MyClub.PlayUp.Development", StringComparison.OrdinalIgnoreCase));
        references.Should().NotContain(static path =>
            path.Contains("MyClub.PlayUp.DevRunner", StringComparison.OrdinalIgnoreCase));
        references.Should().NotContain(static path =>
            path.Contains("MyClub.PlayUp.TestKit", StringComparison.OrdinalIgnoreCase));
        references.Should().Contain(static path =>
            path.Contains("MyClub.PlayUp.Application", StringComparison.OrdinalIgnoreCase));
        references.Should().Contain(static path =>
            path.Contains("MyClub.PlayUp.Infrastructure", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Host_source_does_not_mention_Development_workspace_types()
    {
        var hostDir = Path.GetDirectoryName(LocateHostCsproj())
                      ?? throw new InvalidOperationException("Host project directory not found.");
        var sources = Directory.EnumerateFiles(hostDir, "*.cs", SearchOption.AllDirectories)
            .Where(static path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                                  && !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal));

        foreach (var source in sources)
        {
            var text = File.ReadAllText(source);
            text.Should().NotContain("MyClub.PlayUp.Development", because: source);
            text.Should().NotContain("MyClub.PlayUp.TestKit", because: source);
            text.Should().NotContain("DevelopmentWorkspace", because: source);
            text.Should().NotContain("PersistenceMode", because: source);
            text.Should().NotContain("AddPlayUpInMemoryPersistence", because: source);
            text.Should().NotContain("ScenarioRunner", because: source);
        }
    }

    private static string LocateHostCsproj()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "src", "PlayUp", "MyClub.PlayUp.Host", "MyClub.PlayUp.Host.csproj");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not locate MyClub.PlayUp.Host.csproj from test output.");
    }
}
