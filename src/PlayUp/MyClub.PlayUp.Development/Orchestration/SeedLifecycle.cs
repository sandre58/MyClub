// -----------------------------------------------------------------------
// <copyright file="SeedLifecycle.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Development.Runtime;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Development.Orchestration;

/// <summary>
/// Seeds prepare / start / progress / complete lifecycle for development scenarios.
/// </summary>
internal static class SeedLifecycle
{
    public static void PrepareAndStart(ScenarioContext context, Competition competition, Stage stage)
    {
        stage.Prepare(context.Clock);
        competition.Prepare(context.Clock);
        stage.Start(context.Clock);
        competition.Start(context.Clock);
    }

    /// <summary>
    /// Transitions stage + competition to Ready without starting.
    /// </summary>
    public static void PrepareOnly(ScenarioContext context, Competition competition, Stage stage)
    {
        stage.Prepare(context.Clock);
        competition.Prepare(context.Clock);
    }

    public static void CompleteRunning(ScenarioContext context, Competition competition, Stage stage) =>
        CompleteAllRunning(context, competition, [stage]);

    /// <summary>
    /// Completes every Running/Suspended stage, then the competition (Normal).
    /// </summary>
    public static void CompleteAllRunning(
        ScenarioContext context,
        Competition competition,
        IEnumerable<Stage> stages)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(stages);

        foreach (var stage in stages)
        {
            if (stage.Status is StageStatus.Running or StageStatus.Suspended)
            {
                stage.Complete(context.Clock);
            }
        }

        competition.Complete(CompletionMode.Normal, context.Clock);
    }

    internal static void ApplyStructuredLifecycle(
        ScenarioContext context,
        Competition competition,
        Stage stage,
        IReadOnlyList<Match> matches,
        StructuredSeedLifecycle lifecycle)
    {
        switch (lifecycle)
        {
            case StructuredSeedLifecycle.Ready:
                PrepareOnly(context, competition, stage);
                return;
            case StructuredSeedLifecycle.Suspended:
                PrepareAndStart(context, competition, stage);
                ApplyProgress(context, competition, stage, matches, SeedProgress.Running);
                stage.Suspend(context.Clock);
                competition.Suspend(context.Clock);
                return;
            case StructuredSeedLifecycle.Archived:
                PrepareAndStart(context, competition, stage);
                ApplyProgress(context, competition, stage, matches, SeedProgress.Finished);
                competition.Archive(context.Clock);
                return;
            case StructuredSeedLifecycle.Progressive:
                PrepareAndStart(context, competition, stage);
                ApplyProgress(context, competition, stage, matches, context.Progress);
                return;
            default:
                throw new InvalidOperationException($"Unsupported structured lifecycle '{lifecycle}'.");
        }
    }

    internal static void ApplyStructuredLifecycleSwiss(
        ScenarioContext context,
        Competition competition,
        Stage stage,
        StructuredSeedLifecycle lifecycle)
    {
        switch (lifecycle)
        {
            case StructuredSeedLifecycle.Ready:
                PrepareOnly(context, competition, stage);
                return;
            case StructuredSeedLifecycle.Suspended:
                PrepareAndStart(context, competition, stage);
                ApplySwissProgress(context, competition, stage, SeedProgress.Running);
                stage.Suspend(context.Clock);
                competition.Suspend(context.Clock);
                return;
            case StructuredSeedLifecycle.Archived:
                PrepareAndStart(context, competition, stage);
                ApplySwissProgress(context, competition, stage, SeedProgress.Finished);
                competition.Archive(context.Clock);
                return;
            case StructuredSeedLifecycle.Progressive:
                PrepareAndStart(context, competition, stage);
                ApplySwissProgress(context, competition, stage, context.Progress);
                return;
            default:
                throw new InvalidOperationException($"Unsupported structured lifecycle '{lifecycle}'.");
        }
    }

    public static void ApplyProgress(
        ScenarioContext context,
        Competition competition,
        Stage stage,
        IReadOnlyList<Match> matches,
        SeedProgress progress)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(matches);

        switch (progress)
        {
            case SeedProgress.Prepared:
                ScenarioOrchestration.PlayMatches(context, competition, matches, count: 0);
                return;
            case SeedProgress.Running:
                ScenarioOrchestration.PlayMatches(context, competition, matches, count: matches.Count / 2);
                return;
            case SeedProgress.Finished:
                ScenarioOrchestration.PlayMatches(context, competition, matches, count: matches.Count);
                CompleteRunning(context, competition, stage);
                return;
            default:
                throw new InvalidOperationException($"Unsupported seed progress '{progress}'.");
        }
    }

    /// <summary>
    /// Swiss progress: rounds are created only after Running via GenerateNextRound.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><c>prepared</c> — Running, 0 rounds (Overview ready for GenerateNextRound).</item>
    /// <item><c>running</c> — round 1 finished + round 2 generated, half played (awaiting results).</item>
    /// <item><c>finished</c> — all planned rounds generated and finished; competition Completed.</item>
    /// </list>
    /// </remarks>
    public static void ApplySwissProgress(
        ScenarioContext context,
        Competition competition,
        Stage stage,
        SeedProgress progress)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(stage);
        if (!stage.IsSwiss || stage.SwissSettings is null)
        {
            throw new InvalidOperationException("ApplySwissProgress requires a Swiss stage with SwissSettings.");
        }

        var planned = stage.SwissSettings.RoundCount;
        var allMatches = new List<Match>();

        switch (progress)
        {
            case SeedProgress.Prepared:
                return;
            case SeedProgress.Running:
                {
                    // Round 1 complete → round 2 open with ~50% results (awaiting next GenerateNextRound).
                    var round1 = GenerateSwissRound(context, competition, stage, allMatches);
                    ScenarioOrchestration.PlayMatches(context, competition, round1, count: round1.Count);
                    var round2 = GenerateSwissRound(context, competition, stage, allMatches);
                    ScenarioOrchestration.PlayMatches(context, competition, round2, count: Math.Max(1, round2.Count / 2));
                    return;
                }

            case SeedProgress.Finished:
                {
                    for (var round = 1; round <= planned; round++)
                    {
                        var created = GenerateSwissRound(context, competition, stage, allMatches);
                        ScenarioOrchestration.PlayMatches(context, competition, created, count: created.Count);
                    }

                    CompleteRunning(context, competition, stage);
                    return;
                }

            default:
                throw new InvalidOperationException($"Unsupported seed progress '{progress}'.");
        }
    }

    internal static IReadOnlyList<Match> GenerateSwissRound(
        ScenarioContext context,
        Competition competition,
        Stage stage,
        List<Match> accumulated)
    {
        var result = GenerateNextRound.Execute(competition, stage, accumulated, context.Clock);
        foreach (var match in result.CreatedMatches)
        {
            context.Matches.Add(match);
            accumulated.Add(match);
        }

        MatchEnrichment.ApplyKickoffs(context, competition, stage, result.CreatedMatches);
        return result.CreatedMatches;
    }

    internal static void PrepareAndStartStage(ScenarioContext context, Stage stage)
    {
        stage.Prepare(context.Clock);
        stage.Start(context.Clock);
    }
}
