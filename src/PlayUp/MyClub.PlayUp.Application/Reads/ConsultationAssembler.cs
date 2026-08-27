// -----------------------------------------------------------------------
// <copyright file="ConsultationAssembler.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Standings;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Domain.Standings;

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Assembles the Consultation product destination (Results / Standings / Structure).
/// </summary>
/// <remarks>
/// Derived only — never persists Standing or mutates aggregates.
/// Standing uses <see cref="CalculateStanding"/> / Domain <see cref="StandingCalculator"/>.
/// </remarks>
public static class ConsultationAssembler
{
    /// <summary>Standings not applicable: cup / knockout format.</summary>
    public const string NotApplicableCupFormat = "CupFormat";

    /// <summary>Standings not applicable: no sporting structure yet.</summary>
    public const string NotApplicableNoStructure = "NoStructure";

    /// <summary>Standing table scope: overall ranking.</summary>
    public const string ScopeOverall = "Overall";

    /// <summary>Standing table scope: group ranking.</summary>
    public const string ScopeGroup = "Group";

    /// <summary>
    /// Builds Consultation from loaded competition graph.
    /// </summary>
    /// <param name="competition">Loaded competition.</param>
    /// <param name="stages">Competition stages (canonical order preferred).</param>
    /// <param name="matchesByStage">Matches keyed by stage.</param>
    /// <returns>Consultation view DTO.</returns>
    public static ConsultationViewDto Assemble(
        Competition competition,
        IReadOnlyList<Stage> stages,
        IReadOnlyDictionary<StageId, IReadOnlyList<Match>> matchesByStage)
    {
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(stages);
        ArgumentNullException.ThrowIfNull(matchesByStage);

        var primary = ResolvePrimaryStage(competition, stages);
        var formatKind = primary is null ? null : InferFormat(primary);
        var formatLabel = FormatLabel(formatKind, primary);
        var names = EntryDisplayNames.ToMap(competition);
        var entries = EntryDisplayNames.ToEntries(competition);

        var results = AssembleResults(competition, stages, matchesByStage);
        var standings = AssembleStandings(competition, stages, matchesByStage, names, formatKind);
        var structure = AssembleStructure(stages, entries, formatKind);

        return new ConsultationViewDto(
            competition.Id.Value,
            competition.Name.Value,
            competition.Status,
            competition.CompletionMode,
            formatKind,
            formatLabel,
            results,
            standings,
            structure);
    }

    /// <summary>
    /// Projects standings tables using the same CalculateStanding path as Consultation.
    /// Reused by Cockpit compact standing — does not invent a second ranking algorithm.
    /// </summary>
    public static ConsultationStandingsSectionDto ProjectStandings(
        Competition competition,
        IReadOnlyList<Stage> stages,
        IReadOnlyDictionary<StageId, IReadOnlyList<Match>> matchesByStage)
    {
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(stages);
        ArgumentNullException.ThrowIfNull(matchesByStage);

        var primary = ResolvePrimaryStage(competition, stages);
        var formatKind = primary is null ? null : InferFormat(primary);
        var names = EntryDisplayNames.ToMap(competition);
        return AssembleStandings(competition, stages, matchesByStage, names, formatKind);
    }

    /// <summary>
    /// Projects standing tables for a single stage (Championship / Groups / Swiss).
    /// Cup or unstructured stages return not-applicable — used by Cockpit ReferenceStage standing.
    /// </summary>
    public static ConsultationStandingsSectionDto ProjectStandingsForStage(
        Competition competition,
        Stage stage,
        IReadOnlyList<Match> matches)
    {
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(matches);

        var format = InferFormat(stage);
        if (format is StructureFormatKind.Cup)
        {
            return new ConsultationStandingsSectionDto(false, NotApplicableCupFormat, []);
        }

        if (format is not (StructureFormatKind.Championship or StructureFormatKind.Groups or StructureFormatKind.Swiss))
        {
            return new ConsultationStandingsSectionDto(false, NotApplicableNoStructure, []);
        }

        var names = EntryDisplayNames.ToMap(competition);
        var penalties = CalculateStanding.ToStandingPenalties(stage.Penalties);
        var rules = stage.Regulation.StandingRules;
        var tables = new List<ConsultationStandingTableDto>();

        if (format is StructureFormatKind.Championship or StructureFormatKind.Swiss)
        {
            var participants = ResolveOverallParticipants(competition, matches);
            if (participants.Length == 0)
            {
                return new ConsultationStandingsSectionDto(false, NotApplicableNoStructure, []);
            }

            var standing = CalculateStanding.Execute(participants, matches, rules, MatchFilter.All, penalties);
            tables.Add(new ConsultationStandingTableDto(
                ScopeOverall,
                stage.Id.Value,
                stage.Name.Value,
                GroupId: null,
                GroupName: null,
                MapRows(standing, names)));
        }
        else
        {
            tables.AddRange(from @group in stage.Groups
                where @group.EntryIds.Count > 0
                let standing = CalculateStanding.Execute(@group.EntryIds, matches, rules, MatchFilter.All, penalties)
                select new ConsultationStandingTableDto(
                    ScopeGroup,
                    stage.Id.Value,
                    stage.Name.Value,
                    @group.Id.Value,
                    @group.Name,
                    MapRows(standing, names)));
        }

        return tables.Count == 0
            ? new ConsultationStandingsSectionDto(false, NotApplicableNoStructure, [])
            : new ConsultationStandingsSectionDto(true, null, tables);
    }

