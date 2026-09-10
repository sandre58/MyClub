// -----------------------------------------------------------------------
// <copyright file="StructureViewAssembler.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Globalization;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Assembles <see cref="StructureViewDto"/> from Competition + loaded stages.
/// </summary>
public static class StructureViewAssembler
{
    /// <summary>
    /// Display-name order for entries and declared members (Équipes SoT: nom affiché).
    /// </summary>
    private static readonly StringComparer DisplayNameComparer =
        StringComparer.Create(CultureInfo.CurrentCulture, ignoreCase: true);

    /// <summary>Blocker: fewer Active entries than EntryRules.MinimumTeams.</summary>
    public const string BlockerInsufficientParticipants = "InsufficientParticipants";

    /// <summary>Blocker: no primary stage.</summary>
    public const string BlockerMissingStage = "MissingStage";

    /// <summary>Blocker: primary stage has no structure.</summary>
    public const string BlockerMissingStructure = "MissingStructure";

    /// <summary>Blocker: Groups format without PotRules.</summary>
    public const string BlockerMissingPotRules = "MissingPotRules";

    /// <summary>Blocker: Cup slots not a power of two (V1).</summary>
    public const string BlockerCupBracketInvalid = "CupBracketInvalid";

    /// <summary>Blocker: Qualif/Prog graph has dangling destinations (Draft persistable; Ready/Prepare blocked).</summary>
    public const string BlockerStructureGraphInvalid = "StructureGraphInvalid";

    /// <summary>Action: add participant.</summary>
    public const string ActionAddEntry = "AddEntry";

    /// <summary>Action: configure structure.</summary>
    public const string ActionConfigureStructure = "ConfigureStructure";

    /// <summary>Per-phase action: explicit skeleton rebuild (destructive).</summary>
    public const string ActionRebuildStructure = "RebuildStructure";

    /// <summary>Per-phase action: rename stage (locale).</summary>
    public const string ActionRenameStage = "RenameStage";

    /// <summary>Per-phase action: add matchday (locale).</summary>
    public const string ActionAddMatchday = "AddMatchday";

    /// <summary>Per-phase action: add group (locale).</summary>
    public const string ActionAddGroup = "AddGroup";

    /// <summary>Per-phase action: add round (locale).</summary>
    public const string ActionAddRound = "AddRound";

    /// <summary>Per-phase action: add slot (locale).</summary>
    public const string ActionAddSlot = "AddSlot";

    /// <summary>Per-phase action: set match generation format (locale).</summary>
    public const string ActionReplaceMatchGenerationFormat = "ReplaceMatchGenerationFormat";

    /// <summary>Per-phase action: set Swiss planned rounds (locale).</summary>
    public const string ActionReplaceSwissSettings = "ReplaceSwissSettings";

    /// <summary>Action: replace regulation.</summary>
    public const string ActionReplaceRegulation = "ReplaceRegulation";

    /// <summary>Action: add an additional competition stage (thin authoring).</summary>
    public const string ActionAddCompetitionStage = "AddCompetitionStage";

    /// <summary>Per-phase action: remove this stage (with peer dependency scrubbing).</summary>
    public const string ActionRemoveStage = "RemoveStage";

    /// <summary>Per-phase action: replace qualification rules.</summary>
    public const string ActionReplaceQualificationRules = "ReplaceQualificationRules";

    /// <summary>Per-phase action: replace progression rules.</summary>
    public const string ActionReplaceProgressionRules = "ReplaceProgressionRules";

    /// <summary>Specialize MatchRules (unbind changed heritable parts).</summary>
    public const string ActionReplaceMatchRules = "ReplaceMatchRules";

    /// <summary>Specialize StandingRules (Draft/Ready Structure authoring).</summary>
    public const string ActionReplaceStandingRules = "ReplaceStandingRules";

