// -----------------------------------------------------------------------
// <copyright file="AddStageRound.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Application use case: add a Round (optional TieFormat) to a Stage.
/// </summary>
public static class AddStageRound
{
    /// <summary>
    /// Adds a round with an optional explicit tie format.
    /// </summary>
    /// <param name="stage">Target stage.</param>
    /// <param name="name">Round display name.</param>
    /// <param name="tieFormat">Optional confrontation format (null ⇒ Domain default from stage regulation).</param>
    /// <param name="clock">Clock for domain events.</param>
    /// <returns>The created round.</returns>
    public static Round Execute(Stage stage, string name, TieFormat? tieFormat, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(clock);

        EnsureStageMutable(stage);
        return tieFormat is null
            ? stage.AddRound(name, clock)
            : stage.AddRound(name, tieFormat, clock);
    }

    /// <summary>
    /// Builds a TieFormat from HTTP-friendly parameters, or null when legs omitted.
    /// </summary>
    public static TieFormat? BuildTieFormat(int? numberOfLegs, bool? aggregateScoring)
    {
        if (numberOfLegs is null)
        {
            return null;
        }

        var legs = numberOfLegs.Value;
        if (legs is not (TieFormat.SingleLeg or TieFormat.TwoLegs))
        {
            throw new ApplicationFailureException(
                "NumberOfLegs must be 1 or 2.",
                ApplicationErrorCodes.InvalidStructureIntent);
        }

        var aggregate = aggregateScoring
                        ?? (legs == TieFormat.TwoLegs);
        if (legs == TieFormat.SingleLeg)
        {
            aggregate = false;
        }

        return new TieFormat(legs, aggregate);
    }

    private static void EnsureStageMutable(Stage stage)
    {
        if (stage.Status is StageStatus.Running or StageStatus.Suspended or StageStatus.Completed)
        {
            throw new ApplicationFailureException(
                $"Rounds cannot be added while stage status is '{stage.Status}'.",
                ApplicationErrorCodes.StructureNotMutable);
        }
    }
}
