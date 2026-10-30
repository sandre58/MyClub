// -----------------------------------------------------------------------
// <copyright file="ResolutionFreshnessDetector.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Standings;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
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
/// <see cref="StructureResolutionFreshness.Indetermine"/>: a missing progression path is not a rule change.
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

        var summaries = matchSummariesByStage ?? new Dictionary<StageId, IReadOnlyList<MatchSummaryRow>>();
        var occupants = Collect(stages);
        if (occupants.Count == 0)
        {
            return new Dictionary<Guid, IReadOnlyList<StructureResolutionOccupantDto>>();
        }

        var byId = stages.ToDictionary(stage => stage.Id.Value);
        var standings = new StandingCache(competition, summaries);
        var outcomes = new OutcomeCache(summaries);
        foreach (var occupant in occupants)
        {
            try
            {
                Assess(occupant, byId, standings, outcomes);
            }
            catch (Exception ex) when (ex is DomainException or ApplicationFailureException)
            {
                MarkIndeterminate(occupant);
                occupant.DependsOnStageId = null;
                occupant.DependsOnEntryIds.Clear();
            }
        }

        Propagate(occupants);
        return Project(competition, occupants);
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
                MarkIndeterminate(occupant);
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
        if (!TryReadPrefixedId(occupant.SourcePathId, 'Q', out var sourceId)
            || !stagesById.TryGetValue(sourceId, out var source))
        {
            MarkIndeterminate(occupant);
            return;
        }

        var path = FindQualificationPath(source, occupant.SourcePathId);
        if (path is null)
        {
            MarkStale(occupant, StructureResolutionStalenessCause.RegleQualification);
            return;
        }

        var standing = standings.Resolve(source, path);
        if (standing is null)
        {
            MarkIndeterminate(occupant);
            return;
        }

        RememberDependencies(occupant, source.Id.Value, standing.Rows.Select(row => row.EntryId.Value));
        var storedEntry = new EntryId(occupant.EntryId);
        if (standing.Find(storedEntry) is not null
            && string.Equals(
                ResolutionUpstreamFingerprint.ForQualificationSelection(standing, storedEntry),
                occupant.Fingerprint,
                StringComparison.Ordinal))
        {
            MarkCurrent(occupant);
            return;
        }

        var selection = PathOccupantSelector.SelectQualification(path, standing);
        if (selection.Status == PathOccupantSelectionStatus.Resolved && selection.EntryId == storedEntry)
        {
            MarkCurrent(occupant);
            return;
        }

        if (selection.Status == PathOccupantSelectionStatus.Resolved
            && selection.EntryId is { } selected
            && selected != storedEntry)
        {
            MarkStale(occupant, StructureResolutionStalenessCause.ResultatAmont);
            return;
        }

        MarkIndeterminate(occupant);
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
        if (!TryReadPrefixedId(occupant.SourcePathId, 'P', out var sourceId)
            || !stagesById.TryGetValue(sourceId, out var source))
        {
            MarkIndeterminate(occupant);
            return;
        }

        var path = FindProgressionPath(source, occupant.SourcePathId);
        if (path is null)
        {
            MarkIndeterminate(occupant);
            return;
        }

        var outcome = outcomes.Resolve(source, path);
        if (outcome is null)
        {
            MarkIndeterminate(occupant);
            return;
        }

        RememberDependencies(
            occupant,
            source.Id.Value,
            [outcome.WinnerEntryId.Value, outcome.LoserEntryId.Value]);
        if (string.Equals(
                ResolutionUpstreamFingerprint.ForProgressionOutcome(outcome, path.Outcome),
                occupant.Fingerprint,
                StringComparison.Ordinal))
        {
            MarkCurrent(occupant);
            return;
        }

        var selection = PathOccupantSelector.SelectProgression(path, outcome);
        if (selection.Status == PathOccupantSelectionStatus.Resolved && selection.EntryId == new EntryId(occupant.EntryId))
        {
            MarkCurrent(occupant);
            return;
        }

        if (selection.Status == PathOccupantSelectionStatus.Resolved)
        {
            MarkStale(occupant, StructureResolutionStalenessCause.ResultatAmont);
            return;
        }

        MarkIndeterminate(occupant);
    }

    private static void AssessDraw(WorkingOccupant occupant, Dictionary<Guid, Stage> stagesById)
    {
        if (!TryReadPrefixedId(occupant.SourcePathId, 'T', out var drawId)
            || !stagesById.TryGetValue(occupant.StageId, out var stage))
        {
            MarkIndeterminate(occupant);
            return;
        }

        var draw = stage.Draws.FirstOrDefault(candidate => candidate.Id.Value == drawId);
        if (draw?.Inputs is null)
        {
            MarkIndeterminate(occupant);
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
        List<WorkingOccupant> occupants)
    {
        var order = new Dictionary<Guid, int>(competition.StageIds.Count);
        for (var index = 0; index < competition.StageIds.Count; index++)
        {
            order[competition.StageIds[index].Value] = index;
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
                group => (IReadOnlyList<StructureResolutionOccupantDto>)group.Select(occupant => occupant.ToDto()).ToArray());
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

    private static void RememberDependencies(
        WorkingOccupant occupant,
        Guid sourceStageId,
        IEnumerable<Guid> entryIds)
    {
        occupant.DependsOnStageId = sourceStageId;
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

    private static void MarkCurrent(WorkingOccupant occupant)
    {
        occupant.State = StructureResolutionFreshness.AJour;
        occupant.Cause = null;
    }

    private static void MarkStale(WorkingOccupant occupant, StructureResolutionStalenessCause cause)
    {
        occupant.State = StructureResolutionFreshness.Perime;
        occupant.Cause = cause;
    }

    private static void MarkIndeterminate(WorkingOccupant occupant)
    {
        occupant.State = StructureResolutionFreshness.Indetermine;
        occupant.Cause = null;
    }

    private sealed class WorkingOccupant
    {
        public required Guid StageId { get; init; }

        public required StructureResolutionOccupantTarget Target { get; init; }

        public required Guid EntryId { get; init; }

        public required ResolutionProvenanceKind Kind { get; init; }

        public required string SourcePathId { get; init; }

        public required string Fingerprint { get; init; }

        public string? SlotKey { get; init; }

        public Guid? GroupId { get; init; }

        public StructureResolutionFreshness State { get; set; }

        public StructureResolutionStalenessCause? Cause { get; set; }

        public Guid? DependsOnStageId { get; set; }

        public HashSet<Guid> DependsOnEntryIds { get; } = [];

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

        public StructureResolutionOccupantDto ToDto() =>
            new(
                Target,
                EntryId,
                Kind,
                SourcePathId,
                State,
                SlotKey,
                GroupId,
                Cause);
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

        public Standing? Resolve(Stage source, QualificationPath path)
        {
            try
            {
                var built = Built(source);
                if (built is null)
                {
                    return null;
                }

                return ResolveStanding(source, path, built.Value);
            }
            catch (Exception ex) when (ex is DomainException or ApplicationFailureException)
            {
                return null;
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
    private sealed class OutcomeCache(IReadOnlyDictionary<StageId, IReadOnlyList<MatchSummaryRow>> summaries)
    {
        private readonly Dictionary<(Guid StageId, string PairKey), FixtureOutcome?> _outcomes = [];

        public FixtureOutcome? Resolve(Stage source, ProgressionPath path)
        {
            var key = (source.Id.Value, path.SourcePairKey);
            if (_outcomes.TryGetValue(key, out var cached))
            {
                return cached;
            }

            try
            {
                var fixture = source.FindFixtureByBracketPairKey(path.SourcePairKey);
                if (fixture is null)
                {
                    _outcomes[key] = null;
                    return null;
                }

                var round = source.Rounds.FirstOrDefault(candidate =>
                    candidate.Fixtures.Any(item => item.Id.Equals(fixture.Id)));
                if (round is null)
                {
                    _outcomes[key] = null;
                    return null;
                }

                var rows = summaries.TryGetValue(source.Id, out var found) ? found : [];
                var snapshot = FixtureConfrontationSnapshotAssembler.Assemble(fixture, rows);
                var outcome = FixtureOutcomeResolver.Resolve(TieFormat.OrDefaultOneLeg(round.TieFormat), snapshot);
                _outcomes[key] = outcome;
                return outcome;
            }
            catch (Exception ex) when (ex is DomainException or ApplicationFailureException)
            {
                _outcomes[key] = null;
                return null;
            }
        }
    }
}
