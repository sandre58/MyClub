// -----------------------------------------------------------------------
// <copyright file="ReleaseDefinitiveResolutions.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Application.Stages.CascadeReplace;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using ApplicationErrorCodes = MyClub.PlayUp.Application.ApplicationErrorCodes;
using ApplicationFailureException = MyClub.PlayUp.Application.ApplicationFailureException;

namespace MyClub.PlayUp.Application.Stages.ReleaseResolution;

/// <summary>
/// Releases every definitive place fed by one source phase, after one confirmation.
/// The preview works on copies. A refusal or an application exception on one place is an exclusion.
/// The confirmation judges the same way and writes only when the token and the place list still match.
/// </summary>
public static class ReleaseDefinitiveResolutions
{
    /// <summary>
    /// Computes the grouped release without writing the caller's stages or matches.
    /// </summary>
    /// <param name="competition">Owning competition.</param>
    /// <param name="sourceId">Source phase whose fed places are in scope. Null means places whose source phase was deleted.</param>
    /// <param name="destinationId">When set, only places stored on this phase are in scope.</param>
    /// <param name="stages">Every phase of the competition.</param>
    /// <param name="matches">Every match of those phases.</param>
    /// <param name="clock">Clock used if a place is released on a copy.</param>
    /// <returns>The token, the places that would be cleared, the exclusions, and the downstream freshness changes.</returns>
    public static DefinitiveResolutionReleaseResult Plan(
        Competition competition,
        StageId? sourceId,
        StageId? destinationId,
        IReadOnlyList<Stage> stages,
        IReadOnlyList<Match> matches,
        IClock clock)
    {
        Require(competition, stages, matches, clock);
        return Simulate(competition, sourceId, destinationId, stages, matches, clock).Result;
    }

    /// <summary>
    /// Releases the named primary places on the supplied aggregates when they are still exactly the plan.
    /// Callers persist with a single save. This method does not. A mismatch throws before any caller aggregate is written.
    /// </summary>
    /// <param name="competition">Owning competition.</param>
    /// <param name="sourceId">Source phase whose fed places are in scope. Null means places whose source phase was deleted.</param>
    /// <param name="destinationId">Destination filter used by the preview. Null releases every destination.</param>
    /// <param name="versionToken">Token returned by <see cref="Plan"/>.</param>
    /// <param name="places">Primary places from the preview, in that order. Population side effects are omitted.</param>
    /// <param name="stages">Every phase of the competition.</param>
    /// <param name="matches">Every match of those phases.</param>
    /// <param name="clock">Clock used when a place is released.</param>
    /// <returns>The same shape as the preview, computed on the state that was confirmed.</returns>
    public static DefinitiveResolutionReleaseResult Execute(
        Competition competition,
        StageId? sourceId,
        StageId? destinationId,
        string versionToken,
        IReadOnlyList<DefinitiveResolutionReleasePlace> places,
        IReadOnlyList<Stage> stages,
        IReadOnlyList<Match> matches,
        IClock clock)
    {
        Require(competition, stages, matches, clock);
        ArgumentNullException.ThrowIfNull(places);
        if (string.IsNullOrWhiteSpace(versionToken)
            || !string.Equals(CascadeReplaceVersion.Compute(stages, matches), versionToken, StringComparison.Ordinal))
        {
            throw Fail(
                "The release was confirmed against a state that has changed.",
                ApplicationErrorCodes.ResolutionReleaseConflict);
        }

        var simulation = Simulate(competition, sourceId, destinationId, stages, matches, clock);
        if (places.Any(place => !simulation.Released.Contains(place) && !simulation.Excluded.Contains(place)))
        {
            throw Fail(
                "A place in the confirmation is no longer definitive.",
                ApplicationErrorCodes.ResolutionReleaseNotDefinitive);
        }

        if (!simulation.Released.SequenceEqual(places))
        {
            throw Fail(
                "The places to release are not the places that were confirmed.",
                ApplicationErrorCodes.ResolutionReleaseBatchChanged);
        }

        foreach (var place in simulation.Released)
        {
            var live = Find(Inspect(competition, stages, matches), place);
            var stage = stages.Single(candidate => candidate.Id.Value == place.StageId);
            ReleaseResolution.EnsureReleasable(stage, live, matches);
            ReleaseResolution.Apply(competition, stage, matches, live, clock);
        }

        return simulation.Result;
    }

