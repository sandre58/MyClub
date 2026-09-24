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

    /// <summary>Per-phase action: assign entry to Cup slot (Placement manuel).</summary>
    public const string ActionAssignEntryToSlot = "AssignEntryToSlot";

    /// <summary>Per-phase action: clear Cup DirectAssignment.</summary>
    public const string ActionClearSlotAssignment = "ClearSlotAssignment";

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

    /// <summary>Replace root composition entry set (Affectation).</summary>
    public const string ActionReplaceAffectationAuthoring = "ReplaceAffectationAuthoring";

    /// <summary>Replace or clear stage default TieFormat.</summary>
    public const string ActionReplaceDefaultTieFormat = "ReplaceDefaultTieFormat";

    /// <summary>Replace or clear a Round TieFormat.</summary>
    public const string ActionReplaceRoundTieFormat = "ReplaceRoundTieFormat";

    /// <summary>Replace or clear PlacementAwardRules (final competition ranks).</summary>
    public const string ActionReplacePlacementAwardRules = "ReplacePlacementAwardRules";

    /// <summary>Stage structure issue: qualification destination stage missing from competition.</summary>
    public const string IssueDanglingQualificationTarget = "DanglingQualificationTarget";

    /// <summary>Stage structure issue: qualification Place destination slot missing on target stage.</summary>
    public const string IssueMissingQualificationDestinationSlot = "MissingQualificationDestinationSlot";

    /// <summary>Stage structure issue: qualification Place destination group missing on target stage.</summary>
    public const string IssueMissingQualificationDestinationGroup = "MissingQualificationDestinationGroup";

    /// <summary>Stage structure issue: progression destination stage missing from competition.</summary>
    public const string IssueDanglingProgressionTarget = "DanglingProgressionTarget";

    /// <summary>Stage structure issue: progression destination slot missing on target stage.</summary>
    public const string IssueMissingProgressionDestinationSlot = "MissingProgressionDestinationSlot";

    /// <summary>Stage structure issue: progression destination group missing on target stage.</summary>
    public const string IssueMissingProgressionDestinationGroup = "MissingProgressionDestinationGroup";

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
        var qualificationPaths = MapQualificationPaths(stage, regulation.QualificationRules);
        var qualificationIntents = MapQualificationIntents(stage, regulation.QualificationRules);
        var progressionPaths = MapProgressionPaths(stage, regulation.ProgressionRules);
        var progressionIntents = MapProgressionIntents(stage, regulation.ProgressionRules);
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
            .Select(path =>
            {
                var fixture = stage.FindFixtureByBracketPairKey(path.SourcePairKey);
                return new StructurePlacementAwardDto(
                    path.Rank,
                    path.Outcome,
                    path.SourcePairKey,
                    ResolveProgressionSourceLabel(stage, path.SourcePairKey),
                    fixture?.Id.Value);
            })
            .ToArray();
        IReadOnlyList<StructureDrawConstraintDto>? drawConstraints = draw?.Constraints
            .Select(constraint => new StructureDrawConstraintDto(
                constraint.ConstraintType,
                constraint.Enforcement,
                constraint.MaxPerGroup))
            .ToArray();

        var isRootComposition = IsRootCompositionStage(stage, competitionStages);
        var composition = BuildEntrySetProjection(competition, stage.CompositionEntries);
        var affectation = BuildEntrySetProjection(competition, stage.AffectationAuthoring);
        var compositionCapacity = ResolvePlaces(competition, stage);

        return new StructureStageHubSummaryDto(
            stage.Id.Value,
            stage.Name.Value,
            stage.Status,
            CountStageTeams(competition, stage),
            CountAttachedMatches(stage),
            stage.Groups.Count,
            stage.Rounds.Count,
            stage.Matchdays.Count,
            stage.Slots.Count,
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
            QualificationIntents: qualificationIntents,
            ProgressionPaths: progressionPaths,
            ProgressionIntents: progressionIntents,
            StructureIssues: BuildStructureIssues(stage, competitionStages),
            HalfTimeDuration: match.Duration.HalfTimeDuration,
            DirectAssignmentCount: stage.DirectAssignments.Count,
            CompositionEntryCount: composition.Count,
            CompositionEntryIds: composition.EntryIds,
            CompositionCapacity: compositionCapacity,
            CompositionPreviewNames: composition.PreviewNames,
            CompositionPreviewOverflow: composition.PreviewOverflow,
            CompositionIneligibleCount: composition.IneligibleCount,
            AffectationEntryCount: affectation.Count,
            AffectationEntryIds: affectation.EntryIds,
            AffectationPreviewNames: affectation.PreviewNames,
            AffectationIneligibleCount: affectation.IneligibleCount,
            IsRootComposition: isRootComposition,
            PlacesPerGroup: stage.PlacesPerGroup,
            DefaultTieFormat: MapDefaultTieFormat(regulation.TieFormat),
            DrawExecutionBadge: ResolveDrawExecutionBadge(stage, draw is not null));
    }

    /// <summary>
    /// Topology signal only: DrawRules engaged → four execution states (not pots / « configuré »).
    /// </summary>
    private static StructureDrawExecutionBadge? ResolveDrawExecutionBadge(
        Stage stage,
        bool hasDrawRules)
    {
        if (!hasDrawRules)
        {
            return null;
        }

        Draw? active = null;
        for (var i = stage.Draws.Count - 1; i >= 0; i--)
        {
            var candidate = stage.Draws[i];
            if (candidate.Status == DrawStatus.Cancelled) continue;
            active = candidate;
            break;
        }

        return active is null
            ? StructureDrawExecutionBadge.ToLaunch
            : active.Status == DrawStatus.Draft
            ? StructureDrawExecutionBadge.InProgress
            : active.Status == DrawStatus.Published
            ? DrawAppliedState.IsApplied(active, stage)
                ? StructureDrawExecutionBadge.Applied
                : StructureDrawExecutionBadge.ToApply
            : StructureDrawExecutionBadge.ToLaunch;
    }

    private static StructureTieFormatSummaryDto? MapDefaultTieFormat(TieFormat? tie) =>
        tie is null
            ? null
            : new StructureTieFormatSummaryDto(
                tie.NumberOfLegs,
                tie.AggregateScoring,
                HasAwayGoalsRule: tie.AwayGoalsRule is not null,
                HasTieExtraTime: tie.ExtraTimeRule is not null,
                HasTiePenaltyShootout: tie.PenaltyShootoutRule is not null);

    private readonly record struct EntrySetProjection(
        int Count,
        IReadOnlyList<Guid> EntryIds,
        IReadOnlyList<string> PreviewNames,
        int PreviewOverflow,
        int IneligibleCount);

    private static EntrySetProjection BuildEntrySetProjection(
        Competition competition,
        IReadOnlyList<CompositionEntry> entries)
    {
        var entriesById = competition.Entries.ToDictionary(entry => entry.Id);
        var entryIds = entries.Select(entry => entry.EntryId.Value).ToArray();
        var orderedNames = entries
            .Select(entry => entry.EntryId)
            .Select(entriesById.GetValueOrDefault)
            .Where(entry => entry is not null)
            .Cast<CompetitionEntry>()
            .OrderBy(entry => entry.DisplayName, DisplayNameComparer)
            .Select(entry => entry.DisplayName)
            .ToList();

        var count = entries.Count;
        var preview = orderedNames.ToArray();
        var ineligible = entries.Count(compositionEntry =>
            !entriesById.TryGetValue(compositionEntry.EntryId, out var entry)
            || entry.Status != EntryStatus.Active);

        return new EntrySetProjection(count, entryIds, preview, PreviewOverflow: 0, ineligible);
    }

    /// <summary>
    /// Places N — target cardinality at T (form capacity, or Active for Championship/Swiss).
    /// Never derived from Draw or composition set k. Null = indeterminable (E4), not zero.
    /// DTO name compositionCapacity retained temporarily = target Places, not current k.
    /// Cup: entry places (1st-round cardinality), not total <c>slotCount</c> when multi-round.
    /// </summary>
    private static int? ResolvePlaces(Competition competition, Stage stage) =>
        InferFormat(stage) switch
        {
            StructureFormatKind.Cup => ResolveCupEntryPlaces(stage),
            StructureFormatKind.Championship or StructureFormatKind.Swiss
                => CountActiveEntries(competition),
            StructureFormatKind.Groups => ResolveGroupsPlaces(stage),
            _ => stage.Slots.Count > 0 ? stage.Slots.Count : null
        };

    /// <summary>
    /// Cup Places N = teams to constitute (entry places). Mono-round: <c>slotCount</c>.
    /// Multi-round classic KO: 1st-round size (e.g. 14 units → 8 places), never total slots.
    /// </summary>
    private static int? ResolveCupEntryPlaces(Stage stage)
    {
        var slotCount = stage.Slots.Count;
        if (slotCount == 0)
        {
            return null;
        }

        var roundCount = stage.Rounds.Count;
        if (roundCount <= 1)
        {
            return slotCount;
        }

        // Full tree for R rounds: slots = 2^(R+1) − 2 ⇒ entry = 2^R.
        var fullTreeSlots = (1 << (roundCount + 1)) - 2;
        if (slotCount == fullTreeSlots)
        {
            return 1 << roundCount;
        }

        // First round only materialized: slots = 2^R.
        var firstRoundSlots = 1 << roundCount;
        if (slotCount == firstRoundSlots)
        {
            return firstRoundSlots;
        }

        // Classic full tree independent of declared round count: slots = 2N − 2, N power of two.
        if (slotCount < 2 || (slotCount + 2) % 2 != 0) return null;
        var entryPlaces = (slotCount + 2) / 2;
        return IsPowerOfTwo(entryPlaces) && entryPlaces >= 2 ? entryPlaces : null;
    }

    private static int CountActiveEntries(Competition competition) =>
        competition.Entries.Count(entry => entry.Status == EntryStatus.Active);

    /// <summary>
    /// Groups N = groupCount × placesPerGroup (form fact). Legacy bridge: PotRules only if PlacesPerGroup unset.
    /// </summary>
    private static int? ResolveGroupsPlaces(Stage stage)
    {
        if (stage.Groups.Count == 0)
        {
            return null;
        }

        var perGroup = stage.PlacesPerGroup
                       ?? stage.Regulation.DrawRules?.PotRules?.NumberOfPots;
        return perGroup is null or < 1 ? null : stage.Groups.Count * perGroup.Value;
    }

    private static bool IsRootCompositionStage(Stage stage, IReadOnlyList<Stage> competitionStages)
    {
        foreach (var other in competitionStages)
        {
            if (other.Id.Equals(stage.Id))
            {
                continue;
            }

            var qualification = other.Regulation.QualificationRules;
            if (qualification?.Paths.Any(path => path.Destination.StageId.Equals(stage.Id)) == true)
            {
                return false;
            }

            var progression = other.Regulation.ProgressionRules;
            if (progression?.Paths.Any(path => path.Destination.StageId.Equals(stage.Id)) == true)
            {
                return false;
            }
        }

        return true;
    }

    private static IReadOnlyList<StructureQualificationIntentDto>? MapQualificationIntents(
        Stage stage,
        QualificationRules? rules) =>
        rules is null || rules.Intents.Count == 0
            ? null
            :
            [
                .. rules.Intents.Select(intent =>
                {
                    string? groupName = null;
                    if (intent.GroupId is { } groupId)
                    {
                        groupName = stage.Groups.FirstOrDefault(g => g.Id.Equals(groupId))?.Name;
                    }

                    var destinationCount = CountIntentDestinations(intent, stage.Groups.Count);
                    return new StructureQualificationIntentDto(
                        intent.Id.Value,
                        intent.Order,
                        intent.SourceKind,
                        intent.PositionFrom,
                        intent.PositionTo,
                        intent.DestinationStageId.Value,
                        intent.GroupId?.Value,
                        groupName,
                        intent.AcrossGroupsPosition,
                        intent.Condition?.MinimumPoints,
                        destinationCount,
                        intent.DestinationSlotKeys,
                        intent.DestinationGroupIds.Count == 0 ? null : intent.DestinationGroupIds.Select(g => g.Value).ToArray(),
                        intent.DestinationForm);
                })
            ];

    private static int CountIntentDestinations(QualificationIntent intent, int groupCount)
    {
        var span = intent.PositionTo - intent.PositionFrom + 1;
        return intent.SourceKind switch
        {
            QualificationIntentSourceKind.EachGroup => Math.Max(groupCount, 0) * span,
            _ => span
        };
    }

    private static IReadOnlyList<StructureQualificationPathDto>? MapQualificationPaths(
        Stage stage,
        QualificationRules? rules) =>
        rules is null
            ? null
            : [
                .. rules.Paths.Select(path =>
                {
                    string? groupName = null;
                    if (path.Source.GroupId is { } groupId)
                    {
                        groupName = stage.Groups.FirstOrDefault(g => g.Id.Equals(groupId))?.Name;
                    }

                    return new StructureQualificationPathDto(
                        path.Order,
                        path.Selection.Mode,
                        path.Selection.Value,
                        path.Destination.StageId.Value,
                        path.Source.Scope,
                        path.Source.GroupId?.Value,
                        path.Source.AcrossGroupsPosition,
                        path.Selection.EndValue,
                        path.Condition?.MinimumPoints,
                        groupName,
                        path.Destination.SlotKey,
                        path.Destination.GroupId?.Value,
                        path.Destination.TargetsForm);
                })
            ];

    private static IReadOnlyList<StructureProgressionPathDto>? MapProgressionPaths(
        Stage stage,
        ProgressionRules? rules) =>
        rules is null
            ? null
            :
            [
                .. rules.Paths.Select(path => new StructureProgressionPathDto(
                    path.SourcePairKey,
                    path.Outcome,
                    path.Destination.StageId.Value,
                    path.Destination.SlotKey,
                    ResolveProgressionSourceLabel(stage, path.SourcePairKey),
                    path.Destination.GroupId?.Value,
                    path.Destination.TargetsForm))
            ];

    private static IReadOnlyList<StructureProgressionIntentDto>? MapProgressionIntents(
        Stage stage,
        ProgressionRules? rules)
    {
        if (rules is null || rules.Intents.Count == 0)
        {
            return null;
        }

        var roundNameById = stage.Rounds.ToDictionary(r => r.Id, r => r.Name);
        var expandCount = stage.BracketPairs.Count > 0
            ? stage.BracketPairs.Count
            : 0;
        return
        [
            .. rules.Intents.Select(intent =>
            {
                var count = expandCount > 0
                    ? expandCount
                    : stage.Rounds
                        .FirstOrDefault(r => r.Id.Equals(intent.RoundId))
                        ?.Fixtures.Count ?? 0;
                return new StructureProgressionIntentDto(
                    intent.Id.Value,
                    intent.Order,
                    intent.RoundId.Value,
                    roundNameById.GetValueOrDefault(intent.RoundId),
                    intent.Outcome,
                    intent.DestinationStageId.Value,
                    intent.DestinationSlotKeys,
                    count,
                    intent.DestinationGroupIds.Count == 0 ? null : intent.DestinationGroupIds.Select(g => g.Value).ToArray(),
                    intent.DestinationForm);
            })
        ];
    }

    /// <summary>
    /// Human source label: PairKey, or bound fixture label when materialized.
    /// </summary>
    private static string ResolveProgressionSourceLabel(Stage stage, string sourcePairKey)
    {
        var fixture = stage.FindFixtureByBracketPairKey(sourcePairKey);
        return fixture is not null
            ? ResolveFixtureSourceLabel(stage, fixture.Id) ?? sourcePairKey
            : sourcePairKey;
    }

    /// <summary>
    /// Human fixture label: round/matchday · #order, optionally · slotA vs slotB when keys exist.
    /// Match #order is stable (fixtures ordered by id) so labels match StageSchematic.
    /// </summary>
    private static string? ResolveFixtureSourceLabel(Stage stage, FixtureId fixtureId)
    {
        foreach (var round in stage.Rounds)
        {
            var fixtures = round.Fixtures.OrderBy(fixture => fixture.Id.Value).ToArray();
            for (var i = 0; i < fixtures.Length; i++)
            {
                var fixture = fixtures[i];
                if (!fixture.Id.Equals(fixtureId))
                {
                    continue;
                }

                return FormatFixtureSourceLabel(round.Name, i + 1, fixture);
            }
        }

        foreach (var matchday in stage.Matchdays)
        {
            var fixtures = matchday.Fixtures.OrderBy(fixture => fixture.Id.Value).ToArray();
            for (var i = 0; i < fixtures.Length; i++)
            {
                var fixture = fixtures[i];
                if (!fixture.Id.Equals(fixtureId))
                {
                    continue;
                }

                return FormatFixtureSourceLabel(
                    $"J{matchday.Number.ToString(CultureInfo.InvariantCulture)}",
                    i + 1,
                    fixture);
            }
        }

        return null;
    }

    private static string FormatFixtureSourceLabel(string containerName, int order, Fixture fixture)
    {
        var a = fixture.SlotAKey?.Trim();
        var b = fixture.SlotBKey?.Trim();
        var hasSlots = !string.IsNullOrEmpty(a) || !string.IsNullOrEmpty(b);
        if (!hasSlots)
        {
            return $"{containerName} · #{order.ToString(CultureInfo.InvariantCulture)}";
        }

        var left = string.IsNullOrEmpty(a) ? "—" : a;
        var right = string.IsNullOrEmpty(b) ? "—" : b;
        return $"{containerName} · #{order.ToString(CultureInfo.InvariantCulture)} · {left} vs {right}";
    }

    private static List<string> BuildStageActions(
        Competition competition,
        Stage stage)
    {
        if (competition.Status is CompetitionStatus.Completed
                or CompetitionStatus.Archived
                or CompetitionStatus.Running
                or CompetitionStatus.Suspended ||
            stage.Status is StageStatus.Running or StageStatus.Suspended or StageStatus.Completed)
        {
            return [];
        }

        var actions = new List<string>
        {
            ActionRenameStage, ActionReplaceMatchRules, ActionBindToCompetition
        };

        // V1 exit capacity: classifying → Qualification ; Cup/KO non-classifying → Progression.
        // Attribution (PlacementAwardRules) = KO/Cup only — Championship/Groups use Standing.
        // Domain still allows both Qual/Prog on StageRegulation; UI/actions filter by topology.
        if (StageClassification.IsClassifyingPhase(stage))
        {
            actions.Add(ActionReplaceQualificationRules);
        }

        if (StageClassification.IsNonClassifyingPhase(stage))
        {
            actions.Add(ActionReplaceProgressionRules);
            actions.Add(ActionReplacePlacementAwardRules);
        }

        if (stage.Regulation.StandingRules is not null
            && !StageClassification.IsNonClassifyingPhase(stage))
        {
            actions.Add(ActionReplaceStandingRules);
        }

        if (StageNeedsDrawRulesAction(stage))
        {
            actions.Add(ActionReplaceDrawRules);
        }

        // B2 — Affectation may co-exist with inbound Qualif/Prog on the same phase
        // (V2 I7 / scenario B2). Domain ReplaceAffectationAuthoring is not root-gated.
        actions.Add(ActionReplaceAffectationAuthoring);

        if (StageNeedsTieFormatAction(stage))
        {
            actions.Add(ActionReplaceDefaultTieFormat);
            if (stage.Rounds.Count > 0)
            {
                actions.Add(ActionReplaceRoundTieFormat);
            }
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

        switch (format)
        {
            case StructureFormatKind.Cup or null:
                actions.Add(ActionAddRound);
                actions.Add(ActionAddSlot);
                actions.Add(ActionAssignEntryToSlot);
                actions.Add(ActionClearSlotAssignment);
                break;
            case StructureFormatKind.Championship or StructureFormatKind.Groups:
                actions.Add(ActionReplaceMatchGenerationFormat);
                break;
            case StructureFormatKind.Swiss:
                actions.Add(ActionReplaceSwissSettings);
                break;
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

    private static bool StageNeedsTieFormatAction(Stage stage) =>
        stage.Regulation.TieFormat is not null || stage.Rounds.Any(round => round.TieFormat is not null) ||
        InferFormat(stage) is StructureFormatKind.Cup;

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
                if (path.Destination.StageId.Equals(stage.Id) || !byId.TryGetValue(path.Destination.StageId, out var destination))
                {
                    issues.Add(IssueDanglingQualificationTarget);
                    continue;
                }

                if (path.Destination.TargetsPopulation || path.Destination.TargetsForm)
                {
                    continue;
                }

                if (path.Destination.TargetsGroup)
                {
                    if (destination.FindGroup(path.Destination.GroupId!.Value) is null)
                    {
                        issues.Add(IssueMissingQualificationDestinationGroup);
                    }

                    continue;
                }

                if (destination.FindSlot(path.Destination.SlotKey!) is null)
                {
                    issues.Add(IssueMissingQualificationDestinationSlot);
                }
            }
        }

        if (stage.Regulation.ProgressionRules is not { } progression)
            return [.. issues.Distinct(StringComparer.Ordinal)];

        foreach (var path in progression.Paths)
        {
            if (path.Destination.TargetsPopulation || path.Destination.TargetsForm)
            {
                if (path.Destination.StageId.Equals(stage.Id))
                {
                    issues.Add(IssueDanglingProgressionTarget);
                    continue;
                }

                if (!byId.ContainsKey(path.Destination.StageId))
                {
                    issues.Add(IssueDanglingProgressionTarget);
                }

                continue;
            }

            if (path.Destination.StageId.Equals(stage.Id))
            {
                if (path.Destination.TargetsGroup)
                {
                    if (stage.FindGroup(path.Destination.GroupId!.Value) is null)
                    {
                        issues.Add(IssueMissingProgressionDestinationGroup);
                    }
                }
                else if (stage.FindSlot(path.Destination.SlotKey!) is null)
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

            if (path.Destination.TargetsGroup)
            {
                if (destination.FindGroup(path.Destination.GroupId!.Value) is null)
                {
                    issues.Add(IssueMissingProgressionDestinationGroup);
                }
            }
            else if (destination.FindSlot(path.Destination.SlotKey!) is null)
            {
                issues.Add(IssueMissingProgressionDestinationSlot);
            }
        }

        return [.. issues.Distinct(StringComparer.Ordinal)];
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
                current = current with { Rounds = [.. current.Rounds, roundRef] };
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
            : stage.Slots.Count > 0
                ? stage.Slots.Count
                : competition.Entries.Count;

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
                        readyForMaterialization =
                            assigned >= 2 && primary.Groups.All(group => group.EntryIds.Count >= 2);
                    }

                    break;
                case StructureFormatKind.Cup:
                    var cupEntryPlaces = ResolveCupEntryPlaces(primary);
                    if (cupEntryPlaces is null or < 2 || !IsPowerOfTwo(cupEntryPlaces.Value))
                    {
                        blockers.Add(BlockerCupBracketInvalid);
                    }
                    else
                    {
                        // Draw path = structure (rounds + valid entry bracket).
                        // Cup fixtures come only from MaterializeCupFromOccupiedSlots (BracketPair) —
                        // never from MaterializeMatches skeleton.
                        readyForDraw = structure.RoundCount >= 1;
                        readyForMaterialization = false;
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