    /// <summary>Rebind Match or Standing bundle to Competition defaults.</summary>
    public const string ActionBindToCompetition = "BindToCompetition";

    /// <summary>Replace or clear DrawRules.</summary>
    public const string ActionReplaceDrawRules = "ReplaceDrawRules";

    /// <summary>Replace or clear stage default TieFormat.</summary>
    public const string ActionReplaceDefaultTieFormat = "ReplaceDefaultTieFormat";

    /// <summary>Stage structure issue: qualification destination stage missing from competition.</summary>
    public const string IssueDanglingQualificationTarget = "DanglingQualificationTarget";

    /// <summary>Stage structure issue: progression destination stage missing from competition.</summary>
    public const string IssueDanglingProgressionTarget = "DanglingProgressionTarget";

    /// <summary>Stage structure issue: qualification destination slot missing on target stage.</summary>
    public const string IssueMissingQualificationDestinationSlot = "MissingQualificationDestinationSlot";

    /// <summary>Stage structure issue: progression destination slot missing on target stage.</summary>
    public const string IssueMissingProgressionDestinationSlot = "MissingProgressionDestinationSlot";

    /// <summary>
    /// Builds the Structure view.
    /// </summary>
    /// <param name="competition">Loaded competition.</param>
    /// <param name="stages">Stages loaded for <see cref="Competition.StageIds"/> (same order).</param>
    /// <param name="sheetMemberRefs">
    /// Optional sheet member references. When provided, declared members get
    /// <see cref="DeclaredMemberDto.ReferencedOnMatchSheet"/> without loading full matches.
    /// </param>
    /// <returns>Structure view DTO.</returns>
    public static StructureViewDto Assemble(
        Competition competition,
        IReadOnlyList<Stage> stages,
        IReadOnlyList<MatchSheetMemberRef>? sheetMemberRefs = null)
    {
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(stages);

        var primary = ResolvePrimaryStage(competition, stages);
        var format = BuildFormatSummary(primary);
        var structure = BuildStructureSummary(primary);
        var participants = BuildParticipants(competition, sheetMemberRefs ?? []);
        var regulation = BuildRegulation(competition);
        var stageHubs = BuildStageHubSummaries(competition, stages);
        var attachedMatchCount = CountAttachedMatches(primary);
        var readiness = BuildReadiness(
            competition,
            primary,
            format.Kind,
            structure,
            attachedMatchCount,
            stageHubs);
        var actions = BuildActions(competition);

        return new StructureViewDto(
            competition.Id.Value,
            competition.Name.Value,
            competition.Status,
            participants,
            format,
            regulation,
            structure,
            actions,
            readiness,
            stageHubs,
            competition.ShortName?.Value,
            competition.LogoMediaId?.Value,
            competition.ScheduledStart,
            competition.ScheduledEnd);
    }

    private static List<StructureStageHubSummaryDto> BuildStageHubSummaries(
        Competition competition,
        IReadOnlyList<Stage> stages)
    {
        var byId = stages.ToDictionary(stage => stage.Id);
        var ordered = new List<StructureStageHubSummaryDto>(competition.StageIds.Count);
        foreach (var stageId in competition.StageIds)
        {
            if (!byId.TryGetValue(stageId, out var stage))
            {
                continue;
            }

            ordered.Add(BuildStageHubSummary(competition, stage, stages));
        }

        return ordered;
    }

