// -----------------------------------------------------------------------
// <copyright file="GenerateDrawResolution.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stage;
using MyNet.Generator;
using StageAggregate = MyClub.PlayUp.Domain.Stage.Stage;

namespace MyClub.PlayUp.Application.Stage;

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
        StageAggregate stage,
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
