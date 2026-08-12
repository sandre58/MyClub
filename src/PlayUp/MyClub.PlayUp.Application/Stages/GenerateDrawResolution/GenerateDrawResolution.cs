// -----------------------------------------------------------------------
// <copyright file="GenerateDrawResolution.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using MyNet.Generator;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Application use case: generate a Draw resolution and record it (or MarkNoSolution).
/// Does not Publish, Apply, or Cancel.
/// </summary>
public static class GenerateDrawResolution
{
    /// <summary>
    /// Generates a resolution for a Draft draw and records the outcome on the Stage.
    /// </summary>
    /// <param name="stage">Stage that owns the Draw.</param>
    /// <param name="drawId">Draw identity.</param>
    /// <param name="clock">Clock for domain events.</param>
    /// <param name="slotTargets">Required for Slot kind (destination slot keys).</param>
    /// <param name="constraintContext">Optional maps for Required pairing constraints.</param>
    /// <param name="seed">Optional RNG seed (Application builds <see cref="SeededRandomSource"/>).</param>
    /// <param name="randomSource">Optional injected source; when null, uses seed or <see cref="SystemRandomSource"/>.</param>
    /// <returns>The generation result (also recorded on the Draw).</returns>
    public static DrawGenerationResult Execute(
        Stage stage,
        DrawId drawId,
        IClock clock,
        IReadOnlyList<string>? slotTargets = null,
        DrawConstraintContext? constraintContext = null,
        int? seed = null,
        IRandomSource? randomSource = null)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(clock);

        var draw = stage.GetDraw(drawId);
        EnsureDraftMutable(draw);

        if (draw.Inputs is null)
        {
            throw new ApplicationFailureException(
                $"Draw '{draw.Id}' requires configured inputs before generation.",
                ApplicationErrorCodes.DrawGenerationFailure);
        }

        EnsureSlotTargetsExistOnStage(stage, draw.Kind, slotTargets);

        var request = BuildRequest(
            draw,
            stage.Regulation.DrawRules,
            slotTargets,
            constraintContext ?? DrawConstraintContext.Empty,
            ResolveRandomSource(seed, randomSource));

        var result = DrawResolutionGenerator.Generate(request);
        if (result.IsNoSolution)
        {
            stage.MarkDrawNoSolution(drawId, clock);
        }
        else
        {
            stage.RecordDrawResolution(drawId, result.Resolution!, clock);
        }

        return result;
    }

    private static void EnsureDraftMutable(Draw draw)
    {
        if (draw.Status != DrawStatus.Draft)
        {
            throw new ApplicationFailureException(
                $"Draw '{draw.Id}' must be Draft to generate (status is '{draw.Status}').",
                ApplicationErrorCodes.DrawGenerationFailure);
        }
    }

    /// <summary>
    /// Ensures Slot-kind targets exist on the Stage before Domain generation / Record.
    /// Pairing and Group do not use <paramref name="slotTargets"/>.
    /// </summary>
    private static void EnsureSlotTargetsExistOnStage(
        Stage stage,
        DrawResolutionKind kind,
        IReadOnlyList<string>? slotTargets)
    {
        if (kind != DrawResolutionKind.Slot || slotTargets is null || slotTargets.Count == 0)
        {
            return;
        }

        foreach (var target in slotTargets)
        {
            if (stage.FindSlot(target) is null)
            {
                throw new ApplicationFailureException(
                    $"Slot target '{target}' was not found on stage '{stage.Id}'.",
                    ApplicationErrorCodes.DrawGenerationFailure);
            }
        }
    }

    private static DrawGenerationRequest BuildRequest(
        Draw draw,
        DrawRules? drawRules,
        IReadOnlyList<string>? slotTargets,
        DrawConstraintContext constraintContext,
        IRandomSource randomSource)
    {
        var inputs = draw.Inputs!;
        var constraints = drawRules?.Constraints
            .Where(c => c.Enforcement == ConstraintEnforcement.Required)
            .ToArray()
            ?? [];

        return new DrawGenerationRequest(
            draw.Kind,
            inputs.Entries,
            constraints,
            constraintContext,
            randomSource,
            slotTargets,
            inputs.FixedSlots,
            inputs.FixedPairings);
    }

    private static IRandomSource ResolveRandomSource(int? seed, IRandomSource? randomSource) => randomSource ?? (seed is null ? new SystemRandomSource() : new SeededRandomSource(seed.Value));
}