    private static StructureStageHubSummaryDto BuildStageHubSummary(
        Competition competition,
        Stage stage,
        IReadOnlyList<Stage> competitionStages)
    {
        var regulation = stage.Regulation;
        var match = regulation.MatchRules;
        var standing = regulation.StandingRules;
        var draw = regulation.DrawRules;
        var qualificationPaths = MapQualificationPaths(regulation.QualificationRules);
        var progressionPaths = MapProgressionPaths(regulation.ProgressionRules);
        var placement = regulation.PlacementAwardRules;
        var storedTie = regulation.TieFormat
            ?? stage.Rounds.Select(round => round.TieFormat).FirstOrDefault(tie => tie is not null);
        var hasTie = storedTie is not null;
        var confrontationSegments = hasTie && stage.Rounds.Count > 0
            ? BuildConfrontationSegments(stage)
            : null;
        int? numberOfLegs;
        bool? aggregateScoring;
        bool hasAwayGoalsRule;
        bool hasTieExtraTime;
        bool hasTiePenaltyShootout;
        if (confrontationSegments is { Count: > 0 })
        {
            var first = confrontationSegments[0];
            numberOfLegs = first.NumberOfLegs;
            aggregateScoring = first.AggregateScoring;
            hasAwayGoalsRule = first.HasAwayGoalsRule;
            hasTieExtraTime = first.HasTieExtraTime;
            hasTiePenaltyShootout = first.HasTiePenaltyShootout;
        }
        else if (hasTie)
        {
            var tie = TieFormat.OrDefaultOneLeg(storedTie);
            numberOfLegs = tie.NumberOfLegs;
            aggregateScoring = tie.AggregateScoring;
            hasAwayGoalsRule = tie.AwayGoalsRule is not null;
            hasTieExtraTime = tie.ExtraTimeRule is not null;
            hasTiePenaltyShootout = tie.PenaltyShootoutRule is not null;
        }
        else
        {
            numberOfLegs = null;
            aggregateScoring = null;
            hasAwayGoalsRule = false;
            hasTieExtraTime = false;
            hasTiePenaltyShootout = false;
        }

        IReadOnlyList<StructurePlacementAwardDto>? placementAwards = placement?.Paths
            .OrderBy(path => path.Rank)
            .Select(path => new StructurePlacementAwardDto(path.Rank, path.Outcome))
            .ToArray();
        IReadOnlyList<StructureDrawConstraintDto>? drawConstraints = draw?.Constraints
            .Select(constraint => new StructureDrawConstraintDto(
                constraint.ConstraintType,
                constraint.Enforcement,
                constraint.MaxPerGroup))
            .ToArray();

        return new StructureStageHubSummaryDto(
            stage.Id.Value,
            stage.Name.Value,
            stage.Status,
            CountStageTeams(competition, stage),
            CountAttachedMatches(stage),
            stage.Groups.Count,
            stage.Rounds.Count,
            match.Duration.NumberOfPeriods,
            match.Duration.DurationPerPeriod,
            HasExtraTime: match.ExtraTimePolicy is not null,
            ExtraTimeNumberOfPeriods: match.ExtraTimePolicy?.NumberOfPeriods,
            ExtraTimeDurationPerPeriod: match.ExtraTimePolicy?.DurationPerPeriod,
            HasPenaltyShootout: match.PenaltyShootoutPolicy is not null,
            PenaltyInitialKicksPerTeam: match.PenaltyShootoutPolicy?.InitialKicksPerTeam,
            HasStandingRules: standing is not null,
            standing?.Points.WinPoints,
            standing?.Points.DrawPoints,
            standing?.Points.LossPoints,
            HasDrawRules: draw is not null,
            DrawMode: draw?.Mode,
            NumberOfPots: draw?.PotRules?.NumberOfPots,
            HasQualificationRules: regulation.QualificationRules is not null,
            QualificationPathCount: qualificationPaths?.Count ?? 0,
            HasProgressionRules: regulation.ProgressionRules is not null,
            ProgressionPathCount: progressionPaths?.Count ?? 0,
            HasTieFormat: hasTie,
            NumberOfLegs: numberOfLegs,
            AggregateScoring: aggregateScoring,
            HasAwayGoalsRule: hasAwayGoalsRule,
            HasTieExtraTime: hasTieExtraTime,
            HasTiePenaltyShootout: hasTiePenaltyShootout,
            RankingCriteria: standing?.RankingCriteria,
            HasPlacementAwardRules: placement is not null,
            PlacementAwardCount: placement?.Paths.Count ?? 0,
            PlacementAwards: placementAwards,
            FormatKind: InferFormat(stage),
            SwissRoundCount: stage.SwissSettings?.RoundCount,
            ForfeitWinnerGoals: match.AdministrativeResultPolicy.ForfeitWinnerGoals,
            ForfeitLoserGoals: match.AdministrativeResultPolicy.ForfeitLoserGoals,
            NumberOfSeeds: draw?.SeedingRules?.NumberOfSeeds,
            DrawConstraints: drawConstraints,
            DefaultsBinding: MapDefaultsBinding(stage),
            ConfrontationSegments: confrontationSegments,
            Actions: BuildStageActions(competition, stage),
            QualificationPaths: qualificationPaths,
            ProgressionPaths: progressionPaths,
            StructureIssues: BuildStructureIssues(stage, competitionStages),
            HalfTimeDuration: match.Duration.HalfTimeDuration);
    }