    private static Stage? ResolvePrimaryStage(Competition competition, IReadOnlyList<Stage> stages)
    {
        if (competition.StageIds.Count == 0)
        {
            return null;
        }

        var primaryId = competition.StageIds[0];
        return stages.FirstOrDefault(stage => stage.Id.Equals(primaryId));
    }

    private static StructureFormatKind? InferFormat(Stage stage) =>
        stage.IsSwiss
            ? StructureFormatKind.Swiss
            : stage.Rounds.Count > 0
            ? StructureFormatKind.Cup
            : stage.Groups.Count > 0
            ? StructureFormatKind.Groups
            : stage.Matchdays.Count > 0
            ? StructureFormatKind.Championship
            : null;

    private static string FormatLabel(StructureFormatKind? kind, Stage? primary) =>
        kind switch
        {
            StructureFormatKind.Championship => "Championnat",
            StructureFormatKind.Groups => "Groupes",
            StructureFormatKind.Cup => "Coupe",
            StructureFormatKind.Swiss => "Swiss",
            null when primary is null => "Non configuré",
            _ => "Structure partielle"
        };

    private static List<ConsultationResultDto> AssembleResults(
        Competition competition,
        IReadOnlyList<Stage> stages,
        IReadOnlyDictionary<StageId, IReadOnlyList<Match>> matchesByStage)
    {
        var results = new List<ConsultationResultDto>();
        foreach (var stage in stages)
        {
            var matches = matchesByStage.TryGetValue(stage.Id, out var list) ? list : [];
            var summaries = MatchReadAssembler.AssembleSummaries(stage, competition, matches);

            foreach (var summary in summaries)
            {
                if (summary.Status is not (MatchStatus.Finished or MatchStatus.Cancelled))
                {
                    continue;
                }

                results.Add(new ConsultationResultDto(
                    summary.MatchId,
                    summary.StageId,
                    summary.FixtureId,
                    summary.RoundId,
                    summary.MatchdayNumber,
                    MatchReadAssembler.ConsultationContextLabel(summary),
                    summary.Status,
                    summary.Home,
                    summary.Away,
                    summary.Score,
                    summary.ResultType,
                    summary.ScheduledAt));
            }
        }

        return results;
    }

    private static ConsultationStandingsSectionDto AssembleStandings(
        Competition competition,
        IReadOnlyList<Stage> stages,
        IReadOnlyDictionary<StageId, IReadOnlyList<Match>> matchesByStage,
        IReadOnlyDictionary<EntryId, string> names,
        StructureFormatKind? primaryFormat)
    {
        switch (primaryFormat)
        {
            case StructureFormatKind.Cup:
                return new ConsultationStandingsSectionDto(false, NotApplicableCupFormat, []);
            case StructureFormatKind.Swiss:
                // Ranking uses StandingRules on Matchdays; tables are assembled like Championship.
                break;
            case StructureFormatKind.Championship:
            case StructureFormatKind.Groups:
                break;
            case null when stages.All(stage => InferFormat(stage) is null or StructureFormatKind.Cup):
                {
                    var onlyCup = stages.Count > 0 &&
                                  stages.All(stage => InferFormat(stage) == StructureFormatKind.Cup);
                    return new ConsultationStandingsSectionDto(
                        false,
                        onlyCup ? NotApplicableCupFormat : NotApplicableNoStructure,
                        []);
                }

            default:
                throw new ArgumentOutOfRangeException(nameof(primaryFormat), primaryFormat, null);
        }

        var tables = new List<ConsultationStandingTableDto>();
        foreach (var stage in stages)
        {
            var format = InferFormat(stage);
            if (format is not (StructureFormatKind.Championship or StructureFormatKind.Groups or StructureFormatKind.Swiss))
            {
                continue;
            }

            var matches = matchesByStage.TryGetValue(stage.Id, out var list) ? list : [];
            var penalties = CalculateStanding.ToStandingPenalties(stage.Penalties);
            var rules = stage.Regulation.StandingRules;

            if (format is StructureFormatKind.Championship or StructureFormatKind.Swiss)
            {
                var participants = ResolveOverallParticipants(competition, matches);
                var standing = CalculateStanding.Execute(participants, matches, rules, MatchFilter.All, penalties);
                tables.Add(new ConsultationStandingTableDto(
                    ScopeOverall,
                    stage.Id.Value,
                    stage.Name.Value,
                    GroupId: null,
                    GroupName: null,
                    MapRows(standing, names)));
            }
            else
            {
                tables.AddRange(from @group in stage.Groups
                    where @group.EntryIds.Count > 0
                    let standing = CalculateStanding.Execute(@group.EntryIds, matches, rules, MatchFilter.All, penalties)
                    select new ConsultationStandingTableDto(ScopeGroup, stage.Id.Value, stage.Name.Value, @group.Id.Value, @group.Name, MapRows(standing, names)));
            }
        }

        return tables.Count == 0
            ? new ConsultationStandingsSectionDto(false, NotApplicableNoStructure, [])
            : new ConsultationStandingsSectionDto(true, null, tables);
    }

