// -----------------------------------------------------------------------
// <copyright file="StructurePopulationVolume.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Structural population volume helpers (aligned with SPA <c>structurePopulationVolume.ts</c>).
/// </summary>
public static class StructurePopulationVolume
{
    /// <summary>
    /// Volume of entries promised by one qualification path.
    /// </summary>
    /// <param name="path">Qualification path.</param>
    /// <returns>Positive volume.</returns>
    public static int QualificationPathVolume(QualificationPath path)
    {
        ArgumentNullException.ThrowIfNull(path);

        return path.Selection.Mode switch
        {
            SelectionMode.Position => 1,
            SelectionMode.Range =>
                Math.Max(1, (path.Selection.EndValue ?? path.Selection.Value) - path.Selection.Value + 1),
            SelectionMode.Top or SelectionMode.Bottom => Math.Max(1, path.Selection.Value),
            _ => Math.Max(1, path.Selection.Value)
        };
    }

    /// <summary>
    /// Configured inbound volume toward a destination stage (Qual all sources + Prog Population and Place).
    /// </summary>
    /// <param name="stages">Competition stages.</param>
    /// <param name="destinationStageId">Destination stage.</param>
    /// <returns>Inbound configured volume.</returns>
    public static int InboundConfiguredVolume(IReadOnlyList<Stage> stages, StageId destinationStageId)
    {
        ArgumentNullException.ThrowIfNull(stages);

        var n = 0;
        foreach (var source in stages)
        {
            if (source.Regulation.QualificationRules is { } qualification)
            {
                n += qualification.Paths.Where(path => path.Destination.StageId.Equals(destinationStageId)).Sum(QualificationPathVolume);
            }

            if (source.Regulation.ProgressionRules is { } progression)
            {
                n += progression.Paths.Count(path => path.Destination.StageId.Equals(destinationStageId));
            }
        }

        return n;
    }

    /// <summary>
    /// Expected Places N occupancy = affectation + inbound configured volume.
    /// </summary>
    /// <param name="destination">Destination stage.</param>
    /// <param name="stages">Competition stages.</param>
    /// <returns>Expected occupancy.</returns>
    public static int ExpectedOccupancy(Stage destination, IReadOnlyList<Stage> stages) =>
        destination.AffectationAuthoring.Count + InboundConfiguredVolume(stages, destination.Id);

    /// <summary>
    /// Structural places capacity of a stage when known; otherwise <see langword="null"/>.
    /// </summary>
    /// <param name="stage">Stage.</param>
    /// <returns>Capacity or <see langword="null"/> when unbound.</returns>
    public static int? ResolvePlacesCapacity(Stage stage)
    {
        ArgumentNullException.ThrowIfNull(stage);

        return stage.Slots.Count > 0
            ? stage.Slots.Count
            : stage.Groups.Count > 0 && stage.PlacesPerGroup is { } placesPerGroup ? stage.Groups.Count * placesPerGroup : null;
    }

    /// <summary>
    /// Outbound qualification extraction volume leaving a source stage.
    /// </summary>
    /// <param name="source">Source stage.</param>
    /// <returns>Extracted volume.</returns>
    public static int OutboundQualificationVolume(Stage source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return source.Regulation.QualificationRules is not { } qualification ? 0 : qualification.Paths.Sum(QualificationPathVolume);
    }

    /// <summary>
    /// Volume promised by one qualification path on the Structure read DTO (SPA parity).
    /// </summary>
    public static int QualificationPathVolume(StructureQualificationPathDto path)
    {
        ArgumentNullException.ThrowIfNull(path);

        return path.SelectionMode switch
        {
            SelectionMode.Position => 1,
            SelectionMode.Range =>
                Math.Max(1, (path.SelectionEndValue ?? path.SelectionValue) - path.SelectionValue + 1),
            SelectionMode.Top or SelectionMode.Bottom => Math.Max(1, path.SelectionValue),
            _ => Math.Max(1, path.SelectionValue)
        };
    }

