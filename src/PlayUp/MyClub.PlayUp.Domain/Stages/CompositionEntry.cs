// -----------------------------------------------------------------------
// <copyright file="CompositionEntry.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Stages;

/// <summary>
/// Entry identity row shared by two distinct Stage collections:
/// <see cref="Stage.AffectationAuthoring"/> (manual producers) and
/// <see cref="Stage.CompositionEntries"/> (runtime population / Draw·Live pool).
/// Same shape, different semantics — do not treat the lists as interchangeable.
/// Also distinct from <see cref="DirectAssignment"/> (slot feed) and group/slot resolution.
/// </summary>
public sealed record CompositionEntry
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CompositionEntry"/> class.
    /// </summary>
    /// <param name="entryId">Competition entry identity.</param>
    public CompositionEntry(EntryId entryId) => EntryId = entryId;

    /// <summary>
    /// Initializes a new instance with persisted provenance columns.
    /// </summary>
    public CompositionEntry(
        EntryId entryId,
        ResolutionProvenanceKind? resolutionProvenanceKind,
        string? resolutionSourcePathId,
        string? resolutionUpstreamFingerprint)
    {
        EntryId = entryId;
        ResolutionProvenanceKind = resolutionProvenanceKind;
        ResolutionSourcePathId = resolutionSourcePathId;
        ResolutionUpstreamFingerprint = resolutionUpstreamFingerprint;
    }

    /// <summary>
    /// Gets the competition entry identity.
    /// </summary>
    public EntryId EntryId { get; }

    /// <summary>
    /// Gets the optional Apply resolution provenance kind.
    /// </summary>
    public ResolutionProvenanceKind? ResolutionProvenanceKind { get; private init; }

    /// <summary>
    /// Gets the optional source path identifier.
    /// </summary>
    public string? ResolutionSourcePathId { get; private init; }

    /// <summary>
    /// Gets the optional upstream result fingerprint.
    /// </summary>
    public string? ResolutionUpstreamFingerprint { get; private init; }

    /// <summary>
    /// Gets the Apply resolution provenance when this row was materialised by Qual/Prog/Draw resolution.
    /// </summary>
    public ResolutionProvenance? ResolutionProvenance =>
        ResolutionProvenanceKind is null
            ? null
            : new ResolutionProvenance(
                ResolutionProvenanceKind.Value,
                ResolutionSourcePathId!,
                ResolutionUpstreamFingerprint!);

    /// <summary>
    /// Returns a copy with updated resolution provenance.
    /// </summary>
    public CompositionEntry WithResolutionProvenance(ResolutionProvenance provenance) =>
        this with
        {
            ResolutionProvenanceKind = provenance.Kind,
            ResolutionSourcePathId = provenance.SourcePathId,
            ResolutionUpstreamFingerprint = provenance.UpstreamResultFingerprint,
        };
}
