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
            readiness);
    }

    private static int CountAttachedMatches(Stage? primary)
    {
        if (primary is null)
        {
            return 0;
        }

        return primary.Matchdays.SelectMany(matchday => matchday.Fixtures)
            .Concat(primary.Rounds.SelectMany(round => round.Fixtures))
            .SelectMany(fixture => fixture.MatchIds)
            .Distinct()
            .Count();
    }

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
            .Select(entry => new OrganisationEntryDto(entry.Id.Value, entry.DisplayName, entry.Status))
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

    private static OrganisationFormatSummaryDto BuildFormatSummary(Stage? primary)
    {
        if (primary is null)
        {
            return new OrganisationFormatSummaryDto(null, "Non configuré", null, null, null);
        }

        var kind = InferFormat(primary);
        var label = kind switch
        {
            StructureFormatKind.Championship => "Championnat",
            StructureFormatKind.Groups => "Groupes",
            StructureFormatKind.Cup => "Coupe",
            null => "Structure partielle",
            _ => "Structure partielle"
        };

        return new OrganisationFormatSummaryDto(
            kind,
            label,
            primary.Id.Value,
            primary.Name.Value,
            primary.Status);
    }

    private static StructureFormatKind? InferFormat(Stage stage)
    {
        if (stage.Rounds.Count > 0)
        {
            return StructureFormatKind.Cup;
        }

        if (stage.Groups.Count > 0)
        {
            return StructureFormatKind.Groups;
        }

        if (stage.Matchdays.Count > 0)
        {
            return StructureFormatKind.Championship;
        }

        return null;
    }

    private static OrganisationStructureSummaryDto BuildStructureSummary(Stage? primary)
    {
        if (primary is null)
        {
            return new OrganisationStructureSummaryDto(0, 0, 0, 0, false, null);
        }

        var drawRules = primary.Regulation.DrawRules;
        return new OrganisationStructureSummaryDto(
            primary.Groups.Count,
            primary.Rounds.Count,
            primary.Matchdays.Count,
            primary.Slots.Count,
            drawRules is not null,
            drawRules?.PotRules?.NumberOfPots);
    }

    private static OrganisationReadinessDto BuildReadiness(
        Competition competition,
        Stage? primary,
        StructureFormatKind? formatKind,
        OrganisationStructureSummaryDto structure,
        int attachedMatchCount)
    {
        var blockers = new List<string>();
        var hints = new List<string>();

        var activeCount = competition.Entries.Count(entry => entry.Status == EntryStatus.Active);
        var minimum = competition.Regulation.EntryRules.MinimumTeams;
        if (activeCount < minimum)
        {
            blockers.Add(BlockerInsufficientParticipants);
            hints.Add($"Au moins {minimum} participants actifs sont requis (actuellement {activeCount}).");
        }

        if (primary is null)
        {
            blockers.Add(BlockerMissingStage);
            hints.Add("Configurer la structure (Championnat, Groupes ou Coupe) pour créer la première phase.");
        }
        else if (formatKind is null)
        {
            blockers.Add(BlockerMissingStructure);
            hints.Add("La phase primaire n’a pas encore de structure de format.");
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
                    hints.Add(attachedMatchCount == 0
                        ? "Championnat : matérialiser les matchs (aller simple), puis optionnellement planifier."
                        : "Championnat : matchs matérialisés — calendrier optionnel.");
                    break;
                case StructureFormatKind.Groups:
                    if (structure.NumberOfPots is null)
                    {
                        blockers.Add(BlockerMissingPotRules);
                        hints.Add("Les PotRules sont requis pour un tirage de groupes.");
                    }
                    else
                    {
                        readyForDraw = structure.GroupCount >= 2 && structure.MatchdayCount >= 1;
                        var assigned = primary.Groups.Sum(group => group.EntryIds.Count);
                        readyForMaterialization = assigned >= 2 && primary.Groups.All(group => group.EntryIds.Count >= 2);
                        hints.Add(readyForMaterialization
                            ? "Groupes : tirage appliqué — matérialiser les matchs intra-groupes."
                            : "Groupes : créer/générer/publier/appliquer le tirage, puis matérialiser.");
                    }

                    break;
                case StructureFormatKind.Cup:
                    if (structure.SlotCount < 2 || !IsPowerOfTwo(structure.SlotCount))
                    {
                        blockers.Add(BlockerCupBracketInvalid);
                        hints.Add("Coupe V1 : le nombre de slots doit être une puissance de 2.");
                    }
                    else
                    {
                        readyForDraw = structure.RoundCount >= 1;
                        readyForMaterialization = readyForDraw;
                        hints.Add(attachedMatchCount == 0
                            ? "Coupe : tirage Pairing (Generate/Publish/Apply) pour créer les matchs."
                            : "Coupe : matchs exploitables présents — calendrier optionnel.");
                    }

                    break;
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
            blockers,
            hints);
    }

    private static IReadOnlyList<string> BuildActions(Competition competition)
    {
        if (competition.Status is CompetitionStatus.Completed or CompetitionStatus.Archived)
        {
            return [];
        }

        if (competition.Status is CompetitionStatus.Running or CompetitionStatus.Suspended)
        {
            return ["WithdrawEntry"];
        }

        return [ActionAddEntry, ActionConfigureStructure, ActionReplaceRegulation, "RenameEntry", "WithdrawEntry", "ExcludeEntry"];
    }

    private static bool IsPowerOfTwo(int value) => value > 0 && (value & (value - 1)) == 0;
}
