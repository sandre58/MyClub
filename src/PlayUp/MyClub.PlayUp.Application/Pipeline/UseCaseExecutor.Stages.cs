// -----------------------------------------------------------------------
// <copyright file="UseCaseExecutor.Stages.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Stages.CascadeReplace;
using MyClub.PlayUp.Application.Stages.ReleaseResolution;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Scheduling;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Pipeline;

/// <content>
/// Stage lifecycle, authoring, draws, materialization, and schedule commands.
/// </content>
public sealed partial class UseCaseExecutor
{
    /// <summary>
    /// Loads a stage, runs <see cref="PrepareStage"/>, and saves changes.
    /// </summary>
    /// <param name="stageId">Stage identity.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the stage is prepared and persisted.</returns>
    /// <exception cref="ApplicationFailureException">Thrown when the stage does not exist.</exception>
    public async Task PrepareStageAsync(StageId stageId, CancellationToken cancellationToken = default)
    {
        var competitionId = default(Guid);
        await ExecuteOnStageCompetitionAsync(
            stageId,
            async innerToken =>
            {
                var stage = await stages.GetByIdForUpdateAsync(stageId, innerToken).ConfigureAwait(false)
                            ?? throw new ApplicationFailureException(
                                $"Stage '{stageId}' was not found.",
                                ApplicationErrorCodes.StageNotFound);

                var competition = await competitions.GetByIdForUpdateAsync(stage.CompetitionId, innerToken)
                                      .ConfigureAwait(false)
                                  ?? throw new ApplicationFailureException(
                                      $"Competition '{stage.CompetitionId}' was not found.",
                                      ApplicationErrorCodes.CompetitionNotFound);

                EnsureCompetitionAllowsLifecycleMutation(competition);

                var competitionStages =
                    await LoadCompetitionStagesForUpdateAsync(competition, innerToken).ConfigureAwait(false);

                if (!competitionStages.Exists(candidate => candidate.Id.Equals(stageId)))
                {
                    throw new ApplicationFailureException(
                        $"Stage '{stageId}' is not part of the competition stages list.",
                        ApplicationErrorCodes.StageNotInCompetition);
                }

                // Prefer the tracked instance from the competition list (same identity as StageIds load).
                var target = competitionStages.First(candidate => candidate.Id.Equals(stageId));
                PrepareStage.Execute(target, competitionStages, clock);
                await unitOfWork.SaveChangesAsync(innerToken).ConfigureAwait(false);
                competitionId = stage.CompetitionId.Value;
            },
            cancellationToken).ConfigureAwait(false);
        LogStagePrepared(logger, stageId.Value, competitionId);
    }

    /// <summary>
    /// Loads a stage, runs <see cref="StartStage"/>, and saves changes.
    /// </summary>
    /// <param name="stageId">Stage identity.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the stage is started and persisted.</returns>
    /// <exception cref="ApplicationFailureException">Thrown when the stage does not exist.</exception>
    public async Task StartStageAsync(StageId stageId, CancellationToken cancellationToken = default)
    {
        var competitionId = default(Guid);
        await ExecuteOnStageCompetitionAsync(
            stageId,
            async innerToken =>
            {
                var stage = await stages.GetByIdForUpdateAsync(stageId, innerToken).ConfigureAwait(false)
                            ?? throw new ApplicationFailureException(
                                $"Stage '{stageId}' was not found.",
                                ApplicationErrorCodes.StageNotFound);

                var competition = await competitions.GetByIdForUpdateAsync(stage.CompetitionId, innerToken)
                                      .ConfigureAwait(false)
                                  ?? throw new ApplicationFailureException(
                                      $"Competition '{stage.CompetitionId}' was not found.",
                                      ApplicationErrorCodes.CompetitionNotFound);
                EnsureCompetitionAllowsLifecycleMutation(competition);

                var stageMatches = await matches
                    .ListByStageForUpdateAsync(stageId, innerToken)
                    .ConfigureAwait(false);
                var summaries = StageStartReadinessGuard.ToSummaryRows(stageMatches);
                var competitionStages =
                    await LoadCompetitionStagesForUpdateAsync(competition, innerToken).ConfigureAwait(false);
                StageStartReadinessGuard.EnsureReadyToStart(stage, competition, summaries, competitionStages);

                StartStage.Execute(stage, clock);
                await unitOfWork.SaveChangesAsync(innerToken).ConfigureAwait(false);
                competitionId = stage.CompetitionId.Value;
            },
            cancellationToken).ConfigureAwait(false);
        LogStageStarted(logger, stageId.Value, competitionId);
    }

    /// <summary>
    /// Loads a stage and its matches, runs <see cref="CompleteStage"/>, and saves changes.
    /// </summary>
    /// <param name="stageId">Stage identity.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the stage is completed and persisted.</returns>
    /// <exception cref="ApplicationFailureException">Thrown when the stage does not exist or completion is blocked.</exception>
    public async Task CompleteStageAsync(StageId stageId, CancellationToken cancellationToken = default)
    {
        var competitionId = default(Guid);
        await ExecuteOnStageCompetitionAsync(
            stageId,
            async innerToken =>
            {
                var stage = await stages.GetByIdForUpdateAsync(stageId, innerToken).ConfigureAwait(false)
                            ?? throw new ApplicationFailureException(
                                $"Stage '{stageId}' was not found.",
                                ApplicationErrorCodes.StageNotFound);

                var competition = await competitions.GetByIdForUpdateAsync(stage.CompetitionId, innerToken)
                                      .ConfigureAwait(false)
                                  ?? throw new ApplicationFailureException(
                                      $"Competition '{stage.CompetitionId}' was not found.",
                                      ApplicationErrorCodes.CompetitionNotFound);
                EnsureCompetitionAllowsLifecycleMutation(competition);

                var stageMatches =
                    await matches.ListByStageForUpdateAsync(stageId, innerToken).ConfigureAwait(false);
                CompleteStage.Execute(stage, stageMatches, clock);
                await unitOfWork.SaveChangesAsync(innerToken).ConfigureAwait(false);
                competitionId = stage.CompetitionId.Value;
            },
            cancellationToken).ConfigureAwait(false);
        LogStageCompleted(logger, stageId.Value, competitionId);
    }

    /// <summary>
    /// Loads a stage and reverts Ready → Draft when no matches are attached.
    /// </summary>
    public Task RevertStageToDraftAsync(StageId stageId, CancellationToken cancellationToken = default) =>
        ExecuteOnStageCompetitionAsync(
            stageId,
            async innerToken =>
            {
                var stage = await stages.GetByIdForUpdateAsync(stageId, innerToken).ConfigureAwait(false)
                            ?? throw new ApplicationFailureException(
                                $"Stage '{stageId}' was not found.",
                                ApplicationErrorCodes.StageNotFound);

                var competition = await competitions.GetByIdForUpdateAsync(stage.CompetitionId, innerToken)
                                      .ConfigureAwait(false)
                                  ?? throw new ApplicationFailureException(
                                      $"Competition '{stage.CompetitionId}' was not found.",
                                      ApplicationErrorCodes.CompetitionNotFound);
                EnsureCompetitionAllowsLifecycleMutation(competition);

                var stageMatches =
                    await matches.ListByStageForUpdateAsync(stageId, innerToken).ConfigureAwait(false);
                RevertToDraft.Execute(stage, stageMatches, clock);
                await unitOfWork.SaveChangesAsync(innerToken).ConfigureAwait(false);
            },
            cancellationToken);

    /// <summary>
    /// Loads a stage and suspends it (Running → Suspended) when no match is live.
    /// </summary>
    public Task SuspendStageAsync(StageId stageId, CancellationToken cancellationToken = default) =>
        ExecuteOnStageCompetitionAsync(
            stageId,
            async innerToken =>
            {
                var stage = await stages.GetByIdForUpdateAsync(stageId, innerToken).ConfigureAwait(false)
                            ?? throw new ApplicationFailureException(
                                $"Stage '{stageId}' was not found.",
                                ApplicationErrorCodes.StageNotFound);

                var competition = await competitions.GetByIdForUpdateAsync(stage.CompetitionId, innerToken)
                                      .ConfigureAwait(false)
                                  ?? throw new ApplicationFailureException(
                                      $"Competition '{stage.CompetitionId}' was not found.",
                                      ApplicationErrorCodes.CompetitionNotFound);
                EnsureCompetitionAllowsLifecycleMutation(competition);

                var stageMatches =
                    await matches.ListByStageForUpdateAsync(stageId, innerToken).ConfigureAwait(false);
                SuspendStage.Execute(stage, stageMatches, clock);
                await unitOfWork.SaveChangesAsync(innerToken).ConfigureAwait(false);
            },
            cancellationToken);

    /// <summary>
    /// Loads a stage and resumes it (Suspended → Running).
    /// </summary>
    public Task ResumeStageAsync(StageId stageId, CancellationToken cancellationToken = default) =>
        ExecuteOnStageCompetitionAsync(
            stageId,
            async innerToken =>
            {
                var stage = await stages.GetByIdForUpdateAsync(stageId, innerToken).ConfigureAwait(false)
                            ?? throw new ApplicationFailureException(
                                $"Stage '{stageId}' was not found.",
                                ApplicationErrorCodes.StageNotFound);

                var competition = await competitions.GetByIdForUpdateAsync(stage.CompetitionId, innerToken)
                                      .ConfigureAwait(false)
                                  ?? throw new ApplicationFailureException(
                                      $"Competition '{stage.CompetitionId}' was not found.",
                                      ApplicationErrorCodes.CompetitionNotFound);
                EnsureCompetitionAllowsLifecycleMutation(competition);

                ResumeStage.Execute(stage, clock);
                await unitOfWork.SaveChangesAsync(innerToken).ConfigureAwait(false);
            },
            cancellationToken);

