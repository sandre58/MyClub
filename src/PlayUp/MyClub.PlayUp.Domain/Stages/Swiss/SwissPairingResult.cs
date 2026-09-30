// -----------------------------------------------------------------------
// <copyright file="SwissPairingResult.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Stages;

/// <summary>
/// Outcome of <see cref="SwissPairingEngine.BuildPairings"/> — success or NoSolution (I8).
/// </summary>
public sealed class SwissPairingResult
{
    private readonly SwissPairing[] _pairings;

    private SwissPairingResult(bool isNoSolution, IReadOnlyList<SwissPairing>? pairings, EntryId? byeEntryId)
    {
        IsNoSolution = isNoSolution;
        _pairings = pairings is null ? [] : [.. pairings];
        ByeEntryId = byeEntryId;
    }

    /// <summary>
    /// Gets a value indicating whether no complete legal pairing exists.
    /// </summary>
    public bool IsNoSolution { get; }

    /// <summary>
    /// Gets a value indicating whether a complete pairing was found.
    /// </summary>
    public bool IsSuccess => !IsNoSolution;

    /// <summary>
    /// Gets the pairings when <see cref="IsSuccess"/>; otherwise empty.
    /// </summary>
    public IReadOnlyList<SwissPairing> Pairings => _pairings;

    /// <summary>
    /// Gets the bye recipient when N is odd and success; otherwise <see langword="null"/>.
    /// </summary>
    public EntryId? ByeEntryId { get; }

    /// <summary>
    /// Creates a successful pairing outcome.
    /// </summary>
    /// <param name="pairings">Complete set of pairings.</param>
    /// <param name="byeEntryId">Bye recipient when N odd; otherwise <see langword="null"/>.</param>
    /// <returns>A success result.</returns>
    public static SwissPairingResult Success(IReadOnlyList<SwissPairing> pairings, EntryId? byeEntryId = null)
    {
        ArgumentNullException.ThrowIfNull(pairings);
        return new SwissPairingResult(false, pairings, byeEntryId);
    }

    /// <summary>
    /// Creates a no-solution outcome (I8 — no complete legal matching).
    /// </summary>
    /// <returns>A no-solution result.</returns>
    public static SwissPairingResult NoSolution() => new(true, null, null);
}
