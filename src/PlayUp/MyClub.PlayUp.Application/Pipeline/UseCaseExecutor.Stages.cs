// -----------------------------------------------------------------------
// <copyright file="UseCaseExecutor.Stages.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Domain.Common;
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
        var stage = await stages.GetByIdForUpdateAsync(stageId, cancellationToken).ConfigureAwait(false)
                    ?? throw new ApplicationFailureException(
                        $"Stage '{stageId}' was not found.",
                        ApplicationErrorCodes.StageNotFound);

        var competition = await competitions.GetByIdForUpdateAsync(stage.CompetitionId, cancellationToken)
                              .ConfigureAwait(false)
                          ?? throw new ApplicationFailureException(
                              $"Competition '{stage.CompetitionId}' was not found.",
                              ApplicationErrorCodes.CompetitionNotFound);

        EnsureCompetitionAllowsLifecycleMutation(competition);

        var competitionStages =
            await LoadCompetitionStagesForUpdateAsync(competition, cancellationToken).ConfigureAwait(false);

        if (!competitionStages.Exists(candidate => candidate.Id.Equals(stageId)))
        {
            throw new ApplicationFailureException(
                $"Stage '{stageId}' is not part of the competition stages list.",
                ApplicationErrorCodes.StageNotInCompetition);
        }

        // Prefer the tracked instance from the competition list (same identity as StageIds load).
        var target = competitionStages.First(candidate => candidate.Id.Equals(stageId));
        PrepareStage.Execute(target, competitionStages, clock);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        LogStagePrepared(logger, stageId.Value, stage.CompetitionId.Value);
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
        var stage = await stages.GetByIdForUpdateAsync(stageId, cancellationToken).ConfigureAwait(false)
                    ?? throw new ApplicationFailureException(
                        $"Stage '{stageId}' was not found.",
                        ApplicationErrorCodes.StageNotFound);

        var competition = await competitions.GetByIdForUpdateAsync(stage.CompetitionId, cancellationToken)
                              .ConfigureAwait(false)
                          ?? throw new ApplicationFailureException(
                              $"Competition '{stage.CompetitionId}' was not found.",
                              ApplicationErrorCodes.CompetitionNotFound);
        EnsureCompetitionAllowsLifecycleMutation(competition);

        var stageMatches = await matches
            .ListByStageForUpdateAsync(stageId, cancellationToken)
            .ConfigureAwait(false);
        var summaries = StageStartReadinessGuard.ToSummaryRows(stageMatches);
        var competitionStages =
            await LoadCompetitionStagesForUpdateAsync(competition, cancellationToken).ConfigureAwait(false);
        StageStartReadinessGuard.EnsureReadyToStart(stage, competition, summaries, competitionStages);

        StartStage.Execute(stage, clock);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        LogStageStarted(logger, stageId.Value, stage.CompetitionId.Value);
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
        var stage = await stages.GetByIdForUpdateAsync(stageId, cancellationToken).ConfigureAwait(false)
                    ?? throw new ApplicationFailureException(
                        $"Stage '{stageId}' was not found.",
                        ApplicationErrorCodes.StageNotFound);

        var competition = await competitions.GetByIdForUpdateAsync(stage.CompetitionId, cancellationToken)
                              .ConfigureAwait(false)
                          ?? throw new ApplicationFailureException(
                              $"Competition '{stage.CompetitionId}' was not found.",
                              ApplicationErrorCodes.CompetitionNotFound);
        EnsureCompetitionAllowsLifecycleMutation(competition);

        var stageMatches =
            await matches.ListByStageForUpdateAsync(stageId, cancellationToken).ConfigureAwait(false);
        CompleteStage.Execute(stage, stageMatches, clock);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        LogStageCompleted(logger, stageId.Value, stage.CompetitionId.Value);
    }

    /// <summary>
    /// Loads a stage and reverts Ready → Draft when no matches are attached.
    /// </summary>
    public async Task RevertStageToDraftAsync(StageId stageId, CancellationToken cancellationToken = default)
    {
        var stage = await stages.GetByIdForUpdateAsync(stageId, cancellationToken).ConfigureAwait(false)
                    ?? throw new ApplicationFailureException(
                        $"Stage '{stageId}' was not found.",
                        ApplicationErrorCodes.StageNotFound);

        var competition = await competitions.GetByIdForUpdateAsync(stage.CompetitionId, cancellationToken)
                              .ConfigureAwait(false)
                          ?? throw new ApplicationFailureException(
                              $"Competition '{stage.CompetitionId}' was not found.",
                              ApplicationErrorCodes.CompetitionNotFound);
        EnsureCompetitionAllowsLifecycleMutation(competition);

        var stageMatches =
            await matches.ListByStageForUpdateAsync(stageId, cancellationToken).ConfigureAwait(false);
        RevertToDraft.Execute(stage, stageMatches, clock);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Loads a stage and suspends it (Running → Suspended) when no match is live.
    /// </summary>
    public async Task SuspendStageAsync(StageId stageId, CancellationToken cancellationToken = default)
    {
        var stage = await stages.GetByIdForUpdateAsync(stageId, cancellationToken).ConfigureAwait(false)
                    ?? throw new ApplicationFailureException(
                        $"Stage '{stageId}' was not found.",
                        ApplicationErrorCodes.StageNotFound);

        var competition = await competitions.GetByIdForUpdateAsync(stage.CompetitionId, cancellationToken)
                              .ConfigureAwait(false)
                          ?? throw new ApplicationFailureException(
                              $"Competition '{stage.CompetitionId}' was not found.",
                              ApplicationErrorCodes.CompetitionNotFound);
        EnsureCompetitionAllowsLifecycleMutation(competition);

        var stageMatches =
            await matches.ListByStageForUpdateAsync(stageId, cancellationToken).ConfigureAwait(false);
        SuspendStage.Execute(stage, stageMatches, clock);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Loads a stage and resumes it (Suspended → Running).
    /// </summary>
    public async Task ResumeStageAsync(StageId stageId, CancellationToken cancellationToken = default)
    {
        var stage = await stages.GetByIdForUpdateAsync(stageId, cancellationToken).ConfigureAwait(false)
                    ?? throw new ApplicationFailureException(
                        $"Stage '{stageId}' was not found.",
                        ApplicationErrorCodes.StageNotFound);

        var competition = await competitions.GetByIdForUpdateAsync(stage.CompetitionId, cancellationToken)
                              .ConfigureAwait(false)
                          ?? throw new ApplicationFailureException(
                              $"Competition '{stage.CompetitionId}' was not found.",
                              ApplicationErrorCodes.CompetitionNotFound);
        EnsureCompetitionAllowsLifecycleMutation(competition);

        ResumeStage.Execute(stage, clock);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Loads a stage and reverts it to Ready (Running or Suspended → Ready) when allowed.
    /// </summary>
    public async Task RevertStageToReadyAsync(StageId stageId, CancellationToken cancellationToken = default)
    {
        var stage = await stages.GetByIdForUpdateAsync(stageId, cancellationToken).ConfigureAwait(false)
                    ?? throw new ApplicationFailureException(
                        $"Stage '{stageId}' was not found.",
                        ApplicationErrorCodes.StageNotFound);

        var competition = await competitions.GetByIdForUpdateAsync(stage.CompetitionId, cancellationToken)
                              .ConfigureAwait(false)
                          ?? throw new ApplicationFailureException(
                              $"Competition '{stage.CompetitionId}' was not found.",
                              ApplicationErrorCodes.CompetitionNotFound);
        EnsureCompetitionAllowsLifecycleMutation(competition);

        var stageMatches =
            await matches.ListByStageForUpdateAsync(stageId, cancellationToken).ConfigureAwait(false);
        RevertToReady.Execute(stage, stageMatches, clock);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

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
        CancellationToken cancellationToken = default)
    {
        var source = await stages.GetByIdForUpdateAsync(sourceStageId, cancellationToken).ConfigureAwait(false)
                     ?? throw new ApplicationFailureException(
                         $"Stage '{sourceStageId}' was not found.",
                         ApplicationErrorCodes.StageNotFound);

        var competition = await competitions.GetByIdForUpdateAsync(source.CompetitionId, cancellationToken)
                              .ConfigureAwait(false)
                          ?? throw new ApplicationFailureException(
                              $"Competition '{source.CompetitionId}' was not found.",
                              ApplicationErrorCodes.CompetitionNotFound);

        EnsureCompetitionAllowsConsequenceOperation(competition);

        var competitionStages =
            await LoadCompetitionStagesForUpdateAsync(competition, cancellationToken).ConfigureAwait(false);

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
        var loadedMatches = await matches.GetByIdsForUpdateAsync(matchIds, cancellationToken).ConfigureAwait(false);
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
            : await matches.ListByStageIdsForUpdateAsync(destinationStageIds, cancellationToken)
                .ConfigureAwait(false);
        var destinationMatches = destinationMatchesByStage.SelectMany(pair => pair.Value).ToArray();

        ApplyProgressionOutcome.Execute(
            trackedSource,
            fixtureId,
            loadedMatches,
            competitionStages,
            clock,
            destinationMatches);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

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
        var source = await stages.GetByIdForUpdateAsync(sourceStageId, cancellationToken).ConfigureAwait(false)
                     ?? throw new ApplicationFailureException(
                         $"Stage '{sourceStageId}' was not found.",
                         ApplicationErrorCodes.StageNotFound);

        var competition = await competitions.GetByIdForUpdateAsync(source.CompetitionId, cancellationToken)
                              .ConfigureAwait(false)
                          ?? throw new ApplicationFailureException(
                              $"Competition '{source.CompetitionId}' was not found.",
                              ApplicationErrorCodes.CompetitionNotFound);

        EnsureCompetitionAllowsConsequenceOperation(competition);

        var competitionStages =
            await LoadCompetitionStagesForUpdateAsync(competition, cancellationToken).ConfigureAwait(false);
        if (!competitionStages.Exists(candidate => candidate.Id.Equals(sourceStageId)))
        {
            throw new ApplicationFailureException(
                $"Stage '{sourceStageId}' is not part of the competition stages list.",
                ApplicationErrorCodes.StageNotInCompetition);
        }

        var canonicalSource = competitionStages.First(candidate => candidate.Id.Equals(sourceStageId));
        var stageMatches =
            await matches.ListByStageForUpdateAsync(sourceStageId, cancellationToken).ConfigureAwait(false);
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
            : await matches.ListByStageIdsForUpdateAsync(destinationStageIds, cancellationToken)
                .ConfigureAwait(false);
        var destinationMatches = destinationMatchesByStage.SelectMany(pair => pair.Value).ToArray();

        var applied = ApplyQualification.Execute(
            canonicalSource,
            overall,
            groupStandings,
            stageMatches,
            competitionStages,
            clock,
            destinationMatches);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return applied;
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
        var stage = await stages.GetByIdForUpdateAsync(stageId, cancellationToken).ConfigureAwait(false)
                    ?? throw new ApplicationFailureException(
                        $"Stage '{stageId}' was not found.",
                        ApplicationErrorCodes.StageNotFound);

        await EnsureCompetitionAllowsLifecycleMutationAsync(stage.CompetitionId, cancellationToken)
            .ConfigureAwait(false);

        PublishDraw.Execute(stage, drawId, clock);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        LogDrawPublished(logger, stageId.Value, drawId.Value, stage.CompetitionId.Value);
    }

    /// <summary>
    /// Orchestrates Publish then Apply as two durable steps (not one Domain transaction).
    /// Publish is committed before Apply runs so an Apply failure leaves Published + not applied
    /// (recoverable state), not a rolled-back draft.
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
        await PublishDrawAsync(stageId, drawId, cancellationToken).ConfigureAwait(false);
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
        var stage = await stages.GetByIdForUpdateAsync(stageId, cancellationToken).ConfigureAwait(false)
                    ?? throw new ApplicationFailureException(
                        $"Stage '{stageId}' was not found.",
                        ApplicationErrorCodes.StageNotFound);

        await EnsureCompetitionAllowsLifecycleMutationAsync(stage.CompetitionId, cancellationToken)
            .ConfigureAwait(false);

        CancelDraw.Execute(stage, drawId, clock);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        LogDrawCancelled(logger, stageId.Value, drawId.Value, stage.CompetitionId.Value);
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
        var stage = await stages.GetByIdForUpdateAsync(stageId, cancellationToken).ConfigureAwait(false)
                    ?? throw new ApplicationFailureException(
                        $"Stage '{stageId}' was not found.",
                        ApplicationErrorCodes.StageNotFound);

        await EnsureCompetitionAllowsLifecycleMutationAsync(stage.CompetitionId, cancellationToken)
            .ConfigureAwait(false);

        var stageMatches = await matches.ListByStageForUpdateAsync(stageId, cancellationToken)
            .ConfigureAwait(false);
        var result = ReleaseDrawAlignedPlacements.Execute(stage, drawId, clock, stageMatches);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        LogDrawAlignedPlacementsReleased(
            logger,
            stageId.Value,
            drawId.Value,
            stage.CompetitionId.Value,
            result.ReleasedCount,
            result.SkippedCount);
        return new ReleaseDrawAlignedPlacementsDto(result.ReleasedCount, result.SkippedCount);
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
        CancellationToken cancellationToken = default)
    {
        var stage = await stages.GetByIdForUpdateAsync(stageId, cancellationToken).ConfigureAwait(false)
                    ?? throw new ApplicationFailureException(
                        $"Stage '{stageId}' was not found.",
                        ApplicationErrorCodes.StageNotFound);

        await EnsureCompetitionAllowsLifecycleMutationAsync(stage.CompetitionId, cancellationToken)
            .ConfigureAwait(false);

        var stageMatches = await matches.ListByStageForUpdateAsync(stageId, cancellationToken)
            .ConfigureAwait(false);
        var result = ApplyDraw.Execute(stage, drawId, clock, stageMatches);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        LogDrawApplied(logger, stageId.Value, drawId.Value, stage.CompetitionId.Value, result.CreatedMatches.Count);
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
        var stage = await RequireStageAsync(stageId, cancellationToken).ConfigureAwait(false);
        var competition = await RequireCompetitionAsync(stage.CompetitionId, cancellationToken)
            .ConfigureAwait(false);
        var tieFormat = AddStageRound.BuildTieFormat(numberOfLegs, aggregateScoring);
        var round = AddStageRound.Execute(stage, competition, name, tieFormat, clock);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return round;
    }

    /// <summary>
    /// Adds a positional slot to a stage.
    /// </summary>
    public async Task<Slot> AddStageSlotAsync(
        StageId stageId,
        string slotKey,
        CancellationToken cancellationToken = default)
    {
        var stage = await RequireStageAsync(stageId, cancellationToken).ConfigureAwait(false);
        var competition = await RequireCompetitionAsync(stage.CompetitionId, cancellationToken)
            .ConfigureAwait(false);
        var slot = AddStageSlot.Execute(stage, competition, slotKey);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return slot;
    }

    /// <summary>
    /// Assigns an entry to a Cup slot via DirectAssignment (manual placement).
    /// </summary>
    public async Task AssignEntryToSlotAsync(
        StageId stageId,
        string slotKey,
        EntryId entryId,
        CancellationToken cancellationToken = default)
    {
        var stage = await RequireStageAsync(stageId, cancellationToken).ConfigureAwait(false);
        await EnsureCompetitionAllowsLifecycleMutationAsync(stage.CompetitionId, cancellationToken)
            .ConfigureAwait(false);
        var stageMatches = await matches.ListByStageForUpdateAsync(stageId, cancellationToken)
            .ConfigureAwait(false);
        AssignEntryToSlot.Execute(stage, slotKey, entryId, stageMatches);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Clears a Cup DirectAssignment (and synced occupant) when present.
    /// </summary>
    public async Task ClearSlotAssignmentAsync(
        StageId stageId,
        string slotKey,
        CancellationToken cancellationToken = default)
    {
        var stage = await RequireStageAsync(stageId, cancellationToken).ConfigureAwait(false);
        await EnsureCompetitionAllowsLifecycleMutationAsync(stage.CompetitionId, cancellationToken)
            .ConfigureAwait(false);
        var stageMatches = await matches.ListByStageForUpdateAsync(stageId, cancellationToken)
            .ConfigureAwait(false);
        ClearSlotAssignment.Execute(stage, slotKey, stageMatches);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Renames a stage (locale).
    /// </summary>
    public async Task RenameStageAsync(
        StageId stageId,
        string name,
        CancellationToken cancellationToken = default)
    {
        var stage = await RequireStageAsync(stageId, cancellationToken).ConfigureAwait(false);
        RenameStage.Execute(stage, name);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Adds a matchday to a stage (locale).
    /// </summary>
    public async Task<Matchday> AddStageMatchdayAsync(
        StageId stageId,
        int? number,
        CancellationToken cancellationToken = default)
    {
        var stage = await RequireStageAsync(stageId, cancellationToken).ConfigureAwait(false);
        var competition = await RequireCompetitionAsync(stage.CompetitionId, cancellationToken)
            .ConfigureAwait(false);
        var matchday = AddStageMatchday.Execute(stage, competition, number, clock);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return matchday;
    }

    /// <summary>
    /// Adds a group to a stage (locale).
    /// </summary>
    public async Task<Group> AddStageGroupAsync(
        StageId stageId,
        string? name,
        CancellationToken cancellationToken = default)
    {
        var stage = await RequireStageAsync(stageId, cancellationToken).ConfigureAwait(false);
        var competition = await RequireCompetitionAsync(stage.CompetitionId, cancellationToken)
            .ConfigureAwait(false);
        var group = AddStageGroup.Execute(stage, competition, name, clock);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return group;
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
    /// </summary>
    public async Task ReplaceStageQualificationRulesAsync(
        StageId stageId,
        IReadOnlyList<QualificationPathSpec>? paths,
        CancellationToken cancellationToken = default)
    {
        var stage = await RequireStageAsync(stageId, cancellationToken).ConfigureAwait(false);
        StructureOperationalExitEditGuard.EnsureStatusAllowsExitEditWithoutMatches(stage, "Qualification rules");
        var competition = await RequireCompetitionAsync(stage.CompetitionId, cancellationToken)
            .ConfigureAwait(false);
        var competitionStages = await LoadCompetitionStagesForUpdateAsync(competition, cancellationToken)
            .ConfigureAwait(false);
        var stageMatches = await matches.ListByStageForUpdateAsync(stageId, cancellationToken)
            .ConfigureAwait(false);
        ReplaceStageQualificationRules.Execute(stage, paths, clock, competitionStages, stageMatches);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Replaces qualification rules from authoring intents (null/empty clears).
    /// </summary>
    public async Task ReplaceStageQualificationIntentsAsync(
        StageId stageId,
        IReadOnlyList<QualificationIntentSpec>? intents,
        CancellationToken cancellationToken = default)
    {
        var stage = await RequireStageAsync(stageId, cancellationToken).ConfigureAwait(false);
        StructureOperationalExitEditGuard.EnsureStatusAllowsExitEditWithoutMatches(stage, "Qualification rules");
        var competition = await RequireCompetitionAsync(stage.CompetitionId, cancellationToken)
            .ConfigureAwait(false);
        var competitionStages = await LoadCompetitionStagesForUpdateAsync(competition, cancellationToken)
            .ConfigureAwait(false);
        var stageMatches = await matches.ListByStageForUpdateAsync(stageId, cancellationToken)
            .ConfigureAwait(false);
        ReplaceStageQualificationRules.Execute(stage, intents, clock, competitionStages, stageMatches);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
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
        CancellationToken cancellationToken = default)
    {
        var stage = await RequireStageAsync(stageId, cancellationToken).ConfigureAwait(false);
        var competition = await RequireCompetitionAsync(stage.CompetitionId, cancellationToken)
            .ConfigureAwait(false);
        EnsureCompetitionAllowsLifecycleMutation(competition);
        var competitionStages = await LoadCompetitionStagesForUpdateAsync(competition, cancellationToken)
            .ConfigureAwait(false);
        var stageMatches = await matches.ListByStageForUpdateAsync(stageId, cancellationToken)
            .ConfigureAwait(false);
        ReplaceStageAffectationAuthoring.Execute(
            stage,
            competition,
            entryIds,
            clock,
            competitionStages,
            stageMatches);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

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
    public async Task<DrawSummaryDto> CreateDrawAsync(
        StageId stageId,
        DrawResolutionKind kind,
        CancellationToken cancellationToken = default)
    {
        var stage = await RequireStageAsync(stageId, cancellationToken).ConfigureAwait(false);
        await EnsureCompetitionAllowsLifecycleMutationAsync(stage.CompetitionId, cancellationToken)
            .ConfigureAwait(false);
        var draw = CreateDraw.Execute(stage, kind, clock);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return ToDrawSummary(stage.Id, draw);
    }

    /// <summary>
    /// Configures default draw inputs from phase CompositionEntries.
    /// </summary>
    /// <param name="stageId">Stage that owns the draw.</param>
    /// <param name="drawId">Draw to configure.</param>
    /// <param name="intent">
    /// <see cref="DrawInputsIntent.Default"/> keeps Fixed* from occupancy.
    /// <see cref="DrawInputsIntent.Rerun"/> = full redraw, Fixed* empty.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<DrawSummaryDto> ConfigureDrawInputsAsync(
        StageId stageId,
        DrawId drawId,
        DrawInputsIntent intent = DrawInputsIntent.Default,
        CancellationToken cancellationToken = default)
    {
        var stage = await RequireStageAsync(stageId, cancellationToken).ConfigureAwait(false);
        var competition = await RequireCompetitionAsync(stage.CompetitionId, cancellationToken).ConfigureAwait(false);
        EnsureCompetitionAllowsLifecycleMutation(competition);
        var draw = stage.GetDraw(drawId);
        IReadOnlyList<Stage>? competitionStages = null;
        if (intent == DrawInputsIntent.Rerun)
        {
            competitionStages = await LoadCompetitionStagesForUpdateAsync(competition, cancellationToken)
                .ConfigureAwait(false);
        }

        var inputs = DrawInputsFactory.CreateDefault(stage, draw.Kind, intent);
        ConfigureDrawInputs.Execute(stage, drawId, inputs, intent, competitionStages);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return ToDrawSummary(stage.Id, stage.GetDraw(drawId));
    }

    /// <summary>
    /// Generates a draw resolution (or NoSolution) for a Draft draw.
    /// </summary>
    public async Task<DrawGenerationDto> GenerateDrawAsync(
        StageId stageId,
        DrawId drawId,
        CancellationToken cancellationToken = default)
    {
        var stage = await RequireStageAsync(stageId, cancellationToken).ConfigureAwait(false);
        await EnsureCompetitionAllowsLifecycleMutationAsync(stage.CompetitionId, cancellationToken)
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
            groupTargets);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        var updated = stage.GetDraw(drawId);
        return new DrawGenerationDto(
            updated.Id.Value,
            result.IsResolved,
            result.IsNoSolution,
            updated.Status,
            updated.Resolution.State);
    }

    /// <summary>
    /// Materializes Fixtures/Matches for the stage format (Championship / Groups / Cup fixtures).
    /// </summary>
    public async Task<MaterializeMatchesResult> MaterializeMatchesAsync(
        StageId stageId,
        CancellationToken cancellationToken = default)
    {
        var stage = await RequireStageAsync(stageId, cancellationToken).ConfigureAwait(false);
        var competition = await RequireCompetitionAsync(stage.CompetitionId, cancellationToken).ConfigureAwait(false);
        var existing = await matches.ListByStageForUpdateAsync(stageId, cancellationToken).ConfigureAwait(false);
        var competitionStages =
            await LoadCompetitionStagesForUpdateAsync(competition, cancellationToken).ConfigureAwait(false);
        var result = MaterializeMatches.Execute(competition, stage, existing, clock, competitionStages);
        foreach (var created in result.CreatedMatches)
        {
            matches.Add(created);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return result;
    }

    /// <summary>
    /// Materializes Cup Fixtures/Matches from occupied bracket pairs.
    /// </summary>
    public async Task<MaterializeCupFromOccupiedSlotsResult> MaterializeCupFromOccupiedSlotsAsync(
        StageId stageId,
        IReadOnlyList<string>? pairKeys,
        CancellationToken cancellationToken = default)
    {
        var stage = await RequireStageAsync(stageId, cancellationToken).ConfigureAwait(false);
        var competition = await RequireCompetitionAsync(stage.CompetitionId, cancellationToken).ConfigureAwait(false);
        var existing = await matches.ListByStageForUpdateAsync(stageId, cancellationToken).ConfigureAwait(false);
        var competitionStages = await LoadCompetitionStagesForUpdateAsync(competition, cancellationToken)
            .ConfigureAwait(false);
        var matchSummariesByStage =
            competition.StageIds.Count == 0
                ? new Dictionary<StageId, IReadOnlyList<MatchSummaryRow>>()
                : await matches
                    .ListSummaryRowsByStageIdsReadOnlyAsync(competition.StageIds, cancellationToken)
                    .ConfigureAwait(false);
        var result = MaterializeCupFromOccupiedSlots.Execute(
            competition,
            stage,
            pairKeys,
            existing,
            clock,
            competitionStages,
            matchSummariesByStage);
        foreach (var created in result.CreatedMatches)
        {
            matches.Add(created);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return result;
    }

    /// <summary>
    /// Removes attached materialization for the stage format (Championship / Groups / Cup / Swiss last round).
    /// </summary>
    public async Task<DematerializePhaseResult> DematerializePhaseAsync(
        StageId stageId,
        CancellationToken cancellationToken = default)
    {
        var stage = await RequireStageAsync(stageId, cancellationToken).ConfigureAwait(false);
        var competition = await RequireCompetitionAsync(stage.CompetitionId, cancellationToken).ConfigureAwait(false);
        var existing = await matches.ListByStageForUpdateAsync(stageId, cancellationToken).ConfigureAwait(false);
        var format = StructureSkeleton.InferFormat(stage)
                     ?? throw new ApplicationFailureException(
                         "Dematerialization requires a structured stage.",
                         ApplicationErrorCodes.DematerializationFailure);

        var result = format switch
        {
            StructureFormatKind.Championship or StructureFormatKind.Groups => new DematerializePhaseResult(
                DematerializeChampionshipOrGroups.Execute(competition, stage, existing, matches, clock)
                    .RemovedMatchCount,
                RemovedRoundNumber: null),
            StructureFormatKind.Cup => new DematerializePhaseResult(
                DematerializeCup.Execute(competition, stage, existing, matches, clock).RemovedMatchCount,
                RemovedRoundNumber: null),
            StructureFormatKind.Swiss => DematerializeSwissLastRound.Execute(competition, stage, existing, matches, clock) switch
            {
                var swiss => new DematerializePhaseResult(swiss.RemovedMatchCount, swiss.RemovedRoundNumber)
            },
            _ => throw new ApplicationFailureException(
                $"Dematerialization is not supported for format '{format}'.",
                ApplicationErrorCodes.DematerializationFailure)
        };

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return result;
    }

    /// <summary>
    /// Generates the next Swiss round (pairings + Matchday) while the stage is Running.
    /// </summary>
    public async Task<GenerateNextRoundResult> GenerateNextRoundAsync(
        StageId stageId,
        CancellationToken cancellationToken = default)
    {
        var stage = await RequireStageAsync(stageId, cancellationToken).ConfigureAwait(false);
        var competition = await RequireCompetitionAsync(stage.CompetitionId, cancellationToken).ConfigureAwait(false);
        var existing = await matches.ListByStageForUpdateAsync(stageId, cancellationToken).ConfigureAwait(false);
        var competitionStages =
            await LoadCompetitionStagesForUpdateAsync(competition, cancellationToken).ConfigureAwait(false);
        var result = GenerateNextRound.Execute(competition, stage, existing, clock, competitionStages);
        foreach (var created in result.CreatedMatches)
        {
            matches.Add(created);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return result;
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
                home: MatchParticipantRef.Known(match.HomeEntryId),
                away: MatchParticipantRef.Known(match.AwayEntryId)))
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
                    new MatchSchedulingContext(match.Id, duration, home: MatchParticipantRef.Known(match.HomeEntryId), away: MatchParticipantRef.Known(match.AwayEntryId))
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

        var stage = await RequireStageAsync(stageId, cancellationToken).ConfigureAwait(false);
        await EnsureCompetitionAllowsLifecycleMutationAsync(stage.CompetitionId, cancellationToken)
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
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
