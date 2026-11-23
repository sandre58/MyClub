// -----------------------------------------------------------------------
// <copyright file="UseCaseExecutorExclusiveWriteArchitectureTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Pipeline;

/// <summary>
/// A'6 — every <see cref="MyClub.PlayUp.Application.Pipeline.UseCaseExecutor"/> write that calls
/// <c>SaveChangesAsync</c> must run under a competition lock envelope (or an explicit allow-list).
/// </summary>
public sealed partial class UseCaseExecutorExclusiveWriteArchitectureTests
{
    private static readonly string[] LockMarkers =
    [
        "ExecuteOnStageCompetitionAsync",
        "ExecuteOnMatchCompetitionAsync",
        "ExecuteOnCompetitionAsync",
        "unitOfWork.ExecuteExclusiveAsync",
    ];

    private static readonly HashSet<string> StableAllowList =
    [
        "CreateCompetitionAsync",
        "GenerateScheduleAsync",
        "PreviewCascadeReplaceAsync",
        "PreviewResolutionReleaseAsync",
        "PreviewDefinitiveResolutionReleaseAsync",
        "PreviewDeletedSourceDefinitiveReleaseAsync",
    ];

    /// <summary>
    /// Nominative backlog (REST-1..7). Must only shrink as lots land; size is asserted indirectly via source cross-check.
    /// </summary>
    private static readonly HashSet<string> PendingByLot =
    [
        "PublishDrawAsync",
        "CancelDrawAsync",
        "GenerateNextRoundAsync",
        "ApplyScheduleAsync",
        "AddStageRoundAsync",
        "AddStageSlotAsync",
        "AddStageMatchdayAsync",
        "AddStageGroupAsync",
        "RenameStageAsync",
        "AddCompetitionStageAsync",
        "InsertCompetitionStageAsync",
        "SetCompetitionStageOrderAsync",
        "ReplaceStageMatchGenerationFormatAsync",
        "ReplaceStageSwissSettingsAsync",
        "ReplaceStageProgressionRulesAsync",
        "ReplaceStageProgressionIntentsAsync",
        "ReplaceStageQualificationRulesAsync",
        "ReplaceStageQualificationIntentsAsync",
        "ReplaceStagePlacementAwardRulesAsync",
        "ReplaceStageStandingRulesAsync",
        "ReplaceStageMatchRulesAsync",
        "BindStageRegulationAsync",
        "ReplaceStageDrawRulesAsync",
        "ReplaceStageDefaultTieFormatAsync",
        "ReplaceRoundTieFormatAsync",
        "PrepareCompetitionAsync",
        "StartCompetitionAsync",
        "SuspendCompetitionAsync",
        "ResumeCompetitionAsync",
        "CompleteCompetitionAsync",
        "ArchiveCompetitionAsync",
        "AddEntryAsync",
        "UpdateCompetitionPresentationAsync",
        "SetCompetitionScheduleAsync",
        "UpdateEntryPresentationAsync",
        "RenameEntryAsync",
        "RemoveDeclaredMembersAsync",
        "AddDeclaredMemberAsync",
        "RemoveDeclaredMemberAsync",
        "RenameDeclaredMemberAsync",
        "ReplaceRegulationAsync",
        "BackfillResolutionProvenanceAsync",
    ];

    private static readonly HashSet<string> Rest2LifecycleMethods =
    [
        "PrepareStageAsync",
        "StartStageAsync",
        "CompleteStageAsync",
        "RevertStageToDraftAsync",
        "SuspendStageAsync",
        "ResumeStageAsync",
        "RevertStageToReadyAsync",
    ];

    [Fact]
    public void Public_writes_either_use_competition_lock_or_are_explicitly_allowlistedAsync()
    {
        var pipelineDir = ResolvePipelineDirectory();
        var sources = Directory.GetFiles(pipelineDir, "UseCaseExecutor*.cs")
            .Where(path => !path.EndsWith("UseCaseExecutor.Reads.cs", StringComparison.Ordinal))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        var unlistedWrites = new List<string>();
        var pendingStillUnlocked = new HashSet<string>(PendingByLot, StringComparer.Ordinal);
        var pendingExtraneous = new HashSet<string>(StringComparer.Ordinal);

        foreach (var path in sources)
        {
            var text = File.ReadAllText(path);
            foreach (Match match in MethodRegex().Matches(text))
            {
                var methodName = match.Groups["name"].Value;
                var bodyStart = match.Index + match.Length;
                var nextPublic = text.IndexOf("\n    public ", bodyStart, StringComparison.Ordinal);
                var body = nextPublic >= 0 ? text[bodyStart..nextPublic] : text[bodyStart..];
                if (!body.Contains("SaveChangesAsync", StringComparison.Ordinal))
                {
                    continue;
                }

                var locked = LockMarkers.Any(marker => body.Contains(marker, StringComparison.Ordinal));
                if (locked)
                {
                    pendingStillUnlocked.Remove(methodName);
                    continue;
                }

                if (StableAllowList.Contains(methodName))
                {
                    pendingStillUnlocked.Remove(methodName);
                    continue;
                }

                if (PendingByLot.Contains(methodName))
                {
                    continue;
                }

                unlistedWrites.Add($"{methodName} ({Path.GetFileName(path)})");
            }
        }

        foreach (var name in pendingStillUnlocked)
        {
            if (!MethodExistsInSources(sources, name))
            {
                pendingExtraneous.Add(name);
            }
        }

        PendingByLot.Intersect(Rest2LifecycleMethods)
            .Should()
            .BeEmpty("REST-2 lifecycle writes must not appear on the pending list");

        pendingExtraneous.Should().BeEmpty(
            "the pending list must only name methods that still exist and call SaveChangesAsync without a lock");

        unlistedWrites.Should().BeEmpty(
            "every saving public UseCaseExecutor command must use a competition lock or be on stable/pending lists");
    }

    private static bool MethodExistsInSources(IReadOnlyList<string> sources, string methodName) =>
        sources.Any(path => File.ReadAllText(path).Contains($" {methodName}(", StringComparison.Ordinal));

    private static string ResolvePipelineDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "MyClub.slnx")))
            {
                var pipeline = Path.Combine(
                    directory.FullName,
                    "src",
                    "PlayUp",
                    "MyClub.PlayUp.Application",
                    "Pipeline");
                Directory.Exists(pipeline).Should().BeTrue($"expected UseCaseExecutor sources at {pipeline}");
                return pipeline;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate MyClub.slnx from test output directory.");
    }

    [GeneratedRegex(@"public (?:async )?Task(?:<[^>]+>)? (?<name>\w+)\(", RegexOptions.Multiline)]
    private static partial Regex MethodRegex();
}
