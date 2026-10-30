// -----------------------------------------------------------------------
// <copyright file="ResolutionPathIdentifier.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Globalization;
using System.Text;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;

namespace MyClub.PlayUp.Domain.Stages;

/// <summary>
/// Builds stable source path identifiers for persisted resolution provenance.
/// </summary>
public static class ResolutionPathIdentifier
{
    /// <summary>
    /// Identifier for a qualification path on a source stage.
    /// </summary>
    public static string ForQualification(StageId sourceStageId, QualificationPath path)
    {
        ArgumentNullException.ThrowIfNull(path);
        var sb = new StringBuilder(160);
        sb.Append("Q|");
        sb.Append(sourceStageId.Value.ToString("N"));
        sb.Append('|');
        Append(sb, path.Source.Scope?.ToString());
        sb.Append('|');
        Append(sb, path.Source.GroupId?.Value.ToString("N"));
        sb.Append('|');
        Append(sb, path.Source.AcrossGroupsPosition);
        sb.Append('|');
        Append(sb, path.Selection.Mode.ToString());
        sb.Append('|');
        Append(sb, path.Selection.Value);
        sb.Append('|');
        Append(sb, path.Selection.EndValue);
        sb.Append('|');
        Append(sb, path.Condition?.MinimumPoints);
        sb.Append('|');
        Append(sb, path.Destination.StageId.Value.ToString("N"));
        sb.Append('|');
        AppendDestination(sb, path.Destination);
        return sb.ToString();
    }

    /// <summary>
    /// Identifier for a progression path on a source stage.
    /// </summary>
    public static string ForProgression(StageId sourceStageId, ProgressionPath path)
    {
        ArgumentNullException.ThrowIfNull(path);
        var sb = new StringBuilder(128);
        sb.Append("P|");
        sb.Append(sourceStageId.Value.ToString("N"));
        sb.Append('|');
        sb.Append(path.SourcePairKey);
        sb.Append('|');
        sb.Append(path.Outcome);
        sb.Append('|');
        Append(sb, path.Destination.StageId.Value.ToString("N"));
        sb.Append('|');
        AppendProgressionDestination(sb, path.Destination);
        return sb.ToString();
    }

    /// <summary>
    /// Identifier for a direct slot configuration feed.
    /// </summary>
    public static string ForDirectSlot(string slotKey) =>
        $"D|{Slot.NormalizeKey(slotKey)}";

    /// <summary>
    /// Identifier for a published draw slot placement.
    /// </summary>
    public static string ForDrawSlot(DrawId drawId, string slotKey) =>
        $"T|{drawId.Value:N}|{Slot.NormalizeKey(slotKey)}";

    private static void AppendDestination(StringBuilder sb, QualificationDestination destination)
    {
        if (destination.TargetsSlot)
        {
            sb.Append("S|");
            sb.Append(destination.SlotKey);
            return;
        }

        if (destination.TargetsGroup)
        {
            sb.Append("G|");
            var groupId = destination.GroupId!.Value;
            sb.Append(groupId.Value.ToString("N"));
            return;
        }

        if (destination.TargetsForm)
        {
            sb.Append("F|1");
            return;
        }

        sb.Append("POP");
    }

    private static void AppendProgressionDestination(StringBuilder sb, ProgressionDestination destination)
    {
        if (destination.TargetsSlot)
        {
            sb.Append("S|");
            sb.Append(destination.SlotKey);
            return;
        }

        if (destination.TargetsGroup)
        {
            sb.Append("G|");
            var groupId = destination.GroupId!.Value;
            sb.Append(groupId.Value.ToString("N"));
            return;
        }

        if (destination.TargetsForm)
        {
            sb.Append("F|1");
            return;
        }

        sb.Append("POP");
    }

    private static void Append(StringBuilder sb, string? value) =>
        sb.Append(value ?? "-");

    private static void Append(StringBuilder sb, int? value) =>
        sb.Append(value?.ToString(CultureInfo.InvariantCulture) ?? "-");
}
