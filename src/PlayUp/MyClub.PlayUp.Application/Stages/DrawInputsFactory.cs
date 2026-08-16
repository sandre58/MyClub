// -----------------------------------------------------------------------
// <copyright file="DrawInputsFactory.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Builds default <see cref="DrawInputs"/> for V1 Organisation → Draw flows.
/// </summary>
public static class DrawInputsFactory
{
    /// <summary>
    /// Builds inputs for the draw kind from active competition entries and stage regulation.
    /// </summary>
    public static DrawInputs CreateDefault(Competition competition, Stage stage, DrawResolutionKind kind)
    {
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(stage);

        var entries = competition.Entries
            .Where(entry => entry.Status == EntryStatus.Active)
            .Select(entry => entry.Id)
            .OrderBy(id => id.Value)
            .ToList();

        if (entries.Count == 0)
        {
            throw new ApplicationFailureException(
                "Draw inputs require at least one active entry.",
                ApplicationErrorCodes.DrawGenerationFailure);
        }

        return kind switch
        {
            DrawResolutionKind.Group => DrawInputs.ForGroup(entries, potMembership: BuildSequentialPots(entries, stage)),
            DrawResolutionKind.Pairing => DrawInputs.ForPairing(entries),
            DrawResolutionKind.Slot => DrawInputs.ForSlot(entries),
            _ => throw new ApplicationFailureException(
                $"Unsupported draw kind '{kind}'.",
                ApplicationErrorCodes.DrawKindNotSupported)
        };
    }

    private static PotMembership BuildSequentialPots(IReadOnlyList<EntryId> entries, Stage stage)
    {
        var numberOfPots = stage.Regulation.DrawRules?.PotRules?.NumberOfPots
            ?? throw new ApplicationFailureException(
                "Group draw requires PotRules on the stage regulation.",
                ApplicationErrorCodes.DrawGenerationFailure);

        if (entries.Count % numberOfPots != 0)
        {
            throw new ApplicationFailureException(
                $"Active entry count ({entries.Count}) must be divisible by NumberOfPots ({numberOfPots}).",
                ApplicationErrorCodes.DrawGenerationFailure);
        }

        // Sequential fill: pot capacity = entries / pots; each pot gets that many consecutive entries.
        var capacity = entries.Count / numberOfPots;
        var map = new Dictionary<EntryId, int>();
        for (var index = 0; index < entries.Count; index++)
        {
            map[entries[index]] = (index / capacity) + 1;
        }

        return new PotMembership(map);
    }
}
