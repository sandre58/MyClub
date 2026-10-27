// -----------------------------------------------------------------------
// <copyright file="MaterializeCupFromOccupiedSlots.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Application use case: materialize Cup Fixtures + Matches from occupied <see cref="BracketPair"/>s.
/// </summary>
/// <remarks>
/// Progression only fills Slot.EntryId. This UC creates Fixture(SlotA/B, BracketPairKey) and Matches when
/// a pair is eligible. Reuses leg rules via <see cref="CupConfrontationMaterializer"/>.
/// Does not author ProgressionRules, create Stages, or schedule.
/// </remarks>
public static class MaterializeCupFromOccupiedSlots
{
    /// <summary>
    /// Materializes confrontations for eligible bracket pairs on a Cup stage.
    /// </summary>
    /// <param name="competition">Owning competition.</param>
    /// <param name="stage">Cup stage (rounds only).</param>
    /// <param name="pairKeys">
    /// Optional explicit <see cref="BracketPair.PairKey"/> list.
    /// Null / empty → all eligible pairs; non-empty → each key must exist and be eligible (fail-closed).
    /// </param>
    /// <param name="existingMatches">Matches already known for the stage.</param>
    /// <param name="clock">Clock for domain events.</param>
    /// <param name="competitionStages">Stages of competition.</param>
    /// <param name="matchSummariesByStage">Summaries match by stage.</param>
    public static MaterializeCupFromOccupiedSlotsResult Execute(
        Competition competition,
        Stage stage,
        IReadOnlyList<string>? pairKeys,
        IReadOnlyList<Match> existingMatches,
        IClock clock,
        IReadOnlyList<Stage>? competitionStages = null,
        IReadOnlyDictionary<StageId, IReadOnlyList<MatchSummaryRow>>? matchSummariesByStage = null)
    {
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(existingMatches);
        ArgumentNullException.ThrowIfNull(clock);

        CupBracketPairMaterialization.ResolutionContext? pairContext = competitionStages is null
            ? null
            : new CupBracketPairMaterialization.ResolutionContext(competitionStages, matchSummariesByStage);

        if (!stage.CompetitionId.Equals(competition.Id))
        {
            throw new ApplicationFailureException(
                $"Stage '{stage.Id}' does not belong to competition '{competition.Id}'.",
                ApplicationErrorCodes.StageNotInCompetition);
        }

        EnsureMutable(competition, stage, pairKeys);
        CupCanonicalBracketTopologyGate.EnsureBeforeMaterialize(stage);

        if (stage.Rounds.Count == 0 || stage.Matchdays.Count > 0 || stage.Groups.Count > 0)
        {
            throw new ApplicationFailureException(
                "MaterializeCupFromOccupiedSlots requires a Cup stage (rounds only).",
                ApplicationErrorCodes.MaterializationFailure);
        }

        var requestEmpty = pairKeys is null || pairKeys.Count == 0;
        var stages = competitionStages ?? [stage];
        var targets = ResolveTargetPairs(
            stage,
            pairKeys,
            requestEmpty,
            pairContext,
            competition,
            stages);
        if (targets.Count == 0)
        {
            return requestEmpty && stage.BracketPairs.Count > 0 && stage.BracketPairs.All(pair => CupBracketPairMaterialization.IsConfrontationMaterialized(stage, pair, pairContext)) ? new MaterializeCupFromOccupiedSlotsResult([], [], AlreadyComplete: true) : throw new ApplicationFailureException("No eligible bracket pairs to materialize.", ApplicationErrorCodes.MaterializationFailure);
        }

        var round = stage.Rounds[0];
        var expectedLegs = CupConfrontationMaterializer.ExpectedLegsForRound(round);

        var knownById = existingMatches
            .Where(match => stage.HasMatch(match.Id))
            .ToDictionary(match => match.Id);

        var resolved = new List<(BracketPair Pair, Fixture Fixture, EntryId Home, EntryId Away)>(targets.Count);
        foreach (var pair in targets)
        {
            var evaluation = CupPairEvaluation.Evaluate(
                new CupPairEvaluationInput(
                    competition,
                    stage,
                    pair,
                    stages,
                    matchSummariesByStage,
                    PhaseMutable: true));
            if (!evaluation.EligibleForMaterialize
                || !CupBracketPairMaterialization.TryResolvePairEntries(
                    stage,
                    pair,
                    null,
                    out var home,
                    out var away))
            {
                throw new ApplicationFailureException(
                    $"Bracket pair '{pair.PairKey}' is not eligible for materialization.",
                    CupPairEvaluation.ResolveMaterializeErrorCode(evaluation));
            }

            var fixture = stage.FindFixtureByBracketPairKey(pair.PairKey)
                          ?? stage.AddFixtureForMatchProduction(
                              round.Id,
                              clock,
                              pair.SlotAKey,
                              pair.SlotBKey,
                              pair.PairKey);
            resolved.Add((pair, fixture, home, away));
        }

        var matchedIndexes = new HashSet<int>();
        var matchedMatchIds = new HashSet<MatchId>();
        for (var i = 0; i < resolved.Count; i++)
        {
            var item = resolved[i];
            if (!CupConfrontationMaterializer.TryMatchCompleteLegs(
                    item.Fixture,
                    item.Home,
                    item.Away,
                    knownById,
                    expectedLegs,
                    out var matchedIds))
            {
                continue;
            }

            matchedIndexes.Add(i);
            foreach (var matchId in matchedIds)
            {
                matchedMatchIds.Add(matchId);
            }
        }

        var allMatched = matchedIndexes.Count == resolved.Count;
        var noneMatched = matchedIndexes.Count == 0;
        var anyAttachments = resolved.Any(item => item.Fixture.MatchIds.Count > 0);
        var targetMatchIds = resolved.SelectMany(item => item.Fixture.MatchIds).ToHashSet();
        var unmatchedOnTargets = existingMatches
            .Where(match => targetMatchIds.Contains(match.Id) && !matchedMatchIds.Contains(match.Id))
            .ToArray();

        if (allMatched && unmatchedOnTargets.Length == 0)
        {
            return new MaterializeCupFromOccupiedSlotsResult(
                [],
                [.. matchedMatchIds],
                AlreadyComplete: true);
        }

        if (!noneMatched || unmatchedOnTargets.Length > 0 || anyAttachments)
        {
            throw new ApplicationFailureException(
                "MaterializeCupFromOccupiedSlots requires empty target fixtures or an exact complete match (partial/divergent state is rejected).",
                ApplicationErrorCodes.MaterializationFailure);
        }

        var created = new List<Match>();
        var attached = new List<MatchId>();
        foreach (var legs in resolved.Select(item => CupConfrontationMaterializer.AttachLegs(
                     stage,
                     item.Fixture.Id,
                     item.Home,
                     item.Away,
                     expectedLegs,
                     clock)))
        {
            created.AddRange(legs);
            attached.AddRange(legs.Select(match => match.Id));
        }

        return new MaterializeCupFromOccupiedSlotsResult(created, attached, AlreadyComplete: false);
    }