    private static IReadOnlyList<StructureQualificationPathDto>? MapQualificationPaths(
        QualificationRules? rules)
    {
        if (rules is null)
        {
            return null;
        }

        return
        [
            .. rules.Paths.Select(path => new StructureQualificationPathDto(
                path.Order,
                path.Selection.Mode,
                path.Selection.Value,
                path.Destination.StageId.Value,
                path.Destination.SlotKey,
                path.Source.Scope,
                path.Source.GroupId?.Value,
                path.Source.AcrossGroupsPosition,
                path.Selection.EndValue,
                path.Condition?.MinimumPoints))
        ];
    }

    private static IReadOnlyList<StructureProgressionPathDto>? MapProgressionPaths(
        ProgressionRules? rules)
    {
        if (rules is null)
        {
            return null;
        }

        return
        [
            .. rules.Paths.Select(path => new StructureProgressionPathDto(
                path.SourceFixtureId.Value,
                path.Outcome,
                path.Destination.StageId.Value,
                path.Destination.SlotKey))
        ];
    }

    private static IReadOnlyList<string> BuildStageActions(Competition competition, Stage stage)
    {
        if (competition.Status is CompetitionStatus.Completed
            or CompetitionStatus.Archived
            or CompetitionStatus.Running
            or CompetitionStatus.Suspended)
        {
            return [];
        }

        if (stage.Status is StageStatus.Running or StageStatus.Suspended or StageStatus.Completed)
        {
            return [];
        }

        var actions = new List<string>
        {
            ActionRenameStage,
            ActionReplaceQualificationRules,
            ActionReplaceProgressionRules,
            ActionReplaceMatchRules,
            ActionBindToCompetition
        };

        if (stage.Regulation.StandingRules is not null
            && !StageClassification.IsNonClassifyingPhase(stage))
        {
            actions.Add(ActionReplaceStandingRules);
        }

        if (StageNeedsDrawRulesAction(stage))
        {
            actions.Add(ActionReplaceDrawRules);
        }

        if (StageNeedsTieFormatAction(stage))
        {
            actions.Add(ActionReplaceDefaultTieFormat);
        }

        var format = InferFormat(stage);
        var attachedMatches = CountAttachedMatches(stage);

        if (attachedMatches == 0)
        {
            actions.Add(ActionRebuildStructure);
        }

        if (format is StructureFormatKind.Championship or StructureFormatKind.Groups or null)
        {
            actions.Add(ActionAddMatchday);
        }

        if (format is StructureFormatKind.Groups or null)
        {
            actions.Add(ActionAddGroup);
        }

        if (format is StructureFormatKind.Cup or null)
        {
            actions.Add(ActionAddRound);
            actions.Add(ActionAddSlot);
        }

        if (format is StructureFormatKind.Championship or StructureFormatKind.Groups)
        {
            actions.Add(ActionReplaceMatchGenerationFormat);
        }

        if (format is StructureFormatKind.Swiss)
        {
            actions.Add(ActionReplaceSwissSettings);
        }

        if (competition.StageIds.Count > 1 && attachedMatches == 0)
        {
            actions.Add(ActionRemoveStage);
        }

        return actions;
    }