    private static Simulation Simulate(
        Competition competition,
        StageId? sourceId,
        StageId? destinationId,
        IReadOnlyList<Stage> stages,
        IReadOnlyList<Match> matches,
        IClock clock)
    {
        if (sourceId is { } listed && !stages.Any(stage => stage.Id.Equals(listed)))
        {
            throw Fail($"Stage '{listed}' was not found.", ApplicationErrorCodes.StageNotFound);
        }

        var token = CascadeReplaceVersion.Compute(stages, matches);
        var before = Inspect(competition, stages, matches);
        var order = StageOrder(competition);
        var candidates = before
            .Where(occupant => InScope(competition, occupant, sourceId, destinationId))
            .Select(occupant => (Occupant: occupant, Verdict: Judge(competition, stages, matches, occupant)))
            .Where(row => row.Verdict.Definitive)
            .Select(row => row.Occupant)
            .OrderBy(occupant => order.GetValueOrDefault(occupant.StageId, int.MaxValue))
            .ThenBy(occupant => occupant.Target)
            .ThenBy(occupant => occupant.SlotKey ?? string.Empty, StringComparer.Ordinal)
            .ThenBy(occupant => occupant.GroupId ?? Guid.Empty)
            .ThenBy(occupant => occupant.EntryId)
            .ToList();

        var scratchStages = stages.Select(CascadeAggregateCopy.Copy).ToList();
        var scratchMatches = matches.Select(CascadeAggregateCopy.Copy).ToList();
        var releases = new List<DefinitiveResolutionReleaseRow>();
        var detached = new List<ResolutionReleaseDetachedMatch>();
        var exclusions = new List<DefinitiveResolutionReleaseExclusion>();
        var released = new List<DefinitiveResolutionReleasePlace>();
        var excluded = new List<DefinitiveResolutionReleasePlace>();

        foreach (var candidate in candidates)
        {
            var place = ToPlace(candidate);
            var liveStages = scratchStages;
            var liveMatches = scratchMatches;
            var live = FindOrDefault(Inspect(competition, liveStages, liveMatches), place);
            if (live is null)
            {
                if (!AlreadyReleased(releases, place))
                {
                    exclusions.Add(Exclusion(
                        competition,
                        liveStages,
                        candidate,
                        ApplicationErrorCodes.ResolutionReleaseOccupantNotFound));
                    excluded.Add(place);
                }

                continue;
            }

            var verdict = Judge(competition, liveStages, liveMatches, live);
            if (!verdict.Definitive)
            {
                exclusions.Add(Exclusion(competition, liveStages, live, ApplicationErrorCodes.ResolutionReleaseNotDefinitive));
                excluded.Add(place);
                continue;
            }

            if (!verdict.Releasable)
            {
                var code = verdict.RefusalCode ?? ApplicationErrorCodes.ResolutionReleaseNotReleasable;
                exclusions.Add(Exclusion(competition, liveStages, live, code));
                excluded.Add(place);
                continue;
            }

            var attemptStages = liveStages.Select(CascadeAggregateCopy.Copy).ToList();
            var attemptMatches = liveMatches.Select(CascadeAggregateCopy.Copy).ToList();
            var attempt = Find(Inspect(competition, attemptStages, attemptMatches), place);
            var stage = attemptStages.Single(candidateStage => candidateStage.Id.Value == place.StageId);
            try
            {
                var applied = ReleaseResolution.Apply(competition, stage, attemptMatches, attempt, clock);
                scratchStages = attemptStages;
                scratchMatches = attemptMatches;
                releases.AddRange(Rows(competition, attemptStages, applied));
                detached.AddRange(applied.Detached);
                released.Add(place);
            }
            catch (Exception exception) when (exception is ApplicationFailureException or DomainException)
            {
                exclusions.Add(Exclusion(competition, liveStages, live, CodeOf(exception)));
                excluded.Add(place);
            }
        }

        var after = Inspect(competition, scratchStages, scratchMatches);
        return new Simulation(
            new DefinitiveResolutionReleaseResult(
                token,
                releases,
                detached,
                exclusions,
                Diff(competition, stages, before, after, released)),
            released,
            excluded);
    }

    private static void Require(
        Competition competition,
        IReadOnlyList<Stage> stages,
        IReadOnlyList<Match> matches,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(stages);
        ArgumentNullException.ThrowIfNull(matches);
        ArgumentNullException.ThrowIfNull(clock);
    }

