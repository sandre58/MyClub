// -----------------------------------------------------------------------
// <copyright file="CompetitionFlowAnalyzer.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Pure Application analyzer: competition flow facts (Feed / Topology / Capacity / Execution / Result).
/// </summary>
/// <remarks>
/// Does not decide Hard / Gate / Signal. Prepare and BuildReadiness apply lifecycle policy.
/// One Feed diagnostic per required slot (Invalid &gt; Multiple &gt; Missing &gt; Unique).
/// </remarks>
public static class CompetitionFlowAnalyzer
{
    /// <summary>Feed: no inbound WhoFeeds source for a required Cup slot.</summary>
    public const string SlotFeedMissing = "SlotFeedMissing";

    /// <summary>Feed: two or more sources of the same feed kind for one slot.</summary>
    public const string SlotFeedInvalid = "SlotFeedInvalid";

    /// <summary>Feed: two or more distinct feed kinds for one slot.</summary>
    public const string SlotFeedMultiple = "SlotFeedMultiple";

    /// <summary>Topology: Qual/Prog stage graph contains a cycle.</summary>
    public const string CycleDetected = "CycleDetected";

    /// <summary>Topology: non-entry phase has no inbound Qual/Prog and no affectation (Signal).</summary>
    public const string NonRootUnfed = "NonRootUnfed";

    /// <summary>Topology: routing ambiguity outside WhoFeeds slot taxonomy.</summary>
    public const string RoutingNondeterminism = "RoutingNondeterminism";

    /// <summary>Capacity: inbound + affectation exceeds structural places capacity.</summary>
    public const string OverCapacityEdge = "OverCapacityEdge";

    /// <summary>Capacity: outbound extraction exceeds source structural capacity.</summary>
    public const string OverExtraction = "OverExtraction";

    /// <summary>Capacity: source population incomplete relative to extraction (Signal).</summary>
    public const string SourcePopulationShortfall = "SourcePopulationShortfall";

    /// <summary>Capacity: destination Places N not fully promised (Signal).</summary>
    public const string PopulationShortfall = "PopulationShortfall";

    /// <summary>Result: authored placement award path cannot be produced (missing source confrontation). Signal; never Ready.</summary>
    public const string AuthoredAwardUnreachable = "AuthoredAwardUnreachable";

    /// <summary>Result: expected rank cannot be produced (Signal; never Ready).</summary>
    public const string ExpectedRankUnreachable = "ExpectedRankUnreachable";

    /// <summary>Execution: necessary confrontation TieFormat cannot always decide a draw.</summary>
    public const string ConfrontationUndecidable = "ConfrontationUndecidable";

    /// <summary>
    /// Analyzes competition flow without mutating aggregates.
    /// </summary>
    /// <param name="competition">Loaded competition.</param>
    /// <param name="stages">Competition stages.</param>
    /// <returns>Factual flow analysis.</returns>
    public static CompetitionFlowAnalysis Analyze(
        Competition competition,
        IReadOnlyList<Stage> stages)
    {
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(stages);

        return new CompetitionFlowAnalysis(
            AnalyzeFeeds(stages),
            AnalyzeTopology(stages, competition.StageIds.Count > 0 ? competition.StageIds[0] : null),
            AnalyzeCapacity(
                stages,
                competition.Entries.Count(entry => entry.Status == EntryStatus.Active)),
            AnalyzeExecution(stages),
            AnalyzeResult(stages));
    }

