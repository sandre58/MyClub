// -----------------------------------------------------------------------
// <copyright file="UseCaseExecutorExclusiveWriteArchitectureTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Pipeline;

/// <summary>
/// A'6 — every <see cref="MyClub.PlayUp.Application.Pipeline.UseCaseExecutor"/> write that calls
/// <c>SaveChangesAsync</c> must run under a competition lock envelope (or an explicit allow-list).
/// </summary>
public sealed class UseCaseExecutorExclusiveWriteArchitectureTests
{
    /// <summary>
    /// Ratchet: this count may only decrease when a REST lot lands and removes names from
    /// <see cref="PendingByLot"/> — never raise it; adding a pending name without removing another must fail CI.
    /// </summary>
    private const int ExpectedPendingByLotCount = 40;

    private static readonly string[] LockMarkers =
    [
        "ExecuteOnStageCompetitionAsync",
        "ExecuteOnMatchCompetitionAsync",
        "ExecuteOnCompetitionAsync",
        "unitOfWork.ExecuteExclusiveAsync",
    ];

    private static readonly HashSet<string> StableWriteAllowList =
    [
        "CreateCompetitionAsync",
        "GenerateScheduleAsync",
        "PreviewCascadeReplaceAsync",
        "PreviewResolutionReleaseAsync",
        "PreviewDefinitiveResolutionReleaseAsync",
        "PreviewDeletedSourceDefinitiveReleaseAsync",
    ];

    private static readonly HashSet<string> ReadsSurfaceAllowList =
    [
        "GetStructureViewAsync",
        "ListCompetitionsAsync",
        "GetWorkspaceSummaryAsync",
        "GetNeedsAttentionAsync",
        "GetOverviewViewAsync",
        "GetConsultationAsync",
        "GetMatchHubViewAsync",
        "GetCompetitionDetailAsync",
        "GetStageOverviewAsync",
        "GetStageSchematicAsync",
        "ListMatchesByStageAsync",
        "GetMatchDetailAsync",
    ];

