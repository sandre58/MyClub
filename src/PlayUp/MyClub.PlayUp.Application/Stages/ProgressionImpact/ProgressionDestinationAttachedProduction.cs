// -----------------------------------------------------------------------
// <copyright file="ProgressionDestinationAttachedProduction.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages.ProgressionImpact;

/// <summary>
/// WC-2 helpers: attached match production on a destination stage.
/// </summary>
internal static class ProgressionDestinationAttachedProduction
{
    internal static bool HasAttachedProduction(
        Stage destination,
        IReadOnlyList<MatchParticipantSnapshot> materializedParticipants)
    {
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(materializedParticipants);

        if (materializedParticipants.Count == 0)
        {
            return false;
        }

        var attached = CollectAttachedMatchIds(destination);
        return materializedParticipants.Any(snapshot => attached.Contains(snapshot.MatchId));
    }

    internal static bool EntryAppearsOnAttachedProduction(
        Stage destination,
        EntryId entryId,
        IReadOnlyList<MatchParticipantSnapshot> materializedParticipants)
    {
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(materializedParticipants);

        var attached = CollectAttachedMatchIds(destination);
        return materializedParticipants.Any(snapshot => attached.Contains(snapshot.MatchId) && (snapshot.HomeEntryId.Equals(entryId) || snapshot.AwayEntryId.Equals(entryId)));
    }

    private static HashSet<MatchId> CollectAttachedMatchIds(Stage stage)
    {
        var ids = new HashSet<MatchId>();
        foreach (var round in stage.Rounds)
        {
            foreach (var fixture in round.Fixtures)
            {
                foreach (var attachment in fixture.Attachments)
                {
                    ids.Add(attachment.MatchId);
                }
            }
        }

        return ids;
    }
}
