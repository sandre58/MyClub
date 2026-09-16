// -----------------------------------------------------------------------
// <copyright file="NeedsAttentionAssembler.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Standings;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Progression;
using MyClub.PlayUp.Domain.Qualification;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Domain.Standings;

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Assembles Needs Attention items from competition state (derived; never persisted).
/// </summary>
/// <remarks>
/// Normal incomplete stages / Finished matches without pending consequences are not attentions.
/// Scheduled / Live / Finished matches are not attentions by themselves.
/// Draw Draft / Published / Resolved / Applied / Cancelled are normal states — only NoSolution is attention.
/// Schedule NoSolution is not persisted today — omitted until a durable diagnostic exists.
/// Completion blockers are not Needs Attention (see <see cref="CompletionAnalyzer"/>).
/// Construction participant deficit (<c>InsufficientParticipants</c>) is Needs Attention only in Draft/Ready —
/// never after Start (forfait reducing actives does not reopen this attention).
/// </remarks>
public static class NeedsAttentionAssembler
{
    /// <summary>Draw resolution NoSolution.</summary>
    public const string SourceDrawNoSolution = "DrawNoSolution";

    /// <summary>Qualification destination empty while path is resolvable.</summary>
    public const string SourceQualificationPending = "QualificationPending";

    /// <summary>Qualification destination occupied by a different entry than standing implies.</summary>
    public const string SourceQualificationConflict = "QualificationConflict";

    /// <summary>Progression destination empty while fixture outcome is known.</summary>
    public const string SourceProgressionPending = "ProgressionPending";

    /// <summary>Progression destination occupied by a different entry than outcome implies.</summary>
    public const string SourceProgressionConflict = "ProgressionConflict";

    /// <summary>
    /// Fewer Active entries than EntryRules.MinimumTeams during construction (Draft/Ready only).
    /// </summary>
    public const string SourceInsufficientParticipants = "InsufficientParticipants";

    /// <summary>Blocking severity.</summary>
    public const string SeverityBlocking = "Blocking";

    /// <summary>
    /// Builds Needs Attention for a competition.
    /// </summary>
    /// <param name="competition">Loaded competition.</param>
    /// <param name="stages">Competition stages (canonical).</param>
    /// <param name="matchesByStage">Full matches keyed by stage (Overview path).</param>
    /// <returns>Needs Attention DTO.</returns>
    public static NeedsAttentionDto Assemble(
        Competition competition,
        IReadOnlyList<Stage> stages,
        IReadOnlyDictionary<StageId, IReadOnlyList<Match>> matchesByStage) =>
        Assemble(competition, stages, MatchAttentionSlice.FromMatchesByStage(matchesByStage));

    /// <summary>
    /// Builds Needs Attention from projected match slices.
    /// </summary>
    /// <param name="competition">Loaded competition.</param>
    /// <param name="stages">Competition stages (canonical).</param>
    /// <param name="matchesByStage">Attention slices keyed by stage id.</param>
    /// <returns>Needs Attention DTO.</returns>
    public static NeedsAttentionDto Assemble(
        Competition competition,
        IReadOnlyList<Stage> stages,
        IReadOnlyDictionary<StageId, IReadOnlyList<MatchAttentionSlice>> matchesByStage)
    {
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(stages);
        ArgumentNullException.ThrowIfNull(matchesByStage);

        var items = new List<NeedsAttentionItemDto>();
        CollectInsufficientParticipants(competition, items);
        foreach (var stage in stages)
        {
            CollectDrawNoSolutions(stage, items);
            var matches = matchesByStage.TryGetValue(stage.Id, out var list) ? list : [];
            CollectQualificationAttentions(stage, stages, matches, items);
            CollectProgressionAttentions(stage, stages, matches, items);
        }

        return new NeedsAttentionDto(competition.Id.Value, items);
    }

    private static void CollectInsufficientParticipants(
        Competition competition,
        List<NeedsAttentionItemDto> items)
    {
        if (competition.Status is not (CompetitionStatus.Draft or CompetitionStatus.Ready))
        {
            return;
        }

        var activeCount = competition.Entries.Count(entry => entry.Status == EntryStatus.Active);
        var minimum = competition.Regulation.EntryRules.MinimumTeams;
        if (activeCount >= minimum)
        {
            return;
        }

        items.Add(
            new NeedsAttentionItemDto(
                SourceInsufficientParticipants,
                SeverityBlocking,
                "Competition",
                competition.Id.Value.ToString(),
                new Dictionary<string, string>
                {
                    ["activeCount"] = activeCount.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    ["minimumTeams"] = minimum.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    ["missingCount"] = (minimum - activeCount).ToString(
                        System.Globalization.CultureInfo.InvariantCulture)
                }));
    }

