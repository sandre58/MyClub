// -----------------------------------------------------------------------
// <copyright file="StoredTieFormatPropertyReadArchitectureTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Architecture;

/// <summary>
/// A'9-N1 — stored <c>Round.TieFormat</c> / <c>StageRegulation.TieFormat</c> reads only in whitelisted resolvers,
/// persistence, DTO mapping, and tie-format editing.
/// </summary>
public sealed class StoredTieFormatPropertyReadArchitectureTests
{
    private const string ScannerSnippet = """
        using MyClub.PlayUp.Domain.Rules;
        using MyClub.PlayUp.Domain.Stages;

        namespace MyClub.PlayUp.Application.Tests.Architecture.Snippets;

        internal static class TieFormatReadScannerSnippet
        {
            internal static void WhitelistedResolver(Stage stage, Round round)
            {
                _ = Stage.ResolveEffectiveTieFormat(round, stage.Regulation);
            }

            internal static void ForbiddenDirectAndConditional(Round round)
            {
                _ = round.TieFormat;
                _ = round?.TieFormat;
            }
        }
        """;

    [Fact]
    public void Product_code_reads_stored_TieFormat_only_in_whitelisted_members()
    {
        var violations = StoredTieFormatReadScanner.ScanRepository(FindRepositoryRoot());
        violations.Should().BeEmpty(
            "read stored TieFormat via Stage.ResolveEffectiveTieFormat / Stage.ResolveEffectiveDefault / "
            + "Structure DTO assemblers, or extend the explicit whitelist with justification");
    }

    [Fact]
    public void Scanner_flags_direct_and_conditional_reads_only_outside_whitelisted_members()
    {
        var violations = StoredTieFormatReadScanner.FindViolations(ScannerSnippet, "snippet.cs");
        violations.Should().Contain(v => v.Contains("ForbiddenDirectAndConditional", StringComparison.Ordinal));
        violations.Should().HaveCount(2);
        violations.Should().NotContain(v => v.Contains("WhitelistedResolver", StringComparison.Ordinal));
        violations.Should().NotContain(v => v.Contains("WhitelistedConditional", StringComparison.Ordinal));
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
