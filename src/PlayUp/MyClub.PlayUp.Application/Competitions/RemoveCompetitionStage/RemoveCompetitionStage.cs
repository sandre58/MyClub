// -----------------------------------------------------------------------
// <copyright file="RemoveCompetitionStage.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Competitions;

/// <summary>
/// Application use case: detach and delete a stage, scrubbing inbound Qualif/Prog dependencies.
/// </summary>
public static class RemoveCompetitionStage
{
    /// <summary>
    /// Removes <paramref name="target"/> from the competition and clears peer paths that targeted it.
    /// </summary>
    /// <param name="competition">Owning competition.</param>
    /// <param name="target">Stage to remove (must be in <paramref name="peerStages"/>).</param>
    /// <param name="peerStages">All competition stages loaded for update (including target).</param>
    /// <param name="clock">Clock for domain events.</param>
    /// <returns>Impact summary (paths scrubbed on peer stages).</returns>
    public static RemoveCompetitionStageResult Execute(
        Competition competition,
        Stage target,
        IReadOnlyList<Stage> peerStages,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(peerStages);
        ArgumentNullException.ThrowIfNull(clock);

        if (competition.Status is CompetitionStatus.Running
            or CompetitionStatus.Suspended
            or CompetitionStatus.Completed
            or CompetitionStatus.Archived)
        {
            throw new ApplicationFailureException(
                $"Stages cannot be removed while competition status is '{competition.Status}'.",
                ApplicationErrorCodes.StructureNotMutable);
        }

        if (target.Status is StageStatus.Running or StageStatus.Suspended or StageStatus.Completed)
        {
            throw new ApplicationFailureException(
                $"Stage '{target.Id}' cannot be removed while status is '{target.Status}'.",
                ApplicationErrorCodes.StructureNotMutable);
        }

        if (!competition.StageIds.Contains(target.Id))
        {
            throw new ApplicationFailureException(
                $"Stage '{target.Id}' is not part of competition '{competition.Id}'.",
                ApplicationErrorCodes.StageNotInCompetition);
        }

        if (competition.StageIds.Count <= 1)
        {
            throw new ApplicationFailureException(
                "The last stage of a competition cannot be removed.",
                ApplicationErrorCodes.LastStageCannotBeRemoved);
        }

        if (CountAttachedMatches(target) > 0)
        {
            throw new ApplicationFailureException(
                $"Stage '{target.Id}' cannot be removed while matches are attached.",
                ApplicationErrorCodes.StructureNotMutable);
        }

        var scrubbedQualificationPaths = 0;
        var scrubbedProgressionPaths = 0;
        foreach (var peer in peerStages)
        {
            if (peer.Id.Equals(target.Id))
            {
                continue;
            }

            scrubbedQualificationPaths += ScrubQualificationPathsTargeting(peer, target.Id, clock);
            scrubbedProgressionPaths += ScrubProgressionPathsTargeting(peer, target.Id, clock);
        }

        competition.RemoveStage(target.Id, clock);
        return new RemoveCompetitionStageResult(
            target.Id,
            scrubbedQualificationPaths,
            scrubbedProgressionPaths);
    }

    private static int ScrubQualificationPathsTargeting(Stage peer, StageId removedId, IClock clock)
    {
        if (peer.Regulation.QualificationRules is not { } rules)
        {
            return 0;
        }

        var kept = rules.Paths
            .Where(path => !path.Destination.StageId.Equals(removedId))
            .Select(path => path.Copy())
            .ToArray();
        var removed = rules.Paths.Count - kept.Length;
        if (removed == 0)
        {
            return 0;
        }

        peer.ReplaceQualificationRules(
            kept.Length == 0 ? null : new QualificationRules(kept),
            clock);
        return removed;
    }

    private static int ScrubProgressionPathsTargeting(Stage peer, StageId removedId, IClock clock)
    {
        if (peer.Regulation.ProgressionRules is not { } rules)
        {
            return 0;
        }

        var kept = rules.Paths
            .Where(path => !path.Destination.StageId.Equals(removedId))
            .Select(path => path.Copy())
            .ToArray();
        var removed = rules.Paths.Count - kept.Length;
        if (removed == 0)
        {
            return 0;
        }

        peer.ReplaceProgressionRules(
            kept.Length == 0 ? null : new ProgressionRules(kept),
            clock);
        return removed;
    }

    private static int CountAttachedMatches(Stage stage) =>
        stage.Matchdays.SelectMany(matchday => matchday.Fixtures)
            .Concat(stage.Rounds.SelectMany(round => round.Fixtures))
            .SelectMany(fixture => fixture.MatchIds)
            .Distinct()
            .Count();
}

/// <summary>
/// Impact of removing a stage (peer path scrubbing).
/// </summary>
/// <param name="RemovedStageId">Identity of the removed stage.</param>
/// <param name="ScrubbedQualificationPaths">Qualification paths cleared on peer stages.</param>
/// <param name="ScrubbedProgressionPaths">Progression paths cleared on peer stages.</param>
public sealed record RemoveCompetitionStageResult(
    StageId RemovedStageId,
    int ScrubbedQualificationPaths,
    int ScrubbedProgressionPaths);
