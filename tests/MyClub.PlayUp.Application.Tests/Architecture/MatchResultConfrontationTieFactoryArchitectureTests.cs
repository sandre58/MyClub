// -----------------------------------------------------------------------
// <copyright file="MatchResultConfrontationTieFactoryArchitectureTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Architecture;

/// <summary>
/// Confrontation tie factory calls in product code must stay at eligibility and persistence edges.
/// </summary>
public sealed class MatchResultConfrontationTieFactoryArchitectureTests
{
    [Fact]
    public void MatchResult_public_static_factories_are_tracked()
    {
        var names = MatchResultStaticFactoryScanner.DiscoverMatchResultStaticFactoryNames();
        names.Should().BeEquivalentTo(["CreateConfrontationTiePenaltyShootout", "Rehydrate"]);
    }

    [Fact]
    public void Product_code_references_MatchResult_static_factories_only_at_allowed_sites()
    {
        var repoRoot = FindRepositoryRoot();
        var violations = MatchResultStaticFactoryScanner.ScanRepository(repoRoot);

        violations.Should().BeEmpty(
            "CreateConfrontationTiePenaltyShootout must be called only from ConfrontationTiePenaltyShootoutEligibility, "
            + "Rehydrate only from MatchResultJson and MatchResultJsonConverter, "
            + "and ConfrontationTieEligibility.CreateProof only from eligibility.");
    }

    [Fact]
    public void Scanner_detects_method_group_and_nameof_references()
    {
        const string snippet = """
            using System;
            using MyClub.PlayUp.Domain.Matches;
            static class Probe
            {
                static void Bad()
                {
                    Func<Score, bool, PenaltyShootoutScore, ConfrontationTieEligibilityProof, MatchResult> a =
                        MatchResult.CreateConfrontationTiePenaltyShootout;
                    _ = nameof(MatchResult.Rehydrate);
                }
            }
            """;

        var violations = MatchResultStaticFactoryScanner.FindViolations(snippet, "tests/Architecture/probe.cs");

        violations.Should().Contain(v => v.Contains("CreateConfrontationTiePenaltyShootout", StringComparison.Ordinal));
        violations.Should().Contain(v => v.Contains("Rehydrate", StringComparison.Ordinal));
        violations.Should().HaveCountGreaterThanOrEqualTo(2);
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