    /// <summary>
    /// Analyzes inbound WhoFeeds for all Cup slots across stages.
    /// </summary>
    /// <param name="stages">Competition stages.</param>
    /// <returns>At most one diagnostic per slot (non-Unique only).</returns>
    [SuppressMessage("ReSharper", "LoopCanBeConvertedToQuery", Justification = "False positive")]
    public static IReadOnlyList<CompetitionFlowDiagnostic> AnalyzeFeeds(IReadOnlyList<Stage> stages)
    {
        ArgumentNullException.ThrowIfNull(stages);

        var diagnostics = new List<CompetitionFlowDiagnostic>();
        foreach (var stage in stages)
        {
            if (stage.Slots.Count == 0)
            {
                continue;
            }

            try
            {
                var snapshot = SlotFeedSnapshotAssembler.Assemble(stage, stages);
                foreach (var resolution in SlotFeedResolver.ResolveAll(snapshot))
                {
                    var code = resolution.Status switch
                    {
                        FeedResolutionStatus.InvalidFeed => SlotFeedInvalid,
                        FeedResolutionStatus.MultipleFeeds => SlotFeedMultiple,
                        FeedResolutionStatus.Missing => SlotFeedMissing,
                        FeedResolutionStatus.Unique => null,
                        _ => null
                    };

                    if (code is null)
                    {
                        continue;
                    }

                    diagnostics.Add(new CompetitionFlowDiagnostic(
                        code,
                        stage.Id.Value,
                        resolution.SlotKey));
                }
            }
            catch (ApplicationFailureException)
            {
                // Outbound dangling already projected on source structure issues.
            }
            catch (DomainException)
            {
                // Tolerant read — same spirit as StructureViewAssembler feed projection.
            }
        }

        return diagnostics;
    }

    /// <summary>
    /// Analyzes Qual/Prog topology (cycles, non-root unfed, routing nondeterminism).
    /// </summary>
    /// <param name="stages">Competition stages.</param>
    /// <param name="entryStageId">First competition stage when known (NonRootUnfed baseline).</param>
    /// <returns>Topology diagnostics.</returns>
    public static IReadOnlyList<CompetitionFlowDiagnostic> AnalyzeTopology(
        IReadOnlyList<Stage> stages,
        StageId? entryStageId = null)
    {
        ArgumentNullException.ThrowIfNull(stages);

        var diagnostics = new List<CompetitionFlowDiagnostic>();
        AppendCycles(stages, diagnostics);
        AppendNonRootUnfed(stages, entryStageId, diagnostics);
        AppendRoutingNondeterminism(stages, diagnostics);
        return diagnostics;
    }

    /// <summary>
    /// Analyzes structural capacity / population volume facts.
    /// </summary>
    /// <param name="stages">Competition stages.</param>
    /// <param name="activeEntryCount">Active competition entries (SourcePopulationShortfall heuristic).</param>
    /// <returns>Capacity diagnostics.</returns>
    public static IReadOnlyList<CompetitionFlowDiagnostic> AnalyzeCapacity(
        IReadOnlyList<Stage> stages,
        int activeEntryCount = 0)
    {
        ArgumentNullException.ThrowIfNull(stages);

        var diagnostics = new List<CompetitionFlowDiagnostic>();

        foreach (var stage in stages)
        {
            var capacity = StructurePopulationVolume.ResolvePlacesCapacity(stage);
            var expected = StructurePopulationVolume.ExpectedOccupancy(stage, stages);

            if (capacity is { } places)
            {
                if (expected > places)
                {
                    diagnostics.Add(new CompetitionFlowDiagnostic(OverCapacityEdge, stage.Id.Value));
                }
                else if (expected < places)
                {
                    diagnostics.Add(new CompetitionFlowDiagnostic(PopulationShortfall, stage.Id.Value));
                }
            }

            var outbound = StructurePopulationVolume.OutboundQualificationVolume(stage);
            if (outbound <= 0)
            {
                continue;
            }

            if (capacity is { } sourcePlaces && outbound > sourcePlaces)
            {
                diagnostics.Add(new CompetitionFlowDiagnostic(OverExtraction, stage.Id.Value));
            }
            else
            {
                var available = Math.Max(stage.CompositionEntries.Count, stage.AffectationAuthoring.Count);
                if (available == 0)
                {
                    available = activeEntryCount;
                }

                if (capacity is { } bounded)
                {
                    available = Math.Min(available, bounded);
                }

                if (outbound > available)
                {
                    diagnostics.Add(new CompetitionFlowDiagnostic(SourcePopulationShortfall, stage.Id.Value));
                }
            }
        }

        return diagnostics;
    }

