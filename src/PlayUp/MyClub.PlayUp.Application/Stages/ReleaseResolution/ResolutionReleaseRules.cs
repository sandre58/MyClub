// -----------------------------------------------------------------------
// <copyright file="ResolutionReleaseRules.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Stages;
using ApplicationErrorCodes = MyClub.PlayUp.Application.ApplicationErrorCodes;
using CascadeReplaceUseCase = MyClub.PlayUp.Application.Stages.CascadeReplace.CascadeReplace;

namespace MyClub.PlayUp.Application.Stages.ReleaseResolution;

/// <summary>
/// The occupant a release preview, a confirmation, and the structure read all judge the same way.
/// </summary>
/// <param name="Target">Slot, group, or population.</param>
/// <param name="EntryId">Stored entry.</param>
/// <param name="Kind">Provenance kind.</param>
/// <param name="State">Freshness.</param>
/// <param name="Indeterminacy">Cause when <paramref name="State"/> is indeterminate.</param>
/// <param name="SlotKey">Slot key when <paramref name="Target"/> is a slot.</param>
/// <param name="GroupId">Group id when <paramref name="Target"/> is a group.</param>
public readonly record struct ResolutionReleaseSubject(
    StructureResolutionOccupantTarget Target,
    Guid EntryId,
    ResolutionProvenanceKind Kind,
    StructureResolutionFreshness State,
    StructureResolutionIndeterminacyCause? Indeterminacy,
    string? SlotKey,
    Guid? GroupId);

/// <summary>
/// One match of the target phase, enough to decide whether a team is still on a sheet.
/// </summary>
/// <param name="MatchId">Match identity.</param>
/// <param name="Status">Lifecycle status.</param>
/// <param name="HomeEntryId">Home entry when known.</param>
/// <param name="AwayEntryId">Away entry when known.</param>
public readonly record struct ResolutionReleaseMatch(
    Guid MatchId,
    MatchStatus Status,
    Guid? HomeEntryId,
    Guid? AwayEntryId);

/// <summary>
/// First refusal among the release guards. Null means the place can be released.
/// </summary>
public static class ResolutionReleaseRules
{
    /// <summary>
    /// Returns the application error code of the first refusal, or null when the place can be released.
    /// A direct assignment is stored as provenance <see cref="ResolutionProvenanceKind.Directe"/>,
    /// so that case reports <see cref="ApplicationErrorCodes.ResolutionReleaseDirectAssignment"/>.
    /// </summary>
    /// <param name="stage">Phase that stores the occupant.</param>
    /// <param name="subject">Occupant under consideration.</param>
    /// <param name="matches">Matches of <paramref name="stage"/>.</param>
    /// <returns>The first refusal code, or null.</returns>
    public static string? Refusal(
        Stage stage,
        ResolutionReleaseSubject subject,
        IReadOnlyList<ResolutionReleaseMatch> matches)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(matches);

        if (subject.Kind is not (
            ResolutionProvenanceKind.Qualification or ResolutionProvenanceKind.Progression))
        {
            if (subject.Kind == ResolutionProvenanceKind.Directe || HoldsDirectAssignment(stage, subject))
            {
                return ApplicationErrorCodes.ResolutionReleaseDirectAssignment;
            }

            return ApplicationErrorCodes.ResolutionReleaseNotReleasable;
        }

        if (subject.State != StructureResolutionFreshness.Indetermine)
        {
            return ApplicationErrorCodes.ResolutionReleaseNotIndeterminate;
        }

        if (subject.Indeterminacy == StructureResolutionIndeterminacyCause.ConfigurationInvalide)
        {
            return ApplicationErrorCodes.ResolutionReleaseNotReleasable;
        }

        var entryId = new EntryId(subject.EntryId);
        if (subject.Target == StructureResolutionOccupantTarget.Population
            && CascadeReplaceUseCase.Referenced(stage, entryId))
        {
            return ApplicationErrorCodes.ResolutionReleaseNotReleasable;
        }

        if (stage.Status == StageStatus.Completed)
        {
            return ApplicationErrorCodes.ResolutionReleaseCompleted;
        }

