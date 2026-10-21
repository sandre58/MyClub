// -----------------------------------------------------------------------
// <copyright file="SwissPairingRequest.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Stages;

/// <summary>
/// Explicit inputs for <see cref="SwissPairingEngine.BuildPairings"/> (pure Domain — no DbContext).
/// </summary>
public sealed class SwissPairingRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SwissPairingRequest"/> class.
    /// </summary>
    /// <param name="standings">All participants with points/position (complete set for the round).</param>
    /// <param name="playedPairs">Unordered pairs already played (each as two EntryIds).</param>
    /// <param name="byeCountsByEntry">Prior bye counts per entry (missing ⇒ 0).</param>
    /// <param name="homeCountsByEntry">Prior home-match counts per entry (missing ⇒ 0).</param>
    public SwissPairingRequest(
        IReadOnlyList<SwissParticipantStanding> standings,
        IReadOnlyList<(EntryId First, EntryId Second)> playedPairs,
        IReadOnlyDictionary<EntryId, int>? byeCountsByEntry = null,
        IReadOnlyDictionary<EntryId, int>? homeCountsByEntry = null)
    {
        ArgumentNullException.ThrowIfNull(standings);
        ArgumentNullException.ThrowIfNull(playedPairs);

        if (standings.Count == 0)
        {
            throw new DomainException(
                "Swiss pairing requires at least one participant.",
                StageErrorCodes.SwissPairingInvalid);
        }

        if (standings.Select(row => row.EntryId).Distinct().Count() != standings.Count)
        {
            throw new DomainException(
                "Swiss pairing standings contain duplicate entries.",
                StageErrorCodes.SwissPairingInvalid);
        }

        Standings = standings;
        PlayedPairs = playedPairs;
        ByeCountsByEntry = byeCountsByEntry ?? new Dictionary<EntryId, int>();
        HomeCountsByEntry = homeCountsByEntry ?? new Dictionary<EntryId, int>();
    }

    /// <summary>
    /// Gets participant standings (points + position).
    /// </summary>
    public IReadOnlyList<SwissParticipantStanding> Standings { get; }

    /// <summary>
    /// Gets pairs already played (unordered).
    /// </summary>
    public IReadOnlyList<(EntryId First, EntryId Second)> PlayedPairs { get; }

    /// <summary>
    /// Gets prior bye counts (missing entry ⇒ 0).
    /// </summary>
    public IReadOnlyDictionary<EntryId, int> ByeCountsByEntry { get; }

    /// <summary>
    /// Gets prior home-match counts (missing entry ⇒ 0).
    /// </summary>
    public IReadOnlyDictionary<EntryId, int> HomeCountsByEntry { get; }
}
