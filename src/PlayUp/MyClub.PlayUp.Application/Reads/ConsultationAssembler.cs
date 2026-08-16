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

        var results = AssembleResults(competition, stages, matchesByStage);
        var standings = AssembleStandings(competition, stages, matchesByStage, names, formatKind);
        var structure = AssembleStructure(stages, names, formatKind);

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
        stage.Rounds.Count > 0
            ? StructureFormatKind.Cup
            : stage.Groups.Count > 0 ? StructureFormatKind.Groups : stage.Matchdays.Count > 0 ? StructureFormatKind.Championship : null;

    private static string FormatLabel(StructureFormatKind? kind, Stage? primary) =>
        kind switch
        {
            StructureFormatKind.Championship => "Championnat",
            StructureFormatKind.Groups => "Groupes",
            StructureFormatKind.Cup => "Coupe",
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
            var matchdayByFixture = BuildMatchdayIndex(stage);
            var roundNameById = stage.Rounds.ToDictionary(round => round.Id, round => round.Name);

            foreach (var summary in summaries)
            {
                if (summary.Status is not (MatchStatus.Finished or MatchStatus.Cancelled))
                {
                    continue;
                }

                var match = matches.First(candidate => candidate.Id.Value == summary.MatchId);
                int? matchdayNumber = null;
                string? contextLabel = null;
                if (summary.FixtureId is { } fixtureGuid)
                {
                    var fixtureId = new FixtureId(fixtureGuid);
                    if (matchdayByFixture.TryGetValue(fixtureId, out var number))
                    {
                        matchdayNumber = number;
                        contextLabel = $"Journée {number}";
                    }
                    else if (summary.RoundId is { } roundGuid
                             && roundNameById.TryGetValue(new RoundId(roundGuid), out var roundName))
                    {
                        contextLabel = roundName;
                    }
                }

                results.Add(new ConsultationResultDto(
                    summary.MatchId,
                    summary.StageId,
                    summary.FixtureId,
                    summary.RoundId,
                    matchdayNumber,
                    contextLabel,
                    summary.Status,
                    summary.Home,
                    summary.Away,
                    summary.Score,
                    match.Result?.Type,
                    summary.ScheduledAt));
            }
        }

        return results;
    }

    private static Dictionary<FixtureId, int> BuildMatchdayIndex(Stage stage)
    {
        var index = new Dictionary<FixtureId, int>();
        foreach (var matchday in stage.Matchdays)
        {
            foreach (var fixture in matchday.Fixtures)
            {
                index[fixture.Id] = matchday.Number;
            }
        }

        return index;
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
            if (format is not (StructureFormatKind.Championship or StructureFormatKind.Groups))
            {
                continue;
            }

            var matches = matchesByStage.TryGetValue(stage.Id, out var list) ? list : [];
            var penalties = CalculateStanding.ToStandingPenalties(stage.Penalties);
            var rules = stage.Regulation.StandingRules;

            if (format == StructureFormatKind.Championship)
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
                tables.AddRange(from @group in stage.Groups let standing = CalculateStanding.Execute(@group.EntryIds, matches, rules, MatchFilter.All, penalties) select new ConsultationStandingTableDto(ScopeGroup, stage.Id.Value, stage.Name.Value, @group.Id.Value, @group.Name, MapRows(standing, names)));
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
        IReadOnlyDictionary<EntryId, string> names,
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
                        .. group.EntryIds.Select(entryId => new EntrySideDto(
                            entryId.Value,
                            EntryDisplayNames.Resolve(names, entryId)))
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
                    slot.EntryId is { } entryId ? EntryDisplayNames.Resolve(names, entryId) : null))
                .ToArray();

            return new ConsultationStageStructureDto(
                stage.Id.Value,
                stage.Name.Value,
                stage.Status,
                format,
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
