// -----------------------------------------------------------------------
// <copyright file="ConfigureStructure.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Competitions;

/// <summary>
/// Application use case: configure primary Stage structure from a typed V1 format intent.
/// </summary>
/// <remarks>
/// Orchestrates Domain APIs only — not a generic Stage builder. Atomicity is the caller's
/// single SaveChanges (Stage create + Competition.AddStage + structure in one unit of work).
/// Does not generate Draw, Schedule, or Matches.
/// </remarks>
public static class ConfigureStructure
{
    /// <summary>
    /// Ensures a primary stage exists, clears prior structure, and builds the intent skeleton.
    /// </summary>
    /// <param name="competition">Owning competition.</param>
    /// <param name="primaryStage">Existing first stage when present; otherwise null.</param>
    /// <param name="intent">Typed format intent.</param>
    /// <param name="clock">Clock for domain events.</param>
    /// <returns>Stage (possibly new) ready for persistence.</returns>
    public static ConfigureStructureResult Execute(
        Competition competition,
        Stage? primaryStage,
        StructureIntent intent,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(intent);
        ArgumentNullException.ThrowIfNull(clock);

        if (competition.Status is CompetitionStatus.Running
            or CompetitionStatus.Suspended
            or CompetitionStatus.Completed
            or CompetitionStatus.Archived)
        {
            throw new ApplicationFailureException(
                $"Organisation structure cannot be configured while competition status is '{competition.Status}'.",
                ApplicationErrorCodes.OrganisationNotMutable);
        }

        var stageCreated = false;
        Stage stage;
        if (primaryStage is null)
        {
            if (competition.StageIds.Count > 0)
            {
                throw new ApplicationFailureException(
                    "Competition references stages that were not loaded for ConfigureStructure.",
                    ApplicationErrorCodes.StageNotFound);
            }

            stage = Stage.Create(
                competition.Id,
                new StageName(intent.StageName),
                competition.Regulation,
                clock);
            competition.AddStage(stage.Id, clock);
            stageCreated = true;
        }
        else
        {
            if (competition.StageIds.Count == 0 || !competition.StageIds[0].Equals(primaryStage.Id))
            {
                throw new ApplicationFailureException(
                    $"Stage '{primaryStage.Id}' is not the primary stage of competition '{competition.Id}'.",
                    ApplicationErrorCodes.StageNotInCompetition);
            }

            stage = primaryStage;
            if (!string.Equals(stage.Name.Value, intent.StageName, StringComparison.Ordinal))
            {
                stage.Rename(new StageName(intent.StageName), clock);
            }

            ClearStructure(stage, clock);
        }

        switch (intent.Format)
        {
            case StructureFormatKind.Championship:
                BuildChampionship(stage, intent.MatchdayCount, clock);
                break;
            case StructureFormatKind.Groups:
                BuildGroups(stage, intent.GroupCount, intent.ParticipantsPerGroup, clock);
                break;
            case StructureFormatKind.Cup:
                BuildCup(stage, intent.BracketSize, clock);
                break;
            default:
                throw new ApplicationFailureException(
                    $"Unknown structure format '{intent.Format}'.",
                    ApplicationErrorCodes.InvalidStructureIntent);
        }

        return new ConfigureStructureResult(stage, stageCreated);
    }

    private static void BuildChampionship(Stage stage, int matchdayCount, IClock clock)
    {
        for (var number = 1; number <= matchdayCount; number++)
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

        stage.AddMatchday(1, clock);
        stage.ReplaceDrawRules(
            new DrawRules(DrawMode.Random, potRules: new PotRules(participantsPerGroup)),
            clock);
    }

    private static void BuildCup(Stage stage, int bracketSize, IClock clock)
    {
        stage.AddRound("Tour principal", clock);
        for (var index = 1; index <= bracketSize; index++)
        {
            stage.AddSlot($"S{index}", clock);
        }

        stage.ReplaceDrawRules(new DrawRules(DrawMode.Random), clock);
    }

    private static void ClearStructure(Stage stage, IClock clock)
    {
        foreach (var round in stage.Rounds.ToList())
        {
            stage.RemoveRound(round.Id, clock);
        }

        foreach (var matchday in stage.Matchdays.ToList())
        {
            stage.RemoveMatchday(matchday.Id, clock);
        }

        foreach (var group in stage.Groups.ToList())
        {
            stage.RemoveGroup(group.Id, clock);
        }

        foreach (var slot in stage.Slots.ToList())
        {
            if (stage.DirectAssignments.Any(a =>
                    string.Equals(a.SlotKey, slot.SlotKey, StringComparison.Ordinal)))
            {
                stage.ClearSlotAssignment(slot.SlotKey, clock);
            }

            stage.RemoveSlot(slot.SlotKey, clock);
        }

        stage.ReplaceDrawRules(null, clock);
    }

    private static string GroupLabel(int index) => index < 26 ? ((char)('A' + index)).ToString() : $"G{index + 1}";
}
