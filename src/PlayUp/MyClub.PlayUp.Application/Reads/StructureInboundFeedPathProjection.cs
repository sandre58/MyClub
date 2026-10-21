// -----------------------------------------------------------------------
// <copyright file="StructureInboundFeedPathProjection.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Reads;

internal static class StructureInboundFeedPathProjection
{
    internal static StructureInboundQualificationPathDto MapQualificationPath(
        QualificationPath path,
        Stage sourceStage)
    {
        string? groupName = null;
        if (path.Source.GroupId is { } sourceGroupId)
        {
            groupName = sourceStage.Groups.FirstOrDefault(g => g.Id.Equals(sourceGroupId))?.Name;
        }

        return new StructureInboundQualificationPathDto(
            path.Order,
            path.Selection.Mode,
            path.Selection.Value,
            path.Destination.StageId.Value,
            path.Selection.EndValue,
            path.Destination.SlotKey,
            path.Destination.GroupId?.Value,
            path.Destination.Form,
            path.Source.Scope,
            path.Source.GroupId?.Value,
            groupName,
            path.Source.AcrossGroupsPosition,
            path.Condition?.MinimumPoints);
    }

    internal static StructureInboundProgressionPathDto MapProgressionPath(
        ProgressionPath path,
        Stage sourceStage)
    {
        var matchNumberInContainer = ResolveProgressionMatchNumber(sourceStage, path.SourcePairKey);
        return new StructureInboundProgressionPathDto(
            path.SourcePairKey,
            path.Outcome,
            path.Destination.StageId.Value,
            path.Destination.SlotKey,
            path.Destination.GroupId?.Value,
            path.Destination.Form,
            path.SourcePairKey,
            matchNumberInContainer);
    }

    private static int? ResolveProgressionMatchNumber(Stage sourceStage, string sourcePairKey)
    {
        var fixture = sourceStage.FindFixtureByBracketPairKey(sourcePairKey);
        return fixture is not null
            ? ConfrontationDisplayOrdinal.TryGetForFixture(sourceStage, fixture.Id)
            : ConfrontationDisplayOrdinal.TryGetForPairKey(sourceStage, sourcePairKey);
    }
}
