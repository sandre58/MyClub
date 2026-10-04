// -----------------------------------------------------------------------
// <copyright file="DematerializationCapability.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Shared dematerialization capability (Structure read + command preflight authority).
/// </summary>
public static class DematerializationCapability
{
    /// <summary>Full attached phase materialization (Championship / Groups / Cup).</summary>
    public const string ScopeFullPhase = "FullPhase";

    /// <summary>Last Swiss round only.</summary>
    public const string ScopeLastSwissRound = "LastSwissRound";

    /// <summary>
    /// Evaluates whether dematerialization should appear on the Structure workspace and whether it is enabled.
    /// </summary>
    public static DematerializationCapabilityResult Evaluate(
        Competition competition,
        Stage stage,
        IReadOnlyList<DematerializationMatchGate> stageMatchGates)
    {
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(stageMatchGates);

        var format = StructureSkeleton.InferFormat(stage);
        return format switch
        {
            StructureFormatKind.Championship or StructureFormatKind.Groups =>
                EvaluateFullPhase(competition, stage, stageMatchGates, format.Value),
            StructureFormatKind.Cup => EvaluateFullPhase(competition, stage, stageMatchGates, format.Value),
            StructureFormatKind.Swiss => EvaluateSwiss(competition, stage, stageMatchGates),
            null => DematerializationCapabilityResult.NotExposed,
            _ => DematerializationCapabilityResult.NotExposed
        };
    }

    /// <summary>
    /// Throws when execute preflight blocks dematerialization (command authority).
    /// </summary>
    public static void EnsureExecuteAllowed(
        Competition competition,
        Stage stage,
        IReadOnlyList<Match> stageMatches)
    {
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(stageMatches);

        var gates = stageMatches.Select(DematerializationMatchGate.FromMatch).ToArray();
        var result = Evaluate(competition, stage, gates);
        if (!result.IsExposed)
        {
            throw new ApplicationFailureException(
                "Dematerialization is not available for this stage.",
                ApplicationErrorCodes.DematerializationFailure);
        }

        if (!result.Enabled)
        {
            ThrowForDisabledReason(result.DisabledReason!);
        }
    }

    internal static bool BlocksDematerialization(DematerializationMatchGate match) =>
        match.Status is MatchStatus.Live or MatchStatus.Finished
        || (match.Status is MatchStatus.Cancelled && match.HasObservedLive);

    internal static string? TryOrphanReason(Stage stage, IReadOnlyList<DematerializationMatchGate> stageMatchGates)
    {
        foreach (var gate in stageMatchGates)
        {
            if (!stage.HasMatch(gate.Id))
            {
                return DematerializeDisabledReasons.MaterializationStateInconsistent;
            }
        }

        return null;
    }

    internal static string? TryMatchLifecycleReason(IReadOnlyList<DematerializationMatchGate> scopeGates) =>
        scopeGates.Any(BlocksDematerialization)
            ? DematerializeDisabledReasons.MatchLifecycleStarted
            : null;

    internal static string? TrySwissContiguousReason(Stage stage)
    {
        if (stage.Matchdays.Count == 0)
        {
            return null;
        }

        var numbers = stage.Matchdays.Select(matchday => matchday.Number).Order().ToArray();
        for (var index = 0; index < numbers.Length; index++)
        {
            if (numbers[index] != index + 1)
            {
                return DematerializeDisabledReasons.MaterializationStateInconsistent;
            }
        }

        return null;
    }

    internal static string? TryFullPhaseLifecycleReason(
        Competition competition,
        Stage stage,
        StructureFormatKind format)
    {
        if (format is StructureFormatKind.Cup)
        {
            if (stage.Rounds.Count == 0 || stage.Matchdays.Count > 0 || stage.Groups.Count > 0)
            {
                return DematerializeDisabledReasons.FormatNotSupported;
            }

            if (competition.Status is CompetitionStatus.Suspended
                or CompetitionStatus.Completed
                or CompetitionStatus.Archived)
            {
                return DematerializeDisabledReasons.StageLifecycleNotSupported;
            }

            if (stage.Status is StageStatus.Running or StageStatus.Suspended or StageStatus.Completed)
            {
                return DematerializeDisabledReasons.StageLifecycleNotSupported;
            }

            return null;
        }

        if (format is not (StructureFormatKind.Championship or StructureFormatKind.Groups))
        {
            return DematerializeDisabledReasons.FormatNotSupported;
        }

        if (competition.Status is CompetitionStatus.Running
            or CompetitionStatus.Suspended
            or CompetitionStatus.Completed
            or CompetitionStatus.Archived)
        {
            return DematerializeDisabledReasons.StageLifecycleNotSupported;
        }

        if (stage.Status is StageStatus.Running or StageStatus.Suspended or StageStatus.Completed)
        {
            return DematerializeDisabledReasons.StageLifecycleNotSupported;
        }

        return null;
    }

