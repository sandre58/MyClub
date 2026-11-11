// -----------------------------------------------------------------------
// <copyright file="ReleaseResolution.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Stages.CascadeReplace;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Stages;
using ApplicationErrorCodes = MyClub.PlayUp.Application.ApplicationErrorCodes;
using ApplicationFailureException = MyClub.PlayUp.Application.ApplicationFailureException;
using CascadeReplaceUseCase = MyClub.PlayUp.Application.Stages.CascadeReplace.CascadeReplace;

namespace MyClub.PlayUp.Application.Stages.ReleaseResolution;

/// <summary>
/// Clears one unconfirmed qualification or progression place and the path witness that would put the team back.
/// The preview is computed on copies. The confirmation mutates the caller's aggregates; the caller saves once.
/// </summary>
public static class ReleaseResolution
{
    /// <summary>
    /// Computes the release without writing the caller's stages or matches.
    /// </summary>
    public static ResolutionReleaseResult Plan(
        Competition competition,
        StageId stageId,
        ResolutionReleaseTarget target,
        IReadOnlyList<Stage> stages,
        IReadOnlyList<Match> matches,
        IClock clock)
    {
        Require(competition, target, stages, matches, clock);
        var token = CascadeReplaceVersion.Compute(stages, matches);
        var before = ResolutionFreshnessDetector.Inspect(competition, stages, Summaries(matches));
        var origin = Find(before, stageId, target);
        var stage = stages.Single(candidate => candidate.Id.Equals(stageId));
        RefuseUnlessReleasable(stage, origin, matches);

        var scratchStages = stages.Select(CascadeAggregateCopy.Copy).ToList();
        var scratchMatches = matches.Select(CascadeAggregateCopy.Copy).ToList();
        var scratchStage = scratchStages.Single(candidate => candidate.Id.Equals(stageId));
        var detached = Apply(scratchStage, scratchMatches, origin, clock);
        var after = ResolutionFreshnessDetector.Inspect(
            competition,
            scratchStages,
            Summaries(scratchMatches));
        return Result(token, stageId, origin, detached, Diff(before, after, origin));
    }

    /// <summary>
    /// Releases the place on the supplied aggregates. Callers persist with a single save. This method does not.
    /// </summary>
    public static ResolutionReleaseResult Execute(
        Competition competition,
        StageId stageId,
        ResolutionReleaseTarget target,
        IReadOnlyList<Stage> stages,
        IReadOnlyList<Match> matches,
        IClock clock)
    {
        Require(competition, target, stages, matches, clock);
        var token = CascadeReplaceVersion.Compute(stages, matches);
        var before = ResolutionFreshnessDetector.Inspect(competition, stages, Summaries(matches));
        var origin = Find(before, stageId, target);
        var stage = stages.Single(candidate => candidate.Id.Equals(stageId));
        RefuseUnlessReleasable(stage, origin, matches);
        var detached = Apply(stage, matches, origin, clock);
        var after = ResolutionFreshnessDetector.Inspect(competition, stages, Summaries(matches));
        return Result(token, stageId, origin, detached, Diff(before, after, origin));
    }

    private static void Require(
        Competition competition,
        ResolutionReleaseTarget target,
        IReadOnlyList<Stage> stages,
        IReadOnlyList<Match> matches,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(stages);
        ArgumentNullException.ThrowIfNull(matches);
        ArgumentNullException.ThrowIfNull(clock);
    }

    private static ResolutionOccupantAssessment Find(
        IReadOnlyList<ResolutionOccupantAssessment> assessments,
        StageId stageId,
        ResolutionReleaseTarget target)
    {
        var found = assessments.FirstOrDefault(assessment =>
            assessment.StageId == stageId.Value
            && assessment.Target == target.Target
            && assessment.EntryId == target.EntryId
            && (target.Target != StructureResolutionOccupantTarget.Slot
                || string.Equals(assessment.SlotKey, target.SlotKey, StringComparison.Ordinal))
            && (target.Target != StructureResolutionOccupantTarget.Group
                || assessment.GroupId == target.GroupId));
        return found ?? throw Fail(
            "There is nothing to release.",
            ApplicationErrorCodes.ResolutionReleaseOccupantNotFound);
    }

