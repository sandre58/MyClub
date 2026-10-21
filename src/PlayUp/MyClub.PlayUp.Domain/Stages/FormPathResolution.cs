// -----------------------------------------------------------------------
// <copyright file="FormPathResolution.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Stages;

/// <summary>
/// Execution provenance: which ForForm path materialised which <see cref="CompositionEntry"/>.
/// Not a Placement address — no RosterPlace binding.
/// </summary>
public sealed record FormPathResolution
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FormPathResolution"/> class.
    /// </summary>
    /// <param name="pathFingerprint">Stable content key of the applied ForForm path.</param>
    /// <param name="entryId">Entry written into the form composition.</param>
    public FormPathResolution(string pathFingerprint, EntryId entryId)
    {
        if (string.IsNullOrWhiteSpace(pathFingerprint))
        {
            throw new DomainException(
                "Form path resolution fingerprint is required.",
                StageErrorCodes.InvalidConfiguration);
        }

        PathFingerprint = pathFingerprint;
        EntryId = entryId;
    }

    /// <summary>
    /// Gets the stable ForForm path identity (see <see cref="FormPathResolutionKey"/>).
    /// </summary>
    public string PathFingerprint { get; }

    /// <summary>
    /// Gets the materialised competition entry.
    /// </summary>
    public EntryId EntryId { get; }
}
