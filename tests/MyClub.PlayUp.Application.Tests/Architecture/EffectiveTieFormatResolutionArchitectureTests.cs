// -----------------------------------------------------------------------
// <copyright file="EffectiveTieFormatResolutionArchitectureTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Architecture;

/// <summary>
/// A'9 — effective TieFormat must use living inheritance, not scattered <c>OrDefaultOneLeg</c> on rounds.
/// </summary>
public sealed class EffectiveTieFormatResolutionArchitectureTests
{
    private static readonly string[] Roots =
    [
        Path.Combine("src", "PlayUp", "MyClub.PlayUp.Application"),
        Path.Combine("src", "PlayUp", "MyClub.PlayUp.Host"),
    ];

    private static readonly HashSet<string> AllowListedRelativePaths =
    [
        Path.Combine("Application", "Reads", "StructureViewAssembler.cs"),
        Path.Combine("Application", "Reads", "OverviewAssembler.cs"),
        Path.Combine("Domain", "Rules", "TieFormat", "TieFormat.cs"),
        Path.Combine("Infrastructure", "Persistence", "Converters", "TieFormatJsonConverter.cs"),
        "Infrastructure/Persistence/Migrations",
    ];

    [Fact]
    public void Application_and_host_do_not_resolve_round_effectives_via_OrDefaultOneLeg_on_round()
    {
        var repoRoot = FindRepositoryRoot();
        var violations = new List<string>();
        foreach (var root in Roots)
        {
            var directory = Path.Combine(repoRoot, root);
            foreach (var path in Directory.EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(repoRoot, path).Replace('\\', '/');
                if (IsAllowListed(relative))
                {
                    continue;
                }

                var text = File.ReadAllText(path);
                if (Regex.IsMatch(text, @"OrDefaultOneLeg\s*\(\s*round\.TieFormat", RegexOptions.CultureInvariant))
                {
                    violations.Add(relative);
                }
            }
        }

        violations.Should().BeEmpty(
            "round TieFormat must use Stage.ResolveEffectiveTieFormat / TieFormat.ResolveEffective, not OrDefaultOneLeg(round.TieFormat)");
    }

    private static bool IsAllowListed(string relative)
    {
        foreach (var allowed in AllowListedRelativePaths)
        {
            if (relative.Contains(allowed, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return relative.Contains("/Migrations/", StringComparison.Ordinal);
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