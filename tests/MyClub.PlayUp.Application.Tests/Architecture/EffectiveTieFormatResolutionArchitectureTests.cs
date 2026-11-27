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
/// A'9 — effective TieFormat must use living inheritance, not scattered <c>OrDefaultOneLeg</c>.
/// </summary>
public sealed class EffectiveTieFormatResolutionArchitectureTests
{
    private static readonly string[] Roots =
    [
        Path.Combine("src", "PlayUp", "MyClub.PlayUp.Application"),
        Path.Combine("src", "PlayUp", "MyClub.PlayUp.Host"),
        Path.Combine("src", "PlayUp", "MyClub.PlayUp.Development"),
        Path.Combine("src", "PlayUp", "MyClub.PlayUp.Domain"),
    ];

    private static readonly Regex OrDefaultOneLegCall =
        new(@"OrDefaultOneLeg\s*\(", RegexOptions.CultureInvariant);

    private static readonly string[] OrDefaultOneLegAllowListedPaths =
    [
        "src/PlayUp/MyClub.PlayUp.Domain/Rules/TieFormat/TieFormat.cs",
    ];

    private static readonly Regex RawRegulationTieFallback =
        new(
            @"OrDefaultOneLeg\s*\(\s*\w+\.Regulation\.TieFormat",
            RegexOptions.CultureInvariant);

    private static readonly Regex RawRoundTieFallback =
        new(
            @"OrDefaultOneLeg\s*\(\s*(\w+\.)?TieFormat|OrDefaultOneLeg\s*\(\s*r\.TieFormat",
            RegexOptions.CultureInvariant);

    [Fact]
    public void Product_code_does_not_call_OrDefaultOneLeg_outside_TieFormat_helper()
    {
        var repoRoot = FindRepositoryRoot();
        var violations = new List<string>();
        foreach (var root in Roots)
        {
            var directory = Path.Combine(repoRoot, root);
            if (!Directory.Exists(directory))
            {
                continue;
            }

            foreach (var path in Directory.EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(repoRoot, path).Replace('\\', '/');
                if (IsOrDefaultOneLegAllowListed(relative) || relative.Contains("/Migrations/", StringComparison.Ordinal))
                {
                    continue;
                }

                var text = File.ReadAllText(path);
                if (OrDefaultOneLegCall.IsMatch(text))
                {
                    violations.Add(relative);
                }
            }
        }

        violations.Should().BeEmpty(
            "use TieFormat.ResolveEffective / Stage.ResolveEffectiveTieFormat for effectives; OrDefaultOneLeg belongs in TieFormat.cs only");
    }

    [Fact]
    public void Application_host_and_development_do_not_resolve_effectives_via_raw_regulation_or_round_stored_values()
    {
        var repoRoot = FindRepositoryRoot();
        var violations = new List<string>();
        foreach (var root in Roots.Where(r => !r.Contains("Domain", StringComparison.Ordinal)))
        {
            var directory = Path.Combine(repoRoot, root);
            if (!Directory.Exists(directory))
            {
                continue;
            }

            foreach (var path in Directory.EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(repoRoot, path).Replace('\\', '/');
                if (relative.Contains("/Migrations/", StringComparison.Ordinal))
                {
                    continue;
                }

                var text = File.ReadAllText(path);
                if (RawRegulationTieFallback.IsMatch(text) || RawRoundTieFallback.IsMatch(text))
                {
                    violations.Add(relative);
                }
            }
        }

        violations.Should().BeEmpty(
            "effective leg/decidability resolution must not use OrDefaultOneLeg on Regulation.TieFormat or stored round values");
    }

    [Fact]
    public void Structure_match_production_prefers_hub_number_of_legs_over_raw_phase_default()
    {
        var repoRoot = FindRepositoryRoot();
        var violations = new List<string>();
        var directory = Path.Combine(repoRoot, "src", "PlayUp", "MyClub.PlayUp.Application");
        foreach (var path in Directory.EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(repoRoot, path).Replace('\\', '/');
            if (!relative.Contains("StructureMatchProduction", StringComparison.Ordinal))
            {
                continue;
            }

            var text = File.ReadAllText(path);
            if (text.Contains("defaultTieFormat?.NumberOfLegs ?? numberOfLegs", StringComparison.Ordinal))
            {
                violations.Add(relative);
            }
        }

        violations.Should().BeEmpty(
            "production legs must prefer hub effective numberOfLegs before stored phase default");
    }

    private static bool IsOrDefaultOneLegAllowListed(string relative) =>
        OrDefaultOneLegAllowListedPaths.Any(allowed =>
            relative.Equals(allowed, StringComparison.OrdinalIgnoreCase));

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
