// -----------------------------------------------------------------------
// <copyright file="OrganisationViewAssembler.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Assembles <see cref="OrganisationViewDto"/> from Competition + loaded stages.
/// </summary>
public static class OrganisationViewAssembler
{
    /// <summary>Blocker: fewer Active entries than EntryRules.MinimumTeams.</summary>
    public const string BlockerInsufficientParticipants = "InsufficientParticipants";

    /// <summary>Blocker: no primary stage.</summary>
    public const string BlockerMissingStage = "MissingStage";

    /// <summary>Blocker: primary stage has no structure.</summary>
    public const string BlockerMissingStructure = "MissingStructure";

    /// <summary>Blocker: Groups format without PotRules.</summary>
    public const string BlockerMissingPotRules = "MissingPotRules";

    /// <summary>Blocker: Cup slots not a power of two (V1).</summary>
    public const string BlockerCupBracketInvalid = "CupBracketInvalid";

    /// <summary>Action: add participant.</summary>
    public const string ActionAddEntry = "AddEntry";

    /// <summary>Action: configure structure.</summary>
    public const string ActionConfigureStructure = "ConfigureStructure";

    /// <summary>Action: replace regulation.</summary>
    public const string ActionReplaceRegulation = "ReplaceRegulation";

    /// <summary>
    /// Builds the Organisation view.
    /// </summary>
    /// <param name="competition">Loaded competition.</param>
    /// <param name="stages">Stages loaded for <see cref="Competition.StageIds"/> (same order).</param>
    /// <returns>Organisation view DTO.</returns>
    public static OrganisationViewDto Assemble(Competition competition, IReadOnlyList<Stage> stages)
    {
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(stages);

        var primary = ResolvePrimaryStage(competition, stages);
        var format = BuildFormatSummary(primary);
        var structure = BuildStructureSummary(primary);
        var participants = BuildParticipants(competition);
        var regulation = BuildRegulation(competition);
        var attachedMatchCount = CountAttachedMatches(primary);
        var readiness = BuildReadiness(competition, primary, format.Kind, structure, attachedMatchCount);
        var actions = BuildActions(competition);

        return new OrganisationViewDto(
            competition.Id.Value,
            competition.Name.Value,
            competition.Status,
            participants,
            format,
            regulation,
            structure,
            actions,
            readiness,
            competition.ShortName?.Value,
            competition.LogoUri?.Value,
            competition.ScheduledStart,
            competition.ScheduledEnd);
    }

    private static int CountAttachedMatches(Stage? primary) =>
        primary?.Matchdays.SelectMany(matchday => matchday.Fixtures)
                .Concat(primary.Rounds.SelectMany(round => round.Fixtures))
                .SelectMany(fixture => fixture.MatchIds)
                .Distinct()
                .Count() ?? 0;

    private static Stage? ResolvePrimaryStage(Competition competition, IReadOnlyList<Stage> stages)
    {
        if (competition.StageIds.Count == 0)
        {
            return null;
        }

        var primaryId = competition.StageIds[0];
        return stages.FirstOrDefault(stage => stage.Id.Equals(primaryId));
    }

    private static OrganisationParticipantsSummaryDto BuildParticipants(Competition competition)
    {
        var entries = competition.Entries
            .Select(entry => new OrganisationEntryDto(
                entry.Id.Value,
                entry.DisplayName,
                entry.Status,
                entry.ShortName?.Value,
                entry.LogoUri?.Value,
                entry.PrimaryColor?.Value,
                entry.SecondaryColor?.Value))
            .ToList();
        var active = competition.Entries.Count(entry => entry.Status == EntryStatus.Active);
        var occupying = competition.Entries.Count(entry => entry.IsOccupying);
        return new OrganisationParticipantsSummaryDto(active, occupying, entries);
    }

    private static OrganisationRegulationSummaryDto BuildRegulation(Competition competition)
    {
        var regulation = competition.Regulation;
        return new OrganisationRegulationSummaryDto(
            regulation.EntryRules.MinimumTeams,
            regulation.EntryRules.MaximumTeams,
            regulation.MatchRules.Duration.DurationPerPeriod,
            regulation.MatchRules.Duration.NumberOfPeriods,
            regulation.StandingRules.Points.WinPoints,
            regulation.StandingRules.Points.DrawPoints,
            regulation.StandingRules.Points.LossPoints);
    }

