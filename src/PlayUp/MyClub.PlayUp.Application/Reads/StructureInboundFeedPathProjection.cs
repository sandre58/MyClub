// -----------------------------------------------------------------------
// <copyright file="StructureInboundFeedPathProjection.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
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
        var sourceLabel = StructureInboundProgressionSourceLabel.Resolve(sourceStage, path.SourcePairKey);
        return new StructureInboundProgressionPathDto(
            path.SourcePairKey,
            path.Outcome,
            path.Destination.StageId.Value,
            path.Destination.SlotKey,
            path.Destination.GroupId?.Value,
            path.Destination.Form,
            sourceLabel);
    }
}

/// <summary>Progression source label for inbound feed rows (PairKey or materialized fixture).</summary>
internal static class StructureInboundProgressionSourceLabel
{
    internal static string Resolve(Stage sourceStage, string sourcePairKey)
    {
        var fixture = sourceStage.FindFixtureByBracketPairKey(sourcePairKey);
        if (fixture is null)
        {
            return sourcePairKey;
        }

        var order = ResolveFixtureOrder(sourceStage, fixture.Id);
        return order is { } n
            ? $"#{n}"
            : sourcePairKey;
    }

    private static int? ResolveFixtureOrder(Stage stage, FixtureId fixtureId)
    {
        foreach (var round in stage.Rounds)
        {
            var fixtures = round.Fixtures.OrderBy(fixture => fixture.Id.Value).ToArray();
            for (var i = 0; i < fixtures.Length; i++)
            {
                if (fixtures[i].Id.Equals(fixtureId))
                {
                    return i + 1;
                }
            }
        }

        return null;
    }
}
