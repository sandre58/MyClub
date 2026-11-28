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
/// <remarks>
/// Runtime reflection (<c>GetMethod</c>, <c>Invoke</c>, <c>Activator</c>, <c>GetUninitializedObject</c>) can bypass this scan; that gap is an accepted limitation.
/// </remarks>
public sealed class MatchResultConfrontationTieFactoryArchitectureTests
{
    [Fact]
    public void MatchResult_has_single_public_static_factory_for_flagged_results()
    {
        var names = MatchResultStaticFactoryScanner.DiscoverPublicMatchResultStaticFactoryNames();
        names.Should().BeEquivalentTo(["CreateConfrontationTiePenaltyShootout"]);
    }

    [Fact]
    public void Product_code_references_confrontation_tie_factories_only_at_allowed_sites()
    {
        var repoRoot = FindRepositoryRoot();
        var violations = MatchResultStaticFactoryScanner.ScanRepository(repoRoot);

        violations.Should().BeEmpty(
            "CreateConfrontationTiePenaltyShootout must be called only from ConfrontationTiePenaltyShootoutEligibility, "
            + "and Rehydrate only from MatchResultJson and MatchResultJsonConverter.");
    }

    [Theory]
    [InlineData(
        """
        using MR = MyClub.PlayUp.Domain.Matches.MatchResult;
        static class P { static readonly string N = nameof(MR.Rehydrate); }
        """,
        "Rehydrate")]
    [InlineData(
        """
        static class P {
            static void M() => global::MyClub.PlayUp.Domain.Matches.MatchResult.Rehydrate(
                MyClub.PlayUp.Domain.Matches.ResultType.Played,
                new MyClub.PlayUp.Domain.Common.Score(1, 0),
                false,
                new MyClub.PlayUp.Domain.Matches.PenaltyShootoutScore(5, 4),
                true);
        }
        """,
        "Rehydrate")]
    [InlineData(
        """
        using static MyClub.PlayUp.Domain.Matches.MatchResult;
        using MyClub.PlayUp.Domain.Common;
        using MyClub.PlayUp.Domain.Matches;
        using MyClub.PlayUp.Domain.Rules;
        static class P {
            static void M() => CreateConfrontationTiePenaltyShootout(
                new TieFormat(TieFormat.TwoLegs, aggregateScoring: true, penaltyShootoutRule: new PenaltyShootoutRule()),
                default, default, MatchStatus.Finished, new Score(1, 0), false,
                default, default, new Score(2, 1), false, new PenaltyShootoutScore(5, 4));
        }
        """,
        "CreateConfrontationTiePenaltyShootout")]
    [InlineData(
        """
        using System;
        using MyClub.PlayUp.Domain.Common;
        using MyClub.PlayUp.Domain.Matches;
        using MyClub.PlayUp.Domain.Rules;
        static class P {
            static Func<TieFormat, EntryId, EntryId, MatchStatus, Score, bool, EntryId, EntryId, Score, bool, PenaltyShootoutScore, MatchResult> G =
                MatchResult.CreateConfrontationTiePenaltyShootout;
        }
        """,
        "CreateConfrontationTiePenaltyShootout")]
    [InlineData(
        """
        namespace Evil;
        static class P {
            static void M() => global::MyClub.PlayUp.Domain.Matches.MatchResult.Rehydrate(
                MyClub.PlayUp.Domain.Matches.ResultType.Played,
                new MyClub.PlayUp.Domain.Common.Score(1, 0),
                false,
                new MyClub.PlayUp.Domain.Matches.PenaltyShootoutScore(5, 4),
                true);
        }
        namespace Evil {
        public class MatchResult {
            public static void Rehydrate() { }
        }}
        """,
        "Rehydrate")]
    public void Semantic_scanner_detects_negative_probe(string snippet, string expectedMethod)
    {
        var violations = MatchResultStaticFactoryScanner.FindViolations(snippet, "tests/Architecture/probe.cs");

        violations.Should().Contain(v => v.Contains(expectedMethod, StringComparison.Ordinal), snippet);
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
