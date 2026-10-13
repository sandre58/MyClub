// -----------------------------------------------------------------------
// <copyright file="StructureStageInboundTopology.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Inbound Qual/Prog feed topology for Structure hub and Places N.
/// </summary>
internal static class StructureStageInboundTopology
{
    internal static bool IsRootComposition(Stage stage, IReadOnlyList<Stage> competitionStages)
    {
        foreach (var other in competitionStages)
        {
            if (other.Id.Equals(stage.Id))
            {
                continue;
            }

            var qualification = other.Regulation.QualificationRules;
            if (qualification?.Paths.Any(path => path.Destination.StageId.Equals(stage.Id)) == true)
            {
                return false;
            }

            var progression = other.Regulation.ProgressionRules;
            if (progression?.Paths.Any(path => path.Destination.StageId.Equals(stage.Id)) == true)
            {
                return false;
            }
        }

        return true;
    }
}