    /// <summary>
    /// Analyzes outcome determinability facts (never Ready / Prepare gates).
    /// </summary>
    /// <param name="stages">Competition stages.</param>
    /// <returns>Result diagnostics.</returns>
    public static IReadOnlyList<CompetitionFlowDiagnostic> AnalyzeResult(IReadOnlyList<Stage> stages)
    {
        ArgumentNullException.ThrowIfNull(stages);

        var diagnostics = new List<CompetitionFlowDiagnostic>();

        foreach (var stage in stages)
        {
            if (stage.Regulation.PlacementAwardRules is not { } placement)
            {
                continue;
            }

            var authoredOnStage = new HashSet<int>();
            var reachableOnStage = new HashSet<int>();

            foreach (var path in placement.Paths)
            {
                authoredOnStage.Add(path.Rank);

                // Structural reachability = SourcePairKey maps to a BracketPair (not a fixture).
                // Competition ranks are not comparable to local Places N (e.g. bronze 3rd/4th on a 2-place stage).
                if (stage.FindBracketPair(path.SourcePairKey) is not null)
                {
                    reachableOnStage.Add(path.Rank);
                }
                else
                {
                    diagnostics.Add(new CompetitionFlowDiagnostic(
                        AuthoredAwardUnreachable,
                        stage.Id.Value,
                        path.SourcePairKey));
                }
            }

            // ExpectedRanks for PlacementAwards are the authored ranks on this stage.
            diagnostics.AddRange(from rank in authoredOnStage where !reachableOnStage.Contains(rank) select new CompetitionFlowDiagnostic(ExpectedRankUnreachable, stage.Id.Value));
        }

        return diagnostics;
    }

    /// <summary>
    /// Analyzes decidability of confrontations that are necessary to progression or placement awards.
    /// Only addressable confrontations (pair or fixture present) are evaluated — never EXEC on a
    /// resource RESULT already treats as structurally absent.
    /// </summary>
    /// <param name="stages">Competition stages.</param>
    /// <returns>One <see cref="ConfrontationUndecidable"/> per undecidable necessary source pair.</returns>
    public static IReadOnlyList<CompetitionFlowDiagnostic> AnalyzeExecution(IReadOnlyList<Stage> stages)
    {
        ArgumentNullException.ThrowIfNull(stages);

        var diagnostics = new List<CompetitionFlowDiagnostic>();
        foreach (var stage in stages)
        {
            diagnostics.AddRange(from sourcePairKey in CollectNecessaryConfrontationKeys(stage) where IsAddressableConfrontation(stage, sourcePairKey) let tieFormat = ResolveEffectiveTieFormat(stage, sourcePairKey) where !tieFormat.CanAlwaysDecideDraw() select new CompetitionFlowDiagnostic(ConfrontationUndecidable, stage.Id.Value, sourcePairKey));
        }

        return diagnostics;
    }

    /// <summary>
    /// Returns whether a projected structure-issue code is a construction GraphInvalid gate.
    /// </summary>
    /// <param name="issueCode">Structure issue / flow diagnostic code.</param>
    /// <returns><see langword="true"/> when the code contributes to StructureGraphInvalid.</returns>
    public static bool IsStructureGraphGateIssue(string issueCode) =>
        issueCode switch
        {
            SlotFeedMissing => false,
            NonRootUnfed => false,
            OverCapacityEdge => false,
            OverExtraction => false,
            SourcePopulationShortfall => false,
            PopulationShortfall => false,
            AuthoredAwardUnreachable => false,
            ExpectedRankUnreachable => false,
            ConfrontationUndecidable => false,
            _ => true
        };

