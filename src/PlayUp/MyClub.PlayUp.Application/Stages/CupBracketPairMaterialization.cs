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
        ResolutionContext? context = null)
    {
        if (stage.FindSlot(slotKey)?.EntryId is { } occupant)
        {
            return occupant;
        }

        if (context is null)
        {
            return null;
        }

        return CupInboundSlotEntryResolver.TryResolve(
            stage,
            slotKey,
            context.Value.CompetitionStages,
            context.Value.MatchSummariesByStage);
    }

    internal static bool TryResolvePairEntries(
        Stage stage,
        BracketPair pair,
        ResolutionContext? context,
        out EntryId home,
        out EntryId away)
    {
        home = default;
        away = default;
        var idA = ResolveSlotEntry(stage, pair.SlotAKey, context);
        var idB = ResolveSlotEntry(stage, pair.SlotBKey, context);
        if (idA is null || idB is null || idA.Value.Equals(idB.Value))
        {
            return false;
        }

        home = idA.Value;
        away = idB.Value;
        return true;
    }

    internal static bool IsPairDetermined(Stage stage, BracketPair pair, ResolutionContext? context = null) =>
        TryResolvePairEntries(stage, pair, context, out _, out _);

    internal static int ExpectedLegs(Stage stage, BracketPair pair)
    {
        var fixture = stage.FindFixtureByBracketPairKey(pair.PairKey);
        if (fixture is not null)
        {
            return CupConfrontationMaterializer.ExpectedLegsForFixture(stage, fixture);
        }

        return stage.Rounds.Count > 0
            ? CupConfrontationMaterializer.ExpectedLegsForRound(stage.Rounds[0])
            : 1;
    }

    /// <summary>
    /// True when the pair is determined and attached matches cover the round tie-format legs.
    /// </summary>
    internal static bool IsConfrontationMaterialized(
        Stage stage,
        BracketPair pair,
        ResolutionContext? context = null)
    {
        if (!IsPairDetermined(stage, pair, context))
        {
            return false;
        }

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
        return slotA?.EntryId is { } home
               && slotB?.EntryId is { } away
               && !home.Equals(away);
    }
}
