// -----------------------------------------------------------------------
// <copyright file="BackfillResolutionProvenance.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Standings;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Progression;
using MyClub.PlayUp.Domain.Qualification;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Domain.Standings;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Idempotent historical backfill of resolution provenance on slots, composition, and group membership.
/// </summary>
public static class BackfillResolutionProvenance
{
    /// <summary>
    /// Backfills provenance witnesses for all stages of a competition without changing occupants.
    /// </summary>
    /// <param name="competition">Owning competition (active entries for standings).</param>
    /// <param name="competitionStages">All competition stages, already loaded for update.</param>
    /// <param name="matchesByStage">Optional matches per stage for Qual/Prog replay.</param>
    /// <returns>Aggregate counters for the run.</returns>
    public static BackfillResolutionProvenanceReport Execute(
        Competition competition,
        IReadOnlyList<Stage> competitionStages,
        IReadOnlyDictionary<StageId, IReadOnlyList<Match>>? matchesByStage = null)
    {
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(competitionStages);

        var tracker = new Tracker();
        foreach (var stage in competitionStages.ToArray())
        {
            BackfillDirectAssignments(stage, tracker);
            BackfillOccupiedSlots(
                competition,
                stage,
                competitionStages,
                matchesByStage,
                tracker);
            BackfillCompositionEntries(
                competition,
                stage,
                competitionStages,
                matchesByStage,
                tracker);
            BackfillGroupMembers(
                competition,
                stage,
                competitionStages,
                matchesByStage,
                tracker);
        }

        return new BackfillResolutionProvenanceReport(
            competition.Id,
            tracker.BackfilledExact,
            tracker.NonVerifiee,
            tracker.Indeterminate);
    }

    private static void BackfillDirectAssignments(Stage stage, Tracker tracker)
    {
        foreach (var assignment in stage.DirectAssignments)
        {
            var slot = stage.FindSlot(assignment.SlotKey);
            if (slot?.EntryId is not { } occupant || !occupant.Equals(assignment.EntryId))
            {
                continue;
            }

            tracker.WriteSlot(
                stage,
                assignment.SlotKey,
                ResolutionProvenance.Directe(assignment.SlotKey, assignment.EntryId));
        }
    }

    private static void BackfillOccupiedSlots(
        Competition competition,
        Stage stage,
        IReadOnlyList<Stage> competitionStages,
        IReadOnlyDictionary<StageId, IReadOnlyList<Match>>? matchesByStage,
        Tracker tracker)
    {
        var snapshot = SlotFeedSnapshotAssembler.Assemble(stage, competitionStages);
        foreach (var slot in stage.Slots.ToArray())
        {
            if (slot.EntryId is not { } occupant)
            {
                continue;
            }

            if (stage.DirectAssignments.Any(d =>
                    string.Equals(d.SlotKey, slot.SlotKey, StringComparison.Ordinal)
                    && d.EntryId.Equals(occupant)))
            {
                continue;
            }

            var resolution = SlotFeedResolver.Resolve(snapshot, slot.SlotKey);
            var provenance = ResolveSlotProvenance(
                competition,
                stage,
                slot.SlotKey,
                occupant,
                resolution,
                competitionStages,
                matchesByStage,
                tracker);
            tracker.WriteSlot(stage, slot.SlotKey, provenance);
        }
    }

    private static ResolutionProvenance ResolveSlotProvenance(
        Competition competition,
        Stage stage,
        string slotKey,
        EntryId occupant,
        SlotFeedResolution resolution,
        IReadOnlyList<Stage> competitionStages,
        IReadOnlyDictionary<StageId, IReadOnlyList<Match>>? matchesByStage,
        Tracker tracker)
    {
        return resolution.Status switch
        {
            FeedResolutionStatus.Unique => ResolveUniqueFeedProvenance(
                competition,
                stage,
                slotKey,
                occupant,
                resolution.Source!,
                competitionStages,
                matchesByStage,
                tracker),
            FeedResolutionStatus.Missing
                or FeedResolutionStatus.MultipleFeeds
                or FeedResolutionStatus.InvalidFeed => ResolutionProvenance.NonVerifiee(),
            _ => ResolutionProvenance.NonVerifiee()
        };
    }

