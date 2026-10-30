// -----------------------------------------------------------------------
// <copyright file="GroupResolutionProvenance.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Stages;

/// <summary>
/// Persisted resolution provenance for a group membership produced by Qual/Prog Apply.
/// </summary>
public sealed record GroupResolutionProvenance
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GroupResolutionProvenance"/> class.
    /// </summary>
    public GroupResolutionProvenance(
        GroupId groupId,
        EntryId entryId,
        ResolutionProvenanceKind provenanceKind,
        string sourcePathId,
        string upstreamResultFingerprint)
    {
        GroupId = groupId;
        EntryId = entryId;
        ProvenanceKind = provenanceKind;
        SourcePathId = sourcePathId;
        UpstreamResultFingerprint = upstreamResultFingerprint;
    }

    /// <summary>
    /// Initializes a new instance from a provenance value object.
    /// </summary>
    public GroupResolutionProvenance(GroupId groupId, EntryId entryId, ResolutionProvenance provenance)
        : this(
            groupId,
            entryId,
            provenance.Kind,
            provenance.SourcePathId,
            provenance.UpstreamResultFingerprint)
    {
        ArgumentNullException.ThrowIfNull(provenance);
    }

    /// <summary>
    /// Gets the target group identity.
    /// </summary>
    public GroupId GroupId { get; }

    /// <summary>
    /// Gets the resolved entry identity.
    /// </summary>
    public EntryId EntryId { get; }

    /// <summary>
    /// Gets the provenance category.
    /// </summary>
    public ResolutionProvenanceKind ProvenanceKind { get; }

    /// <summary>
    /// Gets the stable source path identifier.
    /// </summary>
    public string SourcePathId { get; }

    /// <summary>
    /// Gets the upstream result fingerprint.
    /// </summary>
    public string UpstreamResultFingerprint { get; }

    /// <summary>
    /// Gets the provenance value object.
    /// </summary>
    public ResolutionProvenance Provenance =>
        new(ProvenanceKind, SourcePathId, UpstreamResultFingerprint);
}
