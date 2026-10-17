// -----------------------------------------------------------------------
// <copyright file="CupCanonicalBracketTopologyGate.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Prepare / materialize gates for canonical mono-round Cup bracket-pair maintenance.
/// </summary>
internal static class CupCanonicalBracketTopologyGate
{
    internal static void EnsureBeforePrepare(Stage stage)
    {
        ArgumentNullException.ThrowIfNull(stage);

        switch (CupBracketTopologyClassifier.Classify(stage))
        {
            case CupBracketTopologyKind.InvalidCanonicalShape:
                throw new ApplicationFailureException(
                    $"Stage '{stage.Id}' has canonical slot naming but an invalid cup entry bracket shape.",
                    ApplicationErrorCodes.CupCanonicalTopologyInvalid);
            case CupBracketTopologyKind.CanonicalIncomplete:
                CupCanonicalBracketPairEnsurer.EnsureMissing(stage);
                break;
        }
    }

    internal static void EnsureBeforeMaterialize(Stage stage) =>
        CupCanonicalBracketPairEnsurer.EnsureMissing(stage);
}