    /// <summary>
    /// Inbound configured volume toward a destination (Structure view — SPA <c>inboundPopulationConfiguredVolume</c>).
    /// </summary>
    public static int InboundConfiguredVolume(StructureViewDto view, Guid destinationStageId)
    {
        ArgumentNullException.ThrowIfNull(view);

        var volume = 0;
        foreach (var source in view.Stages)
        {
            if (source.QualificationPaths is { } qualificationPaths)
            {
                volume += qualificationPaths
                    .Where(path => path.DestinationStageId == destinationStageId)
                    .Sum(QualificationPathVolume);
            }

            if (source.ProgressionPaths is { } progressionPaths)
            {
                volume += progressionPaths.Count(path => path.DestinationStageId == destinationStageId);
            }
        }

        return volume;
    }

    /// <summary>
    /// Expected Places N occupancy for U1 meter (affectation + inbound qual/prog).
    /// </summary>
    public static int ExpectedOccupancy(StructureViewDto view, StructureStageHubSummaryDto destination)
    {
        ArgumentNullException.ThrowIfNull(view);
        ArgumentNullException.ThrowIfNull(destination);

        return destination.AffectationEntryCount
            + InboundConfiguredVolume(view, destination.StageId);
    }

    /// <summary>
    /// Places N for presentation (SPA <c>resolvePlacesN</c>) — prefers hub <see cref="StructureStageHubSummaryDto.CompositionCapacity"/>.
    /// </summary>
    public static int? ResolvePlacesN(StructureStageHubSummaryDto hub)
    {
        ArgumentNullException.ThrowIfNull(hub);

        if (hub.CompositionCapacity is > 0)
        {
            return hub.CompositionCapacity;
        }

        var kind = hub.FormatKind;
        if (kind == StructureFormatKind.Cup || (kind is null && hub.SlotCount > 0))
        {
            return ResolveCupEntryPlacesFromHub(hub);
        }

        if (kind != StructureFormatKind.Groups && hub.GroupCount <= 0) return null;
        var groups = hub.GroupCount;
        var perGroup = hub.PlacesPerGroup ?? hub.NumberOfPots;
        return groups > 0 && perGroup is >= 2 ? groups * perGroup.Value : null;
    }

    /// <summary>
    /// SPA U1: synthesize <see cref="CompetitionFlowAnalyzer.PopulationShortfall"/> when occupancy &lt; Places N.
    /// </summary>
    public static bool ShouldSynthesizePopulationShortfall(
        StructureViewDto view,
        StructureStageHubSummaryDto stage,
        IReadOnlyList<string> rawIssueCodes)
    {
        ArgumentNullException.ThrowIfNull(view);
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(rawIssueCodes);

        if (rawIssueCodes.Contains(CompetitionFlowAnalyzer.PopulationShortfall, StringComparer.Ordinal))
        {
            return false;
        }

        var places = ResolvePlacesN(stage);
        return places is not (null or <= 0) && ExpectedOccupancy(view, stage) < places.Value;
    }

    private static int? ResolveCupEntryPlacesFromHub(StructureStageHubSummaryDto hub)
    {
        var slotCount = hub.SlotCount;
        if (slotCount <= 0)
        {
            return null;
        }

        var roundCount = hub.RoundCount;
        if (roundCount <= 1)
        {
            return slotCount;
        }

        var fullTreeSlots = (1 << (roundCount + 1)) - 2;
        if (slotCount == fullTreeSlots)
        {
            return 1 << roundCount;
        }

        var firstRoundSlots = 1 << roundCount;
        if (slotCount == firstRoundSlots)
        {
            return firstRoundSlots;
        }

        if (slotCount < 2 || (slotCount + 2) % 2 != 0)
        {
            return null;
        }

        var entryPlaces = (slotCount + 2) / 2;
        return IsPowerOfTwo(entryPlaces) && entryPlaces >= 2 ? entryPlaces : null;
    }

    private static bool IsPowerOfTwo(int value) => value > 0 && (value & (value - 1)) == 0;
}