    private static bool InScope(
        Competition competition,
        ResolutionOccupantAssessment occupant,
        StageId? sourceId,
        StageId? destinationId)
    {
        if (occupant.Kind is not (ResolutionProvenanceKind.Qualification or ResolutionProvenanceKind.Progression))
        {
            return false;
        }

        if (destinationId is { } destination && occupant.StageId != destination.Value)
        {
            return false;
        }

        if (sourceId is { } source)
        {
            return occupant.ParsedSourceStageId == source.Value;
        }

        return occupant.ParsedSourceStageId is Guid parsed
            && !competition.StageIds.Any(id => id.Value == parsed);
    }

    private static bool AlreadyReleased(
        IReadOnlyList<DefinitiveResolutionReleaseRow> releases,
        DefinitiveResolutionReleasePlace place) =>
        releases.Any(row =>
            row.StageId == place.StageId
            && row.Target == place.Target
            && row.EntryId == place.EntryId
            && string.Equals(row.SlotKey, place.SlotKey, StringComparison.Ordinal)
            && row.GroupId == place.GroupId);

    private static ResolutionDefinitivenessVerdict Judge(
        Competition competition,
        IReadOnlyList<Stage> stages,
        IReadOnlyList<Match> matches,
        ResolutionOccupantAssessment occupant)
    {
        var stagesById = stages.ToDictionary(stage => stage.Id.Value);
        stagesById.TryGetValue(occupant.StageId, out var destination);
        Stage? source = occupant.ParsedSourceStageId is Guid parsed && stagesById.TryGetValue(parsed, out var found)
            ? found
            : null;
        var path = source is null || occupant.Kind != ResolutionProvenanceKind.Qualification
            ? null
            : FindQualificationPath(source, occupant.SourcePathId);
        var facts = matches
            .Where(match => match.StageId.Value == occupant.StageId)
            .Select(match => new ResolutionReleaseMatch(
                match.Id.Value,
                match.Status,
                match.HomeEntryId?.Value,
                match.AwayEntryId?.Value))
            .ToArray();
        return ResolutionDefinitiveness.Evaluate(competition, source, path, occupant, destination, facts);
    }

    private static QualificationPath? FindQualificationPath(Stage source, string sourcePathId)
    {
        var paths = source.Regulation.QualificationRules?.Paths;
        if (paths is null)
        {
            return null;
        }

        foreach (var path in paths)
        {
            if (string.Equals(
                    ResolutionPathIdentifier.ForQualification(source.Id, path),
                    sourcePathId,
                    StringComparison.Ordinal))
            {
                return path;
            }
        }

        return null;
    }

    private static List<ResolutionOccupantAssessment> Inspect(
        Competition competition,
        IReadOnlyList<Stage> stages,
        IReadOnlyList<Match> matches) =>
        ResolutionFreshnessDetector.Inspect(competition, stages, Summaries(matches)).ToList();

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

    private static List<DefinitiveResolutionReleaseRow> Rows(
        Competition competition,
        IReadOnlyList<Stage> stages,
        ReleaseResolution.AppliedRelease applied)
    {
        var rows = new List<DefinitiveResolutionReleaseRow>(applied.Releases.Count);
        for (var index = 0; index < applied.Releases.Count; index++)
        {
            var row = applied.Releases[index];

            // Index 0 is the place Apply was asked to release. Any later row is the population
            // side effect Apply appends only when PopulationRemoved is true.
            rows.Add(new DefinitiveResolutionReleaseRow(
                row.StageId,
                row.Target,
                row.SlotKey,
                row.GroupId,
                row.EntryId,
                row.EntryName,
                StageName(stages, row.StageId),
                PlaceLabel(stages, row.StageId, row.SlotKey, row.GroupId),
                index == 0 && applied.PopulationRemoved,
                index > 0));
        }

        return rows;
    }

    private static DefinitiveResolutionReleaseExclusion Exclusion(
        Competition competition,
        IReadOnlyList<Stage> stages,
        ResolutionOccupantAssessment occupant,
        string code) =>
        new(
            occupant.StageId,
            occupant.Target,
            occupant.SlotKey,
            occupant.GroupId,
            occupant.EntryId,
            EntryName(competition, occupant.EntryId),
            StageName(stages, occupant.StageId),
            PlaceLabel(stages, occupant.StageId, occupant.SlotKey, occupant.GroupId),
            code);

