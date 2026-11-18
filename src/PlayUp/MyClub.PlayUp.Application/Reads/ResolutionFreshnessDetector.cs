// -----------------------------------------------------------------------
// <copyright file="ResolutionFreshnessDetector.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Stages.ReleaseResolution;
using MyClub.PlayUp.Application.Standings;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Qualification;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Domain.Standings;

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Replays qualification and progression selection against stored provenance.
/// Each call recomputes from the stages and match rows of that read.
/// Nothing is kept from one read to the next, and the call issues no SQL.
/// Read-only: occupants are never rewritten.
/// A qualification <c>SourcePathId</c> that matches no current path on the source stage is
/// <see cref="StructureResolutionFreshness.Perime"/> with
/// <see cref="StructureResolutionStalenessCause.RegleQualification"/>.
/// A progression <c>SourcePathId</c> that matches no current path is
/// <see cref="StructureResolutionFreshness.Indetermine"/> with
/// <see cref="StructureResolutionIndeterminacyCause.SourceAbsente"/>: a missing progression path is not a rule change.
/// An unfinished progression match is <see cref="StructureResolutionIndeterminacyCause.MatchNonJoue"/>.
/// A level score is <see cref="StructureResolutionIndeterminacyCause.Egalite"/>.
/// A tied standing is <see cref="StructureResolutionIndeterminacyCause.EgaliteClassement"/>.
/// A qualification condition that no longer holds is <see cref="StructureResolutionIndeterminacyCause.ConditionNonRemplie"/>.
/// A configuration exception swallowed while reading a standing or assembling a fixture is
/// <see cref="StructureResolutionIndeterminacyCause.ConfigurationInvalide"/>.
/// <see cref="StructureResolutionIndeterminacyCause.SourceAbsente"/> records
/// <see cref="ResolutionSourceAbsence"/>: the id does not parse, the stage was not loaded, or the path,
/// fixture, or round is gone. A stage missing from this call is not treated as deleted.
/// </summary>
public static class ResolutionFreshnessDetector
{
    /// <summary>
    /// Evaluates every provenance occupant on <paramref name="stages"/>.
    /// Stages with no provenance are omitted.
    /// Recomputed on every call from <paramref name="matchSummariesByStage"/>.
    /// No result is retained for a later read, and no query is added.
    /// </summary>
    /// <param name="competition">Owning competition (active entries for overall standings).</param>
    /// <param name="stages">Competition stages, including destinations that store provenance.</param>
    /// <param name="matchSummariesByStage">Match rows already loaded for the Structure overview.</param>
    /// <returns>Occupants keyed by destination stage id.</returns>
    public static IReadOnlyDictionary<Guid, IReadOnlyList<StructureResolutionOccupantDto>> Evaluate(
        Competition competition,
        IReadOnlyList<Stage> stages,
        IReadOnlyDictionary<StageId, IReadOnlyList<MatchSummaryRow>>? matchSummariesByStage)
    {
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(stages);

        return Project(
            competition,
            stages,
            matchSummariesByStage,
            Analyze(competition, stages, matchSummariesByStage));
    }

    /// <summary>
    /// Same evaluation as <see cref="Evaluate"/>, including the entry a fresh replay would select.
    /// </summary>
    public static IReadOnlyList<ResolutionOccupantAssessment> Inspect(
        Competition competition,
        IReadOnlyList<Stage> stages,
        IReadOnlyDictionary<StageId, IReadOnlyList<MatchSummaryRow>>? matchSummariesByStage)
    {
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(stages);
        return [.. Analyze(competition, stages, matchSummariesByStage).Select(occupant => occupant.ToAssessment())];
    }

    private static List<WorkingOccupant> Analyze(
        Competition competition,
        IReadOnlyList<Stage> stages,
        IReadOnlyDictionary<StageId, IReadOnlyList<MatchSummaryRow>>? matchSummariesByStage)
    {
        var summaries = matchSummariesByStage ?? new Dictionary<StageId, IReadOnlyList<MatchSummaryRow>>();
        var occupants = Collect(stages);
        if (occupants.Count == 0)
        {
            return occupants;
        }

        var byId = stages.ToDictionary(stage => stage.Id.Value);
        var standings = new StandingCache(competition, summaries);
        var names = competition.Entries.ToDictionary(entry => entry.Id.Value, entry => entry.DisplayName);
        var outcomes = new OutcomeCache(summaries, names);
        foreach (var occupant in occupants)
        {
            try
            {
                Assess(occupant, byId, standings, outcomes);
            }
            catch (Exception ex) when (ex is DomainException or ApplicationFailureException)
            {
                MarkIndeterminate(occupant, Classify(ex));
                occupant.DependsOnStageId = null;
                occupant.UpstreamStageId = null;
                occupant.DependsOnEntryIds.Clear();
                occupant.ClearReplay();
            }
        }

        Propagate(occupants);
        return occupants;
    }

