// -----------------------------------------------------------------------
// <copyright file="ResolutionUpstreamFingerprint.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Globalization;
using System.Text;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Qualification;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Standings;

namespace MyClub.PlayUp.Domain.Stages;

/// <summary>
/// Builds stable fingerprints of upstream results used to produce a resolution occupant.
/// </summary>
public static class ResolutionUpstreamFingerprint
{
    /// <summary>
    /// Fingerprint for a qualification selection from a standing view.
    /// </summary>
    public static string ForQualificationSelection(Standing standing, EntryId selectedEntryId)
    {
        ArgumentNullException.ThrowIfNull(standing);
        var row = standing.Find(selectedEntryId)
                  ?? throw new DomainException(
                      "Selected entry was not found in the standing.",
                      QualificationErrorCodes.SelectionUnresolved);

        var sb = new StringBuilder(256);
        sb.Append("sel|");
        sb.Append(selectedEntryId.Value.ToString("N"));
        sb.Append("|pos|");
        sb.Append(row.Position.ToString(CultureInfo.InvariantCulture));
        sb.Append("|pts|");
        sb.Append(row.Points.ToString(CultureInfo.InvariantCulture));
        sb.Append("|order|");
        sb.Append(StandingOrderSignature(standing));
        return sb.ToString();
    }

    /// <summary>
    /// Fingerprint for a progression selection from a fixture outcome.
    /// </summary>
    public static string ForProgressionOutcome(FixtureOutcome outcome, ProgressionOutcome pathOutcome)
    {
        ArgumentNullException.ThrowIfNull(outcome);
        var selected = pathOutcome == ProgressionOutcome.Winner
            ? outcome.WinnerEntryId
            : outcome.LoserEntryId;
        return string.Create(
            CultureInfo.InvariantCulture,
            $"fx|{selected.Value:N}|W{outcome.WinnerEntryId.Value:N}|L{outcome.LoserEntryId.Value:N}");
    }

    /// <summary>
    /// Fingerprint for a direct assignment occupant.
    /// </summary>
    public static string ForDirectEntry(EntryId entryId) =>
        $"direct|{entryId.Value:N}";

    /// <summary>
    /// Fingerprint for a draw slot placement occupant.
    /// </summary>
    public static string ForDrawPlacement(EntryId entryId) =>
        $"draw|{entryId.Value:N}";

    private static string StandingOrderSignature(Standing standing)
    {
        var sb = new StringBuilder(standing.Rows.Count * 40);
        foreach (var row in standing.Rows.OrderBy(r => r.Position).ThenBy(r => r.EntryId.Value))
        {
            sb.Append(row.Position);
            sb.Append(':');
            sb.Append(row.EntryId.Value.ToString("N"));
            sb.Append(':');
            sb.Append(row.Points);
            sb.Append(';');
        }

        return sb.ToString();
    }
}