    private static void RefuseUnlessReleasable(
        Stage stage,
        ResolutionOccupantAssessment origin,
        IReadOnlyList<Match> matches)
    {
        var subject = new ResolutionReleaseSubject(
            origin.Target,
            origin.EntryId,
            origin.Kind,
            origin.State,
            origin.IndeterminacyCause,
            origin.SlotKey,
            origin.GroupId);
        var facts = matches
            .Where(match => match.StageId.Equals(stage.Id))
            .Select(match => new ResolutionReleaseMatch(
                match.Id.Value,
                match.Status,
                match.HomeEntryId?.Value,
                match.AwayEntryId?.Value))
            .ToArray();
        var code = ResolutionReleaseRules.Refusal(stage, subject, facts);
        if (code is not null)
        {
            throw Fail(Message(code), code);
        }
    }

    private static List<ResolutionReleaseDetachedMatch> Apply(
        Stage stage,
        IReadOnlyList<Match> matches,
        ResolutionOccupantAssessment origin,
        IClock clock)
    {
        var entryId = new EntryId(origin.EntryId);
        var detached = new List<ResolutionReleaseDetachedMatch>();
        if (origin.Target == StructureResolutionOccupantTarget.Slot && origin.SlotKey is not null)
        {
            UnresolveBracket(stage, origin.SlotKey, entryId, matches, clock, detached);
        }

        var snapshots = MaterializedParticipantSnapshots.FromMatches(
            matches.Where(match => match.StageId.Equals(stage.Id)));
        if (origin.Target == StructureResolutionOccupantTarget.Slot && origin.SlotKey is not null)
        {
            stage.ClearResolvedEntry(origin.SlotKey, clock, snapshots);
        }
        else if (origin.Target == StructureResolutionOccupantTarget.Group && origin.GroupId is { } groupId)
        {
            stage.RemoveResolvedGroupEntry(new GroupId(groupId), entryId, snapshots);
        }

        foreach (var witness in stage.FormPathResolutions.Where(resolution => resolution.EntryId.Equals(entryId)).ToArray())
        {
            stage.RemoveFormPathResolution(witness.PathFingerprint);
        }

        if (!CascadeReplaceUseCase.Referenced(stage, entryId))
        {
            stage.RemoveResolvedPopulationEntry(entryId, clock, snapshots);
        }

        return detached;
    }

    private static void UnresolveBracket(
        Stage stage,
        string slotKey,
        EntryId entryId,
        IReadOnlyList<Match> matches,
        IClock clock,
        List<ResolutionReleaseDetachedMatch> detached)
    {
        var byId = matches
            .Where(match => match.StageId.Equals(stage.Id))
            .ToDictionary(match => match.Id);
        foreach (var fixture in stage.Rounds.SelectMany(round => round.Fixtures))
        {
            var onA = string.Equals(fixture.SlotAKey, slotKey, StringComparison.Ordinal);
            var onB = string.Equals(fixture.SlotBKey, slotKey, StringComparison.Ordinal);
            if (!onA && !onB)
            {
                continue;
            }

            foreach (var attachment in fixture.Attachments)
            {
                if (!byId.TryGetValue(attachment.MatchId, out var match))
                {
                    continue;
                }

                if (match.HomeEntryId is { } home && home.Equals(entryId))
                {
                    Detach(match, attachment.LegIndex, Side.Home, clock, detached);
                }

                if (match.AwayEntryId is { } away && away.Equals(entryId))
                {
                    Detach(match, attachment.LegIndex, Side.Away, clock, detached);
                }
            }
        }
    }