    private static List<ResolutionReleaseAffectedOccupant> Diff(
        Competition competition,
        IReadOnlyList<Stage> stages,
        IReadOnlyList<ResolutionOccupantAssessment> before,
        IReadOnlyList<ResolutionOccupantAssessment> after,
        IReadOnlyList<DefinitiveResolutionReleasePlace> released)
    {
        var afterByKey = after.ToDictionary(Key);
        var affected = new List<ResolutionReleaseAffectedOccupant>();
        foreach (var occupant in before)
        {
            if (released.Any(place => Same(occupant, place)))
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
                    next.State,
                    EntryName(competition, occupant.EntryId),
                    StageName(stages, occupant.StageId),
                    PlaceLabel(stages, occupant.StageId, occupant.SlotKey, occupant.GroupId)));
            }
        }

        return affected;
    }

    private static Dictionary<Guid, int> StageOrder(Competition competition)
    {
        var order = new Dictionary<Guid, int>(competition.StageIds.Count);
        for (var index = 0; index < competition.StageIds.Count; index++)
        {
            order[competition.StageIds[index].Value] = index;
        }

        return order;
    }

    private static DefinitiveResolutionReleasePlace ToPlace(ResolutionOccupantAssessment occupant) =>
        new(occupant.StageId, occupant.Target, occupant.EntryId, occupant.SlotKey, occupant.GroupId);

    private static ResolutionOccupantAssessment Find(
        IReadOnlyList<ResolutionOccupantAssessment> occupants,
        DefinitiveResolutionReleasePlace place) =>
        FindOrDefault(occupants, place)
        ?? throw Fail("There is nothing to release.", ApplicationErrorCodes.ResolutionReleaseOccupantNotFound);

    private static ResolutionOccupantAssessment? FindOrDefault(
        IReadOnlyList<ResolutionOccupantAssessment> occupants,
        DefinitiveResolutionReleasePlace place) =>
        occupants.FirstOrDefault(occupant => Same(occupant, place));

    private static bool Same(ResolutionOccupantAssessment occupant, DefinitiveResolutionReleasePlace place) =>
        occupant.StageId == place.StageId
        && occupant.Target == place.Target
        && occupant.EntryId == place.EntryId
        && string.Equals(occupant.SlotKey, place.SlotKey, StringComparison.Ordinal)
        && occupant.GroupId == place.GroupId;

    private static string Key(ResolutionOccupantAssessment occupant) =>
        string.Join(
            '|',
            occupant.StageId.ToString("N"),
            occupant.Target,
            occupant.EntryId.ToString("N"),
            occupant.SlotKey ?? string.Empty,
            occupant.GroupId?.ToString("N") ?? string.Empty);

    private static string? EntryName(Competition competition, Guid entryId) =>
        competition.Entries.FirstOrDefault(entry => entry.Id.Value == entryId)?.DisplayName;

    private static string? StageName(IReadOnlyList<Stage> stages, Guid stageId) =>
        stages.FirstOrDefault(stage => stage.Id.Value == stageId)?.Name.Value;

    private static string? PlaceLabel(
        IReadOnlyList<Stage> stages,
        Guid stageId,
        string? slotKey,
        Guid? groupId)
    {
        if (!string.IsNullOrEmpty(slotKey))
        {
            return slotKey;
        }

        if (groupId is not Guid id)
        {
            return null;
        }

        return stages
            .FirstOrDefault(stage => stage.Id.Value == stageId)?
            .Groups.FirstOrDefault(group => group.Id.Value == id)?
            .Name;
    }

    private static string CodeOf(Exception exception) =>
        exception switch
        {
            ApplicationFailureException application when !string.IsNullOrWhiteSpace(application.Code) => application.Code,
            DomainException domain when !string.IsNullOrWhiteSpace(domain.Code) => domain.Code,
            _ => ApplicationErrorCodes.ResolutionReleaseNotReleasable,
        };

    private static ApplicationFailureException Fail(string message, string code) => new(message, code);

    private sealed record Simulation(
        DefinitiveResolutionReleaseResult Result,
        IReadOnlyList<DefinitiveResolutionReleasePlace> Released,
        IReadOnlyList<DefinitiveResolutionReleasePlace> Excluded);
}