    private static ResolutionProvenance ResolveUniqueFeedProvenance(
        Competition competition,
        Stage stage,
        string slotKey,
        EntryId occupant,
        UniqueFeedSource source,
        IReadOnlyList<Stage> competitionStages,
        IReadOnlyDictionary<StageId, IReadOnlyList<Match>>? matchesByStage,
        Tracker tracker)
    {
        return source.Kind switch
        {
            FeedKind.Direct => ResolveDirectFeedProvenance(slotKey, occupant, source.Direct!.ConfiguredEntryId),
            FeedKind.Draw => ResolveDrawFeedProvenance(stage, slotKey, occupant, source.Draw!.DrawId),
            FeedKind.Qualification => ResolveQualificationSlotProvenance(
                competition,
                stage,
                slotKey,
                occupant,
                source.Qualification!,
                competitionStages,
                matchesByStage,
                tracker),
            FeedKind.Progression => ResolveProgressionSlotProvenance(
                stage,
                slotKey,
                occupant,
                source.Progression!,
                competitionStages,
                matchesByStage,
                tracker),
            _ => ResolutionProvenance.NonVerifiee()
        };
    }

    private static ResolutionProvenance ResolveDirectFeedProvenance(
        string slotKey,
        EntryId occupant,
        EntryId configuredEntryId) =>
        occupant.Equals(configuredEntryId)
            ? ResolutionProvenance.Directe(slotKey, configuredEntryId)
            : ResolutionProvenance.NonVerifiee();

    private static ResolutionProvenance ResolveDrawFeedProvenance(
        Stage stage,
        string slotKey,
        EntryId occupant,
        DrawId drawId)
    {
        var draw = stage.GetDraw(drawId);
        if (draw.Status != DrawStatus.Published
            || draw.Resolution.State != DrawResolutionState.Resolved
            || draw.Kind != DrawResolutionKind.Slot)
        {
            return ResolutionProvenance.NonVerifiee();
        }

        var placement = draw.Resolution.SlotResults
            .FirstOrDefault(candidate =>
                string.Equals(candidate.SlotKey, slotKey, StringComparison.Ordinal));
        return placement is not null && placement.EntryId.Equals(occupant)
            ? ResolutionProvenance.Tirage(drawId, slotKey, occupant)
            : ResolutionProvenance.NonVerifiee();
    }

    private static ResolutionProvenance ResolveQualificationSlotProvenance(
        Competition competition,
        Stage destination,
        string slotKey,
        EntryId occupant,
        QualificationFeedRef feed,
        IReadOnlyList<Stage> competitionStages,
        IReadOnlyDictionary<StageId, IReadOnlyList<Match>>? matchesByStage,
        Tracker tracker)
    {
        var source = ResolveCanonicalStage(feed.SourceStageId, competitionStages);
        var path = FindQualificationPath(source, feed.PathOrder);
        if (path is null
            || !string.Equals(path.Destination.SlotKey, slotKey, StringComparison.Ordinal))
        {
            return ResolutionProvenance.NonVerifiee();
        }

        return TryBuildQualificationProvenance(
            competition,
            source,
            path,
            occupant,
            matchesByStage,
            tracker);
    }

    private static ResolutionProvenance ResolveProgressionSlotProvenance(
        Stage destination,
        string slotKey,
        EntryId occupant,
        ProgressionFeedRef feed,
        IReadOnlyList<Stage> competitionStages,
        IReadOnlyDictionary<StageId, IReadOnlyList<Match>>? matchesByStage,
        Tracker tracker)
    {
        var source = ResolveCanonicalStage(feed.SourceStageId, competitionStages);
        var path = FindProgressionPath(source, feed.SourcePairKey, feed.Outcome);
        if (path is null
            || !string.Equals(path.Destination.SlotKey, slotKey, StringComparison.Ordinal))
        {
            return ResolutionProvenance.NonVerifiee();
        }

        return TryBuildProgressionProvenance(source, path, occupant, matchesByStage, tracker);
    }