    /// <summary>
    /// Returns whether a diagnostic is a Prepare Gate fact (policy applied by Prepare, not Analyzer).
    /// </summary>
    /// <param name="issueCode">Flow diagnostic code.</param>
    /// <returns><see langword="true"/> when Prepare must refuse Ready.</returns>
    public static bool IsPrepareGateIssue(string issueCode) =>
        issueCode switch
        {
            SlotFeedInvalid => true,
            SlotFeedMultiple => true,
            SlotFeedMissing => false,
            CycleDetected => true,
            RoutingNondeterminism => true,
            OverCapacityEdge => false,
            OverExtraction => true,
            ConfrontationUndecidable => true,
            _ => false
        };

    private static void AppendCycles(
        IReadOnlyList<Stage> stages,
        List<CompetitionFlowDiagnostic> diagnostics)
    {
        var adjacency = new Dictionary<Guid, HashSet<Guid>>();
        foreach (var stage in stages)
        {
            adjacency[stage.Id.Value] = [];
        }

        foreach (var stage in stages)
        {
            foreach (var destinationId in EnumerateOutboundStageIds(stage))
            {
                if (!adjacency.ContainsKey(destinationId.Value))
                {
                    continue;
                }

                adjacency[stage.Id.Value].Add(destinationId.Value);
            }
        }

        var visiting = new HashSet<Guid>();
        var visited = new HashSet<Guid>();
        var cycleStages = new HashSet<Guid>();

        foreach (var node in adjacency.Keys)
        {
            VisitCycle(node, adjacency, visiting, visited, cycleStages);
        }

        diagnostics.AddRange(cycleStages.Select(stageId => new CompetitionFlowDiagnostic(CycleDetected, stageId)));
    }

    private static bool VisitCycle(
        Guid node,
        IReadOnlyDictionary<Guid, HashSet<Guid>> adjacency,
        HashSet<Guid> visiting,
        HashSet<Guid> visited,
        HashSet<Guid> cycleStages)
    {
        if (visited.Contains(node))
        {
            return false;
        }

        if (!visiting.Add(node))
        {
            cycleStages.Add(node);
            return true;
        }

        var found = false;
        foreach (var unused in adjacency[node].Where(next => VisitCycle(next, adjacency, visiting, visited, cycleStages)))
        {
            cycleStages.Add(node);
            found = true;
        }

        visiting.Remove(node);
        visited.Add(node);
        return found;
    }

    private static void AppendNonRootUnfed(
        IReadOnlyList<Stage> stages,
        StageId? entryStageId,
        List<CompetitionFlowDiagnostic> diagnostics)
    {
        if (stages.Count == 0)
        {
            return;
        }

        var baseline = entryStageId ?? stages[0].Id;
        diagnostics.AddRange(from stage in stages where !stage.Id.Equals(baseline) where !HasInboundFeed(stage, stages) where stage.AffectationAuthoring.Count == 0 select new CompetitionFlowDiagnostic(NonRootUnfed, stage.Id.Value));
    }

    private static void AppendRoutingNondeterminism(
        IReadOnlyList<Stage> stages,
        List<CompetitionFlowDiagnostic> diagnostics)
    {
        var slotFeedCodes = AnalyzeFeeds(stages)
            .Where(d => d.StageId is not null && d.SlotKey is not null)
            .Select(d => (StageId: d.StageId!.Value, SlotKey: d.SlotKey!))
            .ToHashSet();

        foreach (var stage in stages)
        {
            if (stage.Regulation.QualificationRules is not { } qualification)
            {
                continue;
            }

            var bySource = qualification.Paths.GroupBy(QualSourceKey, StringComparer.Ordinal);

            foreach (var group in bySource)
            {
                var paths = group.ToArray();
                if (paths.Length < 2)
                {
                    continue;
                }

                // Same slot conflict is FEED taxonomy — exclude.
                var allSameSlot = paths.All(path => path.Destination.TargetsSlot)
                                  && paths.Select(path => $"{path.Destination.StageId.Value:N}:{path.Destination.SlotKey}")
                                      .Distinct(StringComparer.Ordinal)
                                      .Count() == 1;
                if (allSameSlot)
                {
                    var dest = paths[0].Destination;
                    if (slotFeedCodes.Contains((dest.StageId.Value, dest.SlotKey!)))
                    {
                        continue;
                    }
                }

                diagnostics.Add(new CompetitionFlowDiagnostic(
                    RoutingNondeterminism,
                    stage.Id.Value));
                break;
            }
        }
    }