    private static bool StageNeedsDrawRulesAction(Stage stage)
    {
        if (stage.Regulation.DrawRules is not null)
        {
            return true;
        }

        var format = InferFormat(stage);
        return format is StructureFormatKind.Groups or StructureFormatKind.Cup;
    }

    private static bool StageNeedsTieFormatAction(Stage stage)
    {
        if (stage.Regulation.TieFormat is not null || stage.Rounds.Any(round => round.TieFormat is not null))
        {
            return true;
        }

        return InferFormat(stage) is StructureFormatKind.Cup;
    }

    private static IReadOnlyList<string> BuildStructureIssues(
        Stage stage,
        IReadOnlyList<Stage> competitionStages)
    {
        var issues = new List<string>();
        var byId = competitionStages.ToDictionary(candidate => candidate.Id);

        if (stage.Regulation.QualificationRules is { } qualification)
        {
            foreach (var path in qualification.Paths)
            {
                if (path.Destination.StageId.Equals(stage.Id))
                {
                    if (stage.FindSlot(path.Destination.SlotKey) is null)
                    {
                        issues.Add(IssueMissingQualificationDestinationSlot);
                    }

                    continue;
                }

                if (!byId.TryGetValue(path.Destination.StageId, out var destination))
                {
                    issues.Add(IssueDanglingQualificationTarget);
                    continue;
                }

                if (destination.FindSlot(path.Destination.SlotKey) is null)
                {
                    issues.Add(IssueMissingQualificationDestinationSlot);
                }
            }
        }

        if (stage.Regulation.ProgressionRules is { } progression)
        {
            foreach (var path in progression.Paths)
            {
                if (path.Destination.StageId.Equals(stage.Id))
                {
                    if (stage.FindSlot(path.Destination.SlotKey) is null)
                    {
                        issues.Add(IssueMissingProgressionDestinationSlot);
                    }

                    continue;
                }

                if (!byId.TryGetValue(path.Destination.StageId, out var destination))
                {
                    issues.Add(IssueDanglingProgressionTarget);
                    continue;
                }

                if (destination.FindSlot(path.Destination.SlotKey) is null)
                {
                    issues.Add(IssueMissingProgressionDestinationSlot);
                }
            }
        }

        return issues.Distinct(StringComparer.Ordinal).ToArray();
    }

    /// <summary>
    /// Groups consecutive rounds that share the same effective TieFormat signature.
    /// </summary>
    private static List<StructureConfrontationSegmentDto> BuildConfrontationSegments(Stage stage)
    {
        var segments = new List<StructureConfrontationSegmentDto>();
        StructureConfrontationSegmentDto? current = null;

        for (var index = 0; index < stage.Rounds.Count; index++)
        {
            var round = stage.Rounds[index];
            var tie = TieFormat.OrDefaultOneLeg(round.TieFormat);
            var roundRef = new StructureConfrontationRoundRefDto(round.Id.Value, round.Name, index);
            if (current is not null && SameTieSignature(current, tie))
            {
                current = current with
                {
                    Rounds = [.. current.Rounds, roundRef]
                };
                segments[^1] = current;
                continue;
            }

            current = new StructureConfrontationSegmentDto(
                [roundRef],
                tie.NumberOfLegs,
                tie.AggregateScoring,
                HasAwayGoalsRule: tie.AwayGoalsRule is not null,
                HasTieExtraTime: tie.ExtraTimeRule is not null,
                HasTiePenaltyShootout: tie.PenaltyShootoutRule is not null);
            segments.Add(current);
        }

        return segments;
    }