    private static List<BracketPair> ResolveTargetPairs(
        Stage stage,
        IReadOnlyList<string>? pairKeys,
        bool requestEmpty,
        CupBracketPairMaterialization.ResolutionContext? pairContext,
        Competition? competition = null,
        IReadOnlyList<Stage>? competitionStages = null)
    {
        if (requestEmpty)
        {
            return [.. stage.BracketPairs.Where(pair => IsEligible(stage, pair, pairContext, competition, competitionStages))];
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var targets = new List<BracketPair>(pairKeys!.Count);
        foreach (var rawKey in pairKeys)
        {
            if (string.IsNullOrWhiteSpace(rawKey))
            {
                throw new ApplicationFailureException(
                    "Bracket pair keys must be non-empty.",
                    ApplicationErrorCodes.MaterializationFailure);
            }

            string key;
            try
            {
                key = BracketPair.NormalizePairKey(rawKey);
            }
            catch (DomainException)
            {
                throw new ApplicationFailureException(
                    $"Bracket pair key '{rawKey}' is invalid.",
                    ApplicationErrorCodes.MaterializationFailure);
            }

            if (!seen.Add(key))
            {
                throw new ApplicationFailureException(
                    $"Duplicate bracket pair key '{key}' in the batch.",
                    ApplicationErrorCodes.MaterializationFailure);
            }

            var pair = stage.FindBracketPair(key)
                       ?? throw new ApplicationFailureException(
                           $"Bracket pair '{key}' was not found on the stage.",
                           ApplicationErrorCodes.MaterializationFailure);

            if (!IsEligible(stage, pair, pairContext, competition, competitionStages))
            {
                var evaluation = CupPairEvaluation.Evaluate(
                    new CupPairEvaluationInput(
                        competition!,
                        stage,
                        pair,
                        competitionStages ?? [stage],
                        pairContext?.MatchSummariesByStage,
                        PhaseMutable: true));
                throw new ApplicationFailureException(
                    $"Bracket pair '{key}' is not eligible for materialization.",
                    CupPairEvaluation.ResolveMaterializeErrorCode(evaluation));
            }

            targets.Add(pair);
        }

        return targets;
    }

    /// <summary>
    /// Bracket pairs that <see cref="Execute"/> would materialize when <c>pairKeys</c> is empty.
    /// </summary>
    public static int CountEligiblePairs(
        Stage stage,
        Competition? competition = null,
        IReadOnlyList<Stage>? competitionStages = null,
        IReadOnlyDictionary<StageId, IReadOnlyList<MatchSummaryRow>>? matchSummariesByStage = null)
    {
        ArgumentNullException.ThrowIfNull(stage);
        CupBracketPairMaterialization.ResolutionContext? context = null;
        if (competitionStages is not null)
        {
            context = new CupBracketPairMaterialization.ResolutionContext(competitionStages, matchSummariesByStage);
        }

        return stage.BracketPairs.Count(pair =>
            IsEligible(stage, pair, context, competition, competitionStages));
    }

    /// <summary>Eligible pair keys (no side effects).</summary>
    internal static IReadOnlyList<string> CollectEligiblePairKeys(
        Stage stage,
        Competition? competition = null,
        IReadOnlyList<Stage>? competitionStages = null,
        IReadOnlyDictionary<StageId, IReadOnlyList<MatchSummaryRow>>? matchSummariesByStage = null)
    {
        ArgumentNullException.ThrowIfNull(stage);
        CupBracketPairMaterialization.ResolutionContext? context = null;
        if (competitionStages is not null)
        {
            context = new CupBracketPairMaterialization.ResolutionContext(competitionStages, matchSummariesByStage);
        }

        return
        [
            .. stage.BracketPairs
                .Where(pair => IsEligible(stage, pair, context, competition, competitionStages))
                .Select(pair => pair.PairKey)
        ];
    }

    private static bool IsEligible(
        Stage stage,
        BracketPair pair,
        CupBracketPairMaterialization.ResolutionContext? pairContext = null,
        Competition? competition = null,
        IReadOnlyList<Stage>? competitionStages = null) =>
        CupBracketPairMaterialization.IsEligibleForMaterialization(
            stage,
            pair,
            pairContext,
            competition,
            competitionStages);

    /// <summary>
    /// Late materialization of a not-yet-started Cup stage is allowed while the competition runs.
    /// Does not unlock StructureLocked: a Running stage remains immutable.
    /// </summary>
    private static void EnsureMutable(Competition competition, Stage stage, IReadOnlyList<string>? pairKeys)
    {
        StageMaterializeGuard.EnsureCupCompetition(competition);
        StageMaterializeGuard.EnsureStageReadyForMaterialize(stage);
        StructureMaterializeExecutionGate.EnsureCupStage(stage);
        StructureMaterializeAttributionGate.EnsureCupStage(stage, pairKeys);
        StructureDrawConsumptionGate.EnsureNoObsoleteDrawsOnStage(stage);
    }
}