    private static readonly HashSet<string> PendingByLot =
    [
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

    private static readonly HashSet<string> Rest1DrawMethods =
    [
        "PublishDrawAsync",
        "CancelDrawAsync",
    ];

    [Fact]
    public void Public_writes_either_use_competition_lock_or_are_explicitly_allowlisted()
    {
        PendingByLot.Count.Should().Be(
            ExpectedPendingByLotCount,
            "the pending list is a shrinking ratchet — adding a name without landing a REST lot must fail here");

        var pipelineDir = ResolvePipelineDirectory();
        var sources = Directory.GetFiles(pipelineDir, "UseCaseExecutor*.cs")
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        var unlistedWrites = new List<string>();
        var readsViolations = new List<string>();
        var pendingStillUnlocked = new HashSet<string>(PendingByLot, StringComparer.Ordinal);
        var pendingExtraneous = new HashSet<string>(StringComparer.Ordinal);

        foreach (var path in sources)
        {
            var text = File.ReadAllText(path);
            var isReadsFile = path.EndsWith("UseCaseExecutor.Reads.cs", StringComparison.Ordinal);
            foreach (var (methodName, body) in ExtractPublicMethods(text))
            {
                var saves = body.Contains("SaveChangesAsync", StringComparison.Ordinal);
                if (isReadsFile)
                {
                    if (!ReadsSurfaceAllowList.Contains(methodName))
                    {
                        readsViolations.Add($"unknown public surface on Reads: {methodName}");
                    }

                    if (saves)
                    {
                        readsViolations.Add($"Reads must not persist: {methodName}");
                    }

                    continue;
                }

                if (!saves)
                {
                    continue;
                }

                var locked = LockMarkers.Any(marker => body.Contains(marker, StringComparison.Ordinal));
                if (locked)
                {
                    pendingStillUnlocked.Remove(methodName);
                    continue;
                }

                if (StableWriteAllowList.Contains(methodName))
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

        PendingByLot.Intersect(Rest1DrawMethods)
            .Should()
            .BeEmpty("REST-1 PublishDraw / CancelDraw writes must not appear on the pending list");

        pendingExtraneous.Should().BeEmpty(
            "the pending list must only name methods that still exist and call SaveChangesAsync without a lock");

        readsViolations.Should().BeEmpty("UseCaseExecutor.Reads.cs must stay read-only with a closed method surface");

        unlistedWrites.Should().BeEmpty(
            "every saving public UseCaseExecutor command must use a competition lock or be on stable/pending lists");
    }

    private static IEnumerable<(string Name, string Body)> ExtractPublicMethods(string source)
    {
        var searchStart = 0;
        const string memberPrefix = "\n    public ";
        while (true)
        {
            var publicIndex = source.IndexOf(memberPrefix, searchStart, StringComparison.Ordinal);
            if (publicIndex < 0)
            {
                yield break;
            }

            var cursor = publicIndex + memberPrefix.Length;
            if (source.AsSpan(cursor).StartsWith("partial ", StringComparison.Ordinal))
            {
                searchStart = publicIndex + 1;
                continue;
            }

            if (!TryParseMethod(source, cursor, out var name, out var body, out var methodEnd))
            {
                searchStart = publicIndex + 1;
                continue;
            }

            yield return (name, body);
            searchStart = methodEnd;
        }
    }

    private static bool TryParseMethod(
        string source,
        int cursor,
        out string name,
        out string body,
        out int methodEnd)
    {
        name = string.Empty;
        body = string.Empty;
        methodEnd = cursor;

        if (source.AsSpan(cursor).StartsWith("async ", StringComparison.Ordinal))
        {
            cursor += "async ".Length;
        }

        cursor = SkipReturnType(source, cursor);
        cursor = SkipHorizontalWhitespace(source, cursor);
        var nameStart = cursor;
        while (cursor < source.Length && IsIdentifierChar(source[cursor]))
        {
            cursor++;
        }

        if (cursor == nameStart)
        {
            return false;
        }

        name = source[nameStart..cursor];
        cursor = SkipHorizontalWhitespace(source, cursor);
        if (cursor >= source.Length || source[cursor] != '(')
        {
            return false;
        }

        if (!TryFindMatchingDelimiter(source, cursor, '(', ')', out var afterParams))
        {
            return false;
        }

        cursor = SkipHorizontalWhitespace(source, afterParams);
        if (cursor < source.Length && source.AsSpan(cursor).StartsWith("where ", StringComparison.Ordinal))
        {
            while (cursor < source.Length && source[cursor] != '{'
                   && !source.AsSpan(cursor).StartsWith("=>", StringComparison.Ordinal))
            {
                cursor++;
            }
        }

        cursor = SkipHorizontalWhitespace(source, cursor);
        if (cursor >= source.Length)
        {
            return false;
        }

        if (source.AsSpan(cursor).StartsWith("=>", StringComparison.Ordinal))
        {
            cursor += 2;
            if (!TryFindExpressionBodyEnd(source, cursor, out methodEnd))
            {
                return false;
            }

            body = source[cursor..methodEnd];
            methodEnd++;
            return true;
        }

        if (source[cursor] == '{')
        {
            if (!TryFindMatchingDelimiter(source, cursor, '{', '}', out methodEnd))
            {
                return false;
            }

            body = source[cursor..(methodEnd + 1)];
            methodEnd++;
            return true;
        }

        return false;
    }

    private static int SkipReturnType(string source, int cursor)
    {
        var angleDepth = 0;
        while (cursor < source.Length)
        {
            var c = source[cursor];
            if (c == '<')
            {
                angleDepth++;
            }
            else if (c == '>')
            {
                angleDepth--;
            }
            else if (angleDepth == 0 && char.IsWhiteSpace(c))
            {
                break;
            }

            cursor++;
        }

        return cursor;
    }

    private static int SkipHorizontalWhitespace(string source, int cursor)
    {
        while (cursor < source.Length && source[cursor] is ' ' or '\t')
        {
            cursor++;
        }

        if (cursor < source.Length && source[cursor] == '\r')
        {
            cursor++;
        }

        if (cursor < source.Length && source[cursor] == '\n')
        {
            cursor++;
        }

        while (cursor < source.Length && source[cursor] is ' ' or '\t')
        {
            cursor++;
        }

        return cursor;
    }

    private static bool IsIdentifierChar(char c) => char.IsLetterOrDigit(c) || c == '_';

    private static bool TryFindMatchingDelimiter(
        string source,
        int openIndex,
        char open,
        char close,
        out int closeIndex)
    {
        closeIndex = -1;
        var depth = 0;
        var inString = false;
        var verbatim = false;
        var escape = false;

        for (var i = openIndex; i < source.Length; i++)
        {
            var c = source[i];
            if (inString)
            {
                if (verbatim)
                {
                    if (c == '"' && i + 1 < source.Length && source[i + 1] == '"')
                    {
                        i++;
                        continue;
                    }

                    if (c == '"')
                    {
                        inString = false;
                        verbatim = false;
                    }
                }
                else if (escape)
                {
                    escape = false;
                }
                else if (c == '\\')
                {
                    escape = true;
                }
                else if (c == '"')
                {
                    inString = false;
                }

                continue;
            }

            if (c == '@' && i + 1 < source.Length && source[i + 1] == '"')
            {
                verbatim = true;
                inString = true;
                i++;
                continue;
            }

            if (c == '"')
            {
                inString = true;
                continue;
            }

            if (c == '\'')
            {
                i++;
                while (i < source.Length && source[i] != '\'')
                {
                    if (source[i] == '\\' && i + 1 < source.Length)
                    {
                        i++;
                    }

                    i++;
                }

                continue;
            }

            if (c == open)
            {
                depth++;
            }
            else if (c == close)
            {
                depth--;
                if (depth == 0)
                {
                    closeIndex = i;
                    return true;
                }
            }
        }

        return false;
    }

    private static bool TryFindExpressionBodyEnd(string source, int cursor, out int endIndex)
    {
        endIndex = -1;
        var depthParen = 0;
        var depthAngle = 0;
        var depthBracket = 0;
        var inString = false;
        var escape = false;

        for (var i = cursor; i < source.Length; i++)
        {
            var c = source[i];
            if (inString)
            {
                if (escape)
                {
                    escape = false;
                }
                else if (c == '\\')
                {
                    escape = true;
                }
                else if (c == '"')
                {
                    inString = false;
                }

                continue;
            }

            if (c == '"')
            {
                inString = true;
                continue;
            }

            if (c == '(')
            {
                depthParen++;
            }
            else if (c == ')')
            {
                depthParen--;
            }
            else if (c == '<')
            {
                depthAngle++;
            }
            else if (c == '>')
            {
                depthAngle--;
            }
            else if (c == '[')
            {
                depthBracket++;
            }
            else if (c == ']')
            {
                depthBracket--;
            }
            else if (c == ';' && depthParen == 0 && depthAngle == 0 && depthBracket == 0)
            {
                endIndex = i;
                return true;
            }
        }

        return false;
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
}
