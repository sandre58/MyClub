// -----------------------------------------------------------------------
// <copyright file="FormPathResolutionKey.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Globalization;
using System.Text;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;

namespace MyClub.PlayUp.Domain.Stages;

/// <summary>
/// Builds a stable content fingerprint for a ForForm <see cref="QualificationPath"/> /
/// <see cref="ProgressionPath"/>. Does not use Path.Order (expansion ordinal is fragile).
/// </summary>
public static class FormPathResolutionKey
{
    /// <summary>
    /// Fingerprint for a qualification ForForm path owned by <paramref name="sourceStageId"/>.
    /// </summary>
    public static string FromQualification(StageId sourceStageId, QualificationPath path)
    {
        ArgumentNullException.ThrowIfNull(path);
        if (!path.Destination.TargetsForm)
        {
            throw new DomainException(
                "Form path resolution key requires a ForForm qualification destination.",
                StageErrorCodes.InvalidConfiguration);
        }

        var sb = new StringBuilder(128);
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
        return sb.ToString();
    }

    /// <summary>
    /// Fingerprint for a progression ForForm path owned by <paramref name="sourceStageId"/>.
    /// </summary>
    public static string FromProgression(StageId sourceStageId, ProgressionPath path)
    {
        ArgumentNullException.ThrowIfNull(path);
        if (!path.Destination.TargetsForm)
        {
            throw new DomainException(
                "Form path resolution key requires a ForForm progression destination.",
                StageErrorCodes.InvalidConfiguration);
        }

        var sb = new StringBuilder(96);
        sb.Append("P|");
        sb.Append(sourceStageId.Value.ToString("N"));
        sb.Append('|');
        sb.Append(path.SourcePairKey);
        sb.Append('|');
        sb.Append(path.Outcome.ToString());
        return sb.ToString();
    }

    private static void Append(StringBuilder sb, string? value) =>
        sb.Append(value ?? "-");

    private static void Append(StringBuilder sb, int? value) =>
        sb.Append(value?.ToString(CultureInfo.InvariantCulture) ?? "-");
}
