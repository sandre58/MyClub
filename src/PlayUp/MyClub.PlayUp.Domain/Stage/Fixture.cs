// -----------------------------------------------------------------------
// <copyright file="Fixture.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics;
using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Stage;

/// <summary>
/// Organizational slot for one or more matches within a round or matchday.
/// </summary>
[DebuggerDisplay("Fixture {Id} ({MatchIds.Count} matches)")]
public sealed class Fixture : Entity<FixtureId>
{
    private readonly List<MatchId> _matchIds = [];

    internal Fixture(FixtureId id)
        : base(id)
    {
    }

    /// <summary>
    /// Gets the ordered match identities attached to this fixture.
    /// </summary>
    public IReadOnlyList<MatchId> MatchIds => _matchIds.AsReadOnly();

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
}
