// -----------------------------------------------------------------------
// <copyright file="ResolutionProvenance.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Stages;

/// <summary>
/// Persisted witness of how a resolution occupant was produced (path + upstream result fingerprint).
/// </summary>
public sealed record ResolutionProvenance
{
    /// <summary>
    /// Placeholder path id for explicit non-verified provenance.
    /// </summary>
    public const string NonVerifieePathId = "-";

    /// <summary>
    /// Placeholder fingerprint for explicit non-verified provenance.
    /// </summary>
    public const string NonVerifieeFingerprint = "-";

    /// <summary>
    /// Initializes a new instance of the <see cref="ResolutionProvenance"/> class.
    /// </summary>
    public ResolutionProvenance(
        ResolutionProvenanceKind kind,
        string sourcePathId,
        string upstreamResultFingerprint)
    {
        if (!Enum.IsDefined(kind))
        {
            throw new DomainException(
                "Resolution provenance kind is unknown.",
                StageErrorCodes.ResolutionProvenanceInvalid);
        }

        if (string.IsNullOrWhiteSpace(sourcePathId))
        {
            throw new DomainException(
                "Resolution provenance requires a source path identifier.",
                StageErrorCodes.ResolutionProvenanceInvalid);
        }

        if (string.IsNullOrWhiteSpace(upstreamResultFingerprint))
        {
            throw new DomainException(
                "Resolution provenance requires an upstream result fingerprint.",
                StageErrorCodes.ResolutionProvenanceInvalid);
        }

        Kind = kind;
        SourcePathId = sourcePathId.Trim();
        UpstreamResultFingerprint = upstreamResultFingerprint.Trim();
    }

    /// <summary>
    /// Gets the provenance category.
    /// </summary>
    public ResolutionProvenanceKind Kind { get; }

    /// <summary>
    /// Gets the stable identifier of the source path (qualification, progression, draw, or direct slot key).
    /// </summary>
    public string SourcePathId { get; }

    /// <summary>
    /// Gets the fingerprint of upstream results that produced the occupant.
    /// </summary>
    public string UpstreamResultFingerprint { get; }

    /// <summary>
    /// Creates explicit non-verified provenance.
    /// </summary>
    public static ResolutionProvenance NonVerifiee() =>
        new(ResolutionProvenanceKind.NonVerifiee, NonVerifieePathId, NonVerifieeFingerprint);

    /// <summary>
    /// Creates direct-assignment provenance read from configuration.
    /// </summary>
    public static ResolutionProvenance Directe(string slotKey, EntryId entryId) =>
        new(
            ResolutionProvenanceKind.Directe,
            ResolutionPathIdentifier.ForDirectSlot(slotKey),
            ResolutionUpstreamFingerprint.ForDirectEntry(entryId));

    /// <summary>
    /// Creates draw resolution provenance for a published slot placement.
    /// </summary>
    public static ResolutionProvenance Tirage(DrawId drawId, string slotKey, EntryId entryId) =>
        new(
            ResolutionProvenanceKind.Tirage,
            ResolutionPathIdentifier.ForDrawSlot(drawId, slotKey),
            ResolutionUpstreamFingerprint.ForDrawPlacement(entryId));
}
