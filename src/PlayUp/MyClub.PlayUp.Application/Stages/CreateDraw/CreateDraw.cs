// -----------------------------------------------------------------------
// <copyright file="CreateDraw.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Application use case: create a Draft Draw on a Stage.
/// </summary>
public static class CreateDraw
{
    /// <summary>
    /// Creates a Draft draw of the given resolution kind.
    /// </summary>
    /// <param name="stage">Owning stage.</param>
    /// <param name="kind">Resolution kind (Slot or Group).</param>
    /// <param name="clock">Clock for domain events.</param>
    /// <param name="stageMatches">Matches of stage.</param>
    /// <param name="competitionStages">
    /// Competition phases. Required for a Groups draw so inbound feeds are counted before the draft exists.
    /// When null, only <paramref name="stage"/> is considered.
    /// </param>
    /// <param name="intent">
    /// Groups <see cref="DrawInputsIntent.Rerun"/> runs <see cref="StageDrawRerunLaunchGuard"/> before the draft
    /// exists. Other intents do not.
    /// </param>
    /// <returns>The created draw.</returns>
    public static Draw Execute(
        Stage stage,
        DrawResolutionKind kind,
        IClock clock,
        IReadOnlyList<Match>? stageMatches = null,
        IReadOnlyList<Stage>? competitionStages = null,
        DrawInputsIntent intent = DrawInputsIntent.Default)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(clock);
        StructureOperationalExitEditGuard.Ensure(stage, stageMatches, "Draw");
        EnsureDrawRulesEngaged(stage);
        if (kind == DrawResolutionKind.Group
            && !stage.Draws.Any(draw => draw.Status != DrawStatus.Cancelled))
        {
            var stages = competitionStages ?? [stage];
            GroupDrawLaunchRules.Evaluate(stage, stages).EnsureAllowed();
            if (intent == DrawInputsIntent.Rerun)
            {
                StageDrawRerunLaunchGuard.Ensure(stage, stages, kind);
            }
        }

        return stage.CreateDraw(kind, clock);
    }

    private static void EnsureDrawRulesEngaged(Stage stage)
    {
        if (stage.Regulation.DrawRules is not null)
        {
            return;
        }

        throw new ApplicationFailureException(
            $"Draw cannot be created on stage '{stage.Name.Value}' without DrawRules.",
            ApplicationErrorCodes.DrawRulesRequired);
    }
}