    /// <summary>
    /// Loads a stage and reverts it to Ready (Running or Suspended → Ready) when allowed.
    /// </summary>
    public Task RevertStageToReadyAsync(StageId stageId, CancellationToken cancellationToken = default) =>
        ExecuteOnStageCompetitionAsync(
            stageId,
            async innerToken =>
            {
                var stage = await stages.GetByIdForUpdateAsync(stageId, innerToken).ConfigureAwait(false)
                            ?? throw new ApplicationFailureException(
                                $"Stage '{stageId}' was not found.",
                                ApplicationErrorCodes.StageNotFound);

                var competition = await competitions.GetByIdForUpdateAsync(stage.CompetitionId, innerToken)
                                      .ConfigureAwait(false)
                                  ?? throw new ApplicationFailureException(
                                      $"Competition '{stage.CompetitionId}' was not found.",
                                      ApplicationErrorCodes.CompetitionNotFound);
                EnsureCompetitionAllowsLifecycleMutation(competition);

                var stageMatches =
                    await matches.ListByStageForUpdateAsync(stageId, innerToken).ConfigureAwait(false);
                RevertToReady.Execute(stage, stageMatches, clock);
                await unitOfWork.SaveChangesAsync(innerToken).ConfigureAwait(false);
            },
            cancellationToken);