    private static bool SameTieSignature(StructureConfrontationSegmentDto segment, TieFormat tie) =>
        segment.NumberOfLegs == tie.NumberOfLegs
        && segment.AggregateScoring == tie.AggregateScoring
        && segment.HasAwayGoalsRule == (tie.AwayGoalsRule is not null)
        && segment.HasTieExtraTime == (tie.ExtraTimeRule is not null)
        && segment.HasTiePenaltyShootout == (tie.PenaltyShootoutRule is not null);

    private static StructureStageDefaultsBindingDto MapDefaultsBinding(Stage stage)
    {
        var binding = stage.DefaultsBinding;
        var classifying = stage.Regulation.StandingRules is not null;
        var points = classifying
            ? new StructureHeritablePartBindingDto(binding.IsBound(HeritableRegulationPart.Points))
            : null;
        var rankingCriteria = classifying
            ? new StructureHeritablePartBindingDto(binding.IsBound(HeritableRegulationPart.RankingCriteria))
            : null;
        return new StructureStageDefaultsBindingDto(
            new StructureHeritablePartBindingDto(binding.IsBound(HeritableRegulationPart.MatchDuration)),
            new StructureHeritablePartBindingDto(binding.IsBound(HeritableRegulationPart.ExtraTime)),
            new StructureHeritablePartBindingDto(binding.IsBound(HeritableRegulationPart.PenaltyShootout)),
            new StructureHeritablePartBindingDto(binding.IsBound(HeritableRegulationPart.AdministrativeResult)),
            points,
            rankingCriteria);
    }

    /// <summary>
    /// Topology team count: distinct group occupants, else slot capacity, else competition occupying.
    /// </summary>
    private static int CountStageTeams(Competition competition, Stage stage) =>
        stage.Groups.Count > 0
            ? stage.Groups.SelectMany(group => group.EntryIds).Distinct().Count()
            : stage.Slots.Count > 0 ? stage.Slots.Count : competition.Entries.Count;

    private static int CountAttachedMatches(Stage? primary) =>
        primary?.Matchdays.SelectMany(matchday => matchday.Fixtures)
                .Concat(primary.Rounds.SelectMany(round => round.Fixtures))
                .SelectMany(fixture => fixture.MatchIds)
                .Distinct()
                .Count() ?? 0;

    private static Stage? ResolvePrimaryStage(Competition competition, IReadOnlyList<Stage> stages)
    {
        if (competition.StageIds.Count == 0)
        {
            return null;
        }

        var primaryId = competition.StageIds[0];
        return stages.FirstOrDefault(stage => stage.Id.Equals(primaryId));
    }

    private static StructureParticipantsSummaryDto BuildParticipants(
        Competition competition,
        IReadOnlyList<MatchSheetMemberRef> sheetMemberRefs)
    {
        var sheetReferenced = BuildSheetReferencedMemberIds(sheetMemberRefs);
        var entries = competition.Entries
            .OrderBy(entry => entry.DisplayName, DisplayNameComparer)
            .Select(entry => new StructureEntryDto(
                entry.Id.Value,
                entry.DisplayName,
                entry.Status,
                entry.ShortName?.Value,
                entry.LogoMediaId?.Value,
                entry.PrimaryColor?.Value,
                entry.SecondaryColor?.Value,
                [
                    .. entry.DeclaredMembers
                        .OrderBy(member => member.DisplayName, DisplayNameComparer)
                        .Select(member => new DeclaredMemberDto(
                            member.Id.Value,
                            member.DisplayName,
                            member.Role,
                            sheetReferenced.Contains((entry.Id, member.Id))))
                ]))
            .ToList();
        var active = competition.Entries.Count(entry => entry.Status == EntryStatus.Active);
        var occupying = competition.Entries.Count;
        return new StructureParticipantsSummaryDto(active, occupying, entries);
    }

