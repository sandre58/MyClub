// -----------------------------------------------------------------------
// <copyright file="AddEntry.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;

namespace MyClub.PlayUp.Application.Competitions;

/// <summary>
/// Application use case: add a participating entry to a Competition.
/// </summary>
/// <remarks>
/// Domain owns duplicate-team and Draft/Ready gates. Application enforces <c>EntryRules.MaximumTeams</c>
/// on occupying entries (Domain stores the rule but does not apply it on AddEntry).
/// </remarks>
public static class AddEntry
{
    /// <summary>
    /// Adds an entry; generates <paramref name="teamId"/> / entry id when null.
    /// </summary>
    /// <param name="competition">Target competition (tracked).</param>
    /// <param name="displayName">Entry display name.</param>
    /// <param name="clock">Clock for domain events.</param>
    /// <param name="teamId">Optional team identity; a new id is created when omitted.</param>
    /// <param name="presentation">Optional team presentation.</param>
    /// <param name="entryId">Optional entry identity; a new id is created when omitted.</param>
    /// <returns>The created entry.</returns>
    /// <exception cref="ApplicationFailureException">Thrown when maximum capacity is reached.</exception>
    public static CompetitionEntry Execute(
        Competition competition,
        string displayName,
        IClock clock,
        TeamId? teamId = null,
        EntryPresentation? presentation = null,
        EntryId? entryId = null)
    {
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(clock);

        var occupying = competition.Entries.Count;
        return occupying >= competition.Regulation.EntryRules.MaximumTeams
            ? throw new ApplicationFailureException(
                $"Competition already has the maximum of {competition.Regulation.EntryRules.MaximumTeams} occupying entries.",
                ApplicationErrorCodes.EntryCapacityExceeded)
            : entryId is { } explicitEntryId
                ? competition.AddEntry(
                    teamId ?? TeamId.New(),
                    displayName,
                    explicitEntryId,
                    clock,
                    presentation)
                : competition.AddEntry(teamId ?? TeamId.New(), displayName, clock, presentation);
    }
}
