// -----------------------------------------------------------------------
// <copyright file="MatchResultConfrontationTieFactoryArchitectureTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Architecture;

/// <summary>
/// Confrontation tie factory calls must stay at the persistence and entry-mapping edges.
/// </summary>
public sealed class MatchResultConfrontationTieFactoryArchitectureTests
{
    private static readonly Regex CreateConfrontationTieCall = new(
        @"CreateConfrontationTiePenaltyShootout\s*\(",
        RegexOptions.CultureInvariant);

    private static readonly string[] AllowedRelativePaths =
    [
        "src/PlayUp/MyClub.PlayUp.Domain/Matches/MatchResult.cs",
        "src/PlayUp/MyClub.PlayUp.Application/Matches/MatchResultFromEntry.cs",
        "src/PlayUp/MyClub.PlayUp.Infrastructure/Persistence/Converters/MatchResultJson.cs",
        "src/PlayUp/MyClub.PlayUp.Infrastructure/Persistence/Converters/MatchResultJsonConverter.cs",
    ];

    [Fact]
    public void Product_code_calls_CreateConfrontationTiePenaltyShootout_only_at_allowed_sites()
    {
        var repoRoot = FindRepositoryRoot();
        var violations = new List<string>();
        foreach (var path in Directory.EnumerateFiles(repoRoot, "*.cs", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(repoRoot, path).Replace('\\', '/');
            if (relative.StartsWith("tests/", StringComparison.Ordinal)
                || relative.Contains("/Migrations/", StringComparison.Ordinal))
            {
                continue;
            }

            var text = File.ReadAllText(path);
            if (!CreateConfrontationTieCall.IsMatch(text))
            {
                continue;
            }

            if (!AllowedRelativePaths.Any(allowed => relative.Equals(allowed, StringComparison.OrdinalIgnoreCase)))
            {
                violations.Add(relative);
            }
        }

        violations.Should().BeEmpty(
            "CreateConfrontationTiePenaltyShootout must be invoked only from MatchResult, MatchResultFromEntry, "
            + "MatchResultJson, and MatchResultJsonConverter");
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "MyClub.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root not found.");
    }
}