    private static void BackfillCompositionEntries(
        Competition competition,
        Stage destination,
        IReadOnlyList<Stage> competitionStages,
        IReadOnlyDictionary<StageId, IReadOnlyList<Match>>? matchesByStage,
        Tracker tracker)
    {
        foreach (var row in destination.CompositionEntries.ToArray())
        {
            var provenance = ResolveCompositionProvenance(
                competition,
                destination,
                row.EntryId,
                competitionStages,
                matchesByStage,
                tracker);
            tracker.WriteComposition(destination, row.EntryId, provenance);
        }
    }

    private static ResolutionProvenance ResolveCompositionProvenance(
        Competition competition,
        Stage destination,
        EntryId entryId,
        IReadOnlyList<Stage> competitionStages,
        IReadOnlyDictionary<StageId, IReadOnlyList<Match>>? matchesByStage,
        Tracker tracker)
    {
        foreach (var source in competitionStages)
        {
            if (source.Regulation.QualificationRules is { } qualificationRules)
            {
                foreach (var path in qualificationRules.Paths)
                {
                    if (!path.Destination.StageId.Equals(destination.Id)
                        || !(path.Destination.TargetsForm || path.Destination.TargetsPopulation))
                    {
                        continue;
                    }

                    if (path.Destination.TargetsForm)
                    {
                        var fingerprint = FormPathResolutionKey.FromQualification(source.Id, path);
                        var witness = destination.FormPathResolutions
                            .FirstOrDefault(resolution => resolution.PathFingerprint == fingerprint);
                        if (witness is null || !witness.EntryId.Equals(entryId))
                        {
                            continue;
                        }
                    }
                    else if (path.Destination.TargetsPopulation
                             && !destination.CompositionEntries.Any(entry => entry.EntryId.Equals(entryId)))
                    {
                        continue;
                    }

                    var candidate = TryBuildQualificationProvenance(
                        competition,
                        source,
                        path,
                        entryId,
                        matchesByStage,
                        tracker);
                    if (candidate.Kind != ResolutionProvenanceKind.NonVerifiee)
                    {
                        return candidate;
                    }
                }
            }

            if (source.Regulation.ProgressionRules is not { } progressionRules)
            {
                continue;
            }

            foreach (var path in progressionRules.Paths)
            {
                if (!path.Destination.StageId.Equals(destination.Id)
                    || !(path.Destination.TargetsForm || path.Destination.TargetsPopulation))
                {
                    continue;
                }

                if (path.Destination.TargetsForm)
                {
                    var fingerprint = FormPathResolutionKey.FromProgression(source.Id, path);
                    var witness = destination.FormPathResolutions
                        .FirstOrDefault(resolution => resolution.PathFingerprint == fingerprint);
                    if (witness is null || !witness.EntryId.Equals(entryId))
                    {
                        continue;
                    }
                }

                var candidate = TryBuildProgressionProvenance(
                    source,
                    path,
                    entryId,
                    matchesByStage,
                    tracker);
                if (candidate.Kind != ResolutionProvenanceKind.NonVerifiee)
                {
                    return candidate;
                }
            }
        }

        return ResolutionProvenance.NonVerifiee();
    }

    private static void BackfillGroupMembers(
        Competition competition,
        Stage destination,
        IReadOnlyList<Stage> competitionStages,
        IReadOnlyDictionary<StageId, IReadOnlyList<Match>>? matchesByStage,
        Tracker tracker)
    {
        foreach (var group in destination.Groups.ToArray())
        {
            foreach (var entryId in group.EntryIds.ToArray())
            {
                var provenance = ResolveGroupProvenance(
                    competition,
                    destination,
                    group.Id,
                    entryId,
                    competitionStages,
                    matchesByStage,
                    tracker);
                tracker.WriteGroup(destination, group.Id, entryId, provenance);
            }
        }
    }