    private static List<WorkingOccupant> Collect(IReadOnlyList<Stage> stages)
    {
        var occupants = new List<WorkingOccupant>();
        foreach (var stage in stages)
        {
            foreach (var slot in stage.Slots)
            {
                if (slot.ResolutionProvenance is not { } provenance || slot.EntryId is not { } entryId)
                {
                    continue;
                }

                occupants.Add(WorkingOccupant.Create(
                    stage.Id.Value,
                    StructureResolutionOccupantTarget.Slot,
                    entryId.Value,
                    provenance,
                    slot.SlotKey,
                    groupId: null));
            }

            foreach (var entry in stage.CompositionEntries)
            {
                if (entry.ResolutionProvenance is not { } provenance)
                {
                    continue;
                }

                occupants.Add(WorkingOccupant.Create(
                    stage.Id.Value,
                    StructureResolutionOccupantTarget.Population,
                    entry.EntryId.Value,
                    provenance,
                    slotKey: null,
                    groupId: null));
            }

            foreach (var row in stage.GroupResolutionProvenances)
            {
                occupants.Add(WorkingOccupant.Create(
                    stage.Id.Value,
                    StructureResolutionOccupantTarget.Group,
                    row.EntryId.Value,
                    row.Provenance,
                    slotKey: null,
                    row.GroupId.Value));
            }
        }

        return occupants;
    }

    private static void Assess(
        WorkingOccupant occupant,
        Dictionary<Guid, Stage> stagesById,
        StandingCache standings,
        OutcomeCache outcomes)
    {
        switch (occupant.Kind)
        {
            case ResolutionProvenanceKind.Directe:
                MarkCurrent(occupant);
                return;
            case ResolutionProvenanceKind.NonVerifiee:
                occupant.State = StructureResolutionFreshness.NonVerifiee;
                occupant.Cause = null;
                occupant.ClearGuidance();
                return;
            case ResolutionProvenanceKind.Qualification:
                AssessQualification(occupant, stagesById, standings);
                return;
            case ResolutionProvenanceKind.Progression:
                AssessProgression(occupant, stagesById, outcomes);
                return;
            case ResolutionProvenanceKind.Tirage:
                AssessDraw(occupant, stagesById);
                return;
            default:
                MarkIndeterminate(occupant, StructureResolutionIndeterminacyCause.Autre);
                return;
        }
    }

    /// <summary>
    /// When the source stage is found and no current qualification path matches
    /// <see cref="WorkingOccupant.SourcePathId"/>, the occupant is
    /// <see cref="StructureResolutionFreshness.Perime"/> with cause
    /// <see cref="StructureResolutionStalenessCause.RegleQualification"/>.
    /// A missing source stage stays <see cref="StructureResolutionFreshness.Indetermine"/>.
    /// </summary>
    private static void AssessQualification(
        WorkingOccupant occupant,
        Dictionary<Guid, Stage> stagesById,
        StandingCache standings)
    {
        if (!TryReadPrefixedId(occupant.SourcePathId, 'Q', out var sourceId))
        {
            MarkIndeterminate(
                occupant,
                StructureResolutionIndeterminacyCause.SourceAbsente,
                absence: ResolutionSourceAbsence.Unreadable);
            return;
        }

        if (!stagesById.TryGetValue(sourceId, out var source))
        {
            MarkIndeterminate(
                occupant,
                StructureResolutionIndeterminacyCause.SourceAbsente,
                absence: ResolutionSourceAbsence.StageNotLoaded,
                parsedSourceStageId: sourceId);
            return;
        }

        var path = FindQualificationPath(source, occupant.SourcePathId);
        if (path is null)
        {
            MarkStale(occupant, StructureResolutionStalenessCause.RegleQualification);
            RememberRuleReplacement(occupant, source, standings);
            return;
        }

        var standingRead = standings.Resolve(source, path);
        if (standingRead.ConfigurationInvalid)
        {
            RememberSource(occupant, source.Id.Value);
            MarkIndeterminate(occupant, StructureResolutionIndeterminacyCause.ConfigurationInvalide);
            return;
        }

        var standing = standingRead.Standing!;

        RememberDependencies(occupant, source.Id.Value, standing.Rows.Select(row => row.EntryId.Value));
        var storedEntry = new EntryId(occupant.EntryId);
        if (standing.Find(storedEntry) is not null
            && string.Equals(
                ResolutionUpstreamFingerprint.ForQualificationSelection(standing, storedEntry),
                occupant.Fingerprint,
                StringComparison.Ordinal))
        {
            occupant.RememberReplay(occupant.EntryId, occupant.Fingerprint);
            MarkCurrent(occupant);
            return;
        }

        var selection = PathOccupantSelector.SelectQualification(path, standing);
        if (selection.Status == PathOccupantSelectionStatus.Resolved && selection.EntryId == storedEntry)
        {
            occupant.RememberReplay(
                occupant.EntryId,
                ResolutionUpstreamFingerprint.ForQualificationSelection(standing, storedEntry));
            MarkCurrent(occupant);
            return;
        }

        if (selection.Status == PathOccupantSelectionStatus.Resolved
            && selection.EntryId is { } selected
            && selected != storedEntry)
        {
            occupant.RememberReplay(
                selected.Value,
                ResolutionUpstreamFingerprint.ForQualificationSelection(standing, selected));
            MarkStale(occupant, StructureResolutionStalenessCause.ResultatAmont);
            return;
        }

        occupant.ClearReplay();
        MarkIndeterminate(occupant, ClassifyQualification(selection));
    }

