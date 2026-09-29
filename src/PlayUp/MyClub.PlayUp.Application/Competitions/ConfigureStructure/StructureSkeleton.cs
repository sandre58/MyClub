// -----------------------------------------------------------------------
// <copyright file="StructureSkeleton.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Competitions;

/// <summary>
/// Shared form materialization for V1 structure intents.
/// Skeleton = topology capacity only — no DrawRules, relations, or population.
/// </summary>
internal static class StructureSkeleton
{
    /// <summary>
    /// Infers the current format kind from topology (null when unstructured).
    /// </summary>
    public static StructureFormatKind? InferFormat(Stage stage) =>
        stage.IsSwiss
            ? StructureFormatKind.Swiss
            : stage.Rounds.Count > 0
            ? StructureFormatKind.Cup
            : stage.Groups.Count > 0
            ? StructureFormatKind.Groups
            : stage.Matchdays.Count > 0
            ? StructureFormatKind.Championship
            : null;

    /// <summary>
    /// Counts distinct matches attached to the stage topology.
    /// </summary>
    public static int CountAttachedMatches(Stage stage) =>
        stage.Matchdays.SelectMany(matchday => matchday.Fixtures)
            .Concat(stage.Rounds.SelectMany(round => round.Fixtures))
            .SelectMany(fixture => fixture.MatchIds)
            .Distinct()
            .Count();

    /// <summary>
    /// Clears topology (including DrawRules) and returns an impact snapshot.
    /// </summary>
    public static StructureRebuildImpact Clear(Stage stage, IClock clock)
    {
        var impact = new StructureRebuildImpact(
            ClearedMatchdays: stage.Matchdays.Count,
            ClearedGroups: stage.Groups.Count,
            ClearedRounds: stage.Rounds.Count,
            ClearedSlots: stage.Slots.Count,
            ClearedDirectAssignments: stage.DirectAssignments.Count,
            ClearedCompositionEntries: stage.CompositionEntries.Count,
            ClearedDrawRules: stage.Regulation.DrawRules is not null,
            ClearedSwissSettings: stage.SwissSettings is not null);

        stage.ClearSwissConfiguration();

        foreach (var round in stage.Rounds.ToList())
        {
            stage.RemoveRound(round.Id, clock);
        }

        foreach (var matchday in stage.Matchdays.ToList())
        {
            stage.RemoveMatchday(matchday.Id, clock);
        }

        // BracketPairs after fixtures (rounds/matchdays) so referenced PairKeys can be cleared.
        stage.ClearBracketPairs();

        foreach (var group in stage.Groups.ToList())
        {
            stage.RemoveGroup(group.Id, clock);
        }

        foreach (var slot in stage.Slots.ToList())
        {
            if (stage.DirectAssignments.Any(a =>
                    string.Equals(a.SlotKey, slot.SlotKey, StringComparison.Ordinal)))
            {
                stage.ClearSlotAssignment(slot.SlotKey);
            }

            stage.RemoveSlot(slot.SlotKey);
        }

        stage.ClearCompositionEntries(clock);
        stage.ReplaceDrawRules(null, clock);
        stage.SetPlacesPerGroup(null);
        return impact;
    }

    /// <summary>
    /// Materializes form from <paramref name="intent"/> without seeding DrawRules or relations.
    /// </summary>
    public static void Apply(
        Stage stage,
        StructureIntent intent,
        StandingRules standingDefaults,
        IClock clock)
    {
        switch (intent.Format)
        {
            case StructureFormatKind.Championship:
                BuildChampionship(stage, clock);
                break;
            case StructureFormatKind.Groups:
                BuildGroups(stage, intent.GroupCount, intent.ParticipantsPerGroup, clock);
                break;
            case StructureFormatKind.Cup:
                BuildCup(stage, intent.BracketSize, clock);
                break;
            case StructureFormatKind.Swiss:
                BuildSwiss(stage, intent.SwissRoundCount, clock);
                break;
            default:
                throw new ApplicationFailureException(
                    $"Unknown structure format '{intent.Format}'.",
                    ApplicationErrorCodes.InvalidStructureIntent);
        }

        AlignStandingRulesForIntent(stage, intent.Format, standingDefaults, clock);

        // Domain gate: MatchGenerationFormat only on RR topology (Championship / Groups).
        // Cup / Swiss must not call SetMatchGenerationFormat (rounds / SwissSettings).
        if (intent.Format is StructureFormatKind.Championship or StructureFormatKind.Groups)
        {
            stage.SetMatchGenerationFormat(intent.MatchGenerationFormat);
        }
    }

    /// <summary>
    /// Ensures rebuild intents keep the existing format kind when the stage is already structured.
    /// </summary>
    public static void EnsureSameKind(Stage stage, StructureFormatKind intentFormat)
    {
        var current = InferFormat(stage);
        if (current is null)
        {
            return;
        }

        if (current != intentFormat)
        {
            throw new ApplicationFailureException(
                $"Stage format '{current}' cannot be changed to '{intentFormat}' after creation.",
                ApplicationErrorCodes.StructureFormatImmutable);
        }
    }

    private static void AlignStandingRulesForIntent(
        Stage stage,
        StructureFormatKind format,
        StandingRules standingDefaults,
        IClock clock)
    {
        if (format is StructureFormatKind.Cup)
        {
            stage.ClearStandingRules(clock);
            return;
        }

        stage.SeedStandingRules(standingDefaults, clock);
    }

    private static void BuildChampionship(Stage stage, IClock clock)
    {
        // Seed technique minimal du skeleton. Ne fait pas partie de StructureIntent
        // et ne représente pas le nombre de journées du championnat.
        const int initialMatchdaySeedCount = 1;
        for (var number = 1; number <= initialMatchdaySeedCount; number++)
        {
            stage.AddMatchday(number, clock);
        }

        stage.ReplaceDrawRules(null, clock);
    }

    private static void BuildGroups(Stage stage, int groupCount, int participantsPerGroup, IClock clock)
    {
        for (var index = 0; index < groupCount; index++)
        {
            stage.AddGroup(GroupLabel(index), clock);
        }

        // Technical initial match structure — not a product matchday-count contract.
        stage.AddMatchday(1, clock);
        stage.SetPlacesPerGroup(participantsPerGroup);
        stage.ReplaceDrawRules(null, clock);
    }

    private static void BuildCup(Stage stage, int bracketSize, IClock clock)
    {
        stage.ClearStandingRules(clock);
        stage.AddRound("Tour principal", clock);
        for (var index = 1; index <= bracketSize; index++)
        {
            stage.AddSlot($"S{index}");
        }

        stage.SeedEntryRoundBracketPairs();
        stage.ReplaceDrawRules(null, clock);
    }

    private static void BuildSwiss(Stage stage, int roundCount, IClock clock)
    {
        stage.ReplaceDrawRules(null, clock);
        stage.SetSwissSettings(new SwissSettings(roundCount));
    }

    private static string GroupLabel(int index) =>
        index < 26 ? ((char)('A' + index)).ToString() : $"G{index + 1}";
}