    private static ResolutionProvenance ResolveGroupProvenance(
        Competition competition,
        Stage destination,
        GroupId groupId,
        EntryId entryId,
        IReadOnlyList<Stage> competitionStages,
        IReadOnlyDictionary<StageId, IReadOnlyList<Match>>? matchesByStage,
        Tracker tracker)
    {
        foreach (var source in competitionStages)
        {
            if (source.Regulation.QualificationRules is { } qualificationRules)
            {
                foreach (var path in qualificationRules.Paths)
                {
                    if (!path.Destination.StageId.Equals(destination.Id)
                        || !path.Destination.TargetsGroup
                        || path.Destination.GroupId != groupId)
                    {
                        continue;
                    }

                    var candidate = TryBuildQualificationProvenance(
                        competition,
                        source,
                        path,
                        entryId,
                        matchesByStage,
                        tracker);
                    if (candidate.Kind != ResolutionProvenanceKind.NonVerifiee)
                    {
                        return candidate;
                    }
                }
            }

            if (source.Regulation.ProgressionRules is not { } progressionRules)
            {
                continue;
            }

            foreach (var path in progressionRules.Paths)
            {
                if (!path.Destination.StageId.Equals(destination.Id)
                    || !path.Destination.TargetsGroup
                    || path.Destination.GroupId != groupId)
                {
                    continue;
                }

                var candidate = TryBuildProgressionProvenance(
                    source,
                    path,
                    entryId,
                    matchesByStage,
                    tracker);
                if (candidate.Kind != ResolutionProvenanceKind.NonVerifiee)
                {
                    return candidate;
                }
            }
        }

        return ResolutionProvenance.NonVerifiee();
    }

    private static ResolutionProvenance TryBuildQualificationProvenance(
        Competition competition,
        Stage source,
        QualificationPath path,
        EntryId expectedOccupant,
        IReadOnlyDictionary<StageId, IReadOnlyList<Match>>? matchesByStage,
        Tracker tracker)
    {
        if (matchesByStage is null
            || !matchesByStage.TryGetValue(source.Id, out var matches))
        {
            return ResolutionProvenance.NonVerifiee();
        }

        Standing standing;
        try
        {
            var (overall, groups) = QualificationStandingFactory.Build(competition, source, matches);
            standing = ResolveQualificationStanding(source, path, overall, groups, matches);
        }
        catch (Exception ex) when (ex is ApplicationFailureException or DomainException)
        {
            return ResolutionProvenance.NonVerifiee();
        }

        var selection = PathOccupantSelector.SelectQualification(path, standing);
        if (selection.Status == PathOccupantSelectionStatus.Indeterminate)
        {
            tracker.RecordIndeterminate();
            return ResolutionProvenance.NonVerifiee();
        }

        if (selection.Status != PathOccupantSelectionStatus.Resolved
            || selection.EntryId is null
            || !selection.EntryId.Value.Equals(expectedOccupant))
        {
            return ResolutionProvenance.NonVerifiee();
        }

        return ResolutionProvenanceFactory.ForQualificationApply(
            source.Id,
            path,
            standing,
            selection.EntryId.Value);
    }

    private static ResolutionProvenance TryBuildProgressionProvenance(
        Stage source,
        ProgressionPath path,
        EntryId expectedOccupant,
        IReadOnlyDictionary<StageId, IReadOnlyList<Match>>? matchesByStage,
        Tracker tracker)
    {
        if (matchesByStage is null
            || !matchesByStage.TryGetValue(source.Id, out var matches)
            || !TryResolveFixtureOutcome(source, path, matches, out var outcome))
        {
            return ResolutionProvenance.NonVerifiee();
        }

        var selection = PathOccupantSelector.SelectProgression(path, outcome);
        if (selection.Status == PathOccupantSelectionStatus.Indeterminate)
        {
            tracker.RecordIndeterminate();
            return ResolutionProvenance.NonVerifiee();
        }

        if (selection.EntryId is null || !selection.EntryId.Value.Equals(expectedOccupant))
        {
            return ResolutionProvenance.NonVerifiee();
        }

        return ResolutionProvenanceFactory.ForProgressionApply(source.Id, path, outcome);
    }

    private static bool TryResolveFixtureOutcome(
        Stage source,
        ProgressionPath path,
        IReadOnlyList<Match> matches,
        out FixtureOutcome outcome)
    {
        outcome = null!;
        var fixture = source.FindFixtureByBracketPairKey(path.SourcePairKey);
        if (fixture is null)
        {
            return false;
        }

        var round = source.Rounds.FirstOrDefault(candidate =>
            candidate.Fixtures.Any(f => f.Id.Equals(fixture.Id)));
        if (round is null)
        {
            return false;
        }

        var tieFormat = TieFormat.OrDefaultOneLeg(round.TieFormat);
        var fixtureMatches = matches.Where(match => fixture.MatchIds.Contains(match.Id)).ToArray();
        if (fixtureMatches.Length != tieFormat.NumberOfLegs)
        {
            return false;
        }

        try
        {
            var snapshot = FixtureConfrontationSnapshotAssembler.Assemble(fixture, fixtureMatches);
            outcome = FixtureOutcomeResolver.Resolve(tieFormat, snapshot);
            return true;
        }
        catch (Exception ex) when (ex is ApplicationFailureException or DomainException)
        {
            return false;
        }
    }

