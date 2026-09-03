// -----------------------------------------------------------------------
// <copyright file="CompletionAnalyzer.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Derives sporting completeness for a competition (Application diagnostic — not Domain, not persisted).
/// </summary>
/// <remarks>
/// Unfinished matches are completion blockers, not Needs Attention items.
/// Pending/Conflict Qualification/Progression and Draw NoSolution reuse Needs Attention diagnostics.
/// Cancelled matches are terminal and do not block Normal completion.
/// </remarks>
public static class CompletionAnalyzer
{
    /// <summary>One or more matches still Scheduled.</summary>
    public const string ReasonScheduledMatches = "ScheduledMatches";

    /// <summary>One or more matches still Live.</summary>
    public const string ReasonLiveMatches = "LiveMatches";

    /// <summary>One or more matches still Postponed.</summary>
    public const string ReasonPostponedMatches = "PostponedMatches";

    /// <summary>
    /// Analyzes sporting completeness without mutating aggregates.
    /// </summary>
    /// <param name="competition">Loaded competition.</param>
    /// <param name="stages">Competition stages.</param>
    /// <param name="matchesByStage">Full matches keyed by stage (command / Overview paths).</param>
    /// <returns>Derived completion analysis.</returns>
    public static CompletionAnalysis Analyze(
        Competition competition,
        IReadOnlyList<Stage> stages,
        IReadOnlyDictionary<StageId, IReadOnlyList<Match>> matchesByStage) =>
        Analyze(competition, stages, MatchAttentionSlice.FromMatchesByStage(matchesByStage));

    /// <summary>
    /// Analyzes sporting completeness from projected match slices (status-only for unresolved matches).
    /// </summary>
    /// <param name="competition">Loaded competition.</param>
    /// <param name="stages">Competition stages.</param>
    /// <param name="matchesByStage">Attention slices keyed by stage.</param>
    /// <returns>Derived completion analysis.</returns>
    public static CompletionAnalysis Analyze(
        Competition competition,
        IReadOnlyList<Stage> stages,
        IReadOnlyDictionary<StageId, IReadOnlyList<MatchAttentionSlice>> matchesByStage)
    {
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(stages);
        ArgumentNullException.ThrowIfNull(matchesByStage);

        var reasons = new List<CompletionReasonDto>();
        CollectUnresolvedMatchReasons(matchesByStage, reasons);

        var attention = NeedsAttentionAssembler.Assemble(competition, stages, matchesByStage);
        foreach (var item in attention.Items)
        {
            if (item.Severity != NeedsAttentionAssembler.SeverityBlocking)
            {
                continue;
            }

            if (reasons.Exists(reason => reason.Code == item.Source))
            {
                continue;
            }

            reasons.Add(new CompletionReasonDto(item.Source));
        }

        var sportivelyComplete = reasons.Count == 0;
        var canCompleteNormally = sportivelyComplete
                                  && competition.Status is CompetitionStatus.Running or CompetitionStatus.Suspended;

        return new CompletionAnalysis(sportivelyComplete, canCompleteNormally, reasons);
    }

    private static void CollectUnresolvedMatchReasons(
        IReadOnlyDictionary<StageId, IReadOnlyList<MatchAttentionSlice>> matchesByStage,
        List<CompletionReasonDto> reasons)
    {
        var scheduled = 0;
        var live = 0;
        var postponed = 0;

        foreach (var matches in matchesByStage.Values)
        {
            foreach (var match in matches)
            {
                switch (match.Status)
                {
                    case MatchStatus.Scheduled:
                        scheduled++;
                        break;
                    case MatchStatus.Live:
                        live++;
                        break;
                    case MatchStatus.Postponed:
                        postponed++;
                        break;
                    case MatchStatus.Finished:
                    case MatchStatus.Cancelled:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(matchesByStage));
                }
            }
        }

        if (scheduled > 0)
        {
            reasons.Add(new CompletionReasonDto(ReasonScheduledMatches));
        }

        if (live > 0)
        {
            reasons.Add(new CompletionReasonDto(ReasonLiveMatches));
        }

        if (postponed > 0)
        {
            reasons.Add(new CompletionReasonDto(ReasonPostponedMatches));
        }
    }
}