    /// <summary>
    /// Loads competition stages and fixture matches, runs <see cref="ApplyProgressionOutcome"/>, and saves once.
    /// </summary>
    /// <param name="sourceStageId">Stage that owns the fixture.</param>
    /// <param name="fixtureId">Fixture whose finished legs drive progression.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when progression is applied and persisted.</returns>
    /// <exception cref="ApplicationFailureException">Thrown when required aggregates cannot be loaded.</exception>
    public async Task ApplyProgressionOutcomeAsync(
        StageId sourceStageId,
        FixtureId fixtureId,
        CancellationToken cancellationToken = default) =>
        await ExecuteOnStageCompetitionAsync(
            sourceStageId,
            async innerToken =>
            {
                var source = await stages.GetByIdForUpdateAsync(sourceStageId, innerToken).ConfigureAwait(false)
                             ?? throw new ApplicationFailureException(
                                 $"Stage '{sourceStageId}' was not found.",
                                 ApplicationErrorCodes.StageNotFound);

                var competition = await competitions.GetByIdForUpdateAsync(source.CompetitionId, innerToken)
                                      .ConfigureAwait(false)
                                  ?? throw new ApplicationFailureException(
                                      $"Competition '{source.CompetitionId}' was not found.",
                                      ApplicationErrorCodes.CompetitionNotFound);

                EnsureCompetitionAllowsConsequenceOperation(competition);

                var competitionStages =
                    await LoadCompetitionStagesForUpdateAsync(competition, innerToken).ConfigureAwait(false);

                if (!competitionStages.Exists(candidate => candidate.Id.Equals(sourceStageId)))
                {
                    throw new ApplicationFailureException(
                        $"Stage '{sourceStageId}' is not part of the competition stages list.",
                        ApplicationErrorCodes.StageNotInCompetition);
                }

                // Fixture lookup uses the tracked source instance (same identity as competitionStages entry).
                var trackedSource = competitionStages.First(candidate => candidate.Id.Equals(sourceStageId));
                var fixture = trackedSource.GetFixture(fixtureId);
                var matchIds = fixture.Attachments.Select(attachment => attachment.MatchId).ToArray();
                var loadedMatches = await matches.GetByIdsForUpdateAsync(matchIds, innerToken).ConfigureAwait(false);
                if (loadedMatches.Count != matchIds.Length)
                {
                    var missing = matchIds.First(id => !loadedMatches.Any(match => match.Id.Equals(id)));
                    throw new ApplicationFailureException(
                        $"Match '{missing}' was not found.",
                        ApplicationErrorCodes.MatchNotFound);
                }

                var destinationStageIds = competitionStages
                    .Select(stage => stage.Id)
                    .Where(id => !id.Equals(sourceStageId))
                    .ToArray();
                var destinationMatchesByStage = destinationStageIds.Length == 0
                    ? new Dictionary<StageId, IReadOnlyList<Match>>()
                    : await matches.ListByStageIdsForUpdateAsync(destinationStageIds, innerToken)
                        .ConfigureAwait(false);
                var destinationMatches = destinationMatchesByStage.SelectMany(pair => pair.Value).ToArray();

                ApplyProgressionOutcome.Execute(
                    trackedSource,
                    fixtureId,
                    loadedMatches,
                    competitionStages,
                    clock,
                    destinationMatches);
                await unitOfWork.SaveChangesAsync(innerToken).ConfigureAwait(false);
            },
            cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Loads competition stages and source matches, calculates standings, applies qualification, and saves once.
    /// </summary>
    /// <param name="sourceStageId">Stage that owns qualification rules.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Applied slot assignment instructions.</returns>
    public async Task<IReadOnlyList<QualificationInstruction>> ApplyQualificationAsync(
        StageId sourceStageId,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<QualificationInstruction>? applied = null;
        await ExecuteOnStageCompetitionAsync(
            sourceStageId,
            async innerToken =>
            {
                var source = await stages.GetByIdForUpdateAsync(sourceStageId, innerToken).ConfigureAwait(false)
                             ?? throw new ApplicationFailureException(
                                 $"Stage '{sourceStageId}' was not found.",
                                 ApplicationErrorCodes.StageNotFound);

                var competition = await competitions.GetByIdForUpdateAsync(source.CompetitionId, innerToken)
                                      .ConfigureAwait(false)
                                  ?? throw new ApplicationFailureException(
                                      $"Competition '{source.CompetitionId}' was not found.",
                                      ApplicationErrorCodes.CompetitionNotFound);

                EnsureCompetitionAllowsConsequenceOperation(competition);

                var competitionStages =
                    await LoadCompetitionStagesForUpdateAsync(competition, innerToken).ConfigureAwait(false);
                if (!competitionStages.Exists(candidate => candidate.Id.Equals(sourceStageId)))
                {
                    throw new ApplicationFailureException(
                        $"Stage '{sourceStageId}' is not part of the competition stages list.",
                        ApplicationErrorCodes.StageNotInCompetition);
                }

                var canonicalSource = competitionStages.First(candidate => candidate.Id.Equals(sourceStageId));
                var stageMatches =
                    await matches.ListByStageForUpdateAsync(sourceStageId, innerToken).ConfigureAwait(false);
                var (overall, groupStandings) = QualificationStandingFactory.Build(
                    competition,
                    canonicalSource,
                    stageMatches);

                var destinationStageIds = competitionStages
                    .Select(stage => stage.Id)
                    .Where(id => !id.Equals(sourceStageId))
                    .ToArray();
                var destinationMatchesByStage = destinationStageIds.Length == 0
                    ? new Dictionary<StageId, IReadOnlyList<Match>>()
                    : await matches.ListByStageIdsForUpdateAsync(destinationStageIds, innerToken)
                        .ConfigureAwait(false);
                var destinationMatches = destinationMatchesByStage.SelectMany(pair => pair.Value).ToArray();

                applied = ApplyQualification.Execute(
                    canonicalSource,
                    overall,
                    groupStandings,
                    stageMatches,
                    competitionStages,
                    clock,
                    destinationMatches);
                await unitOfWork.SaveChangesAsync(innerToken).ConfigureAwait(false);
            },
            cancellationToken).ConfigureAwait(false);
        return applied ?? [];
    }

    /// <summary>
    /// Loads a stage, runs <see cref="PublishDraw"/>, and saves changes.
    /// </summary>
    /// <param name="stageId">Stage that owns the draw.</param>
    /// <param name="drawId">Draw identity.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the draw is published and persisted.</returns>
    /// <exception cref="ApplicationFailureException">Thrown when the stage does not exist.</exception>
    public async Task PublishDrawAsync(
        StageId stageId,
        DrawId drawId,
        CancellationToken cancellationToken = default)
    {
        var competitionId = default(Guid);
        await ExecuteOnStageCompetitionAsync(
            stageId,
            async innerToken =>
            {
                var stage = await stages.GetByIdForUpdateAsync(stageId, innerToken).ConfigureAwait(false)
                            ?? throw new ApplicationFailureException(
                                $"Stage '{stageId}' was not found.",
                                ApplicationErrorCodes.StageNotFound);

                await EnsureCompetitionAllowsLifecycleMutationAsync(stage.CompetitionId, innerToken)
                    .ConfigureAwait(false);

                PublishDraw.Execute(stage, drawId, clock);
                await unitOfWork.SaveChangesAsync(innerToken).ConfigureAwait(false);
                competitionId = stage.CompetitionId.Value;
            },
            cancellationToken).ConfigureAwait(false);
        LogDrawPublished(logger, stageId.Value, drawId.Value, competitionId);
    }

    /// <summary>
    /// Orchestrates Publish then Apply as two durable steps (not one Domain transaction).
    /// Publish runs under the competition lock and is committed before Apply.
    /// Tracked entities are cleared between the commits so Apply loads the stage again under its own lock.
    /// An Apply failure leaves the draw published.
    /// </summary>
    /// <param name="stageId">Stage that owns the draw.</param>
    /// <param name="drawId">Draw identity.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when both steps succeed, or fails after a durable Publish.</returns>
    /// <exception cref="ApplicationFailureException">Thrown when the stage does not exist or Apply fails.</exception>
    public async Task PublishAndApplyDrawAsync(
        StageId stageId,
        DrawId drawId,
        CancellationToken cancellationToken = default)
    {
        var competitionId = default(Guid);
        await ExecuteOnStageCompetitionAsync(
            stageId,
            async innerToken =>
            {
                var stage = await stages.GetByIdForUpdateAsync(stageId, innerToken).ConfigureAwait(false)
                            ?? throw new ApplicationFailureException(
                                $"Stage '{stageId}' was not found.",
                                ApplicationErrorCodes.StageNotFound);

                await EnsureCompetitionAllowsLifecycleMutationAsync(stage.CompetitionId, innerToken)
                    .ConfigureAwait(false);

                PublishDraw.Execute(stage, drawId, clock);
                await unitOfWork.SaveChangesAsync(innerToken).ConfigureAwait(false);
                competitionId = stage.CompetitionId.Value;
            },
            cancellationToken).ConfigureAwait(false);
        LogDrawPublished(logger, stageId.Value, drawId.Value, competitionId);

        unitOfWork.ClearTrackedChanges();
        await ApplyDrawAsync(stageId, drawId, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Loads a stage, runs <see cref="CancelDraw"/>, and saves changes.
    /// </summary>
    /// <param name="stageId">Stage that owns the draw.</param>
    /// <param name="drawId">Draw identity.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the draw is cancelled and persisted.</returns>
    /// <exception cref="ApplicationFailureException">Thrown when the stage does not exist.</exception>
    public async Task CancelDrawAsync(
        StageId stageId,
        DrawId drawId,
        CancellationToken cancellationToken = default)
    {
        var competitionId = default(Guid);
        await ExecuteOnStageCompetitionAsync(
            stageId,
            async innerToken =>
            {
                var stage = await stages.GetByIdForUpdateAsync(stageId, innerToken).ConfigureAwait(false)
                            ?? throw new ApplicationFailureException(
                                $"Stage '{stageId}' was not found.",
                                ApplicationErrorCodes.StageNotFound);

                await EnsureCompetitionAllowsLifecycleMutationAsync(stage.CompetitionId, innerToken)
                    .ConfigureAwait(false);

                CancelDraw.Execute(stage, drawId, clock);
                await unitOfWork.SaveChangesAsync(innerToken).ConfigureAwait(false);
                competitionId = stage.CompetitionId.Value;
            },
            cancellationToken).ConfigureAwait(false);
        LogDrawCancelled(logger, stageId.Value, drawId.Value, competitionId);
    }

    /// <summary>
    /// Loads a stage, runs <see cref="ReleaseDrawAlignedPlacements"/>, and saves once.
    /// </summary>
    /// <param name="stageId">Stage that owns the draw.</param>
    /// <param name="drawId">Draw whose resolution defines aligned placements.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Released vs skipped counts.</returns>
    public async Task<ReleaseDrawAlignedPlacementsDto> ReleaseDrawAlignedPlacementsAsync(
        StageId stageId,
        DrawId drawId,
        CancellationToken cancellationToken = default)
    {
        ReleaseDrawAlignedPlacementsDto? dto = null;
        await ExecuteOnStageCompetitionAsync(
            stageId,
            async innerToken =>
            {
                var stage = await stages.GetByIdForUpdateAsync(stageId, innerToken).ConfigureAwait(false)
                            ?? throw new ApplicationFailureException(
                                $"Stage '{stageId}' was not found.",
                                ApplicationErrorCodes.StageNotFound);

                await EnsureCompetitionAllowsLifecycleMutationAsync(stage.CompetitionId, innerToken)
                    .ConfigureAwait(false);

                var stageMatches = await matches.ListByStageForUpdateAsync(stageId, innerToken)
                    .ConfigureAwait(false);
                var result = ReleaseDrawAlignedPlacements.Execute(stage, drawId, clock, stageMatches);
                await unitOfWork.SaveChangesAsync(innerToken).ConfigureAwait(false);
                LogDrawAlignedPlacementsReleased(
                    logger,
                    stageId.Value,
                    drawId.Value,
                    stage.CompetitionId.Value,
                    result.ReleasedCount,
                    result.SkippedCount);
                dto = new ReleaseDrawAlignedPlacementsDto(result.ReleasedCount, result.SkippedCount);
            },
            cancellationToken).ConfigureAwait(false);
        return dto ?? new ReleaseDrawAlignedPlacementsDto(0, 0);
    }

    /// <summary>
    /// Loads a stage, runs <see cref="ApplyDraw"/>, and saves once.
    /// </summary>
    /// <param name="stageId">Stage that owns the draw.</param>
    /// <param name="drawId">Draw identity.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the draw is applied and persisted.</returns>
    /// <exception cref="ApplicationFailureException">Thrown when the stage cannot be loaded.</exception>
    public async Task ApplyDrawAsync(
        StageId stageId,
        DrawId drawId,
        CancellationToken cancellationToken = default) =>
        await ExecuteOnStageCompetitionAsync(
            stageId,
            async innerToken =>
            {
                var stage = await stages.GetByIdForUpdateAsync(stageId, innerToken).ConfigureAwait(false)
                            ?? throw new ApplicationFailureException(
                                $"Stage '{stageId}' was not found.",
                                ApplicationErrorCodes.StageNotFound);

                await EnsureCompetitionAllowsLifecycleMutationAsync(stage.CompetitionId, innerToken)
                    .ConfigureAwait(false);

                var stageMatches = await matches.ListByStageForUpdateAsync(stageId, innerToken)
                    .ConfigureAwait(false);
                var result = ApplyDraw.Execute(stage, drawId, clock, stageMatches);
                await unitOfWork.SaveChangesAsync(innerToken).ConfigureAwait(false);
                LogDrawApplied(
                    logger,
                    stageId.Value,
                    drawId.Value,
                    stage.CompetitionId.Value,
                    result.CreatedMatches.Count);
            },
            cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Loads a stage, runs <see cref="RedoDraw"/> (cancel → release → preview), and saves once.
    /// </summary>
    /// <param name="stageId">Stage that owns the draw.</param>
    /// <param name="drawId">Applied draw to redo.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The new draw's identity and generation outcome.</returns>
    public async Task<DrawGenerationDto> RedoDrawAsync(
        StageId stageId,
        DrawId drawId,
        CancellationToken cancellationToken = default)
    {
        DrawGenerationDto? dto = null;
        await ExecuteOnStageCompetitionAsync(
            stageId,
            async innerToken =>
            {
                var stage = await stages.GetByIdForUpdateAsync(stageId, innerToken).ConfigureAwait(false)
                            ?? throw new ApplicationFailureException(
                                $"Stage '{stageId}' was not found.",
                                ApplicationErrorCodes.StageNotFound);
                var competition = await RequireCompetitionAsync(stage.CompetitionId, innerToken).ConfigureAwait(false);
                EnsureCompetitionAllowsLifecycleMutation(competition);

                var competitionStages = await LoadCompetitionStagesForUpdateAsync(competition, innerToken)
                    .ConfigureAwait(false);
                var stageMatches = await matches.ListByStageForUpdateAsync(stageId, innerToken)
                    .ConfigureAwait(false);
                var result = RedoDraw.Execute(stage, drawId, clock, competitionStages, stageMatches);
                await unitOfWork.SaveChangesAsync(innerToken).ConfigureAwait(false);
                var newDraw = stage.GetDraw(result.DrawId);
                LogDrawRedone(logger, stageId.Value, drawId.Value, newDraw.Id.Value, stage.CompetitionId.Value);
                dto = new DrawGenerationDto(
                    newDraw.Id.Value,
                    result.IsResolved,
                    result.IsNoSolution,
                    newDraw.Status,
                    newDraw.Resolution.State);
            },
            cancellationToken).ConfigureAwait(false);
        return dto ?? throw new InvalidOperationException("Draw redo did not run.");
    }

    /// <summary>
    /// Adds a round (optional TieFormat) to a stage.
    /// </summary>
    public async Task<Round> AddStageRoundAsync(
        StageId stageId,
        string name,
        int? numberOfLegs,
        bool? aggregateScoring,
        CancellationToken cancellationToken = default)
    {
        Round? round = null;
        await ExecuteOnStageCompetitionAsync(
            stageId,
            async innerToken =>
            {
                var stage = await RequireStageAsync(stageId, innerToken).ConfigureAwait(false);
                var competition = await RequireCompetitionAsync(stage.CompetitionId, innerToken)
                    .ConfigureAwait(false);
                var tieFormat = AddStageRound.BuildTieFormat(numberOfLegs, aggregateScoring);
                round = AddStageRound.Execute(stage, competition, name, tieFormat, clock);
                await unitOfWork.SaveChangesAsync(innerToken).ConfigureAwait(false);
            },
            cancellationToken).ConfigureAwait(false);
        return round!;
    }

    /// <summary>
    /// Adds a positional slot to a stage.
    /// </summary>
    public async Task<Slot> AddStageSlotAsync(
        StageId stageId,
        string slotKey,
        CancellationToken cancellationToken = default)
    {
        Slot? slot = null;
        await ExecuteOnStageCompetitionAsync(
            stageId,
            async innerToken =>
            {
                var stage = await RequireStageAsync(stageId, innerToken).ConfigureAwait(false);
                var competition = await RequireCompetitionAsync(stage.CompetitionId, innerToken)
                    .ConfigureAwait(false);
                slot = AddStageSlot.Execute(stage, competition, slotKey);
                await unitOfWork.SaveChangesAsync(innerToken).ConfigureAwait(false);
            },
            cancellationToken).ConfigureAwait(false);
        return slot!;
    }

    /// <summary>
    /// Assigns an entry to a Cup slot via DirectAssignment (manual placement).
    /// Skips <c>SaveChanges</c> when the occupant is already <paramref name="entryId"/>.
    /// </summary>
    /// <param name="stageId">Stage that owns the slot.</param>
    /// <param name="slotKey">Target slot key.</param>
    /// <param name="entryId">Entry to pin.</param>
    /// <param name="expectedEntryId">Resolved occupant the caller observed, or null when the slot was free.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task AssignEntryToSlotAsync(
        StageId stageId,
        string slotKey,
        EntryId entryId,
        EntryId? expectedEntryId,
        CancellationToken cancellationToken = default) =>
        await ExecuteOnStageCompetitionAsync(
            stageId,
            async innerToken =>
            {
                var stage = await RequireStageAsync(stageId, innerToken).ConfigureAwait(false);
                var competition = await RequireCompetitionAsync(stage.CompetitionId, innerToken)
                    .ConfigureAwait(false);
                EnsureCompetitionAllowsLifecycleMutation(competition);
                var stageMatches = await matches.ListByStageForUpdateAsync(stageId, innerToken)
                    .ConfigureAwait(false);
                var mutated = AssignEntryToSlot.Execute(
                    stage,
                    slotKey,
                    entryId,
                    expectedEntryId,
                    stageMatches);
                if (mutated)
                {
                    await unitOfWork.SaveChangesAsync(innerToken).ConfigureAwait(false);
                }
            },
            cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Clears a Cup DirectAssignment (and synced occupant) when present.
    /// Skips <c>SaveChanges</c> when the slot is already free or the clear does not change the occupant.
    /// </summary>
    /// <param name="stageId">Stage that owns the slot.</param>
    /// <param name="slotKey">Target slot key.</param>
    /// <param name="expectedEntryId">Resolved occupant the caller observed, or null when the slot was free.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task ClearSlotAssignmentAsync(
        StageId stageId,
        string slotKey,
        EntryId? expectedEntryId,
        CancellationToken cancellationToken = default) =>
        await ExecuteOnStageCompetitionAsync(
            stageId,
            async innerToken =>
            {
                var stage = await RequireStageAsync(stageId, innerToken).ConfigureAwait(false);
                var competition = await RequireCompetitionAsync(stage.CompetitionId, innerToken)
                    .ConfigureAwait(false);
                EnsureCompetitionAllowsLifecycleMutation(competition);
                var stageMatches = await matches.ListByStageForUpdateAsync(stageId, innerToken)
                    .ConfigureAwait(false);
                var mutated = ClearSlotAssignment.Execute(stage, slotKey, expectedEntryId, stageMatches);
                if (mutated)
                {
                    await unitOfWork.SaveChangesAsync(innerToken).ConfigureAwait(false);
                }
            },
            cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Assigns an entry to a group (manual placement).
    /// </summary>
    public async Task AssignEntryToGroupAsync(
        StageId stageId,
        GroupId groupId,
        EntryId entryId,
        CancellationToken cancellationToken = default) =>
        await ExecuteOnStageCompetitionAsync(
            stageId,
            async innerToken =>
            {
                var stage = await RequireStageAsync(stageId, innerToken).ConfigureAwait(false);
                var competition = await RequireCompetitionAsync(stage.CompetitionId, innerToken)
                    .ConfigureAwait(false);
                EnsureCompetitionAllowsLifecycleMutation(competition);

                // Assign consults sibling qualification and progression paths (reserved places).
                // Siblings are loaded only when the competition has another phase.
                var competitionStages = await LoadSiblingStagesForManualAssignAsync(
                        competition,
                        stage,
                        innerToken)
                    .ConfigureAwait(false);
                var stageMatches = await matches.ListByStageForUpdateAsync(stageId, innerToken)
                    .ConfigureAwait(false);
                AssignEntryToGroup.Execute(
                    stage,
                    competition,
                    groupId,
                    entryId,
                    stageMatches,
                    competitionStages);
                await unitOfWork.SaveChangesAsync(innerToken).ConfigureAwait(false);
            },
            cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Clears manual group membership for an entry.
    /// </summary>
    public async Task ClearEntryFromGroupAsync(
        StageId stageId,
        GroupId groupId,
        EntryId entryId,
        CancellationToken cancellationToken = default) =>
        await ExecuteOnStageCompetitionAsync(
            stageId,
            async innerToken =>
            {
                var stage = await RequireStageAsync(stageId, innerToken).ConfigureAwait(false);
                var competition = await RequireCompetitionAsync(stage.CompetitionId, innerToken)
                    .ConfigureAwait(false);
                EnsureCompetitionAllowsLifecycleMutation(competition);

                // Clear does not load sibling phases. Reserved inbound feeds apply to assign only.
                var stageMatches = await matches.ListByStageForUpdateAsync(stageId, innerToken)
                    .ConfigureAwait(false);
                ClearEntryFromGroup.Execute(stage, competition, groupId, entryId, stageMatches);
                await unitOfWork.SaveChangesAsync(innerToken).ConfigureAwait(false);
            },
            cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Replaces the team on a group seat. The seat index is kept.
    /// </summary>
    public async Task ReplaceEntryInGroupAsync(
        StageId stageId,
        GroupId groupId,
        EntryId currentEntryId,
        EntryId replacementEntryId,
        CancellationToken cancellationToken = default) =>
        await ExecuteOnStageCompetitionAsync(
            stageId,
            async innerToken =>
            {
                var stage = await RequireStageAsync(stageId, innerToken).ConfigureAwait(false);
                var competition = await RequireCompetitionAsync(stage.CompetitionId, innerToken)
                    .ConfigureAwait(false);
                EnsureCompetitionAllowsLifecycleMutation(competition);
                var stageMatches = await matches.ListByStageForUpdateAsync(stageId, innerToken)
                    .ConfigureAwait(false);
                ReplaceEntryInGroup.Execute(
                    stage,
                    competition,
                    groupId,
                    currentEntryId,
                    replacementEntryId,
                    stageMatches);
                await unitOfWork.SaveChangesAsync(innerToken).ConfigureAwait(false);
            },
            cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Places several entries in one group, all or nothing.
    /// </summary>
    public async Task AssignEntriesToGroupAsync(
        StageId stageId,
        GroupId groupId,
        IReadOnlyList<EntryId> entryIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entryIds);
        await ExecuteOnStageCompetitionAsync(
            stageId,
            async innerToken =>
            {
                var stage = await RequireStageAsync(stageId, innerToken).ConfigureAwait(false);
                var competition = await RequireCompetitionAsync(stage.CompetitionId, innerToken)
                    .ConfigureAwait(false);
                EnsureCompetitionAllowsLifecycleMutation(competition);
                var competitionStages = await LoadSiblingStagesForManualAssignAsync(
                        competition,
                        stage,
                        innerToken)
                    .ConfigureAwait(false);
                var stageMatches = await matches.ListByStageForUpdateAsync(stageId, innerToken)
                    .ConfigureAwait(false);
                AssignEntriesToGroup.Execute(
                    stage,
                    competition,
                    groupId,
                    entryIds,
                    stageMatches,
                    competitionStages);
                await unitOfWork.SaveChangesAsync(innerToken).ConfigureAwait(false);
            },
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Atomically sets the membership of a group. Skips <c>SaveChanges</c> on a set noop.
    /// </summary>
    public async Task SetGroupEntriesAsync(
        StageId stageId,
        GroupId groupId,
        IReadOnlyList<EntryId> entryIds,
        IReadOnlyList<EntryId>? expectedEntryIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entryIds);
        await ExecuteOnStageCompetitionAsync(
            stageId,
            async innerToken =>
            {
                var stage = await RequireStageAsync(stageId, innerToken).ConfigureAwait(false);
                var competition = await RequireCompetitionAsync(stage.CompetitionId, innerToken)
                    .ConfigureAwait(false);
                EnsureCompetitionAllowsLifecycleMutation(competition);
                var competitionStages = await LoadSiblingStagesForManualAssignAsync(
                        competition,
                        stage,
                        innerToken)
                    .ConfigureAwait(false);
                var stageMatches = await matches.ListByStageForUpdateAsync(stageId, innerToken)
                    .ConfigureAwait(false);
                var mutated = SetGroupEntries.Execute(
                    stage,
                    competition,
                    groupId,
                    entryIds,
                    expectedEntryIds,
                    stageMatches,
                    competitionStages);
                if (mutated)
                {
                    await unitOfWork.SaveChangesAsync(innerToken).ConfigureAwait(false);
                }
            },
            cancellationToken).ConfigureAwait(false);
    }

    private async Task<List<Stage>> LoadSiblingStagesForManualAssignAsync(
        Competition competition,
        Stage stage,
        CancellationToken cancellationToken)
    {
        if (competition.StageIds.Count <= 1)
        {
            return [stage];
        }

        var siblingIds = competition.StageIds.Where(id => !id.Equals(stage.Id)).ToArray();
        var siblings = await stages.GetByIdsForUpdateAsync(siblingIds, cancellationToken)
            .ConfigureAwait(false);
        if (siblings.Count != siblingIds.Length)
        {
            throw new ApplicationFailureException(
                "One or more competition stages were not found.",
                ApplicationErrorCodes.StageNotFound);
        }

        List<Stage> loaded =
        [
            with(siblingIds.Length + 1), stage,
            .. siblings
        ];
        return loaded;
    }

    /// <summary>
    /// Renames a stage (locale).
    /// </summary>
    public async Task RenameStageAsync(
        StageId stageId,
        string name,
        CancellationToken cancellationToken = default) =>
        await ExecuteOnStageCompetitionAsync(
            stageId,
            async innerToken =>
            {
                var stage = await RequireStageAsync(stageId, innerToken).ConfigureAwait(false);
                RenameStage.Execute(stage, name);
                await unitOfWork.SaveChangesAsync(innerToken).ConfigureAwait(false);
            },
            cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Adds a matchday to a stage (locale).
    /// </summary>
    public async Task<Matchday> AddStageMatchdayAsync(
        StageId stageId,
        int? number,
        CancellationToken cancellationToken = default)
    {
        Matchday? matchday = null;
        await ExecuteOnStageCompetitionAsync(
            stageId,
            async innerToken =>
            {
                var stage = await RequireStageAsync(stageId, innerToken).ConfigureAwait(false);
                var competition = await RequireCompetitionAsync(stage.CompetitionId, innerToken)
                    .ConfigureAwait(false);
                matchday = AddStageMatchday.Execute(stage, competition, number, clock);
                await unitOfWork.SaveChangesAsync(innerToken).ConfigureAwait(false);
            },
            cancellationToken).ConfigureAwait(false);
        return matchday!;
    }

    /// <summary>
    /// Adds a group to a stage (locale).
    /// </summary>
    public async Task<Group> AddStageGroupAsync(
        StageId stageId,
        string? name,
        CancellationToken cancellationToken = default)
    {
        Group? group = null;
        await ExecuteOnStageCompetitionAsync(
            stageId,
            async innerToken =>
            {
                var stage = await RequireStageAsync(stageId, innerToken).ConfigureAwait(false);
                var competition = await RequireCompetitionAsync(stage.CompetitionId, innerToken)
                    .ConfigureAwait(false);
                group = AddStageGroup.Execute(stage, competition, name, clock);
                await unitOfWork.SaveChangesAsync(innerToken).ConfigureAwait(false);
            },
            cancellationToken).ConfigureAwait(false);
        return group!;
    }

    /// <summary>
    /// Sets Championship / Groups match generation format (locale).
    /// </summary>
    public async Task ReplaceStageMatchGenerationFormatAsync(
        StageId stageId,
        MatchGenerationFormat format,
        CancellationToken cancellationToken = default)
    {
        var stage = await RequireStageAsync(stageId, cancellationToken).ConfigureAwait(false);
        var competition = await RequireCompetitionAsync(stage.CompetitionId, cancellationToken)
            .ConfigureAwait(false);
        ReplaceStageMatchGenerationFormat.Execute(stage, competition, format);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sets Swiss planned round count K (locale).
    /// </summary>
    public async Task ReplaceStageSwissSettingsAsync(
        StageId stageId,
        int roundCount,
        CancellationToken cancellationToken = default)
    {
        var stage = await RequireStageAsync(stageId, cancellationToken).ConfigureAwait(false);
        var competition = await RequireCompetitionAsync(stage.CompetitionId, cancellationToken)
            .ConfigureAwait(false);
        ReplaceStageSwissSettings.Execute(stage, competition, roundCount);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Replaces progression rules on a stage (null/empty clears).
    /// </summary>
    public async Task ReplaceStageProgressionRulesAsync(
        StageId stageId,
        IReadOnlyList<ProgressionPathSpec>? paths,
        CancellationToken cancellationToken = default)
    {
        var stage = await RequireStageAsync(stageId, cancellationToken).ConfigureAwait(false);
        StructureOperationalExitEditGuard.EnsureStatusAllowsExitEditWithoutMatches(stage, "Progression rules");
        var competition = await RequireCompetitionAsync(stage.CompetitionId, cancellationToken)
            .ConfigureAwait(false);
        var competitionStages = await LoadCompetitionStagesForUpdateAsync(competition, cancellationToken)
            .ConfigureAwait(false);
        var stageMatches = await matches.ListByStageForUpdateAsync(stageId, cancellationToken)
            .ConfigureAwait(false);
        var destinationMatches = await LoadDestinationMatchesForProgressionReplaceAsync(
                stageId,
                competitionStages,
                cancellationToken)
            .ConfigureAwait(false);
        ReplaceStageProgressionRules.Execute(
            stage,
            paths,
            clock,
            competitionStages,
            stageMatches,
            destinationMatches);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Replaces progression rules from authoring intents (null/empty clears).
    /// </summary>
    public async Task ReplaceStageProgressionIntentsAsync(
        StageId stageId,
        IReadOnlyList<ProgressionIntentSpec>? intents,
        CancellationToken cancellationToken = default)
    {
        var stage = await RequireStageAsync(stageId, cancellationToken).ConfigureAwait(false);
        StructureOperationalExitEditGuard.EnsureStatusAllowsExitEditWithoutMatches(stage, "Progression rules");
        var competition = await RequireCompetitionAsync(stage.CompetitionId, cancellationToken)
            .ConfigureAwait(false);
        var competitionStages = await LoadCompetitionStagesForUpdateAsync(competition, cancellationToken)
            .ConfigureAwait(false);
        var stageMatches = await matches.ListByStageForUpdateAsync(stageId, cancellationToken)
            .ConfigureAwait(false);
        var destinationMatches = await LoadDestinationMatchesForProgressionReplaceAsync(
                stageId,
                competitionStages,
                cancellationToken)
            .ConfigureAwait(false);
        ReplaceStageProgressionRules.Execute(
            stage,
            intents,
            clock,
            competitionStages,
            stageMatches,
            destinationMatches);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Replaces qualification rules on a stage from legacy path specs (null/empty clears).
    /// A <see cref="StageStatus.Running"/> stage with no qualification Apply on its own graph
    /// issues <see cref="ICompetitionRepository.CountStagesReadOnlyAsync"/> before the refusal.
    /// That query belongs to this write path, not to Structure reads.
    /// </summary>
    public async Task ReplaceStageQualificationRulesAsync(
        StageId stageId,
        IReadOnlyList<QualificationPathSpec>? paths,
        CancellationToken cancellationToken = default)
    {
        var stage = await RequireStageAsync(stageId, cancellationToken).ConfigureAwait(false);
        var (competitionStages, stageMatches) =
            await LoadQualificationReplaceScopeAsync(stage, cancellationToken).ConfigureAwait(false);
        ReplaceStageQualificationRules.Execute(stage, paths, clock, competitionStages, stageMatches);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Replaces qualification rules from authoring intents (null/empty clears).
    /// A <see cref="StageStatus.Running"/> stage with no qualification Apply on its own graph
    /// issues <see cref="ICompetitionRepository.CountStagesReadOnlyAsync"/> before the refusal.
    /// That query belongs to this write path, not to Structure reads.
    /// </summary>
    public async Task ReplaceStageQualificationIntentsAsync(
        StageId stageId,
        IReadOnlyList<QualificationIntentSpec>? intents,
        CancellationToken cancellationToken = default)
    {
        var stage = await RequireStageAsync(stageId, cancellationToken).ConfigureAwait(false);
        var (competitionStages, stageMatches) =
            await LoadQualificationReplaceScopeAsync(stage, cancellationToken).ConfigureAwait(false);
        ReplaceStageQualificationRules.Execute(stage, intents, clock, competitionStages, stageMatches);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Loads the stages needed to see a qualification Apply. A Running or Completed stage with no Apply
    /// on its own graph refuses before the competition aggregate and the match graph when it is the only stage.
    /// The extra <see cref="ICompetitionRepository.CountStagesReadOnlyAsync"/> query runs on this write path
    /// when the stage is <see cref="StageStatus.Running"/> and stores no qualification Apply.
    /// </summary>
    private async Task<(List<Stage> Stages, IReadOnlyList<Match> Matches)> LoadQualificationReplaceScopeAsync(
        Stage stage,
        CancellationToken cancellationToken)
    {
        if (await RefuseQualificationReplaceBeforeScopeLoadAsync(stage, cancellationToken).ConfigureAwait(false))
        {
            StructureOperationalExitEditGuard.EnsureStatusAllowsExitEditWithoutMatches(stage, "Qualification rules");
        }

        var competition = await RequireCompetitionAsync(stage.CompetitionId, cancellationToken)
            .ConfigureAwait(false);
        var competitionStages = competition.StageIds.Count == 1 && competition.StageIds[0].Equals(stage.Id)
            ? [stage]
            : await LoadCompetitionStagesForUpdateAsync(competition, cancellationToken).ConfigureAwait(false);
        if (stage.Status is StageStatus.Completed
            || !QualificationResolutionApplyWitness.Exists(stage, competitionStages))
        {
            StructureOperationalExitEditGuard.EnsureStatusAllowsExitEditWithoutMatches(stage, "Qualification rules");
        }

        var stageMatches = await matches.ListByStageForUpdateAsync(stage.Id, cancellationToken)
            .ConfigureAwait(false);
        return (competitionStages, stageMatches);
    }

    /// <summary>
    /// True when status forbids the edit and no sibling stage can hold the qualification Apply witness.
    /// Calls <see cref="ICompetitionRepository.CountStagesReadOnlyAsync"/> only on this write path,
    /// for a <see cref="StageStatus.Running"/> stage with no qualification Apply on its own graph.
    /// </summary>
    private async Task<bool> RefuseQualificationReplaceBeforeScopeLoadAsync(
        Stage stage,
        CancellationToken cancellationToken)
    {
        if (stage.Status is StageStatus.Completed)
        {
            return true;
        }

        if (stage.Status is StageStatus.Draft or StageStatus.Ready
            || QualificationResolutionApplyWitness.Exists(stage, [stage]))
        {
            return false;
        }

        var stageCount = await competitions
            .CountStagesReadOnlyAsync(stage.CompetitionId, cancellationToken)
            .ConfigureAwait(false);
        return stageCount == 1;
    }

    /// <summary>
    /// Replaces placement award rules on a stage (null/empty clears). Configuration only — no award resolution.
    /// </summary>
    public async Task ReplaceStagePlacementAwardRulesAsync(
        StageId stageId,
        IReadOnlyList<PlacementAwardPathSpec>? paths,
        CancellationToken cancellationToken = default)
    {
        var stage = await RequireStageAsync(stageId, cancellationToken).ConfigureAwait(false);
        ReplaceStagePlacementAwardRules.Execute(stage, paths, clock);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Replaces StandingRules on a classifying stage (allowed after Start). No standing recalculation write-side.
    /// </summary>
    public async Task ReplaceStageStandingRulesAsync(
        StageId stageId,
        StandingRules standingRules,
        CancellationToken cancellationToken = default)
    {
        var stage = await RequireStageAsync(stageId, cancellationToken).ConfigureAwait(false);
        var competition = await RequireCompetitionAsync(stage.CompetitionId, cancellationToken)
            .ConfigureAwait(false);
        ReplaceStageStandingRules.Execute(stage, competition, standingRules, clock);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Specializes MatchRules on a stage (unbinds changed heritable parts).
    /// </summary>
    public async Task ReplaceStageMatchRulesAsync(
        StageId stageId,
        MatchRules matchRules,
        CancellationToken cancellationToken = default)
    {
        var stage = await RequireStageAsync(stageId, cancellationToken).ConfigureAwait(false);
        var competition = await RequireCompetitionAsync(stage.CompetitionId, cancellationToken)
            .ConfigureAwait(false);
        ReplaceStageMatchRules.Execute(stage, competition, matchRules, clock);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Rebinds Match or Standing heritable parts to Competition defaults.
    /// </summary>
    public async Task BindStageRegulationAsync(
        StageId stageId,
        string scope,
        CancellationToken cancellationToken = default)
    {
        var stage = await RequireStageAsync(stageId, cancellationToken).ConfigureAwait(false);
        var competition = await RequireCompetitionAsync(stage.CompetitionId, cancellationToken)
            .ConfigureAwait(false);
        BindStageRegulation.Execute(stage, competition, scope, clock);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Replaces or clears DrawRules on a stage.
    /// </summary>
    public async Task ReplaceStageDrawRulesAsync(
        StageId stageId,
        DrawRules? drawRules,
        CancellationToken cancellationToken = default)
    {
        var stage = await RequireStageAsync(stageId, cancellationToken).ConfigureAwait(false);
        var competition = await RequireCompetitionAsync(stage.CompetitionId, cancellationToken)
            .ConfigureAwait(false);
        ReplaceStageDrawRules.Execute(stage, competition, drawRules, clock);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Replaces Affectation authoring on a stage (syncs runtime CompositionEntries by diff).
    /// </summary>
    public async Task ReplaceStageAffectationAuthoringAsync(
        StageId stageId,
        IReadOnlyList<EntryId> entryIds,
        CancellationToken cancellationToken = default) =>
        await ExecuteOnStageCompetitionAsync(
            stageId,
            async innerToken =>
            {
                var stage = await RequireStageAsync(stageId, innerToken).ConfigureAwait(false);
                var competition = await RequireCompetitionAsync(stage.CompetitionId, innerToken)
                    .ConfigureAwait(false);
                EnsureCompetitionAllowsLifecycleMutation(competition);
                var competitionStages = await LoadCompetitionStagesForUpdateAsync(competition, innerToken)
                    .ConfigureAwait(false);
                var stageMatches = await matches.ListByStageForUpdateAsync(stageId, innerToken)
                    .ConfigureAwait(false);
                ReplaceStageAffectationAuthoring.Execute(
                    stage,
                    competition,
                    entryIds,
                    clock,
                    competitionStages,
                    stageMatches);
                await unitOfWork.SaveChangesAsync(innerToken).ConfigureAwait(false);
            },
            cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Replaces or clears the stage default TieFormat.
    /// </summary>
    public async Task ReplaceStageDefaultTieFormatAsync(
        StageId stageId,
        TieFormat? tieFormat,
        CancellationToken cancellationToken = default)
    {
        var stage = await RequireStageAsync(stageId, cancellationToken).ConfigureAwait(false);
        var competition = await RequireCompetitionAsync(stage.CompetitionId, cancellationToken)
            .ConfigureAwait(false);
        ReplaceStageDefaultTieFormat.Execute(stage, competition, tieFormat, clock);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Replaces or clears a Round's TieFormat.
    /// </summary>
    public async Task ReplaceRoundTieFormatAsync(
        StageId stageId,
        RoundId roundId,
        TieFormat? tieFormat,
        CancellationToken cancellationToken = default)
    {
        var stage = await RequireStageAsync(stageId, cancellationToken).ConfigureAwait(false);
        var competition = await RequireCompetitionAsync(stage.CompetitionId, cancellationToken)
            .ConfigureAwait(false);
        ReplaceRoundTieFormat.Execute(stage, competition, roundId, tieFormat, clock);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Creates a Draft Draw on a stage.
    /// </summary>
    /// <param name="stageId">Stage that owns the draw.</param>
    /// <param name="kind">Resolution kind.</param>
    /// <param name="intent">
    /// Applied on create. Groups <see cref="DrawInputsIntent.Rerun"/> refuses an occupied grid before any draft exists.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<DrawSummaryDto> CreateDrawAsync(
        StageId stageId,
        DrawResolutionKind kind,
        DrawInputsIntent intent = DrawInputsIntent.Default,
        CancellationToken cancellationToken = default)
    {
        DrawSummaryDto? dto = null;
        await ExecuteOnStageCompetitionAsync(
            stageId,
            async innerToken =>
            {
                var stage = await RequireStageAsync(stageId, innerToken).ConfigureAwait(false);
                var competition = await RequireCompetitionAsync(stage.CompetitionId, innerToken)
                    .ConfigureAwait(false);
                EnsureCompetitionAllowsLifecycleMutation(competition);
                var competitionStages = await LoadCompetitionStagesForUpdateAsync(competition, innerToken)
                    .ConfigureAwait(false);
                var draw = CreateDraw.Execute(
                    stage,
                    kind,
                    clock,
                    competitionStages: competitionStages,
                    intent: intent);
                await unitOfWork.SaveChangesAsync(innerToken).ConfigureAwait(false);
                dto = ToDrawSummary(stage.Id, draw);
            },
            cancellationToken).ConfigureAwait(false);
        return dto ?? throw new InvalidOperationException("Draw create did not run.");
    }

    /// <summary>
    /// Configures default draw inputs from phase CompositionEntries.
    /// </summary>
    /// <param name="stageId">Stage that owns the draw.</param>
    /// <param name="drawId">Draw to configure.</param>
    /// <param name="intent">
    /// <see cref="DrawInputsIntent.Default"/> keeps Fixed* from occupancy.
    /// <see cref="DrawInputsIntent.Rerun"/> = full redraw, Fixed* empty.
    /// <see cref="DrawInputsIntent.Partial"/> draws unplaced teams into free slots only.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<DrawSummaryDto> ConfigureDrawInputsAsync(
        StageId stageId,
        DrawId drawId,
        DrawInputsIntent intent = DrawInputsIntent.Default,
        CancellationToken cancellationToken = default)
    {
        DrawSummaryDto? dto = null;
        await ExecuteOnStageCompetitionAsync(
            stageId,
            async innerToken =>
            {
                var stage = await RequireStageAsync(stageId, innerToken).ConfigureAwait(false);
                var competition = await RequireCompetitionAsync(stage.CompetitionId, innerToken)
                    .ConfigureAwait(false);
                EnsureCompetitionAllowsLifecycleMutation(competition);
                var competitionStages = await LoadCompetitionStagesForUpdateAsync(competition, innerToken)
                    .ConfigureAwait(false);
                var draw = stage.GetDraw(drawId);
                var inputs = intent == DrawInputsIntent.Partial
                    ? DrawInputsFactory.CreatePartial(stage, competitionStages, draw.Kind)
                    : DrawInputsFactory.CreateDefault(stage, draw.Kind, intent);
                ConfigureDrawInputs.Execute(stage, drawId, inputs, intent, competitionStages);
                await unitOfWork.SaveChangesAsync(innerToken).ConfigureAwait(false);
                dto = ToDrawSummary(stage.Id, stage.GetDraw(drawId));
            },
            cancellationToken).ConfigureAwait(false);
        return dto ?? throw new InvalidOperationException("Draw input configuration did not run.");
    }

    /// <summary>
    /// Generates a draw resolution (or NoSolution) for a Draft draw.
    /// </summary>
    public async Task<DrawGenerationDto> GenerateDrawAsync(
        StageId stageId,
        DrawId drawId,
        CancellationToken cancellationToken = default)
    {
        DrawGenerationDto? dto = null;
        await ExecuteOnStageCompetitionAsync(
            stageId,
            async innerToken =>
            {
                var stage = await RequireStageAsync(stageId, innerToken).ConfigureAwait(false);
                var competition = await RequireCompetitionAsync(stage.CompetitionId, innerToken)
                    .ConfigureAwait(false);
                EnsureCompetitionAllowsLifecycleMutation(competition);
                var competitionStages = await LoadCompetitionStagesForUpdateAsync(competition, innerToken)
                    .ConfigureAwait(false);
                var draw = stage.GetDraw(drawId);
                IReadOnlyList<string>? slotTargets = draw.Kind == DrawResolutionKind.Slot
                    ? stage.Slots.Select(slot => slot.SlotKey).ToArray()
                    : null;
                IReadOnlyList<GroupId>? groupTargets = draw.Kind == DrawResolutionKind.Group
                    ? stage.Groups.Select(group => group.Id).ToArray()
                    : null;
                var result = GenerateDrawResolution.Execute(
                    stage,
                    drawId,
                    clock,
                    slotTargets,
                    groupTargets,
                    competitionStages: competitionStages);
                await unitOfWork.SaveChangesAsync(innerToken).ConfigureAwait(false);
                var updated = stage.GetDraw(drawId);
                dto = new DrawGenerationDto(
                    updated.Id.Value,
                    result.IsResolved,
                    result.IsNoSolution,
                    updated.Status,
                    updated.Resolution.State);
            },
            cancellationToken).ConfigureAwait(false);
        return dto ?? throw new InvalidOperationException("Draw generation did not run.");
    }

    /// <summary>
    /// Materializes Fixtures/Matches for the stage format (Championship / Groups).
    /// </summary>
    public async Task<MaterializeMatchesResult> MaterializeMatchesAsync(
        StageId stageId,
        CancellationToken cancellationToken = default)
    {
        MaterializeMatchesResult? result = null;
        await ExecuteOnStageCompetitionAsync(
            stageId,
            async innerToken =>
            {
                var stage = await RequireStageAsync(stageId, innerToken).ConfigureAwait(false);
                var competition = await RequireCompetitionAsync(stage.CompetitionId, innerToken)
                    .ConfigureAwait(false);
                var existing = await matches.ListByStageForUpdateAsync(stageId, innerToken).ConfigureAwait(false);
                var competitionStages =
                    await LoadCompetitionStagesForUpdateAsync(competition, innerToken).ConfigureAwait(false);
                var materialized = MaterializeMatches.Execute(
                    competition,
                    stage,
                    existing,
                    clock,
                    competitionStages);
                foreach (var created in materialized.CreatedMatches)
                {
                    matches.Add(created);
                }

                await unitOfWork.SaveChangesAsync(innerToken).ConfigureAwait(false);
                result = materialized;
            },
            cancellationToken).ConfigureAwait(false);
        return result ?? throw new InvalidOperationException("Match materialization did not run.");
    }

    /// <summary>
    /// Materializes Cup Fixtures/Matches from occupied bracket pairs.
    /// </summary>
    public async Task<MaterializeCupFromOccupiedSlotsResult> MaterializeCupFromOccupiedSlotsAsync(
        StageId stageId,
        IReadOnlyList<string>? pairKeys,
        bool planEntireRound = false,
        CancellationToken cancellationToken = default)
    {
        MaterializeCupFromOccupiedSlotsResult? result = null;
        await ExecuteOnStageCompetitionAsync(
            stageId,
            async innerToken =>
            {
                var stage = await RequireStageAsync(stageId, innerToken).ConfigureAwait(false);
                var competition = await RequireCompetitionAsync(stage.CompetitionId, innerToken)
                    .ConfigureAwait(false);
                var existing = await matches.ListByStageForUpdateAsync(stageId, innerToken).ConfigureAwait(false);
                var competitionStages = await LoadCompetitionStagesForUpdateAsync(competition, innerToken)
                    .ConfigureAwait(false);
                var matchSummariesByStage =
                    competition.StageIds.Count == 0
                        ? new Dictionary<StageId, IReadOnlyList<MatchSummaryRow>>()
                        : await matches
                            .ListSummaryRowsByStageIdsReadOnlyAsync(competition.StageIds, innerToken)
                            .ConfigureAwait(false);
                var materialized = MaterializeCupFromOccupiedSlots.Execute(
                    competition,
                    stage,
                    pairKeys,
                    existing,
                    clock,
                    competitionStages,
                    matchSummariesByStage,
                    planEntireRound);
                foreach (var created in materialized.CreatedMatches)
                {
                    matches.Add(created);
                }

                await unitOfWork.SaveChangesAsync(innerToken).ConfigureAwait(false);
                result = materialized;
            },
            cancellationToken).ConfigureAwait(false);
        return result ?? throw new InvalidOperationException("Cup materialization did not run.");
    }

    /// <summary>
    /// Removes attached materialization for the stage format (Championship / Groups / Cup / Swiss last round).
    /// </summary>
    public async Task<DematerializePhaseResult> DematerializePhaseAsync(
        StageId stageId,
        CancellationToken cancellationToken = default)
    {
        DematerializePhaseResult? result = null;
        await ExecuteOnStageCompetitionAsync(
            stageId,
            async innerToken =>
            {
                var stage = await RequireStageAsync(stageId, innerToken).ConfigureAwait(false);
                var competition = await RequireCompetitionAsync(stage.CompetitionId, innerToken)
                    .ConfigureAwait(false);
                var existing = await matches.ListByStageForUpdateAsync(stageId, innerToken).ConfigureAwait(false);
                var format = StructureSkeleton.InferFormat(stage)
                             ?? throw new ApplicationFailureException(
                                 "Dematerialization requires a structured stage.",
                                 ApplicationErrorCodes.DematerializationFailure);

                result = format switch
                {
                    StructureFormatKind.Championship or StructureFormatKind.Groups => new DematerializePhaseResult(
                        DematerializeChampionshipOrGroups.Execute(competition, stage, existing, matches, clock)
                            .RemovedMatchCount,
                        RemovedRoundNumber: null),
                    StructureFormatKind.Cup => new DematerializePhaseResult(
                        DematerializeCup.Execute(competition, stage, existing, matches, clock).RemovedMatchCount,
                        RemovedRoundNumber: null),
                    StructureFormatKind.Swiss => DematerializeSwissLastRound.Execute(
                        competition,
                        stage,
                        existing,
                        matches,
                        clock) switch
                    {
                        var swiss => new DematerializePhaseResult(swiss.RemovedMatchCount, swiss.RemovedRoundNumber)
                    },
                    _ => throw new ApplicationFailureException(
                        $"Dematerialization is not supported for format '{format}'.",
                        ApplicationErrorCodes.DematerializationFailure)
                };

                await unitOfWork.SaveChangesAsync(innerToken).ConfigureAwait(false);
            },
            cancellationToken).ConfigureAwait(false);
        return result ?? throw new InvalidOperationException("Phase dematerialization did not run.");
    }

    /// <summary>
    /// Generates the next Swiss round (pairings + Matchday) while the stage is Running.
    /// </summary>
    public async Task<GenerateNextRoundResult> GenerateNextRoundAsync(
        StageId stageId,
        CancellationToken cancellationToken = default)
    {
        GenerateNextRoundResult? result = null;
        await ExecuteOnStageCompetitionAsync(
            stageId,
            async innerToken =>
            {
                var stage = await RequireStageAsync(stageId, innerToken).ConfigureAwait(false);
                var competition = await RequireCompetitionAsync(stage.CompetitionId, innerToken)
                    .ConfigureAwait(false);
                var existing = await matches.ListByStageForUpdateAsync(stageId, innerToken).ConfigureAwait(false);
                var competitionStages =
                    await LoadCompetitionStagesForUpdateAsync(competition, innerToken).ConfigureAwait(false);
                result = GenerateNextRound.Execute(competition, stage, existing, clock, competitionStages);
                foreach (var created in result.CreatedMatches)
                {
                    matches.Add(created);
                }

                await unitOfWork.SaveChangesAsync(innerToken).ConfigureAwait(false);
            },
            cancellationToken).ConfigureAwait(false);
        return result ?? throw new InvalidOperationException("Swiss next round generation did not run.");
    }

    /// <summary>
    /// Generates a schedule proposal without mutating the Stage.
    /// </summary>
    public async Task<ScheduleProposalDto> GenerateScheduleAsync(
        StageId stageId,
        DateTimeOffset horizonStart,
        DateTimeOffset horizonEnd,
        int granularityMinutes,
        string timeZoneId,
        IReadOnlyList<Guid>? targetMatchIds,
        IReadOnlyList<Guid> resourceIds,
        int matchDurationMinutes,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(resourceIds);
        if (resourceIds.Count == 0)
        {
            throw new ApplicationFailureException(
                "GenerateSchedule requires at least one resource.",
                ApplicationErrorCodes.ScheduleGenerationFailure);
        }

        var stage = await RequireStageAsync(stageId, cancellationToken).ConfigureAwait(false);
        StagePhaseExploitationGuard.EnsureScheduleExploitation(stage);
        var loadedMatches = await matches.ListByStageForUpdateAsync(stageId, cancellationToken).ConfigureAwait(false);
        var targets = ResolveScheduleTargets(stage, loadedMatches, targetMatchIds);
        var duration = new SchedulingDuration(matchDurationMinutes);
        var horizon = new Horizon(horizonStart, horizonEnd);
        var window = new TimeWindow(horizonStart, horizonEnd);
        var matchContexts = loadedMatches
            .Where(match => targets.Contains(match.Id) || stage.MatchPlacements.Any(p => p.MatchId.Equals(match.Id)))
            .Select(match => new MatchSchedulingContext(
                match.Id,
                duration,
                allowedStartWindows: null,
                allowedResourceIds: null,
                imposedStart: null,
                home: ToSchedulingParticipant(match.HomeEntryId),
                away: ToSchedulingParticipant(match.AwayEntryId)))
            .ToArray();

        // Ensure every target has a context.
        var contextIds = matchContexts.Select(context => context.MatchId).ToHashSet();
        matchContexts =
            (from target in targets
             where !contextIds.Contains(target)
             select loadedMatches.FirstOrDefault(candidate => candidate.Id.Equals(target)) ??
                    throw new ApplicationFailureException($"Match '{target}' was not found for scheduling.",
                        ApplicationErrorCodes.MatchNotFound)).Aggregate(matchContexts,
                (current, match) =>
                [
                    .. current,
                    new MatchSchedulingContext(
                        match.Id,
                        duration,
                        home: ToSchedulingParticipant(match.HomeEntryId),
                        away: ToSchedulingParticipant(match.AwayEntryId))
                ]);

        var resources = resourceIds
            .Select(id => new ResourceSchedulingContext(new ResourceId(id), [window]))
            .ToArray();

        var result = GenerateSchedule.Execute(
            stage,
            targets,
            horizon,
            new TimeGranularity(granularityMinutes),
            timeZoneId,
            matchContexts,
            resources);

        return ToScheduleProposal(result);
    }

    /// <summary>
    /// Applies a successful schedule proposal onto Stage placements.
    /// </summary>
    public async Task ApplyScheduleAsync(
        StageId stageId,
        IReadOnlyList<ScheduleAssignmentDto> assignments,
        IReadOnlyList<Guid> targetMatchIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(assignments);
        ArgumentNullException.ThrowIfNull(targetMatchIds);

        await ExecuteOnStageCompetitionAsync(
            stageId,
            async innerToken =>
            {
                var stage = await RequireStageAsync(stageId, innerToken).ConfigureAwait(false);
                await EnsureCompetitionAllowsLifecycleMutationAsync(stage.CompetitionId, innerToken)
                    .ConfigureAwait(false);
                StagePhaseExploitationGuard.EnsureScheduleExploitation(stage);
                var schedule = new Schedule(
                [
                    .. assignments.Select(a => new ScheduleAssignment(
                        new MatchId(a.MatchId),
                        new ResourceId(a.ResourceId),
                        a.Start))
                ]);
                var result = SchedulingResult.Success(schedule);
                var targets = targetMatchIds.Select(id => new MatchId(id)).ToArray();
                ApplySchedule.Execute(stage, result, targets);
                await unitOfWork.SaveChangesAsync(innerToken).ConfigureAwait(false);
            },
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Backfills persisted resolution provenance for all stages of a competition (idempotent rattrapage).
    /// </summary>
    /// <param name="competitionId">Competition identity.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Counter report for the run.</returns>
    public async Task<BackfillResolutionProvenanceReport> BackfillResolutionProvenanceAsync(
        CompetitionId competitionId,
        CancellationToken cancellationToken = default)
    {
        var competition = await competitions.GetByIdForUpdateAsync(competitionId, cancellationToken)
                              .ConfigureAwait(false)
                          ?? throw new ApplicationFailureException(
                              $"Competition '{competitionId}' was not found.",
                              ApplicationErrorCodes.CompetitionNotFound);

        var competitionStages =
            await LoadCompetitionStagesForUpdateAsync(competition, cancellationToken).ConfigureAwait(false);
        var stageIds = competitionStages.Select(stage => stage.Id).ToArray();
        var matchesByStage = stageIds.Length == 0
            ? new Dictionary<StageId, IReadOnlyList<Match>>()
            : await matches.ListByStageIdsForUpdateAsync(stageIds, cancellationToken).ConfigureAwait(false);

        var report = BackfillResolutionProvenance.Execute(
            competition,
            competitionStages,
            matchesByStage);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return report;
    }

    /// <summary>
    /// Previews a cascade replacement without persisting it.
    /// </summary>
    public async Task<CascadeReplaceResult> PreviewCascadeReplaceAsync(
        StageId stageId,
        CascadeReplaceTarget target,
        CancellationToken cancellationToken = default)
    {
        var (competition, stageList, matchList) =
            await LoadCascadeReadAsync(stageId, cancellationToken).ConfigureAwait(false);
        return CascadeReplace.Plan(competition, stageId, target, stageList, matchList, clock);
    }

    /// <summary>
    /// Confirms a cascade replacement when <paramref name="versionToken"/> still matches, then saves once.
    /// </summary>
    public async Task<CascadeReplaceResult> ConfirmCascadeReplaceAsync(
        StageId stageId,
        CascadeReplaceTarget target,
        string versionToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(versionToken))
        {
            throw new ApplicationFailureException(
                "A cascade confirmation requires the preview version token.",
                ApplicationErrorCodes.CascadeReplaceConflict);
        }

        var competitionId = await stages.GetCompetitionIdReadOnlyAsync(stageId, cancellationToken)
                                .ConfigureAwait(false)
                            ?? throw new ApplicationFailureException(
                                $"Stage '{stageId}' was not found.",
                                ApplicationErrorCodes.StageNotFound);
        CascadeReplaceResult? result = null;
        await unitOfWork.ExecuteExclusiveAsync(
            competitionId,
            async innerToken =>
            {
                var (competition, stageList, matchList) =
                    await LoadCascadeForUpdateAsync(stageId, innerToken).ConfigureAwait(false);
                var current = CascadeReplaceVersion.Compute(stageList, matchList);
                if (!string.Equals(current, versionToken, StringComparison.Ordinal))
                {
                    throw new ApplicationFailureException(
                        "The cascade was confirmed against a state that has changed.",
                        ApplicationErrorCodes.CascadeReplaceConflict);
                }

                result = CascadeReplace.Execute(
                    competition,
                    stageId,
                    target,
                    stageList,
                    matchList,
                    clock);
                await unitOfWork.SaveChangesAsync(innerToken).ConfigureAwait(false);
            },
            cancellationToken).ConfigureAwait(false);
        return result ?? throw new InvalidOperationException("Cascade confirmation did not run.");
    }

    /// <summary>
    /// Previews a resolution release without persisting it.
    /// </summary>
    public async Task<ResolutionReleaseResult> PreviewResolutionReleaseAsync(
        StageId stageId,
        ResolutionReleaseTarget target,
        CancellationToken cancellationToken = default)
    {
        var (competition, stageList, matchList) =
            await LoadCascadeReadAsync(stageId, cancellationToken).ConfigureAwait(false);
        return ReleaseResolution.Plan(competition, stageId, target, stageList, matchList, clock);
    }

    /// <summary>
    /// Confirms a resolution release when <paramref name="versionToken"/> still matches, then saves once.
    /// </summary>
    public async Task<ResolutionReleaseResult> ConfirmResolutionReleaseAsync(
        StageId stageId,
        ResolutionReleaseTarget target,
        string versionToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(versionToken))
        {
            throw new ApplicationFailureException(
                "A release confirmation requires the preview version token.",
                ApplicationErrorCodes.ResolutionReleaseConflict);
        }

        var competitionId = await stages.GetCompetitionIdReadOnlyAsync(stageId, cancellationToken)
                                .ConfigureAwait(false)
                            ?? throw new ApplicationFailureException(
                                $"Stage '{stageId}' was not found.",
                                ApplicationErrorCodes.StageNotFound);
        ResolutionReleaseResult? result = null;
        await unitOfWork.ExecuteExclusiveAsync(
            competitionId,
            async innerToken =>
            {
                var (competition, stageList, matchList) =
                    await LoadCascadeForUpdateAsync(stageId, innerToken).ConfigureAwait(false);
                var current = CascadeReplaceVersion.Compute(stageList, matchList);
                if (!string.Equals(current, versionToken, StringComparison.Ordinal))
                {
                    throw new ApplicationFailureException(
                        "The release was confirmed against a state that has changed.",
                        ApplicationErrorCodes.ResolutionReleaseConflict);
                }

                result = ReleaseResolution.Execute(
                    competition,
                    stageId,
                    target,
                    stageList,
                    matchList,
                    clock);
                await unitOfWork.SaveChangesAsync(innerToken).ConfigureAwait(false);
            },
            cancellationToken).ConfigureAwait(false);
        return result ?? throw new InvalidOperationException("Resolution release did not run.");
    }

    /// <summary>
    /// Previews a grouped release of definitive places fed by <paramref name="sourceId"/> without persisting it.
    /// </summary>
    public async Task<DefinitiveResolutionReleaseResult> PreviewDefinitiveResolutionReleaseAsync(
        StageId sourceId,
        StageId? destinationId,
        CancellationToken cancellationToken = default)
    {
        var (competition, stageList, matchList) =
            await LoadCascadeReadAsync(sourceId, cancellationToken).ConfigureAwait(false);
        return ReleaseDefinitiveResolutions.Plan(
            competition,
            sourceId,
            destinationId,
            stageList,
            matchList,
            clock);
    }

    /// <summary>
    /// Confirms a grouped release when the token and the place list still match, then saves once.
    /// </summary>
    public async Task<DefinitiveResolutionReleaseResult> ConfirmDefinitiveResolutionReleaseAsync(
        StageId sourceId,
        StageId? destinationId,
        string versionToken,
        IReadOnlyList<DefinitiveResolutionReleasePlace> places,
        CancellationToken cancellationToken = default)
    {
        var competitionId = await stages.GetCompetitionIdReadOnlyAsync(sourceId, cancellationToken)
                                .ConfigureAwait(false)
                            ?? throw new ApplicationFailureException(
                                $"Stage '{sourceId}' was not found.",
                                ApplicationErrorCodes.StageNotFound);
        DefinitiveResolutionReleaseResult? result = null;
        await unitOfWork.ExecuteExclusiveAsync(
            competitionId,
            async innerToken =>
            {
                var (competition, stageList, matchList) =
                    await LoadCascadeForUpdateAsync(sourceId, innerToken).ConfigureAwait(false);
                result = ReleaseDefinitiveResolutions.Execute(
                    competition,
                    sourceId,
                    destinationId,
                    versionToken,
                    places,
                    stageList,
                    matchList,
                    clock);
                await unitOfWork.SaveChangesAsync(innerToken).ConfigureAwait(false);
            },
            cancellationToken).ConfigureAwait(false);
        return result ?? throw new InvalidOperationException("Definitive resolution release did not run.");
    }

    /// <summary>
    /// Previews a grouped release of definitive places whose source phase was deleted.
    /// The route is the surviving destination. A missing destination is not found.
    /// </summary>
    public async Task<DefinitiveResolutionReleaseResult> PreviewDeletedSourceDefinitiveReleaseAsync(
        StageId destinationId,
        CancellationToken cancellationToken = default)
    {
        var (competition, stageList, matchList) =
            await LoadCascadeReadAsync(destinationId, cancellationToken).ConfigureAwait(false);
        return ReleaseDefinitiveResolutions.Plan(
            competition,
            null,
            destinationId,
            stageList,
            matchList,
            clock);
    }

    /// <summary>
    /// Confirms that deleted-source release when the token and the place list still match, then saves once.
    /// </summary>
    public async Task<DefinitiveResolutionReleaseResult> ConfirmDeletedSourceDefinitiveReleaseAsync(
        StageId destinationId,
        string versionToken,
        IReadOnlyList<DefinitiveResolutionReleasePlace> places,
        CancellationToken cancellationToken = default)
    {
        var competitionId = await stages.GetCompetitionIdReadOnlyAsync(destinationId, cancellationToken)
                                .ConfigureAwait(false)
                            ?? throw new ApplicationFailureException(
                                $"Stage '{destinationId}' was not found.",
                                ApplicationErrorCodes.StageNotFound);
        DefinitiveResolutionReleaseResult? result = null;
        await unitOfWork.ExecuteExclusiveAsync(
            competitionId,
            async innerToken =>
            {
                var (competition, stageList, matchList) =
                    await LoadCascadeForUpdateAsync(destinationId, innerToken).ConfigureAwait(false);
                result = ReleaseDefinitiveResolutions.Execute(
                    competition,
                    null,
                    destinationId,
                    versionToken,
                    places,
                    stageList,
                    matchList,
                    clock);
                await unitOfWork.SaveChangesAsync(innerToken).ConfigureAwait(false);
            },
            cancellationToken).ConfigureAwait(false);
        return result ?? throw new InvalidOperationException("Definitive resolution release did not run.");
    }

    private async Task<(Competition Competition, List<Stage> Stages, List<Match> Matches)>
        LoadCascadeReadAsync(StageId stageId, CancellationToken cancellationToken)
    {
        var competitionId = await stages.GetCompetitionIdReadOnlyAsync(stageId, cancellationToken)
                                .ConfigureAwait(false)
                            ?? throw new ApplicationFailureException(
                                $"Stage '{stageId}' was not found.",
                                ApplicationErrorCodes.StageNotFound);
        var competition = await competitions.GetByIdReadOnlyAsync(competitionId, cancellationToken)
                              .ConfigureAwait(false)
                          ?? throw new ApplicationFailureException(
                              $"Competition '{competitionId}' was not found.",
                              ApplicationErrorCodes.CompetitionNotFound);
        var stageList = await LoadCompetitionStagesReadOnlyAsync(
            competition,
            StageReadCapabilities.FromProfile(StageLoadProfile.Full),
            cancellationToken).ConfigureAwait(false);
        var grouped = await matches.ListByStageIdsReadOnlyAsync(
            [.. stageList.Select(stage => stage.Id)],
            cancellationToken).ConfigureAwait(false);
        return (competition, stageList, [.. grouped.SelectMany(pair => pair.Value)]);
    }

    private async Task<(Competition Competition, List<Stage> Stages, List<Match> Matches)>
        LoadCascadeForUpdateAsync(StageId stageId, CancellationToken cancellationToken)
    {
        var origin = await stages.GetByIdForUpdateAsync(stageId, cancellationToken).ConfigureAwait(false)
                     ?? throw new ApplicationFailureException(
                         $"Stage '{stageId}' was not found.",
                         ApplicationErrorCodes.StageNotFound);
        var competition = await competitions.GetByIdForUpdateAsync(origin.CompetitionId, cancellationToken)
                              .ConfigureAwait(false)
                          ?? throw new ApplicationFailureException(
                              $"Competition '{origin.CompetitionId}' was not found.",
                              ApplicationErrorCodes.CompetitionNotFound);
        EnsureCompetitionAllowsConsequenceOperation(competition);
        var stageList = await LoadCompetitionStagesForUpdateAsync(competition, cancellationToken)
            .ConfigureAwait(false);
        var grouped = await matches.ListByStageIdsForUpdateAsync(
            [.. stageList.Select(stage => stage.Id)],
            cancellationToken).ConfigureAwait(false);
        return (competition, stageList, [.. grouped.SelectMany(pair => pair.Value)]);
    }
}
