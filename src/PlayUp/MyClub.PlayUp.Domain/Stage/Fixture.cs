// -----------------------------------------------------------------------
// <copyright file="Fixture.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics;
using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Stage;

/// <summary>
/// Organizational container for one or more matches within a round or matchday.
/// Optional SlotA/SlotB are bracket positions (not Match home/away).
/// </summary>
[DebuggerDisplay("Fixture {Id} ({MatchIds.Count} matches)")]
public sealed class Fixture : Entity<FixtureId>
{
    private readonly List<MatchAttachment> _attachments = [];

    internal Fixture(FixtureId id, string? slotAKey = null, string? slotBKey = null)
        : base(id) =>
        BindSlots(slotAKey, slotBKey);

    /// <summary>
    /// Gets the match attachments (MatchId + LegIndex). Order is not a business semantic for legs.
    /// </summary>
    public IReadOnlyList<MatchAttachment> Attachments => _attachments.AsReadOnly();

    /// <summary>
    /// Gets the attached match identities (projection of <see cref="Attachments"/>). No leg semantics.
    /// </summary>
    public IReadOnlyList<MatchId> MatchIds => [.._attachments.Select(a => a.MatchId)];

    /// <summary>
    /// Gets bracket position A when set; otherwise <see langword="null"/>.
    /// </summary>
    public string? SlotAKey { get; private set; }

    /// <summary>
    /// Gets bracket position B when set; otherwise <see langword="null"/>.
    /// </summary>
    public string? SlotBKey { get; private set; }

    internal bool Contains(MatchId matchId) => _attachments.Exists(a => a.MatchId.Equals(matchId));

    /// <summary>
    /// Attaches a match identity with an explicit leg index.
    /// Returns <see langword="false"/> when the same match is already attached (no-op).
    /// </summary>
    internal bool AttachMatch(MatchId matchId, int legIndex)
    {
        if (Contains(matchId))
        {
            return false;
        }

        if (_attachments.Exists(a => a.LegIndex == legIndex))
        {
            throw new DomainException(
                $"Fixture already has a match attachment for leg index {legIndex}.",
                StageErrorCodes.InvalidConfiguration);
        }

        _attachments.Add(new MatchAttachment(matchId, legIndex));
        return true;
    }

    /// <summary>
    /// Detaches a match identity. Returns <see langword="false"/> when not present.
    /// </summary>
    internal bool DetachMatch(MatchId matchId)
    {
        var index = _attachments.FindIndex(a => a.MatchId.Equals(matchId));
        if (index < 0)
        {
            return false;
        }

        _attachments.RemoveAt(index);
        return true;
    }

    internal void BindSlots(string? slotAKey, string? slotBKey)
    {
        var a = NormalizeOptionalSlotKey(slotAKey);
        var b = NormalizeOptionalSlotKey(slotBKey);

        if (a is not null && b is not null && string.Equals(a, b, StringComparison.Ordinal))
        {
            throw new DomainException(
                "Fixture slot keys must be distinct.",
                StageErrorCodes.InvalidConfiguration);
        }

        SlotAKey = a;
        SlotBKey = b;
    }

    private static string? NormalizeOptionalSlotKey(string? slotKey) => slotKey is null ? null : Slot.NormalizeKey(slotKey);
}