    /// <summary>
    /// Members still listed on a match composition sheet for their entry side
    /// (same gate as <c>RemoveDeclaredMember</c> for that entry + member).
    /// </summary>
    private static HashSet<(EntryId EntryId, MemberId MemberId)> BuildSheetReferencedMemberIds(
        IReadOnlyList<MatchSheetMemberRef> sheetMemberRefs)
    {
        var referenced = new HashSet<(EntryId, MemberId)>(sheetMemberRefs.Count);
        foreach (var sheetRef in sheetMemberRefs)
        {
            referenced.Add((sheetRef.EntryId, sheetRef.MemberId));
        }

        return referenced;
    }

    private static StructureRegulationSummaryDto BuildRegulation(Competition competition)
    {
        var regulation = competition.Regulation;
        var match = regulation.MatchRules;
        var extra = match.ExtraTimePolicy;
        return new StructureRegulationSummaryDto(
            regulation.EntryRules.MinimumTeams,
            regulation.EntryRules.MaximumTeams,
            match.Duration.DurationPerPeriod,
            match.Duration.NumberOfPeriods,
            regulation.StandingRules.Points.WinPoints,
            regulation.StandingRules.Points.DrawPoints,
            regulation.StandingRules.Points.LossPoints,
            regulation.DisciplinaryRules.AllowedTypes,
            match.Duration.HalfTimeDuration,
            HasExtraTime: extra is not null,
            ExtraTimeDurationPerPeriod: extra?.DurationPerPeriod,
            ExtraTimeNumberOfPeriods: extra?.NumberOfPeriods,
            HasPenaltyShootout: match.PenaltyShootoutPolicy is not null,
            PenaltyInitialKicksPerTeam: match.PenaltyShootoutPolicy?.InitialKicksPerTeam,
            RankingCriteria: regulation.StandingRules.RankingCriteria,
            ForfeitWinnerGoals: match.AdministrativeResultPolicy.ForfeitWinnerGoals,
            ForfeitLoserGoals: match.AdministrativeResultPolicy.ForfeitLoserGoals);
    }

    private static StructureFormatSummaryDto BuildFormatSummary(Stage? primary) =>
        primary is null
            ? new StructureFormatSummaryDto(null, null, null, null)
            : new StructureFormatSummaryDto(
                InferFormat(primary),
                primary.Id.Value,
                primary.Name.Value,
                primary.Status);

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

    private static StructureTopologySummaryDto BuildStructureSummary(Stage? primary)
    {
        if (primary is null)
        {
            return new StructureTopologySummaryDto(
                0,
                0,
                0,
                0,
                false,
                null,
                MatchGenerationFormat.SingleRoundRobin);
        }

        var drawRules = primary.Regulation.DrawRules;
        return new StructureTopologySummaryDto(
            primary.Groups.Count,
            primary.Rounds.Count,
            primary.Matchdays.Count,
            primary.Slots.Count,
            drawRules is not null,
            drawRules?.PotRules?.NumberOfPots,
            primary.MatchGenerationFormat,
            primary.SwissSettings?.RoundCount);
    }