    /// <summary>
    /// When the source stage is found and no current progression path matches
    /// <see cref="WorkingOccupant.SourcePathId"/>, the occupant is
    /// <see cref="StructureResolutionFreshness.Indetermine"/>.
    /// Unlike <see cref="AssessQualification"/>, a missing progression path is not
    /// <see cref="StructureResolutionFreshness.Perime"/> and has no rule cause.
    /// </summary>
    private static void AssessProgression(
        WorkingOccupant occupant,
        Dictionary<Guid, Stage> stagesById,
        OutcomeCache outcomes)
    {
        if (!TryReadPrefixedId(occupant.SourcePathId, 'P', out var sourceId))
        {
            MarkIndeterminate(
                occupant,
                StructureResolutionIndeterminacyCause.SourceAbsente,
                absence: ResolutionSourceAbsence.Unreadable);
            return;
        }

        if (!stagesById.TryGetValue(sourceId, out var source))
        {
            MarkIndeterminate(
                occupant,
                StructureResolutionIndeterminacyCause.SourceAbsente,
                absence: ResolutionSourceAbsence.StageNotLoaded,
                parsedSourceStageId: sourceId);
            return;
        }

        var path = FindProgressionPath(source, occupant.SourcePathId);
        if (path is null)
        {
            RememberSource(occupant, source.Id.Value);
            MarkIndeterminate(
                occupant,
                StructureResolutionIndeterminacyCause.SourceAbsente,
                absence: ResolutionSourceAbsence.PathGone,
                parsedSourceStageId: source.Id.Value);
            return;
        }

        var read = outcomes.Resolve(source, path);
        if (read.Outcome is not { } outcome)
        {
            RememberSource(occupant, source.Id.Value);
            MarkIndeterminate(
                occupant,
                read.Failure ?? StructureResolutionIndeterminacyCause.Autre,
                read.MatchId,
                read.MatchLabel,
                read.Absence,
                source.Id.Value);
            return;
        }

        RememberDependencies(
            occupant,
            source.Id.Value,
            [outcome.WinnerEntryId.Value, outcome.LoserEntryId.Value]);
        var progressionFingerprint = ResolutionUpstreamFingerprint.ForProgressionOutcome(outcome, path.Outcome);
        if (string.Equals(progressionFingerprint, occupant.Fingerprint, StringComparison.Ordinal))
        {
            occupant.RememberReplay(occupant.EntryId, occupant.Fingerprint);
            MarkCurrent(occupant);
            return;
        }

        var selection = PathOccupantSelector.SelectProgression(path, outcome);
        if (selection.Status == PathOccupantSelectionStatus.Resolved && selection.EntryId == new EntryId(occupant.EntryId))
        {
            occupant.RememberReplay(occupant.EntryId, progressionFingerprint);
            MarkCurrent(occupant);
            return;
        }

        if (selection.Status == PathOccupantSelectionStatus.Resolved && selection.EntryId is { } selected)
        {
            occupant.RememberReplay(selected.Value, progressionFingerprint);
            MarkStale(occupant, StructureResolutionStalenessCause.ResultatAmont);
            return;
        }

        occupant.ClearReplay();
        MarkIndeterminate(occupant, StructureResolutionIndeterminacyCause.Autre);
    }

    private static void AssessDraw(WorkingOccupant occupant, Dictionary<Guid, Stage> stagesById)
    {
        if (!TryReadPrefixedId(occupant.SourcePathId, 'T', out var drawId)
            || !stagesById.TryGetValue(occupant.StageId, out var stage))
        {
            MarkIndeterminate(occupant, StructureResolutionIndeterminacyCause.SourceAbsente);
            return;
        }

        var draw = stage.Draws.FirstOrDefault(candidate => candidate.Id.Value == drawId);
        if (draw?.Inputs is null)
        {
            MarkIndeterminate(occupant, StructureResolutionIndeterminacyCause.SourceAbsente);
            return;
        }

        RememberDependencies(occupant, stage.Id.Value, draw.Inputs.Entries.Select(entry => entry.Value));
        if (DrawPopulationChanged(stage, draw))
        {
            MarkStale(occupant, StructureResolutionStalenessCause.ResultatAmont);
            return;
        }

        MarkCurrent(occupant);
    }

