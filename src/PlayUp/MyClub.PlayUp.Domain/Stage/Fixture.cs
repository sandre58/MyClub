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
    private readonly List<MatchId> _matchIds = [];

    internal Fixture(FixtureId id, string? slotAKey = null, string? slotBKey = null)
        : base(id) =>
        BindSlots(slotAKey, slotBKey);

    /// <summary>
    /// Gets the ordered match identities attached to this fixture.
    /// </summary>
    public IReadOnlyList<MatchId> MatchIds => _matchIds.AsReadOnly();

    /// <summary>
    /// Gets bracket position A when set; otherwise <see langword="null"/>.
    /// </summary>
    public string? SlotAKey { get; private set; }

    /// <summary>
    /// Gets bracket position B when set; otherwise <see langword="null"/>.
    /// </summary>
    public string? SlotBKey { get; private set; }

    internal bool Contains(MatchId matchId) => _matchIds.Contains(matchId);

    /// <summary>
    /// Attaches a match identity. Returns <see langword="false"/> when already present (no-op).
    /// </summary>
    internal bool AttachMatch(MatchId matchId)
    {
        if (_matchIds.Contains(matchId))
        {
            return false;
        }

        _matchIds.Add(matchId);
        return true;
    }

    /// <summary>
    /// Detaches a match identity. Returns <see langword="false"/> when not present.
    /// </summary>
    internal bool DetachMatch(MatchId matchId) => _matchIds.Remove(matchId);

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
