// -----------------------------------------------------------------------
// <copyright file="StructureAppliedDrawFeedRulesChangeGate.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Refuses qualification rule changes when a draw is Applied on the stage.
/// Progression uses <see cref="StructureProgressionImpactGate"/>.
/// </summary>
internal static class StructureAppliedDrawFeedRulesChangeGate
{
    internal static void EnsureQualificationReplaceAllowed(Stage stage, QualificationRules? afterRules)
    {
        ArgumentNullException.ThrowIfNull(stage);

        if (Equals(stage.Regulation.QualificationRules, afterRules))
        {
            return;
        }

        EnsureNoAppliedDraw(stage, "Qualification rules");
    }

    private static void EnsureNoAppliedDraw(Stage stage, string surface)
    {
        if (!stage.Draws.Any(draw =>
                draw.Status != DrawStatus.Cancelled && DrawAppliedState.IsApplied(draw, stage)))
        {
            return;
        }

        throw new ApplicationFailureException(
            $"{surface} cannot change while an applied draw is on this stage.",
            ApplicationErrorCodes.AppliedDrawFeedRulesChangeBlocked);
    }
}
