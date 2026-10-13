// -----------------------------------------------------------------------
// <copyright file="StageMaterializationTestSupport.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.TestKit;

/// <summary>
/// Phase V2: batch materialization requires <see cref="StageStatus.Ready"/> and Host Prepare gates.
/// </summary>
public static class StageMaterializationTestSupport
{
    /// <summary>
    /// Runs composition fill + Prepare places gate + <see cref="PrepareStage"/> when still Draft.
    /// </summary>
    public static void EnsureReadyForMaterialize(
        Competition competition,
        Stage stage,
        IReadOnlyList<Stage> competitionStages,
        IClock clock) =>
        StageLifecycleTestSupport.EnsureReadyForMaterialize(competition, stage, competitionStages, clock);
}
