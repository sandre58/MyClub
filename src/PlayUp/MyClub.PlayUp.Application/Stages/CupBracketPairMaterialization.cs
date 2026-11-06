// -----------------------------------------------------------------------
// <copyright file="CupBracketPairMaterialization.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Cup bracket-pair grain for materialization: fixture alone is not enough — matches must cover expected legs.
/// </summary>
internal static class CupBracketPairMaterialization
{
    internal readonly record struct ResolutionContext(
        IReadOnlyList<Stage> CompetitionStages,
        IReadOnlyDictionary<StageId, IReadOnlyList<MatchSummaryRow>>? MatchSummariesByStage);

    internal static EntryId? ResolveSlotEntry(
        Stage stage,
        string slotKey,
        ResolutionContext? context = null) =>
        stage.FindSlot(slotKey)?.EntryId ?? (context is null
            ? null
            : CupInboundSlotEntryResolver.TryResolve(
                stage,
                slotKey,
                context.Value.CompetitionStages,
                context.Value.MatchSummariesByStage));

    internal static bool TryResolvePairEntries(
        Stage stage,
        BracketPair pair,
        ResolutionContext? context,
        out EntryId? home,
        out EntryId? away)
    {
        home = null;
        away = null;
        if (stage.FindSlot(pair.SlotAKey) is null || stage.FindSlot(pair.SlotBKey) is null)
        {
            return false;
        }

        home = ResolveSlotEntry(stage, pair.SlotAKey, context);
        away = ResolveSlotEntry(stage, pair.SlotBKey, context);
        return home is not { } resolvedHome || away is not { } resolvedAway || !resolvedHome.Equals(resolvedAway);
    }

    internal static bool IsPairDetermined(Stage stage, BracketPair pair, ResolutionContext? context = null) =>
        TryResolvePairEntries(stage, pair, context, out var home, out var away)
        && home is not null
        && away is not null;

    internal static int ExpectedLegs(Stage stage, BracketPair pair)
    {
        var fixture = stage.FindFixtureByBracketPairKey(pair.PairKey);
        return fixture is not null ? CupConfrontationMaterializer.ExpectedLegsForFixture(stage, fixture) : stage.Rounds.Count > 0 ? CupConfrontationMaterializer.ExpectedLegsForRound(stage.Rounds[0]) : 1;
    }

    /// <summary>
    /// True when attached matches cover the round tie-format legs, including a side that is still unknown.
    /// </summary>
    internal static bool IsConfrontationMaterialized(
        Stage stage,
        BracketPair pair,
        ResolutionContext? context = null)
    {
        _ = context;
        var fixture = stage.FindFixtureByBracketPairKey(pair.PairKey);
        if (fixture is null)
        {
            return false;
        }

        var expectedLegs = ExpectedLegs(stage, pair);
        return fixture.Attachments.Count >= expectedLegs;
    }

    internal static bool IsEligibleForMaterialization(
        Stage stage,
        BracketPair pair,
        ResolutionContext? context = null,
        Competition? competition = null,
        IReadOnlyList<Stage>? competitionStages = null,
        bool phaseMutable = true) =>
        competition is not null && competitionStages is not null
            ? CupPairEvaluation.Evaluate(
                new CupPairEvaluationInput(
                    competition,
                    stage,
                    pair,
                    competitionStages,
                    context?.MatchSummariesByStage,
                    phaseMutable)).EligibleForMaterialize
            : IsPairPersistedDistinct(stage, pair)
              && !IsConfrontationMaterialized(stage, pair, context);

    internal static bool IsPairPersistedDistinct(Stage stage, BracketPair pair)
    {
        var slotA = stage.FindSlot(pair.SlotAKey);
        var slotB = stage.FindSlot(pair.SlotBKey);
        if (slotA is null || slotB is null)
        {
            return false;
        }

        return slotA.EntryId is not { } home
               || slotB.EntryId is not { } away
               || !home.Equals(away);
    }
}