    private static void CollectDrawNoSolutions(Stage stage, List<NeedsAttentionItemDto> items) =>
        items.AddRange(
            from draw in stage.Draws
            where draw.Status != DrawStatus.Cancelled
            where draw.Resolution.State == DrawResolutionState.NoSolution
            select new NeedsAttentionItemDto(
                SourceDrawNoSolution,
                SeverityBlocking,
                "Draw",
                draw.Id.Value.ToString()));

    private static void CollectQualificationAttentions(
        Stage source,
        IReadOnlyList<Stage> stages,
        IReadOnlyList<MatchAttentionSlice> matches,
        List<NeedsAttentionItemDto> items)
    {
        var paths = source.Regulation.QualificationRules?.Paths;
        if (paths is null || paths.Count == 0)
        {
            return;
        }

        Standing? overall;
        Dictionary<GroupId, Standing> groups;
        try
        {
            overall = BuildOverall(source, matches);
            groups = BuildGroups(source, matches);
        }
        catch (Exception)
        {
            return;
        }

        foreach (var path in paths)
        {
            Standing standing;
            try
            {
                standing = ResolveStanding(source, path, overall, groups, matches);
            }
            catch (Exception)
            {
                continue;
            }

            SlotAssignmentInstruction? instruction;
            try
            {
                instruction = QualificationApplier.Apply(path, standing);
            }
            catch (Exception)
            {
                continue;
            }

            if (instruction is null)
            {
                continue;
            }

            var destination = stages.FirstOrDefault(stage => stage.Id.Equals(path.Destination.StageId));
            var slot = destination?.FindSlot(path.Destination.SlotKey);
            if (destination is null || slot is null)
            {
                items.Add(new NeedsAttentionItemDto(
                    SourceQualificationPending,
                    SeverityBlocking,
                    "Stage",
                    path.Destination.StageId.Value.ToString()));
                continue;
            }

            if (slot.EntryId is null)
            {
                items.Add(new NeedsAttentionItemDto(
                    SourceQualificationPending,
                    SeverityBlocking,
                    "Slot",
                    $"{destination.Id.Value}:{path.Destination.SlotKey}"));
            }
            else if (!slot.EntryId.Equals(instruction.EntryId))
            {
                items.Add(new NeedsAttentionItemDto(
                    SourceQualificationConflict,
                    SeverityBlocking,
                    "Slot",
                    $"{destination.Id.Value}:{path.Destination.SlotKey}"));
            }
        }
    }

    private static void CollectProgressionAttentions(
        Stage source,
        IReadOnlyList<Stage> stages,
        IReadOnlyList<MatchAttentionSlice> matches,
        List<NeedsAttentionItemDto> items)
    {
        var paths = source.Regulation.ProgressionRules?.Paths;
        if (paths is null || paths.Count == 0)
        {
            return;
        }

        foreach (var pathGroup in paths.GroupBy(path => path.SourceFixtureId))
        {
            var fixtureId = pathGroup.Key;
            Fixture fixture;
            try
            {
                fixture = source.GetFixture(fixtureId);
            }
            catch (Exception)
            {
                continue;
            }

            if (!AllLegsFinished(fixture, matches))
            {
                continue;
            }

            var round = source.Rounds.FirstOrDefault(candidate =>
                candidate.Fixtures.Any(fixtureCandidate => fixtureCandidate.Id.Equals(fixtureId)));
            if (round is null)
            {
                continue;
            }

            FixtureOutcome outcome;
            try
            {
                var snapshot = FixtureConfrontationSnapshotAssembler.Assemble(fixture, matches);
                outcome = FixtureOutcomeResolver.Resolve(
                    TieFormat.OrDefaultOneLeg(round.TieFormat),
                    snapshot);
            }
            catch (Exception)
            {
                continue;
            }

            foreach (var path in pathGroup)
            {
                ProgressionInstruction instruction;
                try
                {
                    instruction = ProgressionApplier.Apply(path, fixtureId, outcome);
                }
                catch (Exception)
                {
                    continue;
                }

                var destination = stages.FirstOrDefault(stage => stage.Id.Equals(instruction.StageId));
                if (destination is null)
                {
                    items.Add(new NeedsAttentionItemDto(
                        SourceProgressionPending,
                        SeverityBlocking,
                        "Fixture",
                        fixtureId.Value.ToString()));
                    continue;
                }

                if (instruction.TargetsPopulation)
                {
                    if (destination.CompositionEntries.All(entry => !entry.EntryId.Equals(instruction.EntryId)))
                    {
                        items.Add(new NeedsAttentionItemDto(
                            SourceProgressionPending,
                            SeverityBlocking,
                            "Fixture",
                            fixtureId.Value.ToString()));
                    }

                    continue;
                }

                var slot = destination.FindSlot(instruction.SlotKey!);
                if (slot is null)
                {
                    items.Add(new NeedsAttentionItemDto(
                        SourceProgressionPending,
                        SeverityBlocking,
                        "Fixture",
                        fixtureId.Value.ToString()));
                    continue;
                }

                if (slot.EntryId is null)
                {
                    items.Add(new NeedsAttentionItemDto(
                        SourceProgressionPending,
                        SeverityBlocking,
                        "Slot",
                        $"{destination.Id.Value}:{instruction.SlotKey}"));
                }
                else if (!slot.EntryId.Equals(instruction.EntryId))
                {
                    items.Add(new NeedsAttentionItemDto(
                        SourceProgressionConflict,
                        SeverityBlocking,
                        "Slot",
                        $"{destination.Id.Value}:{instruction.SlotKey}"));
                }
            }
        }
    }

