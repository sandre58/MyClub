// -----------------------------------------------------------------------
// <copyright file="CompletionAnalyzer.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

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
    /// <param name="matchesByStage">Matches keyed by stage.</param>
    /// <returns>Derived completion analysis.</returns>
    public static CompletionAnalysis Analyze(
        Competition competition,
        IReadOnlyList<Stage> stages,
        IReadOnlyDictionary<StageId, IReadOnlyList<Match>> matchesByStage)
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

            reasons.Add(new CompletionReasonDto(item.Source, item.Reason));
        }

        var sportivelyComplete = reasons.Count == 0;
        var canCompleteNormally = sportivelyComplete
            && competition.Status is CompetitionStatus.Running or CompetitionStatus.Suspended;

        return new CompletionAnalysis(sportivelyComplete, canCompleteNormally, reasons);
    }

    private static void CollectUnresolvedMatchReasons(
        IReadOnlyDictionary<StageId, IReadOnlyList<Match>> matchesByStage,
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
                }
            }
        }

        if (scheduled > 0)
        {
            var message = scheduled == 1
                ? "1 match est encore planifié."
                : $"{scheduled} matchs sont encore planifiés.";
            reasons.Add(new CompletionReasonDto(ReasonScheduledMatches, message));
        }

        if (live > 0)
        {
            var message = live == 1
                ? "1 match est encore en cours."
                : $"{live} matchs sont encore en cours.";
            reasons.Add(new CompletionReasonDto(ReasonLiveMatches, message));
        }

        if (postponed > 0)
        {
            var message = postponed == 1
                ? "1 match est encore reporté."
                : $"{postponed} matchs sont encore reportés.";
            reasons.Add(new CompletionReasonDto(ReasonPostponedMatches, message));
        }
    }
}