    private static string QualSourceKey(QualificationPath path) =>
        string.Join(
            '|',
            path.Source.Scope?.ToString() ?? "Overall",
            path.Source.GroupId?.Value.ToString("N") ?? "-",
            path.Source.AcrossGroupsPosition?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "-",
            path.Selection.Mode.ToString(),
            path.Selection.Value.ToString(System.Globalization.CultureInfo.InvariantCulture),
            path.Selection.EndValue?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "-");

    private static bool HasInboundFeed(Stage stage, IReadOnlyList<Stage> stages)
    {
        foreach (var other in stages)
        {
            if (other.Id.Equals(stage.Id))
            {
                continue;
            }

            if (other.Regulation.QualificationRules?.Paths
                    .Any(path => path.Destination.StageId.Equals(stage.Id)) == true)
            {
                return true;
            }

            if (other.Regulation.ProgressionRules?.Paths
                    .Any(path => path.Destination.StageId.Equals(stage.Id)) == true)
            {
                return true;
            }
        }

        return false;
    }

    private static IEnumerable<StageId> EnumerateOutboundStageIds(Stage stage)
    {
        if (stage.Regulation.QualificationRules is { } qualification)
        {
            foreach (var path in qualification.Paths)
            {
                if (!path.Destination.StageId.Equals(stage.Id))
                {
                    yield return path.Destination.StageId;
                }
            }
        }

        if (stage.Regulation.ProgressionRules is not { } progression) yield break;
        foreach (var path in progression.Paths)
        {
            if (!path.Destination.StageId.Equals(stage.Id))
            {
                yield return path.Destination.StageId;
            }
        }
    }

    private static HashSet<string> CollectNecessaryConfrontationKeys(Stage stage)
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);

        if (stage.Regulation.ProgressionRules is { } progression)
        {
            foreach (var path in progression.Paths)
            {
                keys.Add(path.SourcePairKey);
            }
        }

        if (stage.Regulation.PlacementAwardRules is not { } placement) return keys;
        foreach (var path in placement.Paths)
        {
            keys.Add(path.SourcePairKey);
        }

        return keys;
    }

    /// <summary>
    /// Returns whether a confrontation key exists on the stage form (pair or fixture).
    /// </summary>
    private static bool IsAddressableConfrontation(Stage stage, string sourcePairKey) =>
        stage.FindBracketPair(sourcePairKey) is not null
        || stage.FindFixtureByBracketPairKey(sourcePairKey) is not null;

    /// <summary>
    /// Resolves the effective TieFormat for a structural confrontation (same round binding as Domain progression).
    /// </summary>
    private static TieFormat ResolveEffectiveTieFormat(Stage stage, string sourcePairKey)
    {
        var owningRound = stage.Rounds.FirstOrDefault(round =>
                              round.Fixtures.Any(fixture =>
                                  string.Equals(fixture.BracketPairKey, sourcePairKey, StringComparison.Ordinal)))
                          ?? (stage.FindBracketPair(sourcePairKey) is not null && stage.Rounds.Count == 1
                              ? stage.Rounds[0]
                              : null);

        return owningRound is not null ? TieFormat.OrDefaultOneLeg(owningRound.TieFormat ?? stage.Regulation.TieFormat) : TieFormat.OrDefaultOneLeg(stage.Regulation.TieFormat);
    }
}