    private static bool AllLegsFinished(Fixture fixture, IReadOnlyList<MatchAttentionSlice> matches)
    {
        if (fixture.Attachments.Count == 0)
        {
            return false;
        }

        var byId = matches.ToDictionary(match => match.Id);
        foreach (var attachment in fixture.Attachments)
        {
            if (!byId.TryGetValue(attachment.MatchId, out var match)
                || match.Status != MatchStatus.Finished
                || match.Result is null)
            {
                return false;
            }
        }

        return true;
    }

    private static Standing BuildOverall(Stage source, IReadOnlyList<MatchAttentionSlice> matches)
    {
        var participants = matches
            .Where(match => match is { Status: MatchStatus.Finished, Result: not null })
            .SelectMany(match => new[] { match.HomeEntryId, match.AwayEntryId })
            .Distinct()
            .ToArray();
        return CalculateStanding.Execute(
            participants,
            matches,
            RequireStandingRules(source),
            MatchFilter.All,
            CalculateStanding.ToStandingPenalties(source.Penalties));
    }

    private static Dictionary<GroupId, Standing> BuildGroups(Stage source, IReadOnlyList<MatchAttentionSlice> matches)
    {
        var result = new Dictionary<GroupId, Standing>();
        var rules = RequireStandingRules(source);
        foreach (var group in source.Groups)
        {
            result[group.Id] = CalculateStanding.Execute(
                group.EntryIds,
                matches,
                rules,
                MatchFilter.All,
                CalculateStanding.ToStandingPenalties(source.Penalties));
        }

        return result;
    }

    private static StandingRules RequireStandingRules(Stage source) =>
        source.Regulation.StandingRules
        ?? throw new ApplicationFailureException(
            "Standing rules are required for qualification attention evaluation.",
            StandingErrorCodes.RulesRequired);

    private static Standing ResolveStanding(
        Stage sourceStage,
        QualificationPath path,
        Standing? overallStanding,
        Dictionary<GroupId, Standing> groupStandings,
        IReadOnlyList<MatchAttentionSlice> matches)
    {
        if (path.Source.Scope == RankingScope.AcrossGroups)
        {
            var position = path.Source.AcrossGroupsPosition
                ?? throw new InvalidOperationException("AcrossGroups position missing.");
            return CrossGroupStandingAssembler.BuildFromAttentionSlices(
                sourceStage.Groups,
                groupStandings,
                position,
                matches,
                RequireStandingRules(sourceStage),
                CalculateStanding.ToStandingPenalties(sourceStage.Penalties));
        }

        if (path.Source.GroupId is null && path.Source.Scope != RankingScope.Group)
            return overallStanding ?? throw new InvalidOperationException("Overall standing missing.");
        var groupId = path.Source.GroupId
                      ?? throw new InvalidOperationException("Group id missing.");
        return groupStandings[groupId];
    }
}
