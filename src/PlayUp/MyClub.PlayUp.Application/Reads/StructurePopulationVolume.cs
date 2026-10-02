// -----------------------------------------------------------------------
// <copyright file="StructurePopulationVolume.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

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
}