    private static OrganisationFormatSummaryDto BuildFormatSummary(Stage? primary) =>
        primary is null
            ? new OrganisationFormatSummaryDto(null, null, null, null)
            : new OrganisationFormatSummaryDto(
                InferFormat(primary),
                primary.Id.Value,
                primary.Name.Value,
                primary.Status);

    private static StructureFormatKind? InferFormat(Stage stage) =>
        stage.Rounds.Count > 0
            ? StructureFormatKind.Cup
            : stage.Groups.Count > 0 ? StructureFormatKind.Groups : stage.Matchdays.Count > 0 ? StructureFormatKind.Championship : null;

    private static OrganisationStructureSummaryDto BuildStructureSummary(Stage? primary)
    {
        if (primary is null)
        {
            return new OrganisationStructureSummaryDto(
                0,
                0,
                0,
                0,
                false,
                null,
                MatchGenerationFormat.SingleRoundRobin);
        }

        var drawRules = primary.Regulation.DrawRules;
        return new OrganisationStructureSummaryDto(
            primary.Groups.Count,
            primary.Rounds.Count,
            primary.Matchdays.Count,
            primary.Slots.Count,
            drawRules is not null,
            drawRules?.PotRules?.NumberOfPots,
            primary.MatchGenerationFormat);
    }

    private static OrganisationReadinessDto BuildReadiness(
        Competition competition,
        Stage? primary,
        StructureFormatKind? formatKind,
        OrganisationStructureSummaryDto structure,
        int attachedMatchCount)
    {
        var blockers = new List<string>();

        var activeCount = competition.Entries.Count(entry => entry.Status == EntryStatus.Active);
        var minimum = competition.Regulation.EntryRules.MinimumTeams;
        if (activeCount < minimum)
        {
            blockers.Add(BlockerInsufficientParticipants);
        }

        if (primary is null)
        {
            blockers.Add(BlockerMissingStage);
        }
        else if (formatKind is null)
        {
            blockers.Add(BlockerMissingStructure);
        }

        var readyForDraw = false;
        var readyForSchedulePath = false;
        var readyForMaterialization = false;

        if (primary is not null && formatKind is not null && blockers.Count == 0)
        {
            switch (formatKind)
            {
                case StructureFormatKind.Championship:
                    readyForSchedulePath = structure.MatchdayCount > 0;
                    readyForMaterialization = activeCount >= 2 && structure.MatchdayCount > 0;
                    break;
                case StructureFormatKind.Groups:
                    if (structure.NumberOfPots is null)
                    {
                        blockers.Add(BlockerMissingPotRules);
                    }
                    else
                    {
                        readyForDraw = structure is { GroupCount: >= 2, MatchdayCount: >= 1 };
                        var assigned = primary.Groups.Sum(group => group.EntryIds.Count);
                        readyForMaterialization = assigned >= 2 && primary.Groups.All(group => group.EntryIds.Count >= 2);
                    }

                    break;
                case StructureFormatKind.Cup:
                    if (structure.SlotCount < 2 || !IsPowerOfTwo(structure.SlotCount))
                    {
                        blockers.Add(BlockerCupBracketInvalid);
                    }
                    else
                    {
                        readyForDraw = structure.RoundCount >= 1;
                        readyForMaterialization = readyForDraw;
                    }

                    break;
                case null:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(formatKind), formatKind, null);
            }
        }

        var readyForSchedule = attachedMatchCount > 0;
        var readyForMatchOperation = attachedMatchCount > 0
            && competition.Status is not CompetitionStatus.Completed
            and not CompetitionStatus.Archived;
        var readyForNext = blockers.Count == 0 && (readyForDraw || readyForSchedulePath);

        return new OrganisationReadinessDto(
            readyForNext,
            readyForDraw,
            readyForMaterialization,
            readyForSchedule,
            readyForMatchOperation,
            readyForSchedulePath,
            attachedMatchCount,
            blockers);
    }

    private static IReadOnlyList<string> BuildActions(Competition competition) =>
        competition.Status switch
        {
            CompetitionStatus.Completed or CompetitionStatus.Archived => [],
            CompetitionStatus.Running or CompetitionStatus.Suspended => ["WithdrawEntry"],
            _ =>
            [
                ActionAddEntry, ActionConfigureStructure, ActionReplaceRegulation, "RenameEntry", "WithdrawEntry",
                "ExcludeEntry"
            ]
        };

    private static bool IsPowerOfTwo(int value) => value > 0 && (value & (value - 1)) == 0;
}