    internal static string? TrySwissLifecycleReason(Competition competition, Stage stage)
    {
        if (competition.Status is CompetitionStatus.Suspended
            or CompetitionStatus.Completed
            or CompetitionStatus.Archived)
        {
            return DematerializeDisabledReasons.StageLifecycleNotSupported;
        }

        if (stage.Status is not StageStatus.Running)
        {
            return DematerializeDisabledReasons.StageLifecycleNotSupported;
        }

        return null;
    }

    internal static void ThrowForDisabledReason(string reason)
    {
        throw reason switch
        {
            DematerializeDisabledReasons.MatchLifecycleStarted => new ApplicationFailureException(
                "Dematerialization is blocked because a match in scope was already played or is live.",
                ApplicationErrorCodes.DematerializationBlocked),
            DematerializeDisabledReasons.MaterializationStateInconsistent => new ApplicationFailureException(
                "Dematerialization is blocked because materialization state is inconsistent.",
                ApplicationErrorCodes.MaterializationStateInconsistent),
            DematerializeDisabledReasons.StageLifecycleNotSupported
                or DematerializeDisabledReasons.FormatNotSupported => new ApplicationFailureException(
                    "Dematerialization is not allowed in the current lifecycle state.",
                    ApplicationErrorCodes.StructureNotMutable),
            _ => new ApplicationFailureException(
                "Dematerialization is not allowed.",
                ApplicationErrorCodes.DematerializationFailure)
        };
    }

    private static DematerializationCapabilityResult EvaluateFullPhase(
        Competition competition,
        Stage stage,
        IReadOnlyList<DematerializationMatchGate> stageMatchGates,
        StructureFormatKind format)
    {
        var scope = CollectFullPhaseScope(stage, stageMatchGates);
        if (scope.AttachedMatchIdCount == 0)
        {
            return DematerializationCapabilityResult.NotExposed;
        }

        if (scope.MissingPersistedMatch)
        {
            return DematerializationCapabilityResult.Disabled(
                ScopeFullPhase,
                DematerializeDisabledReasons.MaterializationStateInconsistent);
        }

        var lifecycle = TryFullPhaseLifecycleReason(competition, stage, format);
        if (lifecycle is not null)
        {
            return DematerializationCapabilityResult.Disabled(ScopeFullPhase, lifecycle);
        }

        var orphan = TryOrphanReason(stage, stageMatchGates);
        if (orphan is not null)
        {
            return DematerializationCapabilityResult.Disabled(ScopeFullPhase, orphan);
        }

        var played = TryMatchLifecycleReason(scope.Gates);
        if (played is not null)
        {
            return DematerializationCapabilityResult.Disabled(ScopeFullPhase, played);
        }

        return DematerializationCapabilityResult.Enabled(ScopeFullPhase);
    }

