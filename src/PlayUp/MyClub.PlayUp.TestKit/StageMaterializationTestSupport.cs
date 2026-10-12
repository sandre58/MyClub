// -----------------------------------------------------------------------
// <copyright file="StageMaterializationTestSupport.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.TestKit;

/// <summary>
/// Phase V2: batch materialization requires <see cref="StageStatus.Ready"/>.
/// </summary>
public static class StageMaterializationTestSupport
{
    /// <summary>
    /// Runs <see cref="PrepareStage"/> when the stage is still Draft.
    /// </summary>
    public static void EnsureReadyForMaterialize(
        Stage stage,
        IReadOnlyList<Stage> competitionStages,
        IClock clock)
    {
        if (stage.Status is StageStatus.Ready)
        {
            return;
        }

        PrepareStage.Execute(stage, competitionStages, clock);
    }
}