    private static EntryId[] ResolveOverallParticipants(
        Competition competition,
        IReadOnlyList<Match> matches)
    {
        var active = competition.Entries
            .Where(entry => entry.Status == EntryStatus.Active)
            .Select(entry => entry.Id)
            .ToArray();
        return active.Length > 0
            ? active
            : [
            .. matches
                .Where(match => match is { Status: MatchStatus.Finished, Result: not null })
                .SelectMany(match => new[] { match.HomeEntryId, match.AwayEntryId })
                .Distinct()
        ];
    }

    private static IReadOnlyList<ConsultationStandingRowDto> MapRows(
        Standing standing,
        IReadOnlyDictionary<EntryId, string> names) =>
    [
        .. standing.Rows.Select(row => new ConsultationStandingRowDto(
            row.Position,
            row.EntryId.Value,
            EntryDisplayNames.Resolve(names, row.EntryId) ?? row.EntryId.Value.ToString(),
            row.Played,
            row.Wins,
            row.Draws,
            row.Losses,
            row.GoalsFor,
            row.GoalsAgainst,
            row.GoalDifference,
            row.Points))
    ];

    private static ConsultationStructureDto AssembleStructure(
        IReadOnlyList<Stage> stages,
        IReadOnlyDictionary<EntryId, CompetitionEntry> entries,
        StructureFormatKind? primaryFormat)
    {
        var stageDtos = stages.Select(stage =>
        {
            var format = InferFormat(stage);
            var groups = stage.Groups
                .Select(group => new ConsultationGroupStructureDto(
                    group.Id.Value,
                    group.Name,
                    [
                        .. group.EntryIds.Select(entryId => EntryDisplayNames.ToSide(entries, entryId))
                    ]))
                .ToArray();

            var matchdays = stage.Matchdays
                .Select(matchday => new ConsultationMatchdayStructureDto(
                    matchday.Id.Value,
                    matchday.Number,
                    [.. matchday.Fixtures.Select(MapFixture)]))
                .ToArray();

            var rounds = stage.Rounds
                .Select(round => new ConsultationRoundStructureDto(
                    round.Id.Value,
                    round.Name,
                    [.. round.Fixtures.Select(MapFixture)]))
                .ToArray();

            var slots = stage.Slots
                .Select(slot => new ConsultationSlotStructureDto(
                    slot.SlotKey,
                    slot.EntryId?.Value,
                    slot.EntryId is { } entryId ? EntryDisplayNames.ToSide(entries, entryId).DisplayName : null))
                .ToArray();

            return new ConsultationStageStructureDto(
                stage.Id.Value,
                stage.Name.Value,
                stage.Status,
                format,
                stage.MatchGenerationFormat,
                groups,
                matchdays,
                rounds,
                slots);
        }).ToArray();

        return new ConsultationStructureDto(primaryFormat, stageDtos);
    }

    private static ConsultationFixtureStructureDto MapFixture(Fixture fixture) =>
        new(
            fixture.Id.Value,
            fixture.SlotAKey,
            fixture.SlotBKey,
            [
                .. fixture.Attachments
                    .OrderBy(attachment => attachment.LegIndex)
                    .Select(attachment => new ConsultationFixtureMatchRefDto(
                        attachment.MatchId.Value,
                        attachment.LegIndex))
            ]);
}