    private static DematerializationCapabilityResult EvaluateSwiss(
        Competition competition,
        Stage stage,
        IReadOnlyList<DematerializationMatchGate> stageMatchGates)
    {
        if (!stage.IsSwiss)
        {
            return DematerializationCapabilityResult.NotExposed;
        }

        var swissScope = TryCollectSwissLastRound(stage, stageMatchGates);
        if (swissScope is null)
        {
            return DematerializationCapabilityResult.NotExposed;
        }

        var lifecycle = TrySwissLifecycleReason(competition, stage);
        if (lifecycle is not null)
        {
            return DematerializationCapabilityResult.Disabled(ScopeLastSwissRound, lifecycle);
        }

        var contiguous = TrySwissContiguousReason(stage);
        if (contiguous is not null)
        {
            return DematerializationCapabilityResult.Disabled(ScopeLastSwissRound, contiguous);
        }

        var orphan = TryOrphanReason(stage, stageMatchGates);
        if (orphan is not null)
        {
            return DematerializationCapabilityResult.Disabled(ScopeLastSwissRound, orphan);
        }

        if (swissScope.Gates.Count == 0)
        {
            return DematerializationCapabilityResult.Disabled(
                ScopeLastSwissRound,
                DematerializeDisabledReasons.MaterializationStateInconsistent);
        }

        var played = TryMatchLifecycleReason(swissScope.Gates);
        if (played is not null)
        {
            return DematerializationCapabilityResult.Disabled(ScopeLastSwissRound, played);
        }

        return DematerializationCapabilityResult.Enabled(ScopeLastSwissRound);
    }

    private static FullPhaseCapabilityScope CollectFullPhaseScope(
        Stage stage,
        IReadOnlyList<DematerializationMatchGate> stageMatchGates)
    {
        var format = StructureSkeleton.InferFormat(stage);
        var matchIds = format switch
        {
            StructureFormatKind.Cup => stage.Rounds
                .SelectMany(round => round.Fixtures)
                .SelectMany(fixture => fixture.MatchIds),
            StructureFormatKind.Championship or StructureFormatKind.Groups => stage.Matchdays
                .SelectMany(matchday => matchday.Fixtures)
                .SelectMany(fixture => fixture.MatchIds),
            _ => Array.Empty<MatchId>()
        };

        var distinctIds = matchIds.Distinct().ToArray();
        var gatesById = stageMatchGates.ToDictionary(gate => gate.Id);
        var gates = new List<DematerializationMatchGate>(distinctIds.Length);
        var missing = false;
        foreach (var id in distinctIds)
        {
            if (gatesById.TryGetValue(id, out var gate))
            {
                gates.Add(gate);
            }
            else
            {
                missing = true;
            }
        }

        return new FullPhaseCapabilityScope(distinctIds.Length, missing, gates);
    }

    private static SwissLastRoundCapabilityScope? TryCollectSwissLastRound(
        Stage stage,
        IReadOnlyList<DematerializationMatchGate> stageMatchGates)
    {
        if (stage.Matchdays.Count == 0)
        {
            return null;
        }

        var lastMatchday = stage.Matchdays.MaxBy(matchday => matchday.Number)!;
        var matchIds = lastMatchday.Fixtures
            .SelectMany(fixture => fixture.MatchIds)
            .Distinct()
            .ToHashSet();

        var gatesById = stageMatchGates.ToDictionary(gate => gate.Id);
        var gates = matchIds
            .Select(id => gatesById[id])
            .ToArray();

        var hasBye = stage.SwissByeHistory.Any(bye => bye.RoundIndex == lastMatchday.Number);
        if (gates.Length == 0 && !hasBye && lastMatchday.Fixtures.Count == 0)
        {
            return null;
        }

        return new SwissLastRoundCapabilityScope(gates);
    }

    private sealed record FullPhaseCapabilityScope(
        int AttachedMatchIdCount,
        bool MissingPersistedMatch,
        IReadOnlyList<DematerializationMatchGate> Gates);

    private sealed record SwissLastRoundCapabilityScope(IReadOnlyList<DematerializationMatchGate> Gates);
}

/// <summary>
/// Outcome of <see cref="DematerializationCapability.Evaluate"/>.
/// </summary>
public sealed record DematerializationCapabilityResult(
    bool IsExposed,
    bool Enabled,
    string? DisabledReason,
    string? Scope)
{
    public static DematerializationCapabilityResult NotExposed { get; } =
        new(IsExposed: false, Enabled: false, DisabledReason: null, Scope: null);

    public static DematerializationCapabilityResult Enabled(string scope) =>
        new(IsExposed: true, Enabled: true, DisabledReason: null, Scope: scope);

    public static DematerializationCapabilityResult Disabled(string scope, string reason) =>
        new(IsExposed: true, Enabled: false, DisabledReason: reason, Scope: scope);
}