    private static void Detach(
        Match match,
        int legIndex,
        Side side,
        IClock clock,
        List<ResolutionReleaseDetachedMatch> detached)
    {
        if (match.Status is not (MatchStatus.Scheduled or MatchStatus.Postponed))
        {
            throw Fail(
                "A live or finished match involves this team.",
                ApplicationErrorCodes.ResolutionReleaseBlockedByPlayedMatch);
        }

        detached.Add(new ResolutionReleaseDetachedMatch(
            match.Id.Value,
            match.StageId.Value,
            legIndex,
            side,
            match.HomeEntryId?.Value,
            match.AwayEntryId?.Value));
        match.UnresolveParticipant(side, clock);
    }

    private static ResolutionReleaseResult Result(
        string token,
        StageId stageId,
        ResolutionOccupantAssessment origin,
        IReadOnlyList<ResolutionReleaseDetachedMatch> detached,
        IReadOnlyList<ResolutionReleaseAffectedOccupant> affected) =>
        new(
            token,
            [
                new ResolutionReleaseRow(
                    stageId.Value,
                    origin.Target,
                    origin.SlotKey,
                    origin.GroupId,
                    origin.EntryId)
            ],
            detached,
            affected);

    private static List<ResolutionReleaseAffectedOccupant> Diff(
        IReadOnlyList<ResolutionOccupantAssessment> before,
        IReadOnlyList<ResolutionOccupantAssessment> after,
        ResolutionOccupantAssessment origin)
    {
        var afterByKey = after.ToDictionary(Key);
        var affected = new List<ResolutionReleaseAffectedOccupant>();
        foreach (var occupant in before)
        {
            if (SamePlace(occupant, origin))
            {
                continue;
            }

            if (afterByKey.TryGetValue(Key(occupant), out var next) && next.State != occupant.State)
            {
                affected.Add(new ResolutionReleaseAffectedOccupant(
                    occupant.StageId,
                    occupant.Target,
                    occupant.SlotKey,
                    occupant.GroupId,
                    occupant.EntryId,
                    occupant.State,
                    next.State));
            }
        }

        return affected;
    }

    private static bool SamePlace(ResolutionOccupantAssessment left, ResolutionOccupantAssessment right) =>
        left.StageId == right.StageId
        && left.Target == right.Target
        && left.EntryId == right.EntryId
        && string.Equals(left.SlotKey, right.SlotKey, StringComparison.Ordinal)
        && left.GroupId == right.GroupId;

    private static string Key(ResolutionOccupantAssessment occupant) =>
        string.Join(
            '|',
            occupant.StageId.ToString("N"),
            occupant.Target,
            occupant.EntryId.ToString("N"),
            occupant.SlotKey ?? string.Empty,
            occupant.GroupId?.ToString("N") ?? string.Empty);

    private static Dictionary<StageId, IReadOnlyList<MatchSummaryRow>> Summaries(IReadOnlyList<Match> matches) =>
        matches
            .GroupBy(match => match.StageId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<MatchSummaryRow>)group.Select(match => new MatchSummaryRow(
                    match.Id,
                    match.StageId,
                    match.Status,
                    match.HomeEntryId,
                    match.AwayEntryId,
                    match.Result,
                    match.HasObservedLive)).ToArray());

    private static string Message(string code) =>
        code switch
        {
            ApplicationErrorCodes.ResolutionReleaseNotIndeterminate =>
                "The place is no longer unconfirmed.",
            ApplicationErrorCodes.ResolutionReleaseCompleted =>
                "A completed phase cannot release a place.",
            ApplicationErrorCodes.ResolutionReleaseDirectAssignment =>
                "A direct assignment is not released.",
            ApplicationErrorCodes.ResolutionReleaseBlockedByPlayedMatch =>
                "A live or finished match involves this team.",
            ApplicationErrorCodes.ResolutionReleaseBlockedByMatches =>
                "Generated matches involve this team. Remove them before releasing the place.",
            ApplicationErrorCodes.ResolutionReleaseBlockedByMatchesRunning =>
                "Matches are already generated and the phase is in progress: the place cannot be released. Decide the upstream result first.",
            ApplicationErrorCodes.ResolutionReleaseBlockedByDraw =>
                "The team is still in a draw whose placements remain.",
            _ => "This place cannot be released.",
        };

    private static ApplicationFailureException Fail(string message, string code) =>
        new(message, code);
}
