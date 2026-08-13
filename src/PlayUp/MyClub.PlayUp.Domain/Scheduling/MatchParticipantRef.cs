// -----------------------------------------------------------------------
// <copyright file="MatchParticipantRef.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Scheduling;

/// <summary>
/// Participant reference for a scheduled match (known, structurally unknown, or missing-expected).
/// </summary>
public sealed record MatchParticipantRef
{
    private MatchParticipantRef(MatchParticipantKind kind, EntryId? entryId)
    {
        Kind = kind;
        EntryId = entryId;
    }

    /// <summary>
    /// Gets the participant knowledge kind.
    /// </summary>
    public MatchParticipantKind Kind { get; }

    /// <summary>
    /// Gets the entry identity when <see cref="Kind"/> is <see cref="MatchParticipantKind.Known"/>; otherwise <see langword="null"/>.
    /// </summary>
    public EntryId? EntryId { get; }

    /// <summary>
    /// Creates a known participant reference.
    /// </summary>
    public static MatchParticipantRef Known(EntryId entryId) => new(MatchParticipantKind.Known, entryId);

    /// <summary>
    /// Creates a structurally unknown participant reference.
    /// </summary>
    public static MatchParticipantRef UnknownStructural() => new(MatchParticipantKind.UnknownStructural, null);

    /// <summary>
    /// Creates a missing-expected participant reference (InvalidRequest).
    /// </summary>
    public static MatchParticipantRef MissingExpected() => new(MatchParticipantKind.MissingExpected, null);
}