        if (HoldsDirectAssignment(stage, subject))
        {
            return ApplicationErrorCodes.ResolutionReleaseDirectAssignment;
        }

        if (matches.Any(match => Involves(match, subject.EntryId)
            && match.Status is MatchStatus.Live or MatchStatus.Finished))
        {
            return ApplicationErrorCodes.ResolutionReleaseBlockedByPlayedMatch;
        }

        if (!IsBracketSlot(stage, subject) && AttachedMatchInvolves(stage, matches, subject.EntryId))
        {
            return stage.Status is StageStatus.Running or StageStatus.Suspended
                ? ApplicationErrorCodes.ResolutionReleaseBlockedByMatchesRunning
                : ApplicationErrorCodes.ResolutionReleaseBlockedByMatches;
        }

        if (DrawBlocks(stage, entryId))
        {
            return ApplicationErrorCodes.ResolutionReleaseBlockedByDraw;
        }

        return null;
    }

    private static bool HoldsDirectAssignment(Stage stage, ResolutionReleaseSubject subject) =>
        subject.Target == StructureResolutionOccupantTarget.Slot
        && subject.SlotKey is not null
        && stage.DirectAssignments.Any(assignment =>
            string.Equals(assignment.SlotKey, subject.SlotKey, StringComparison.Ordinal));

    private static bool IsBracketSlot(Stage stage, ResolutionReleaseSubject subject) =>
        subject.Target == StructureResolutionOccupantTarget.Slot
        && subject.SlotKey is not null
        && stage.Rounds.SelectMany(round => round.Fixtures).Any(fixture =>
            string.Equals(fixture.SlotAKey, subject.SlotKey, StringComparison.Ordinal)
            || string.Equals(fixture.SlotBKey, subject.SlotKey, StringComparison.Ordinal));

    private static bool Involves(ResolutionReleaseMatch match, Guid entryId) =>
        match.HomeEntryId == entryId || match.AwayEntryId == entryId;

    private static bool AttachedMatchInvolves(
        Stage stage,
        IReadOnlyList<ResolutionReleaseMatch> matches,
        Guid entryId)
    {
        var attached = new HashSet<Guid>();
        foreach (var matchday in stage.Matchdays)
        {
            foreach (var fixture in matchday.Fixtures)
            {
                foreach (var attachment in fixture.Attachments)
                {
                    attached.Add(attachment.MatchId.Value);
                }
            }
        }

        foreach (var round in stage.Rounds)
        {
            foreach (var fixture in round.Fixtures)
            {
                foreach (var attachment in fixture.Attachments)
                {
                    attached.Add(attachment.MatchId.Value);
                }
            }
        }

        return matches.Any(match => attached.Contains(match.MatchId) && Involves(match, entryId));
    }

    private static bool DrawBlocks(Stage stage, EntryId entryId) =>
        stage.Draws.Any(draw => TeamInDraw(draw, entryId) && DrawStillHoldsPlacements(stage, draw));

    private static bool TeamInDraw(Draw draw, EntryId entryId) =>
        draw.Inputs?.Entries.Any(entry => entry.Equals(entryId)) == true
        || draw.Resolution.SlotResults.Any(placement => placement.EntryId.Equals(entryId))
        || draw.Resolution.GroupResults.Any(placement => placement.EntryId.Equals(entryId));

    private static bool DrawStillHoldsPlacements(Stage stage, Draw draw)
    {
        if (draw.Status == DrawStatus.Published)
        {
            return true;
        }

        if (draw.Status != DrawStatus.Cancelled
            || draw.Resolution.State != DrawResolutionState.Resolved)
        {
            return false;
        }

        foreach (var placement in draw.Resolution.SlotResults)
        {
            if (stage.FindSlot(placement.SlotKey)?.EntryId is { } occupant
                && occupant.Equals(placement.EntryId))
            {
                return true;
            }
        }

        foreach (var placement in draw.Resolution.GroupResults)
        {
            var group = stage.FindGroup(placement.GroupId);
            if (group is not null && group.EntryIds.Contains(placement.EntryId))
            {
                return true;
            }
        }

        return false;
    }
}