    private static StructureReadinessDto BuildReadiness(
        Competition competition,
        Stage? primary,
        StructureFormatKind? formatKind,
        StructureTopologySummaryDto structure,
        int attachedMatchCount,
        IReadOnlyList<StructureStageHubSummaryDto> stageHubs)
    {
        var blockers = new List<string>();

        var activeCount = competition.Entries.Count(entry => entry.Status == EntryStatus.Active);
        var minimum = competition.Regulation.EntryRules.MinimumTeams;
        if (activeCount < minimum)
        {
            blockers.Add(BlockerInsufficientParticipants);
        }

        if (primary is null)
        {
            blockers.Add(BlockerMissingStage);
        }
        else if (formatKind is null)
        {
            blockers.Add(BlockerMissingStructure);
        }

        if (stageHubs.Any(hub => hub.StructureIssues is { Count: > 0 }))
        {
            blockers.Add(BlockerStructureGraphInvalid);
        }

        var readyForDraw = false;
        var readyForSchedulePath = false;
        var readyForMaterialization = false;

        if (primary is not null && formatKind is not null
            && !blockers.Contains(BlockerInsufficientParticipants)
            && !blockers.Contains(BlockerMissingStage)
            && !blockers.Contains(BlockerMissingStructure))
        {
            switch (formatKind)
            {
                case StructureFormatKind.Championship:
                    readyForSchedulePath = structure.MatchdayCount > 0;
                    readyForMaterialization = activeCount >= 2 && structure.MatchdayCount > 0;
                    break;
                case StructureFormatKind.Groups:
                    if (structure.NumberOfPots is null)
                    {
                        blockers.Add(BlockerMissingPotRules);
                    }
                    else
                    {
                        readyForDraw = structure is { GroupCount: >= 2, MatchdayCount: >= 1 };
                        var assigned = primary.Groups.Sum(group => group.EntryIds.Count);
                        readyForMaterialization = assigned >= 2 && primary.Groups.All(group => group.EntryIds.Count >= 2);
                    }

                    break;
                case StructureFormatKind.Cup:
                    if (structure.SlotCount < 2 || !IsPowerOfTwo(structure.SlotCount))
                    {
                        blockers.Add(BlockerCupBracketInvalid);
                    }
                    else
                    {
                        // Draw path = structure (rounds + valid bracket).
                        // MaterializeMatches = empty Fixture skeleton for Pairing — distinct from
                        // Overview from-slots (occupied SlotA/B on a later stage).
                        readyForDraw = structure.RoundCount >= 1;
                        var expectedSkeletonFixtures = structure.SlotCount / 2;
                        var skeletonFixtures = primary.Rounds.Count > 0
                            ? primary.Rounds[0].Fixtures.Count
                            : 0;
                        readyForMaterialization = readyForDraw && skeletonFixtures < expectedSkeletonFixtures;
                    }

                    break;
                case StructureFormatKind.Swiss:
                    // Matchdays come from GenerateNextRound — not MaterializeMatches.
                    readyForSchedulePath = structure.SwissRoundCount is >= 1;
                    readyForMaterialization = false;
                    break;
                case null:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(formatKind), formatKind, null);
            }
        }

        var readyForSchedule = attachedMatchCount > 0;
        var readyForMatchOperation = attachedMatchCount > 0
            && competition.Status is not CompetitionStatus.Completed
            and not CompetitionStatus.Archived;
        var constructionBlocked = blockers.Contains(BlockerInsufficientParticipants)
            || blockers.Contains(BlockerMissingStage)
            || blockers.Contains(BlockerMissingStructure)
            || blockers.Contains(BlockerMissingPotRules)
            || blockers.Contains(BlockerCupBracketInvalid)
            || blockers.Contains(BlockerStructureGraphInvalid);
        var readyForNext = !constructionBlocked && (readyForDraw || readyForSchedulePath);

        return new StructureReadinessDto(
            readyForNext,
            readyForDraw,
            readyForMaterialization,
            readyForSchedule,
            readyForMatchOperation,
            readyForSchedulePath,
            attachedMatchCount,
            blockers);
    }

    private static IReadOnlyList<string> BuildActions(Competition competition) =>
        competition.Status switch
        {
            CompetitionStatus.Completed or CompetitionStatus.Archived => [],
            CompetitionStatus.Running or CompetitionStatus.Suspended => ["WithdrawEntry", "RenameEntry"],
            _ =>
            [
                ActionAddEntry,
                ActionConfigureStructure,
                ActionAddCompetitionStage,
                ActionReplaceRegulation,
                "RenameEntry",
                "DeleteEntry"
            ]
        };

    private static bool IsPowerOfTwo(int value) => value > 0 && (value & (value - 1)) == 0;
}