    private static void Propagate(List<WorkingOccupant> occupants)
    {
        var limit = occupants.Count;
        for (var pass = 0; pass < limit; pass++)
        {
            var changed = false;
            foreach (var occupant in occupants)
            {
                if (occupant.State != StructureResolutionFreshness.AJour
                    || occupant.Kind is ResolutionProvenanceKind.Directe or ResolutionProvenanceKind.NonVerifiee
                    || occupant.DependsOnStageId is not Guid sourceStageId
                    || occupant.DependsOnEntryIds.Count == 0)
                {
                    continue;
                }

                var upstreamStale = occupants.Any(other =>
                    !ReferenceEquals(other, occupant)
                    && other.StageId == sourceStageId
                    && other.State == StructureResolutionFreshness.Perime
                    && occupant.DependsOnEntryIds.Contains(other.EntryId));
                if (!upstreamStale)
                {
                    continue;
                }

                MarkStale(occupant, StructureResolutionStalenessCause.AmontPerime);
                changed = true;
            }

            if (!changed)
            {
                return;
            }
        }
    }

    private static Dictionary<Guid, IReadOnlyList<StructureResolutionOccupantDto>> Project(
        Competition competition,
        IReadOnlyList<Stage> stages,
        IReadOnlyDictionary<StageId, IReadOnlyList<MatchSummaryRow>>? matchSummariesByStage,
        List<WorkingOccupant> occupants)
    {
        var order = new Dictionary<Guid, int>(competition.StageIds.Count);
        for (var index = 0; index < competition.StageIds.Count; index++)
        {
            order[competition.StageIds[index].Value] = index;
        }

        var stagesById = stages.ToDictionary(stage => stage.Id.Value);
        var matchesByStage = new Dictionary<Guid, ResolutionReleaseMatch[]>();
        if (matchSummariesByStage is not null)
        {
            foreach (var (stageId, rows) in matchSummariesByStage)
            {
                matchesByStage[stageId.Value] = rows
                    .Select(row => new ResolutionReleaseMatch(
                        row.Id.Value,
                        row.Status,
                        row.HomeEntryId?.Value,
                        row.AwayEntryId?.Value))
                    .ToArray();
            }
        }

        return occupants
            .OrderBy(occupant => order.GetValueOrDefault(occupant.StageId, int.MaxValue))
            .ThenBy(occupant => occupant.Target)
            .ThenBy(occupant => occupant.SlotKey ?? string.Empty, StringComparer.Ordinal)
            .ThenBy(occupant => occupant.GroupId ?? Guid.Empty)
            .ThenBy(occupant => occupant.EntryId)
            .GroupBy(occupant => occupant.StageId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<StructureResolutionOccupantDto>)group.Select(occupant =>
                {
                    stagesById.TryGetValue(occupant.StageId, out var stage);
                    var source = SourceStage(occupant, stagesById);
                    IReadOnlyList<ResolutionReleaseMatch>? matches = null;
                    if (matchSummariesByStage is not null)
                    {
                        // A missing stage key means this caller loaded no rows for it.
                        // The structure read writes an empty list for every stage, so a
                        // null dictionary is the case where matches were not loaded at all.
                        matchesByStage.TryGetValue(occupant.StageId, out var loaded);
                        matches = loaded ?? [];
                    }

                    var verdict = ResolutionDefinitiveness.Evaluate(
                        competition,
                        source,
                        QualificationPathOf(source, occupant),
                        occupant.ToAssessment(),
                        stage,
                        matches);
                    return occupant.ToDto(verdict.Releasable, verdict.Definitive, stage);
                }).ToArray());
    }

    /// <summary>
    /// A replaced rule no longer matches the stored path. Replay the single current path
    /// that still addresses this occupant, so the cascade can write the new selection.
    /// </summary>
    private static void RememberRuleReplacement(
        WorkingOccupant occupant,
        Stage source,
        StandingCache standings)
    {
        var paths = source.Regulation.QualificationRules?.Paths;
        if (paths is null)
        {
            occupant.ClearReplay();
            return;
        }

        QualificationPath? match = null;
        foreach (var candidate in paths)
        {
            if (!Targets(candidate.Destination, occupant))
            {
                continue;
            }

            if (match is not null)
            {
                occupant.ClearReplay();
                return;
            }

            match = candidate;
        }

        if (match is null)
        {
            occupant.ClearReplay();
            return;
        }

        var standingRead = standings.Resolve(source, match);
        if (standingRead.Standing is not { } standing)
        {
            occupant.ClearReplay();
            return;
        }

        RememberDependencies(occupant, source.Id.Value, standing.Rows.Select(row => row.EntryId.Value));
        occupant.RememberPath(ResolutionPathIdentifier.ForQualification(source.Id, match));
        var selection = PathOccupantSelector.SelectQualification(match, standing);
        if (selection.Status == PathOccupantSelectionStatus.Resolved && selection.EntryId is { } selected)
        {
            occupant.RememberReplay(
                selected.Value,
                ResolutionUpstreamFingerprint.ForQualificationSelection(standing, selected));
            return;
        }

        occupant.ClearReplay();
    }

    private static bool Targets(QualificationDestination destination, WorkingOccupant occupant)
    {
        if (destination.StageId.Value != occupant.StageId)
        {
            return false;
        }

        return occupant.Target switch
        {
            StructureResolutionOccupantTarget.Slot =>
                destination.TargetsSlot
                && string.Equals(destination.SlotKey, occupant.SlotKey, StringComparison.Ordinal),
            StructureResolutionOccupantTarget.Group =>
                destination.TargetsGroup && destination.GroupId?.Value == occupant.GroupId,
            StructureResolutionOccupantTarget.Population => destination.TargetsPopulation,
            _ => false
        };
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

    private static ProgressionPath? FindProgressionPath(Stage source, string sourcePathId)
    {
        var paths = source.Regulation.ProgressionRules?.Paths;
        if (paths is null)
        {
            return null;
        }

        foreach (var path in paths)
        {
            if (string.Equals(
                    ResolutionPathIdentifier.ForProgression(source.Id, path),
                    sourcePathId,
                    StringComparison.Ordinal))
            {
                return path;
            }
        }

        return null;
    }

    private static bool DrawPopulationChanged(Stage stage, Draw draw)
    {
        var pool = draw.Inputs!.Entries.ToHashSet();
        var composition = stage.CompositionEntries.Select(entry => entry.EntryId).ToHashSet();
        if (!pool.IsSubsetOf(composition))
        {
            return true;
        }

        foreach (var extra in composition)
        {
            if (pool.Contains(extra))
            {
                continue;
            }

            var placed = draw.Kind == DrawResolutionKind.Group
                ? stage.Groups.Any(group => group.EntryIds.Contains(extra))
                : stage.Slots.Any(slot => slot.EntryId == extra);
            if (!placed)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Records the phase a selection was read from.
    /// Draw propagation stays on <see cref="WorkingOccupant.DependsOnStageId"/> and does not
    /// expose that same phase as a navigation target.
    /// </summary>
    private static void RememberSource(WorkingOccupant occupant, Guid sourceStageId)
    {
        occupant.DependsOnStageId = sourceStageId;
        occupant.ParsedSourceStageId = sourceStageId;
        if (occupant.Kind != ResolutionProvenanceKind.Tirage)
        {
            occupant.UpstreamStageId = sourceStageId;
        }
    }

    private static Stage? SourceStage(WorkingOccupant occupant, Dictionary<Guid, Stage> stagesById)
    {
        if (occupant.ParsedSourceStageId is Guid parsed
            && stagesById.TryGetValue(parsed, out var parsedStage))
        {
            return parsedStage;
        }

        return occupant.DependsOnStageId is Guid depends && stagesById.TryGetValue(depends, out var depended)
            ? depended
            : null;
    }

    private static QualificationPath? QualificationPathOf(Stage? source, WorkingOccupant occupant) =>
        source is null || occupant.Kind != ResolutionProvenanceKind.Qualification
            ? null
            : FindQualificationPath(source, occupant.SourcePathId);

    private static void RememberDependencies(
        WorkingOccupant occupant,
        Guid sourceStageId,
        IEnumerable<Guid> entryIds)
    {
        RememberSource(occupant, sourceStageId);
        occupant.DependsOnEntryIds.Clear();
        foreach (var entryId in entryIds)
        {
            occupant.DependsOnEntryIds.Add(entryId);
        }
    }

    private static bool TryReadPrefixedId(string sourcePathId, char prefix, out Guid id)
    {
        id = default;
        if (sourcePathId.Length < 3 || sourcePathId[0] != prefix || sourcePathId[1] != '|')
        {
            return false;
        }

        var separator = sourcePathId.IndexOf('|', 2, StringComparison.Ordinal);
        if (separator < 0)
        {
            return false;
        }

        return Guid.TryParseExact(sourcePathId[2..separator], "N", out id);
    }

    private static StructureResolutionIndeterminacyCause ClassifyQualification(
        PathOccupantSelectionResult selection)
    {
        if (selection.Status == PathOccupantSelectionStatus.Skipped)
        {
            return StructureResolutionIndeterminacyCause.ConditionNonRemplie;
        }

        if (selection.Status == PathOccupantSelectionStatus.Indeterminate
            && selection.Indeterminacy == PathOccupantIndeterminacyReason.Empty)
        {
            return StructureResolutionIndeterminacyCause.SelectionVide;
        }

        if (selection.Status == PathOccupantSelectionStatus.Indeterminate
            && selection.Indeterminacy == PathOccupantIndeterminacyReason.Ambiguous)
        {
            return StructureResolutionIndeterminacyCause.EgaliteClassement;
        }

        return StructureResolutionIndeterminacyCause.Autre;
    }

    private static StructureResolutionIndeterminacyCause Classify(Exception exception) =>
        exception switch
        {
            DomainException { Code: StageErrorCodes.FixtureOutcomeNotFinished } =>
                StructureResolutionIndeterminacyCause.MatchNonJoue,
            DomainException { Code: StageErrorCodes.FixtureOutcomeUndecided } =>
                StructureResolutionIndeterminacyCause.Egalite,
            _ => StructureResolutionIndeterminacyCause.ConfigurationInvalide,
        };

    private static void MarkCurrent(WorkingOccupant occupant)
    {
        occupant.State = StructureResolutionFreshness.AJour;
        occupant.Cause = null;
        occupant.SourceAbsence = null;
        occupant.ClearGuidance();
    }

    private static void MarkStale(WorkingOccupant occupant, StructureResolutionStalenessCause cause)
    {
        occupant.State = StructureResolutionFreshness.Perime;
        occupant.Cause = cause;
        occupant.SourceAbsence = null;
        occupant.ClearGuidance();
    }

    private static void MarkIndeterminate(
        WorkingOccupant occupant,
        StructureResolutionIndeterminacyCause cause,
        Guid? matchId = null,
        string? matchLabel = null,
        ResolutionSourceAbsence? absence = null,
        Guid? parsedSourceStageId = null)
    {
        occupant.State = StructureResolutionFreshness.Indetermine;
        occupant.Cause = null;
        occupant.Indeterminacy = cause;
        occupant.ActionMatchId = matchId;
        occupant.ActionMatchLabel = matchLabel;
        occupant.SourceAbsence = cause == StructureResolutionIndeterminacyCause.SourceAbsente
            ? absence
            : null;
        if (parsedSourceStageId is Guid parsed)
        {
            occupant.ParsedSourceStageId = parsed;
        }
    }

    private sealed class WorkingOccupant
    {
        public required Guid StageId { get; init; }

        public required StructureResolutionOccupantTarget Target { get; init; }

        public required Guid EntryId { get; init; }

        public required ResolutionProvenanceKind Kind { get; init; }

        public required string SourcePathId { get; set; }

        public required string Fingerprint { get; init; }

        public string? SlotKey { get; init; }

        public Guid? GroupId { get; init; }

        public StructureResolutionFreshness State { get; set; }

        public StructureResolutionStalenessCause? Cause { get; set; }

        public StructureResolutionIndeterminacyCause? Indeterminacy { get; set; }

        public Guid? ActionMatchId { get; set; }

        public string? ActionMatchLabel { get; set; }

        public Guid? DependsOnStageId { get; set; }

        /// <summary>
        /// Gets or sets the phase the organizer should open. Distinct from
        /// <see cref="DependsOnStageId"/>, which also carries draw propagation edges.
        /// </summary>
        public Guid? UpstreamStageId { get; set; }

        public HashSet<Guid> DependsOnEntryIds { get; } = [];

        public Guid? ReplayEntryId { get; private set; }

        public string? ReplayFingerprint { get; private set; }

        public ResolutionSourceAbsence? SourceAbsence { get; set; }

        public Guid? ParsedSourceStageId { get; set; }

        public static WorkingOccupant Create(
            Guid stageId,
            StructureResolutionOccupantTarget target,
            Guid entryId,
            ResolutionProvenance provenance,
            string? slotKey,
            Guid? groupId) =>
            new()
            {
                StageId = stageId,
                Target = target,
                EntryId = entryId,
                Kind = provenance.Kind,
                SourcePathId = provenance.SourcePathId,
                Fingerprint = provenance.UpstreamResultFingerprint,
                SlotKey = slotKey,
                GroupId = groupId,
            };

        public void RememberReplay(Guid entryId, string fingerprint)
        {
            ReplayEntryId = entryId;
            ReplayFingerprint = fingerprint;
        }

        public void RememberPath(string sourcePathId)
        {
            SourcePathId = sourcePathId;
        }

        public void ClearReplay()
        {
            ReplayEntryId = null;
            ReplayFingerprint = null;
        }

        public void ClearGuidance()
        {
            Indeterminacy = null;
            ActionMatchId = null;
            ActionMatchLabel = null;
        }

        public StructureResolutionOccupantDto ToDto(bool releasable, bool definitive, Stage? stage)
        {
            var cascadeReplaceable = State == StructureResolutionFreshness.Perime
                && (Kind == ResolutionProvenanceKind.Qualification
                    || Kind == ResolutionProvenanceKind.Progression)
                && Cause != StructureResolutionStalenessCause.AmontPerime
                && ReplayEntryId is not null
                && ReplayFingerprint is not null
                && !BlocksCascadeReplace(stage);
            return new(
                Target,
                EntryId,
                Kind,
                SourcePathId,
                State,
                SlotKey,
                GroupId,
                Cause,
                UpstreamStageId,
                Indeterminacy,
                ActionMatchId,
                ActionMatchLabel,
                cascadeReplaceable,
                releasable,
                definitive);
        }

        /// <summary>
        /// True when the clicked slot carries a direct assignment (same rule as cascade origin guard).
        /// Does not walk downstream closure members; preview may still refuse and the client shows a toast.
        /// </summary>
        private bool BlocksCascadeReplace(Stage? stage)
        {
            if (stage is null
                || Target != StructureResolutionOccupantTarget.Slot
                || SlotKey is null)
            {
                return false;
            }

            return stage.DirectAssignments.Any(assignment =>
                string.Equals(assignment.SlotKey, SlotKey, StringComparison.Ordinal));
        }

        public ResolutionOccupantAssessment ToAssessment() =>
            new(
                StageId,
                Target,
                EntryId,
                Kind,
                SourcePathId,
                State,
                Cause,
                SlotKey,
                GroupId,
                DependsOnStageId,
                [.. DependsOnEntryIds],
                ReplayEntryId,
                ReplayFingerprint,
                Indeterminacy,
                ActionMatchId,
                ActionMatchLabel,
                SourceAbsence,
                ParsedSourceStageId);
    }

    /// <summary>
    /// A standing read for one qualification path.
    /// <see cref="ConfigurationInvalid"/> is set when the cache swallowed a configuration exception.
    /// </summary>
    private readonly record struct StandingRead(Standing? Standing)
    {
        public static StandingRead Invalid => new(null);

        public bool ConfigurationInvalid => Standing is null;
    }

    /// <summary>
    /// Standings built during this evaluation only. Discarded when the call returns.
    /// Not a cache from one Structure read to the next.
    /// </summary>
    private sealed class StandingCache(
        Competition competition,
        IReadOnlyDictionary<StageId, IReadOnlyList<MatchSummaryRow>> summaries)
    {
        private readonly Dictionary<Guid, (Standing? Overall, IReadOnlyDictionary<GroupId, Standing> Groups)?> _built = [];
        private readonly Dictionary<(Guid StageId, int Position), Standing?> _across = [];

        public StandingRead Resolve(Stage source, QualificationPath path)
        {
            try
            {
                var built = Built(source);
                if (built is null)
                {
                    return StandingRead.Invalid;
                }

                var standing = ResolveStanding(source, path, built.Value);
                return standing is null ? StandingRead.Invalid : new StandingRead(standing);
            }
            catch (Exception ex) when (ex is DomainException or ApplicationFailureException)
            {
                return StandingRead.Invalid;
            }
        }

        private (Standing? Overall, IReadOnlyDictionary<GroupId, Standing> Groups)? Built(Stage source)
        {
            if (_built.TryGetValue(source.Id.Value, out var cached))
            {
                return cached;
            }

            try
            {
                var rows = summaries.TryGetValue(source.Id, out var found) ? found : [];
                var built = QualificationStandingFactory.Build(competition, source, rows);
                _built[source.Id.Value] = built;
                return built;
            }
            catch (Exception ex) when (ex is DomainException or ApplicationFailureException)
            {
                _built[source.Id.Value] = null;
                return null;
            }
        }

        private Standing? ResolveStanding(
            Stage source,
            QualificationPath path,
            (Standing? Overall, IReadOnlyDictionary<GroupId, Standing> Groups) built)
        {
            if (path.Source.Scope == RankingScope.AcrossGroups)
            {
                var position = path.Source.AcrossGroupsPosition
                               ?? throw new ApplicationFailureException(
                                   "Across-groups qualification path is missing a position.",
                                   ApplicationErrorCodes.QualificationCandidatesEmpty);
                var key = (source.Id.Value, position);
                if (_across.TryGetValue(key, out var cached))
                {
                    return cached;
                }

                var rules = source.Regulation.StandingRules
                            ?? throw new ApplicationFailureException(
                                "Standing rules are required for across-groups qualification.",
                                StandingErrorCodes.RulesRequired);
                var rows = summaries.TryGetValue(source.Id, out var found) ? found : [];
                var derived = CrossGroupStandingAssembler.BuildFromSummaryRows(
                    source.Groups,
                    built.Groups,
                    position,
                    rows,
                    rules,
                    CalculateStanding.ToStandingPenalties(source.Penalties));
                _across[key] = derived;
                return derived;
            }

            if (path.Source.GroupId is null && path.Source.Scope != RankingScope.Group)
            {
                return built.Overall
                       ?? throw new ApplicationFailureException(
                           "No overall standing was provided for an overall qualification path.",
                           ApplicationErrorCodes.QualificationStandingMissing);
            }

            var groupId = path.Source.GroupId
                          ?? throw new ApplicationFailureException(
                              "Group-scoped qualification path is missing a group identity.",
                              ApplicationErrorCodes.QualificationGroupNotFound);
            if (source.FindGroup(groupId) is null
                || !built.Groups.TryGetValue(groupId, out var groupStanding))
            {
                throw new ApplicationFailureException(
                    $"No standing was provided for qualification group '{groupId}'.",
                    ApplicationErrorCodes.QualificationStandingMissing);
            }

            return groupStanding;
        }
    }

    /// <summary>
    /// Fixture outcomes built during this evaluation only. Discarded when the call returns.
    /// Not a cache from one Structure read to the next.
    /// </summary>
    private sealed class OutcomeCache(
        IReadOnlyDictionary<StageId, IReadOnlyList<MatchSummaryRow>> summaries,
        IReadOnlyDictionary<Guid, string> entryNames)
    {
        private readonly Dictionary<(Guid StageId, string PairKey), OutcomeRead> _outcomes = [];

        private static FixtureLegSnapshot? GuidanceLeg(FixtureConfrontationSnapshot snapshot, DomainException exception)
        {
            if (snapshot.Legs.Count == 0)
            {
                return null;
            }

            var legs = snapshot.Legs.OrderBy(leg => leg.LegIndex).ToArray();
            if (exception.Code == StageErrorCodes.FixtureOutcomeNotFinished)
            {
                return legs.FirstOrDefault(leg => leg.Status != MatchStatus.Finished) ?? legs[0];
            }

            return legs[^1];
        }

        public OutcomeRead Resolve(Stage source, ProgressionPath path)
        {
            var key = (source.Id.Value, path.SourcePairKey);
            if (_outcomes.TryGetValue(key, out var cached))
            {
                return cached;
            }

            var read = Read(source, path);
            _outcomes[key] = read;
            return read;
        }

        private OutcomeRead Read(Stage source, ProgressionPath path)
        {
            var fixture = source.FindFixtureByBracketPairKey(path.SourcePairKey);
            if (fixture is null)
            {
                return OutcomeRead.Failed(
                    StructureResolutionIndeterminacyCause.SourceAbsente,
                    absence: ResolutionSourceAbsence.PathGone);
            }

            var round = source.Rounds.FirstOrDefault(candidate =>
                candidate.Fixtures.Any(item => item.Id.Equals(fixture.Id)));
            if (round is null)
            {
                return OutcomeRead.Failed(
                    StructureResolutionIndeterminacyCause.SourceAbsente,
                    absence: ResolutionSourceAbsence.PathGone);
            }

            FixtureConfrontationSnapshot snapshot;
            try
            {
                var rows = summaries.TryGetValue(source.Id, out var found) ? found : [];
                snapshot = FixtureConfrontationSnapshotAssembler.Assemble(fixture, rows);
            }
            catch (Exception ex) when (ex is DomainException or ApplicationFailureException)
            {
                return OutcomeRead.Failed(Classify(ex));
            }

            var format = TieFormat.OrDefaultOneLeg(round.TieFormat);
            if (snapshot.Legs.Count != format.NumberOfLegs)
            {
                var missing = snapshot.Legs.OrderBy(leg => leg.LegIndex).FirstOrDefault();
                return OutcomeRead.Failed(
                    StructureResolutionIndeterminacyCause.MatchNonJoue,
                    missing?.MatchId.Value,
                    MatchLabel(missing));
            }

            try
            {
                var outcome = FixtureOutcomeResolver.Resolve(format, snapshot);
                return OutcomeRead.Resolved(outcome);
            }
            catch (DomainException ex)
            {
                var leg = GuidanceLeg(snapshot, ex);
                return OutcomeRead.Failed(Classify(ex), leg?.MatchId.Value, MatchLabel(leg));
            }
        }

        private string? MatchLabel(FixtureLegSnapshot? leg)
        {
            if (leg?.HomeEntryId is not { } home || leg.AwayEntryId is not { } away)
            {
                return null;
            }

            if (!entryNames.TryGetValue(home.Value, out var homeName)
                || !entryNames.TryGetValue(away.Value, out var awayName))
            {
                return null;
            }

            return $"{homeName} – {awayName}";
        }
    }

    private readonly record struct OutcomeRead(
        FixtureOutcome? Outcome,
        StructureResolutionIndeterminacyCause? Failure,
        Guid? MatchId,
        string? MatchLabel,
        ResolutionSourceAbsence? Absence = null)
    {
        public static OutcomeRead Resolved(FixtureOutcome outcome) => new(outcome, null, null, null);

        public static OutcomeRead Failed(
            StructureResolutionIndeterminacyCause cause,
            Guid? matchId = null,
            string? matchLabel = null,
            ResolutionSourceAbsence? absence = null) =>
            new(null, cause, matchId, matchLabel, absence);
    }
}