    private static Standing ResolveQualificationStanding(
        Stage sourceStage,
        QualificationPath path,
        Standing? overallStanding,
        IReadOnlyDictionary<GroupId, Standing> groupStandings,
        IReadOnlyList<Match> matches)
    {
        if (path.Source.Scope == RankingScope.AcrossGroups)
        {
            var position = path.Source.AcrossGroupsPosition
                           ?? throw new ApplicationFailureException(
                               "Across-groups qualification path is missing a position.",
                               ApplicationErrorCodes.QualificationCandidatesEmpty);
            var rules = sourceStage.Regulation.StandingRules
                        ?? throw new ApplicationFailureException(
                            "Standing rules are required for across-groups qualification.",
                            StandingErrorCodes.RulesRequired);
            return CrossGroupStandingAssembler.Build(
                sourceStage.Groups,
                groupStandings,
                position,
                matches,
                rules,
                CalculateStanding.ToStandingPenalties(sourceStage.Penalties));
        }

        if (!IsGroupScoped(path))
        {
            return overallStanding
                   ?? throw new ApplicationFailureException(
                       "No overall standing was provided for an overall qualification path.",
                       ApplicationErrorCodes.QualificationStandingMissing);
        }

        var groupId = path.Source.GroupId
                      ?? throw new ApplicationFailureException(
                          "Group-scoped qualification path is missing a group identity.",
                          ApplicationErrorCodes.QualificationGroupNotFound);
        return groupStandings.TryGetValue(groupId, out var groupStanding)
            ? groupStanding
            : throw new ApplicationFailureException(
                $"No standing was provided for qualification group '{groupId}'.",
                ApplicationErrorCodes.QualificationStandingMissing);
    }

    private static QualificationPath? FindQualificationPath(Stage source, int pathOrder) =>
        source.Regulation.QualificationRules?.Paths.FirstOrDefault(path => path.Order == pathOrder);

    private static ProgressionPath? FindProgressionPath(
        Stage source,
        string sourcePairKey,
        ProgressionOutcome outcome) =>
        source.Regulation.ProgressionRules?.Paths.FirstOrDefault(path =>
            string.Equals(path.SourcePairKey, sourcePairKey, StringComparison.Ordinal)
            && path.Outcome == outcome);

    private static Stage ResolveCanonicalStage(StageId stageId, IReadOnlyList<Stage> competitionStages) =>
        competitionStages.First(candidate => candidate.Id.Equals(stageId));

    private static bool IsGroupScoped(QualificationPath path) =>
        path.Source.GroupId is not null || path.Source.Scope == RankingScope.Group;

    private sealed class Tracker
    {
        public int BackfilledExact { get; private set; }

        public int NonVerifiee { get; private set; }

        public int Indeterminate { get; private set; }

        public void RecordIndeterminate() => Indeterminate++;

        public void WriteSlot(Stage stage, string slotKey, ResolutionProvenance provenance)
        {
            Count(provenance);
            stage.WriteSlotResolutionProvenance(slotKey, provenance);
        }

        public void WriteComposition(Stage stage, EntryId entryId, ResolutionProvenance provenance)
        {
            Count(provenance);
            stage.WriteCompositionResolutionProvenance(entryId, provenance);
        }

        public void WriteGroup(
            Stage stage,
            GroupId groupId,
            EntryId entryId,
            ResolutionProvenance provenance)
        {
            Count(provenance);
            stage.WriteGroupResolutionProvenance(groupId, entryId, provenance);
        }

        private void Count(ResolutionProvenance provenance)
        {
            if (provenance.Kind == ResolutionProvenanceKind.NonVerifiee)
            {
                NonVerifiee++;
                return;
            }

            BackfilledExact++;
        }
    }
}
