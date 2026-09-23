// -----------------------------------------------------------------------
// <copyright file="UseCaseExecutor.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Matches;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Scheduling;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Pipeline;

/// <summary>
/// Minimal persistence orchestration for Application use cases and named read methods
/// (CreateCompetition, Structure Slice 2, PrepareStage, StartStage, ApplyProgressionOutcome,
/// PublishDraw, PublishAndApplyDraw, ApplyDraw, StartMatch, FinishMatch, PrepareCompetition, StartCompetition,
/// CompleteCompetition, ArchiveCompetition, ListCompetitions, GetWorkspaceSummary,
/// GetCompetitionDetail, GetStructureView, GetStageOverview, ListMatchesByStage,
/// GetMatchDetail, GetConsultation).
/// </summary>
/// <remarks>
/// Command methods load aggregates via ports, run the static use case, then commit once via <see cref="IUnitOfWork"/>.
/// Read methods load aggregates, assemble product DTOs, and never call SaveChanges.
/// PrepareStage and ApplyProgressionOutcome load all competition stages (cross-stage destinations / feeds).
/// Does not know HTTP, EF Core, or Domain Event dispatch. Not a CQRS mediator — named methods only;
/// do not introduce generic dispatch without a demonstrated need.
/// Initializes a new instance of the <see cref="UseCaseExecutor"/> class.
/// Lifecycle commands emit structured Information logs (IDs only); Domain has no logging;
/// mapped HTTP 4xx stay silent in Host handlers.
/// </remarks>
/// <param name="stages">Stage persistence port.</param>
/// <param name="matches">Match persistence port.</param>
/// <param name="competitions">Competition persistence port (StageIds for multi-stage load).</param>
/// <param name="unitOfWork">Unit of work for a single commit after the use case.</param>
/// <param name="clock">Clock forwarded to Domain / Application.</param>
/// <param name="mediaReferences">Port that verifies Media identities exist before storing logo refs.</param>
/// <param name="logger">Structured logger for lifecycle milestones.</param>
public sealed partial class UseCaseExecutor(
    IStageRepository stages,
    IMatchRepository matches,
    ICompetitionRepository competitions,
    IUnitOfWork unitOfWork,
    IClock clock,
    IMediaReferenceChecker mediaReferences,
    ILogger<UseCaseExecutor> logger)
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

        var competitionStages = new List<Stage>(competition.StageIds.Count);
        foreach (var competitionStageId in competition.StageIds)
        {
            var loaded = await stages.GetByIdForUpdateAsync(competitionStageId, cancellationToken).ConfigureAwait(false)
                         ?? throw new ApplicationFailureException(
                             $"Stage '{competitionStageId}' was not found.",
                             ApplicationErrorCodes.StageNotFound);
            competitionStages.Add(loaded);
        }

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

        StartStage.Execute(stage, clock);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        LogStageStarted(logger, stageId.Value, stage.CompetitionId.Value);
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

        var competitionStages = new List<Stage>(competition.StageIds.Count);
        foreach (var stageId in competition.StageIds)
        {
            var stage = await stages.GetByIdForUpdateAsync(stageId, cancellationToken).ConfigureAwait(false)
                        ?? throw new ApplicationFailureException(
                            $"Stage '{stageId}' was not found.",
                            ApplicationErrorCodes.StageNotFound);
            competitionStages.Add(stage);
        }

        if (!competitionStages.Exists(candidate => candidate.Id.Equals(sourceStageId)))
        {
            throw new ApplicationFailureException(
                $"Stage '{sourceStageId}' is not part of the competition stages list.",
                ApplicationErrorCodes.StageNotInCompetition);
        }

        // Fixture lookup uses the tracked source instance (same identity as competitionStages entry).
        var fixture = source.GetFixture(fixtureId);
        var loadedMatches = new List<Match>(fixture.Attachments.Count);
        foreach (var attachment in fixture.Attachments)
        {
            var match = await matches.GetByIdForUpdateAsync(attachment.MatchId, cancellationToken).ConfigureAwait(false)
                        ?? throw new ApplicationFailureException(
                            $"Match '{attachment.MatchId}' was not found.",
                            ApplicationErrorCodes.MatchNotFound);
            loadedMatches.Add(match);
        }

        ApplyProgressionOutcome.Execute(source, fixtureId, loadedMatches, competitionStages, clock);
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

        var applied = ApplyQualification.Execute(
            canonicalSource,
            overall,
            groupStandings,
            stageMatches,
            competitionStages,
            clock);
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
    /// — the V1 recovery state, not a rolled-back draft.
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

        var result = ApplyDraw.Execute(stage, drawId, clock);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        LogDrawApplied(logger, stageId.Value, drawId.Value, stage.CompetitionId.Value, result.CreatedMatches.Count);
    }

    /// <summary>
    /// Loads a match, runs <see cref="StartMatch"/>, and saves changes.
    /// </summary>
    /// <param name="matchId">Match identity.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the match is started and persisted.</returns>
    /// <exception cref="ApplicationFailureException">Thrown when the match does not exist or the competition is closed.</exception>
    public async Task StartMatchAsync(MatchId matchId, CancellationToken cancellationToken = default)
    {
        var match = await matches.GetByIdForUpdateAsync(matchId, cancellationToken).ConfigureAwait(false)
                    ?? throw new ApplicationFailureException(
                        $"Match '{matchId}' was not found.",
                        ApplicationErrorCodes.MatchNotFound);

        await EnsureCompetitionAllowsMatchOperationAsync(match.CompetitionId, cancellationToken)
            .ConfigureAwait(false);

        StartMatch.Execute(match, clock);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        LogMatchStarted(logger, matchId.Value, match.CompetitionId.Value);
    }

    /// <summary>
    /// Loads a match, runs <see cref="FinishMatch"/>, and saves changes.
    /// </summary>
    /// <param name="matchId">Match identity.</param>
    /// <param name="result">Domain match result.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the match is finished and persisted.</returns>
    /// <exception cref="ApplicationFailureException">Thrown when the match does not exist or the competition is closed.</exception>
    public async Task FinishMatchAsync(
        MatchId matchId,
        MatchResult result,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(result);

        var match = await matches.GetByIdForUpdateAsync(matchId, cancellationToken).ConfigureAwait(false)
                    ?? throw new ApplicationFailureException(
                        $"Match '{matchId}' was not found.",
                        ApplicationErrorCodes.MatchNotFound);

        await EnsureCompetitionAllowsMatchOperationAsync(match.CompetitionId, cancellationToken)
            .ConfigureAwait(false);

        FinishMatch.Execute(match, result, clock);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        LogMatchFinished(logger, matchId.Value, match.CompetitionId.Value);
    }

    /// <summary>
    /// Adds an eligible player to the match composition sheet and saves changes.
    /// </summary>
    public async Task AddDeclaredParticipationAsync(
        MatchId matchId,
        MemberId memberId,
        Side side,
        CompositionStatus compositionStatus,
        int? jerseyNumber = null,
        CancellationToken cancellationToken = default)
    {
        var (match, competition) = await RequireMatchWithCompetitionForOperationAsync(matchId, cancellationToken)
            .ConfigureAwait(false);
        AddDeclaredParticipation.Execute(match, competition, memberId, side, compositionStatus, clock, jerseyNumber);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Removes a declared participation from the match sheet and saves changes.
    /// </summary>
    public async Task RemoveDeclaredParticipationAsync(
        MatchId matchId,
        MemberId memberId,
        CancellationToken cancellationToken = default)
    {
        var match = await RequireMatchForOperationAsync(matchId, cancellationToken).ConfigureAwait(false);
        RemoveDeclaredParticipation.Execute(match, memberId, clock);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Changes starter/bench status on the match sheet and saves changes.
    /// </summary>
    public async Task ChangeDeclaredParticipationCompositionStatusAsync(
        MatchId matchId,
        MemberId memberId,
        CompositionStatus compositionStatus,
        CancellationToken cancellationToken = default)
    {
        var match = await RequireMatchForOperationAsync(matchId, cancellationToken).ConfigureAwait(false);
        ChangeDeclaredParticipationCompositionStatus.Execute(match, memberId, compositionStatus, clock);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sets or clears a jersey number on the match sheet and saves changes.
    /// </summary>
    public async Task SetDeclaredParticipationJerseyNumberAsync(
        MatchId matchId,
        MemberId memberId,
        int? jerseyNumber,
        CancellationToken cancellationToken = default)
    {
        var match = await RequireMatchForOperationAsync(matchId, cancellationToken).ConfigureAwait(false);
        SetDeclaredParticipationJerseyNumber.Execute(match, memberId, jerseyNumber, clock);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Replaces the observed Live running score and saves changes.
    /// </summary>
    public async Task SetRunningScoreAsync(
        MatchId matchId,
        int homeGoals,
        int awayGoals,
        CancellationToken cancellationToken = default)
    {
        var match = await RequireMatchForOperationAsync(matchId, cancellationToken).ConfigureAwait(false);
        SetRunningScore.Execute(match, homeGoals, awayGoals, clock);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Records a nominative goal and saves changes.
    /// </summary>
    public async Task RecordGoalAsync(
        MatchId matchId,
        MemberId scorerMemberId,
        Side creditedSide,
        MemberId? assisterMemberId = null,
        CancellationToken cancellationToken = default)
    {
        var match = await RequireMatchForOperationAsync(matchId, cancellationToken).ConfigureAwait(false);
        RecordGoal.Execute(match, scorerMemberId, creditedSide, clock, assisterMemberId);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Corrects a nominative goal and saves changes.
    /// </summary>
    public async Task CorrectRecordedGoalAsync(
        MatchId matchId,
        GoalId goalId,
        MemberId scorerMemberId,
        Side creditedSide,
        MemberId? assisterMemberId = null,
        CancellationToken cancellationToken = default)
    {
        var match = await RequireMatchForOperationAsync(matchId, cancellationToken).ConfigureAwait(false);
        CorrectRecordedGoal.Execute(match, goalId, scorerMemberId, creditedSide, clock, assisterMemberId);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Removes a nominative goal and saves changes.
    /// </summary>
    public async Task RemoveRecordedGoalAsync(
        MatchId matchId,
        GoalId goalId,
        CancellationToken cancellationToken = default)
    {
        var match = await RequireMatchForOperationAsync(matchId, cancellationToken).ConfigureAwait(false);
        RemoveRecordedGoal.Execute(match, goalId, clock);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Records a substitution fact and saves changes.
    /// </summary>
    public async Task RecordSubstitutionAsync(
        MatchId matchId,
        MemberId outMemberId,
        MemberId inMemberId,
        Side side,
        CancellationToken cancellationToken = default)
    {
        var match = await RequireMatchForOperationAsync(matchId, cancellationToken).ConfigureAwait(false);
        RecordSubstitution.Execute(match, outMemberId, inMemberId, side, clock);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Corrects a substitution fact and saves changes.
    /// </summary>
    public async Task CorrectRecordedSubstitutionAsync(
        MatchId matchId,
        SubstitutionId substitutionId,
        MemberId outMemberId,
        MemberId inMemberId,
        Side side,
        CancellationToken cancellationToken = default)
    {
        var match = await RequireMatchForOperationAsync(matchId, cancellationToken).ConfigureAwait(false);
        CorrectRecordedSubstitution.Execute(match, substitutionId, outMemberId, inMemberId, side, clock);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Removes a substitution fact and saves changes.
    /// </summary>
    public async Task RemoveRecordedSubstitutionAsync(
        MatchId matchId,
        SubstitutionId substitutionId,
        CancellationToken cancellationToken = default)
    {
        var match = await RequireMatchForOperationAsync(matchId, cancellationToken).ConfigureAwait(false);
        RemoveRecordedSubstitution.Execute(match, substitutionId, clock);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Records a disciplinary fact when authorized by competition rules and saves changes.
    /// </summary>
    public async Task RecordDisciplinaryEventAsync(
        MatchId matchId,
        MemberId memberId,
        DisciplinaryType type,
        CancellationToken cancellationToken = default)
    {
        var (match, competition) = await RequireMatchWithCompetitionForOperationAsync(matchId, cancellationToken)
            .ConfigureAwait(false);
        RecordDisciplinaryEvent.Execute(match, competition, memberId, type, clock);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Corrects a disciplinary fact when authorized by competition rules and saves changes.
    /// </summary>
    public async Task CorrectRecordedDisciplinaryEventAsync(
        MatchId matchId,
        DisciplinaryEventId disciplinaryEventId,
        MemberId memberId,
        DisciplinaryType type,
        CancellationToken cancellationToken = default)
    {
        var (match, competition) = await RequireMatchWithCompetitionForOperationAsync(matchId, cancellationToken)
            .ConfigureAwait(false);
        CorrectRecordedDisciplinaryEvent.Execute(match, competition, disciplinaryEventId, memberId, type, clock);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Removes a disciplinary fact and saves changes.
    /// </summary>
    public async Task RemoveRecordedDisciplinaryEventAsync(
        MatchId matchId,
        DisciplinaryEventId disciplinaryEventId,
        CancellationToken cancellationToken = default)
    {
        var match = await RequireMatchForOperationAsync(matchId, cancellationToken).ConfigureAwait(false);
        RemoveRecordedDisciplinaryEvent.Execute(match, disciplinaryEventId, clock);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Prepares a competition (Draft → Ready). Domain owns preconditions.
    /// </summary>
    /// <param name="competitionId">Competition identity.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the competition is prepared and persisted.</returns>
    public async Task PrepareCompetitionAsync(
        CompetitionId competitionId,
        CancellationToken cancellationToken = default)
    {
        var competition = await RequireCompetitionAsync(competitionId, cancellationToken).ConfigureAwait(false);
        EnsureCompetitionAllowsLifecycleMutation(competition);
        PrepareCompetition.Execute(competition, clock);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        LogCompetitionPrepared(logger, competitionId.Value);
    }

    /// <summary>
    /// Starts a competition (Ready → Running). Domain owns preconditions.
    /// </summary>
    /// <param name="competitionId">Competition identity.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the competition is started and persisted.</returns>
    public async Task StartCompetitionAsync(
        CompetitionId competitionId,
        CancellationToken cancellationToken = default)
    {
        var competition = await RequireCompetitionAsync(competitionId, cancellationToken).ConfigureAwait(false);
        EnsureCompetitionAllowsLifecycleMutation(competition);
        StartCompetition.Execute(competition, clock);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        LogCompetitionStarted(logger, competitionId.Value);
    }

    /// <summary>
    /// Completes a competition (Normal gated by <see cref="CompletionAnalyzer"/>).
    /// </summary>
    /// <param name="competitionId">Competition identity.</param>
    /// <param name="mode">Completion manner.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the competition is completed and persisted.</returns>
    public async Task CompleteCompetitionAsync(
        CompetitionId competitionId,
        CompletionMode mode,
        CancellationToken cancellationToken = default)
    {
        var competition = await RequireCompetitionAsync(competitionId, cancellationToken).ConfigureAwait(false);
        var competitionStages =
            await LoadCompetitionStagesForUpdateAsync(competition, cancellationToken).ConfigureAwait(false);
        var matchesByStage = await LoadMatchesByStageReadOnlyAsync(competitionStages, cancellationToken)
            .ConfigureAwait(false);
        var analysis = CompletionAnalyzer.Analyze(competition, competitionStages, matchesByStage);
        CompleteCompetition.Execute(competition, mode, analysis, clock);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        LogCompetitionCompleted(logger, competitionId.Value, mode);
    }

    /// <summary>
    /// Archives a completed competition.
    /// </summary>
    /// <param name="competitionId">Competition identity.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the competition is archived and persisted.</returns>
    public async Task ArchiveCompetitionAsync(
        CompetitionId competitionId,
        CancellationToken cancellationToken = default)
    {
        var competition = await RequireCompetitionAsync(competitionId, cancellationToken).ConfigureAwait(false);
        ArchiveCompetition.Execute(competition, clock);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        LogCompetitionArchived(logger, competitionId.Value);
    }

    /// <summary>
    /// Creates a Competition (bootstrap regulation), persists it, and returns <see cref="WorkspaceSummaryDto"/>.
    /// </summary>
    /// <param name="name">Display name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Workspace summary for the new Draft competition.</returns>
    public async Task<WorkspaceSummaryDto> CreateCompetitionAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        var competition = CreateCompetition.Execute(name, clock);
        competitions.Add(competition);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        LogCompetitionCreated(logger, competition.Id.Value);
        return WorkspaceSummaryAssembler.Assemble(competition);
    }

    /// <summary>
    /// Adds an entry and returns the updated <see cref="StructureViewDto"/>.
    /// </summary>
    public async Task<StructureViewDto> AddEntryAsync(
        CompetitionId competitionId,
        string displayName,
        Guid? teamId = null,
        string? shortName = null,
        Guid? logoMediaId = null,
        string? primaryColor = null,
        string? secondaryColor = null,
        CancellationToken cancellationToken = default)
    {
        var competition = await RequireCompetitionAsync(competitionId, cancellationToken).ConfigureAwait(false);
        await EnsureLogoMediaExistsAsync(logoMediaId, cancellationToken).ConfigureAwait(false);
        var resolvedShortName = string.IsNullOrWhiteSpace(shortName)
            ? ShortName.FromDisplayName(displayName)
            : ShortName.CreateRequired(shortName);
        var presentation = new EntryPresentation(
            resolvedShortName,
            LogoMediaId.Create(logoMediaId),
            TeamColor.Create(primaryColor),
            TeamColor.Create(secondaryColor));
        AddEntry.Execute(
            competition,
            displayName,
            clock,
            teamId is null ? null : new TeamId(teamId.Value),
            presentation);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return await AssembleStructureViewAsync(competition, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Updates competition presentation and returns the updated structure view.
    /// </summary>
    public async Task<StructureViewDto> UpdateCompetitionPresentationAsync(
        CompetitionId competitionId,
        string? shortName,
        Guid? logoMediaId,
        CancellationToken cancellationToken = default)
    {
        var competition = await RequireCompetitionAsync(competitionId, cancellationToken).ConfigureAwait(false);
        await EnsureLogoMediaExistsAsync(logoMediaId, cancellationToken).ConfigureAwait(false);
        UpdateCompetitionPresentation.Execute(competition, shortName, logoMediaId, clock);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return await AssembleStructureViewAsync(competition, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sets declared competition schedule and returns the updated structure view.
    /// </summary>
    public async Task<StructureViewDto> SetCompetitionScheduleAsync(
        CompetitionId competitionId,
        DateTimeOffset? scheduledStart,
        DateTimeOffset? scheduledEnd,
        CancellationToken cancellationToken = default)
    {
        var competition = await RequireCompetitionAsync(competitionId, cancellationToken).ConfigureAwait(false);
        SetCompetitionSchedule.Execute(competition, scheduledStart, scheduledEnd, clock);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return await AssembleStructureViewAsync(competition, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Updates entry presentation and returns the updated structure view.
    /// </summary>
    public async Task<StructureViewDto> UpdateEntryPresentationAsync(
        CompetitionId competitionId,
        EntryId entryId,
        string? shortName,
        Guid? logoMediaId,
        string? primaryColor,
        string? secondaryColor,
        CancellationToken cancellationToken = default)
    {
        var competition = await RequireCompetitionAsync(competitionId, cancellationToken).ConfigureAwait(false);
        await EnsureLogoMediaExistsAsync(logoMediaId, cancellationToken).ConfigureAwait(false);
        UpdateEntryPresentation.Execute(
            competition,
            entryId,
            shortName ?? string.Empty,
            logoMediaId,
            primaryColor,
            secondaryColor,
            clock);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return await AssembleStructureViewAsync(competition, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Renames an entry and returns the updated structure view.
    /// </summary>
    public async Task<StructureViewDto> RenameEntryAsync(
        CompetitionId competitionId,
        EntryId entryId,
        string displayName,
        CancellationToken cancellationToken = default)
    {
        var competition = await RequireCompetitionAsync(competitionId, cancellationToken).ConfigureAwait(false);
        RenameEntry.Execute(competition, entryId, displayName, clock);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return await AssembleStructureViewAsync(competition, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Withdraws an entry (forfait) and returns the updated structure view.
    /// </summary>
    public async Task<StructureViewDto> WithdrawEntryAsync(
        CompetitionId competitionId,
        EntryId entryId,
        CancellationToken cancellationToken = default)
    {
        var competition = await RequireCompetitionAsync(competitionId, cancellationToken).ConfigureAwait(false);
        var (competitionStages, competitionMatches) =
            await LoadCompetitionStagesAndMatchesForUpdateAsync(competition, cancellationToken)
                .ConfigureAwait(false);
        WithdrawEntry.Execute(competition, entryId, competitionStages, competitionMatches, clock);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return await AssembleStructureViewAsync(competition, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Hard-deletes an entry and returns the updated structure view.
    /// </summary>
    public async Task<StructureViewDto> DeleteEntryAsync(
        CompetitionId competitionId,
        EntryId entryId,
        CancellationToken cancellationToken = default)
    {
        var competition = await RequireCompetitionAsync(competitionId, cancellationToken).ConfigureAwait(false);
        var (competitionStages, competitionMatches) =
            await LoadCompetitionStagesAndMatchesForUpdateAsync(competition, cancellationToken)
                .ConfigureAwait(false);
        DeleteEntry.Execute(competition, entryId, competitionStages, competitionMatches, matches, clock);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return await AssembleStructureViewAsync(competition, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Hard-deletes several entries atomically and returns the updated structure view.
    /// </summary>
    public async Task<StructureViewDto> DeleteEntriesAsync(
        CompetitionId competitionId,
        IReadOnlyList<EntryId> entryIds,
        CancellationToken cancellationToken = default)
    {
        var competition = await RequireCompetitionAsync(competitionId, cancellationToken).ConfigureAwait(false);
        var (competitionStages, competitionMatches) =
            await LoadCompetitionStagesAndMatchesForUpdateAsync(competition, cancellationToken)
                .ConfigureAwait(false);
        DeleteEntries.Execute(competition, entryIds, competitionStages, competitionMatches, matches, clock);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return await AssembleStructureViewAsync(competition, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Withdraws several entries atomically and returns the updated structure view.
    /// </summary>
    public async Task<StructureViewDto> WithdrawEntriesAsync(
        CompetitionId competitionId,
        IReadOnlyList<EntryId> entryIds,
        CancellationToken cancellationToken = default)
    {
        var competition = await RequireCompetitionAsync(competitionId, cancellationToken).ConfigureAwait(false);
        var (competitionStages, competitionMatches) =
            await LoadCompetitionStagesAndMatchesForUpdateAsync(competition, cancellationToken)
                .ConfigureAwait(false);
        WithdrawEntries.Execute(competition, entryIds, competitionStages, competitionMatches, clock);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return await AssembleStructureViewAsync(competition, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Removes several declared members atomically and returns the updated structure view.
    /// </summary>
    public async Task<StructureViewDto> RemoveDeclaredMembersAsync(
        CompetitionId competitionId,
        EntryId entryId,
        IReadOnlyList<MemberId> memberIds,
        CancellationToken cancellationToken = default)
    {
        var competition = await RequireCompetitionAsync(competitionId, cancellationToken).ConfigureAwait(false);
        var competitionMatches =
            await LoadCompetitionMatchesAsync(competition, cancellationToken).ConfigureAwait(false);
        RemoveDeclaredMembers.Execute(competition, entryId, memberIds, competitionMatches, clock);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return await AssembleStructureViewAsync(competition, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Adds a declared member to an entry roster and returns the updated structure view.
    /// </summary>
    public async Task<StructureViewDto> AddDeclaredMemberAsync(
        CompetitionId competitionId,
        EntryId entryId,
        string displayName,
        DeclaredMemberRole role,
        CancellationToken cancellationToken = default)
    {
        var competition = await RequireCompetitionAsync(competitionId, cancellationToken).ConfigureAwait(false);
        AddDeclaredMember.Execute(competition, entryId, displayName, role, clock);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return await AssembleStructureViewAsync(competition, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Removes a declared member from an entry roster and returns the updated structure view.
    /// </summary>
    public async Task<StructureViewDto> RemoveDeclaredMemberAsync(
        CompetitionId competitionId,
        EntryId entryId,
        MemberId memberId,
        CancellationToken cancellationToken = default)
    {
        var competition = await RequireCompetitionAsync(competitionId, cancellationToken).ConfigureAwait(false);
        var competitionMatches =
            await LoadCompetitionMatchesAsync(competition, cancellationToken).ConfigureAwait(false);
        RemoveDeclaredMember.Execute(competition, entryId, memberId, competitionMatches, clock);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return await AssembleStructureViewAsync(competition, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Renames a declared member and returns the updated structure view.
    /// </summary>
    public async Task<StructureViewDto> RenameDeclaredMemberAsync(
        CompetitionId competitionId,
        EntryId entryId,
        MemberId memberId,
        string displayName,
        CancellationToken cancellationToken = default)
    {
        var competition = await RequireCompetitionAsync(competitionId, cancellationToken).ConfigureAwait(false);
        RenameDeclaredMember.Execute(competition, entryId, memberId, displayName, clock);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return await AssembleStructureViewAsync(competition, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Replaces competition regulation and returns the updated structure view.
    /// </summary>
    /// <param name="competitionId">Competition identity.</param>
    /// <param name="buildReplacement">
    /// Builds the replacement regulation from the current persisted regulation
    /// (so omitted disciplinary AllowedTypes can preserve existing rules).
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<StructureViewDto> ReplaceRegulationAsync(
        CompetitionId competitionId,
        Func<Regulation, Regulation> buildReplacement,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(buildReplacement);

        var competition = await RequireCompetitionAsync(competitionId, cancellationToken).ConfigureAwait(false);
        var competitionStagesForUpdate = await LoadCompetitionStagesForUpdateAsync(competition, cancellationToken).ConfigureAwait(false);
        var regulation = buildReplacement(competition.Regulation);
        ReplaceRegulation.Execute(competition, competitionStagesForUpdate, regulation, clock);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return await AssembleStructureViewAsync(competition, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Configures primary stage structure from a typed intent (atomic SaveChanges).
    /// First-time create or explicit rebuild of the primary skeleton.
    /// </summary>
    public async Task<(ConfigureStructureResult Result, StructureViewDto View)> ConfigureStructureAsync(
        CompetitionId competitionId,
        StructureIntent intent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(intent);

        var competition = await RequireCompetitionAsync(competitionId, cancellationToken).ConfigureAwait(false);
        Stage? primary = null;
        if (competition.StageIds.Count > 0)
        {
            primary = await stages.GetByIdForUpdateAsync(competition.StageIds[0], cancellationToken)
                          .ConfigureAwait(false)
                      ?? throw new ApplicationFailureException(
                          $"Stage '{competition.StageIds[0]}' was not found.",
                          ApplicationErrorCodes.StageNotFound);
        }

        var result = ConfigureStructure.Execute(competition, primary, intent, clock);
        if (result.StageCreated)
        {
            stages.Add(result.Stage);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        var view = await AssembleStructureViewAsync(competition, cancellationToken).ConfigureAwait(false);
        return (result, view);
    }

    /// <summary>
    /// Atomic stage birth: identity + skeleton in one SaveChanges.
    /// </summary>
    public async Task<(Stage Stage, StructureViewDto View)> AddCompetitionStageAsync(
        CompetitionId competitionId,
        StructureIntent intent,
        CancellationToken cancellationToken = default)
    {
        var competition = await RequireCompetitionAsync(competitionId, cancellationToken).ConfigureAwait(false);
        var stage = AddCompetitionStage.Execute(competition, intent, clock);
        stages.Add(stage);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        var view = await AssembleStructureViewAsync(competition, cancellationToken).ConfigureAwait(false);
        return (stage, view);
    }

    /// <summary>
    /// Rebuilds a stage skeleton (same format kind; 0 attached matches).
    /// </summary>
    /// <param name="stageId">Target stage.</param>
    /// <param name="buildIntent">
    /// Builds the intent using the current stage display name (for optional rename omission).
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<(StructureRebuildImpact Impact, StructureViewDto View)> RebuildStageStructureAsync(
        StageId stageId,
        Func<string, StructureIntent> buildIntent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(buildIntent);
        var stage = await RequireStageAsync(stageId, cancellationToken).ConfigureAwait(false);
        var competition = await RequireCompetitionAsync(stage.CompetitionId, cancellationToken)
            .ConfigureAwait(false);
        var intent = buildIntent(stage.Name.Value);
        var impact = RebuildStageStructure.Execute(competition, stage, intent, clock);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        var view = await AssembleStructureViewAsync(competition, cancellationToken).ConfigureAwait(false);
        return (impact, view);
    }

    /// <summary>
    /// Removes a stage and scrubs peer Qualif/Prog paths that targeted it.
    /// </summary>
    public async Task<(RemoveCompetitionStageResult Impact, StructureViewDto View)> RemoveCompetitionStageAsync(
        CompetitionId competitionId,
        StageId stageId,
        CancellationToken cancellationToken = default)
    {
        var competition = await RequireCompetitionAsync(competitionId, cancellationToken).ConfigureAwait(false);
        var peerStages = await LoadCompetitionStagesForUpdateAsync(competition, cancellationToken)
            .ConfigureAwait(false);
        var target = peerStages.FirstOrDefault(stage => stage.Id.Equals(stageId))
            ?? throw new ApplicationFailureException(
                $"Stage '{stageId}' was not found.",
                ApplicationErrorCodes.StageNotFound);

        var impact = RemoveCompetitionStage.Execute(competition, target, peerStages, clock);
        stages.Remove(target);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        var view = await AssembleStructureViewAsync(competition, cancellationToken).ConfigureAwait(false);
        return (impact, view);
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
        var tieFormat = AddStageRound.BuildTieFormat(numberOfLegs, aggregateScoring);
        var round = AddStageRound.Execute(stage, name, tieFormat, clock);
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
        var slot = AddStageSlot.Execute(stage, slotKey);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return slot;
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
        var matchday = AddStageMatchday.Execute(stage, number, clock);
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
        var group = AddStageGroup.Execute(stage, name, clock);
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
        ReplaceStageMatchGenerationFormat.Execute(stage, format);
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
        ReplaceStageSwissSettings.Execute(stage, roundCount);
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
        ReplaceStageProgressionRules.Execute(stage, paths, clock);
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
        var competition = await RequireCompetitionAsync(stage.CompetitionId, cancellationToken)
            .ConfigureAwait(false);
        await LoadCompetitionStagesForUpdateAsync(competition, cancellationToken)
            .ConfigureAwait(false);
        ReplaceStageProgressionRules.Execute(stage, intents, clock);
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
        ReplaceStageQualificationRules.Execute(stage, paths, clock);
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
        var competition = await RequireCompetitionAsync(stage.CompetitionId, cancellationToken)
            .ConfigureAwait(false);
        await LoadCompetitionStagesForUpdateAsync(competition, cancellationToken)
            .ConfigureAwait(false);
        ReplaceStageQualificationRules.Execute(stage, intents, clock);
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
        ReplaceStageStandingRules.Execute(stage, standingRules, clock);
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
        ReplaceStageMatchRules.Execute(stage, matchRules, clock);
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
        ReplaceStageDrawRules.Execute(stage, drawRules, clock);
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
        ReplaceStageAffectationAuthoring.Execute(stage, competition, entryIds, clock);
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
        ReplaceStageDefaultTieFormat.Execute(stage, tieFormat, clock);
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
        ReplaceRoundTieFormat.Execute(stage, roundId, tieFormat, clock);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Assembles <see cref="StructureViewDto"/> for the Structure hub.
    /// </summary>
    public async Task<StructureViewDto> GetStructureViewAsync(
        CompetitionId competitionId,
        CancellationToken cancellationToken = default)
    {
        var competition =
            await competitions.GetByIdReadOnlyAsync(competitionId, cancellationToken).ConfigureAwait(false)
            ?? throw new ApplicationFailureException(
                $"Competition '{competitionId}' was not found.",
                ApplicationErrorCodes.CompetitionNotFound);
        return await AssembleStructureViewAsync(competition, cancellationToken).ConfigureAwait(false);
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
    /// <see cref="DrawInputsIntent.Default"/> = Encoding F (Fixed* from occupancy).
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
        var inputs = DrawInputsFactory.CreateDefault(stage, draw.Kind, intent);
        ConfigureDrawInputs.Execute(stage, drawId, inputs);
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
        var result = MaterializeMatches.Execute(competition, stage, existing, clock);
        foreach (var created in result.CreatedMatches)
        {
            matches.Add(created);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return result;
    }

    /// <summary>
    /// Materializes Cup Fixtures/Matches from occupied bracket slots (Lot C2).
    /// </summary>
    public async Task<MaterializeCupFromOccupiedSlotsResult> MaterializeCupFromOccupiedSlotsAsync(
        StageId stageId,
        IReadOnlyList<CupSlotPair> slotPairs,
        CancellationToken cancellationToken = default)
    {
        var stage = await RequireStageAsync(stageId, cancellationToken).ConfigureAwait(false);
        var competition = await RequireCompetitionAsync(stage.CompetitionId, cancellationToken).ConfigureAwait(false);
        var existing = await matches.ListByStageForUpdateAsync(stageId, cancellationToken).ConfigureAwait(false);
        var result = MaterializeCupFromOccupiedSlots.Execute(competition, stage, slotPairs, existing, clock);
        foreach (var created in result.CreatedMatches)
        {
            matches.Add(created);
        }

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
        var result = GenerateNextRound.Execute(competition, stage, existing, clock);
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

    private static DrawSummaryDto ToDrawSummary(StageId stageId, Draw draw) =>
        new(
            draw.Id.Value,
            stageId.Value,
            draw.Kind,
            draw.Status,
            draw.Resolution.State,
            draw.Resolution.State == DrawResolutionState.NoSolution);

    private static ScheduleProposalDto ToScheduleProposal(SchedulingResult result) =>
        new(
            result.IsSuccess,
            result.IsNoSolution,
            result.IsInvalidRequest,
            result.Schedule?.Assignments.Select(a => new ScheduleAssignmentDto(a.MatchId.Value, a.Start, a.ResourceId.Value)).ToArray() ?? []);

    private static MatchId[] ResolveScheduleTargets(
        Stage stage,
        IReadOnlyList<Match> loadedMatches,
        IReadOnlyList<Guid>? targetMatchIds)
    {
        if (targetMatchIds is { Count: > 0 })
        {
            return [.. targetMatchIds.Select(id => new MatchId(id))];
        }

        var attached = loadedMatches.Where(match => stage.HasMatch(match.Id)).Select(match => match.Id).ToArray();
        return attached.Length == 0
            ? throw new ApplicationFailureException(
                "GenerateSchedule requires attached matches (materialize first).",
                ApplicationErrorCodes.ScheduleGenerationFailure)
            : attached;
    }

    private async Task<Stage> RequireStageAsync(StageId stageId, CancellationToken cancellationToken) =>
        await stages.GetByIdForUpdateAsync(stageId, cancellationToken).ConfigureAwait(false)
        ?? throw new ApplicationFailureException(
            $"Stage '{stageId}' was not found.",
            ApplicationErrorCodes.StageNotFound);

    private async Task<Competition> RequireCompetitionAsync(
        CompetitionId competitionId,
        CancellationToken cancellationToken) =>
        await competitions.GetByIdForUpdateAsync(competitionId, cancellationToken).ConfigureAwait(false)
        ?? throw new ApplicationFailureException(
            $"Competition '{competitionId}' was not found.",
            ApplicationErrorCodes.CompetitionNotFound);

    private async Task EnsureLogoMediaExistsAsync(Guid? logoMediaId, CancellationToken cancellationToken)
    {
        if (logoMediaId is null)
        {
            return;
        }

        if (!await mediaReferences.ExistsAsync(logoMediaId.Value, cancellationToken).ConfigureAwait(false))
        {
            throw new ApplicationFailureException(
                $"Media '{logoMediaId}' was not found.",
                ApplicationErrorCodes.MediaNotFound);
        }
    }

    private async Task<StructureViewDto> AssembleStructureViewAsync(
        Competition competition,
        CancellationToken cancellationToken)
    {
        var bundle = await LoadStructureReadBundleAsync(competition, cancellationToken).ConfigureAwait(false);
        return StructureViewAssembler.Assemble(
            bundle.Competition,
            bundle.Stages,
            bundle.SheetMemberRefs);
    }

    /// <summary>
    /// Lists competitions for the organizer Competition List.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List item DTOs (possibly empty).</returns>
    public async Task<IReadOnlyList<CompetitionListItemDto>> ListCompetitionsAsync(
        CancellationToken cancellationToken = default)
    {
        var list = await competitions.ListAsync(cancellationToken).ConfigureAwait(false);
        return CompetitionListAssembler.Assemble(list);
    }

    /// <summary>
    /// Loads a competition and assembles the minimal Accueil <see cref="WorkspaceSummaryDto"/>.
    /// </summary>
    /// <param name="competitionId">Competition identity.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Workspace summary.</returns>
    /// <exception cref="ApplicationFailureException">Thrown when the competition is missing.</exception>
    public async Task<WorkspaceSummaryDto> GetWorkspaceSummaryAsync(
        CompetitionId competitionId,
        CancellationToken cancellationToken = default)
    {
        var bundle = await LoadAttentionReadBundleAsync(competitionId, cancellationToken).ConfigureAwait(false);
        var attention = NeedsAttentionAssembler.Assemble(
            bundle.Competition,
            bundle.Stages,
            bundle.MatchesByStage);
        var analysis = bundle.Competition.Status is CompetitionStatus.Running or CompetitionStatus.Suspended
            ? CompletionAnalyzer.Analyze(bundle.Competition, bundle.Stages, bundle.MatchesByStage)
            : null;

        return WorkspaceSummaryAssembler.Assemble(bundle.Competition, attention.Count, analysis);
    }

    /// <summary>
    /// Assembles Needs Attention for a competition (derived Read).
    /// </summary>
    /// <param name="competitionId">Competition identity.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Needs Attention DTO.</returns>
    public async Task<NeedsAttentionDto> GetNeedsAttentionAsync(
        CompetitionId competitionId,
        CancellationToken cancellationToken = default)
    {
        var bundle = await LoadAttentionReadBundleAsync(competitionId, cancellationToken).ConfigureAwait(false);
        return NeedsAttentionAssembler.Assemble(bundle.Competition, bundle.Stages, bundle.MatchesByStage);
    }

    /// <summary>
    /// Assembles the Overview Read projection for a competition (Phase 16.1).
    /// </summary>
    /// <param name="competitionId">Competition identity.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Overview view DTO.</returns>
    public async Task<OverviewViewDto> GetOverviewViewAsync(
        CompetitionId competitionId,
        CancellationToken cancellationToken = default)
    {
        var bundle = await LoadCompetitionReadBundleAsync(
            competitionId,
            CompetitionReadBundleSpec.Overview,
            cancellationToken).ConfigureAwait(false);
        return OverviewAssembler.Assemble(bundle.Competition, bundle.Stages, bundle.MatchesByStage);
    }

    /// <summary>
    /// Assembles Consultation (Results / Standings / Structure) for a competition.
    /// </summary>
    /// <param name="competitionId">Competition identity.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Consultation view DTO.</returns>
    public async Task<ConsultationViewDto> GetConsultationAsync(
        CompetitionId competitionId,
        CancellationToken cancellationToken = default)
    {
        var bundle = await LoadConsultationReadBundleAsync(competitionId, cancellationToken).ConfigureAwait(false);
        return ConsultationAssembler.Assemble(bundle.Competition, bundle.Stages, bundle.MatchesByStage);
    }

    /// <summary>
    /// Match Hub read surface — competition detail and all stage match lists in one load.
    /// </summary>
    /// <param name="competitionId">Competition identity.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Match Hub view.</returns>
    public async Task<MatchHubViewDto> GetMatchHubViewAsync(
        CompetitionId competitionId,
        CancellationToken cancellationToken = default)
    {
        var competition =
            await competitions.GetByIdReadOnlyAsync(competitionId, cancellationToken).ConfigureAwait(false)
            ?? throw new ApplicationFailureException(
                $"Competition '{competitionId}' was not found.",
                ApplicationErrorCodes.CompetitionNotFound);

        if (competition.StageIds.Count == 0)
        {
            var emptyDetail = CompetitionDetailAssembler.Assemble(competition, Array.Empty<StageSummaryRow>());
            return new MatchHubViewDto(emptyDetail, []);
        }

        var stageSummaries = await stages.ListSummariesReadOnlyAsync(competition.StageIds, cancellationToken)
            .ConfigureAwait(false);
        if (stageSummaries.Count != competition.StageIds.Count)
        {
            throw new ApplicationFailureException(
                "One or more competition stages were not found.",
                ApplicationErrorCodes.StageNotFound);
        }

        var detail = CompetitionDetailAssembler.Assemble(competition, stageSummaries);
        var structureStages = await stages
            .GetByIdsReadOnlyAsync(competition.StageIds, StageLoadProfile.Structure, cancellationToken)
            .ConfigureAwait(false);
        var rowsByStage = await matches
            .ListSummaryRowsByStageIdsReadOnlyAsync(competition.StageIds, cancellationToken)
            .ConfigureAwait(false);

        var hubStages = new List<MatchHubStageMatchesDto>(structureStages.Count);
        foreach (var stage in structureStages)
        {
            var rows = rowsByStage.TryGetValue(stage.Id, out var list) ? list : [];
            var summaries = MatchReadAssembler.AssembleSummaries(stage, competition, rows);
            hubStages.Add(new MatchHubStageMatchesDto(
                stage.Id.Value,
                stage.Name.Value,
                stage.Status,
                summaries));
        }

        return new MatchHubViewDto(detail, hubStages);
    }

    /// <summary>
    /// Loads a competition and its stages, then assembles <see cref="CompetitionDetailDto"/>.
    /// </summary>
    /// <param name="competitionId">Competition identity.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The competition overview.</returns>
    /// <exception cref="ApplicationFailureException">Thrown when the competition or a referenced stage is missing.</exception>
    public async Task<CompetitionDetailDto> GetCompetitionDetailAsync(
        CompetitionId competitionId,
        CancellationToken cancellationToken = default)
    {
        var competition =
            await competitions.GetByIdReadOnlyAsync(competitionId, cancellationToken).ConfigureAwait(false)
            ?? throw new ApplicationFailureException(
                $"Competition '{competitionId}' was not found.",
                ApplicationErrorCodes.CompetitionNotFound);

        var stageSummaries = await stages.ListSummariesReadOnlyAsync(competition.StageIds, cancellationToken)
            .ConfigureAwait(false);
        return stageSummaries.Count != competition.StageIds.Count
            ? throw new ApplicationFailureException(
                "One or more competition stages were not found.",
                ApplicationErrorCodes.StageNotFound)
            : CompetitionDetailAssembler.Assemble(competition, stageSummaries);
    }

    /// <summary>
    /// Loads a stage and its competition, then assembles <see cref="StageOverviewDto"/>.
    /// </summary>
    /// <param name="stageId">Stage identity.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The stage overview.</returns>
    /// <exception cref="ApplicationFailureException">Thrown when the stage or competition is missing.</exception>
    public async Task<StageOverviewDto> GetStageOverviewAsync(
        StageId stageId,
        CancellationToken cancellationToken = default)
    {
        var stage = await stages.GetByIdReadOnlyAsync(stageId, StageLoadProfile.Full, cancellationToken)
                        .ConfigureAwait(false)
                    ?? throw new ApplicationFailureException(
                        $"Stage '{stageId}' was not found.",
                        ApplicationErrorCodes.StageNotFound);

        var competition = await competitions.GetByIdReadOnlyAsync(stage.CompetitionId, cancellationToken)
                              .ConfigureAwait(false)
                          ?? throw new ApplicationFailureException(
                              $"Competition '{stage.CompetitionId}' was not found.",
                              ApplicationErrorCodes.CompetitionNotFound);

        return StageOverviewAssembler.Assemble(stage, competition);
    }

    /// <summary>
    /// Loads a stage schematic read model (form units + placed entries).
    /// </summary>
    /// <param name="stageId">Stage identity.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The stage schematic.</returns>
    /// <exception cref="ApplicationFailureException">Thrown when the stage or competition is missing.</exception>
    public async Task<StageSchematicDto> GetStageSchematicAsync(
        StageId stageId,
        CancellationToken cancellationToken = default)
    {
        var stage = await stages.GetByIdReadOnlyAsync(stageId, StageLoadProfile.Full, cancellationToken)
                        .ConfigureAwait(false)
                    ?? throw new ApplicationFailureException(
                        $"Stage '{stageId}' was not found.",
                        ApplicationErrorCodes.StageNotFound);

        var competition = await competitions.GetByIdReadOnlyAsync(stage.CompetitionId, cancellationToken)
                              .ConfigureAwait(false)
                          ?? throw new ApplicationFailureException(
                              $"Competition '{stage.CompetitionId}' was not found.",
                              ApplicationErrorCodes.CompetitionNotFound);

        IReadOnlyList<Stage> competitionStages;
        if (competition.StageIds.Count == 0)
        {
            competitionStages = [stage];
        }
        else
        {
            var loaded = await stages.GetByIdsReadOnlyAsync(
                    competition.StageIds,
                    StageLoadProfile.Full,
                    cancellationToken)
                .ConfigureAwait(false);
            if (loaded.Count != competition.StageIds.Count)
            {
                throw new ApplicationFailureException(
                    "One or more competition stages were not found.",
                    ApplicationErrorCodes.StageNotFound);
            }

            competitionStages = loaded.All(candidate => !candidate.Id.Equals(stage.Id))
                ? [.. loaded, stage]
                : loaded;
        }

        // Match rows enrich cup fixture connections when sides are already attached.
        var matchRows = await matches.ListSummaryRowsByStageReadOnlyAsync(stageId, cancellationToken)
            .ConfigureAwait(false);

        return StageSchematicAssembler.Assemble(stage, competition, competitionStages, matchRows);
    }

    /// <summary>
    /// Lists matches for a stage as <see cref="MatchSummaryDto"/> rows (stable fixture/round order).
    /// </summary>
    /// <param name="stageId">Stage identity.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Match summaries (possibly empty).</returns>
    /// <exception cref="ApplicationFailureException">Thrown when the stage or competition is missing.</exception>
    public async Task<IReadOnlyList<MatchSummaryDto>> ListMatchesByStageAsync(
        StageId stageId,
        CancellationToken cancellationToken = default)
    {
        var stage = await stages.GetByIdReadOnlyAsync(stageId, StageLoadProfile.Structure, cancellationToken)
                        .ConfigureAwait(false)
                    ?? throw new ApplicationFailureException(
                        $"Stage '{stageId}' was not found.",
                        ApplicationErrorCodes.StageNotFound);

        var competition = await competitions.GetByIdReadOnlyAsync(stage.CompetitionId, cancellationToken)
                              .ConfigureAwait(false)
                          ?? throw new ApplicationFailureException(
                              $"Competition '{stage.CompetitionId}' was not found.",
                              ApplicationErrorCodes.CompetitionNotFound);

        var matchRows = await matches.ListSummaryRowsByStageReadOnlyAsync(stageId, cancellationToken)
            .ConfigureAwait(false);
        return MatchReadAssembler.AssembleSummaries(stage, competition, matchRows);
    }

    /// <summary>
    /// Loads a match and assembles <see cref="MatchDetailDto"/> (no Winner).
    /// </summary>
    /// <param name="matchId">Match identity.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The match detail.</returns>
    /// <exception cref="ApplicationFailureException">Thrown when the match or competition is missing.</exception>
    public async Task<MatchDetailDto> GetMatchDetailAsync(
        MatchId matchId,
        CancellationToken cancellationToken = default)
    {
        var match = await matches.GetByIdReadOnlyAsync(matchId, cancellationToken).ConfigureAwait(false)
                    ?? throw new ApplicationFailureException(
                        $"Match '{matchId}' was not found.",
                        ApplicationErrorCodes.MatchNotFound);

        var competition = await competitions.GetByIdReadOnlyAsync(match.CompetitionId, cancellationToken)
                              .ConfigureAwait(false)
                          ?? throw new ApplicationFailureException(
                              $"Competition '{match.CompetitionId}' was not found.",
                              ApplicationErrorCodes.CompetitionNotFound);

        var stage = await stages.GetByIdReadOnlyAsync(match.StageId, StageLoadProfile.Structure, cancellationToken)
            .ConfigureAwait(false);
        return MatchReadAssembler.AssembleDetail(match, competition, stage);
    }

    private static void EnsureCompetitionAllowsConsequenceOperation(Competition competition)
    {
        if (competition.Status is CompetitionStatus.Completed or CompetitionStatus.Archived)
        {
            throw new ApplicationFailureException(
                $"Consequence operations are not allowed when competition is '{competition.Status}'.",
                ApplicationErrorCodes.ConsequenceOperationNotAllowed);
        }
    }

    private static void EnsureCompetitionAllowsLifecycleMutation(Competition competition)
    {
        if (competition.Status is CompetitionStatus.Completed or CompetitionStatus.Archived)
        {
            throw new ApplicationFailureException(
                $"Lifecycle mutations are not allowed when competition is '{competition.Status}'.",
                ApplicationErrorCodes.CompetitionClosed);
        }
    }

    private async Task EnsureCompetitionAllowsMatchOperationAsync(
        CompetitionId competitionId,
        CancellationToken cancellationToken)
    {
        var competition =
            await competitions.GetByIdForUpdateAsync(competitionId, cancellationToken).ConfigureAwait(false)
            ?? throw new ApplicationFailureException(
                $"Competition '{competitionId}' was not found.",
                ApplicationErrorCodes.CompetitionNotFound);

        if (competition.Status is CompetitionStatus.Completed or CompetitionStatus.Archived)
        {
            throw new ApplicationFailureException(
                $"Match operations are not allowed when competition is '{competition.Status}'.",
                ApplicationErrorCodes.MatchOperationNotAllowed);
        }
    }

    private async Task<Match> RequireMatchForOperationAsync(
        MatchId matchId,
        CancellationToken cancellationToken)
    {
        var match = await matches.GetByIdForUpdateAsync(matchId, cancellationToken).ConfigureAwait(false)
                    ?? throw new ApplicationFailureException(
                        $"Match '{matchId}' was not found.",
                        ApplicationErrorCodes.MatchNotFound);

        await EnsureCompetitionAllowsMatchOperationAsync(match.CompetitionId, cancellationToken)
            .ConfigureAwait(false);
        return match;
    }

    private async Task<(Match Match, Competition Competition)> RequireMatchWithCompetitionForOperationAsync(
        MatchId matchId,
        CancellationToken cancellationToken)
    {
        var match = await matches.GetByIdForUpdateAsync(matchId, cancellationToken).ConfigureAwait(false)
                    ?? throw new ApplicationFailureException(
                        $"Match '{matchId}' was not found.",
                        ApplicationErrorCodes.MatchNotFound);

        var competition = await competitions.GetByIdForUpdateAsync(match.CompetitionId, cancellationToken)
                              .ConfigureAwait(false)
                          ?? throw new ApplicationFailureException(
                              $"Competition '{match.CompetitionId}' was not found.",
                              ApplicationErrorCodes.CompetitionNotFound);

        return competition.Status is CompetitionStatus.Completed or CompetitionStatus.Archived
            ? throw new ApplicationFailureException(
                $"Match operations are not allowed when competition is '{competition.Status}'.",
                ApplicationErrorCodes.MatchOperationNotAllowed)
            : (match, competition);
    }

    private async Task EnsureCompetitionAllowsLifecycleMutationAsync(
        CompetitionId competitionId,
        CancellationToken cancellationToken)
    {
        var competition =
            await competitions.GetByIdForUpdateAsync(competitionId, cancellationToken).ConfigureAwait(false)
            ?? throw new ApplicationFailureException(
                $"Competition '{competitionId}' was not found.",
                ApplicationErrorCodes.CompetitionNotFound);

        EnsureCompetitionAllowsLifecycleMutation(competition);
    }

    private async Task<Dictionary<StageId, IReadOnlyList<Match>>> LoadMatchesByStageReadOnlyAsync(
        List<Stage> competitionStages,
        MatchLoadProfile profile,
        CancellationToken cancellationToken) =>
        competitionStages.Count == 0
            ? []
            : profile != MatchLoadProfile.Full
                ? throw new ArgumentOutOfRangeException(
                    nameof(profile),
                    profile,
                    "CompetitionReadBundle currently supports Full match loads only.")
                : await LoadMatchesByStageReadOnlyAsync(competitionStages, cancellationToken).ConfigureAwait(false);

    private async Task<Dictionary<StageId, IReadOnlyList<MatchSummaryRow>>> LoadSummaryRowsByStageReadOnlyAsync(
        List<Stage> competitionStages,
        CancellationToken cancellationToken)
    {
        if (competitionStages.Count == 0)
        {
            return [];
        }

        var stageIds = competitionStages.Select(stage => stage.Id).ToArray();
        var loaded = await matches
            .ListSummaryRowsByStageIdsReadOnlyAsync(stageIds, cancellationToken)
            .ConfigureAwait(false);
        var rowsByStage = new Dictionary<StageId, IReadOnlyList<MatchSummaryRow>>(competitionStages.Count);
        foreach (var stage in competitionStages)
        {
            rowsByStage[stage.Id] = loaded.TryGetValue(stage.Id, out var list)
                ? list
                : [];
        }

        return rowsByStage;
    }

    private async Task<Dictionary<StageId, IReadOnlyList<MatchAttentionSlice>>> LoadAttentionSlicesByStageReadOnlyAsync(
        List<Stage> competitionStages,
        CancellationToken cancellationToken)
    {
        if (competitionStages.Count == 0)
        {
            return [];
        }

        var stageIds = competitionStages.Select(stage => stage.Id).ToArray();
        var loaded = await matches
            .ListAttentionSlicesByStageIdsReadOnlyAsync(stageIds, cancellationToken)
            .ConfigureAwait(false);
        var matchesByStage = new Dictionary<StageId, IReadOnlyList<MatchAttentionSlice>>(competitionStages.Count);
        foreach (var stage in competitionStages)
        {
            matchesByStage[stage.Id] = loaded.TryGetValue(stage.Id, out var list)
                ? list
                : [];
        }

        return matchesByStage;
    }

    private async Task<Dictionary<StageId, IReadOnlyList<Match>>> LoadMatchesByStageReadOnlyAsync(
        List<Stage> competitionStages,
        CancellationToken cancellationToken)
    {
        if (competitionStages.Count == 0)
        {
            return [];
        }

        var stageIds = competitionStages.Select(stage => stage.Id).ToArray();
        var loaded = await matches.ListByStageIdsReadOnlyAsync(stageIds, cancellationToken).ConfigureAwait(false);
        var matchesByStage = new Dictionary<StageId, IReadOnlyList<Match>>(competitionStages.Count);
        foreach (var stage in competitionStages)
        {
            matchesByStage[stage.Id] = loaded.TryGetValue(stage.Id, out var list)
                ? list
                : [];
        }

        return matchesByStage;
    }

    private async Task<IReadOnlyList<Match>> LoadCompetitionMatchesAsync(
        Competition competition,
        CancellationToken cancellationToken)
    {
        if (competition.StageIds.Count == 0)
        {
            return [];
        }

        var competitionStages =
            await LoadCompetitionStagesForUpdateAsync(competition, cancellationToken).ConfigureAwait(false);
        var matchesByStage = await LoadMatchesByStageReadOnlyAsync(competitionStages, cancellationToken)
            .ConfigureAwait(false);
        return [.. matchesByStage.Values.SelectMany(stageMatches => stageMatches)];
    }

    private async Task<(List<Stage> Stages, IReadOnlyList<Match> Matches)> LoadCompetitionStagesAndMatchesForUpdateAsync(
        Competition competition,
        CancellationToken cancellationToken)
    {
        if (competition.StageIds.Count == 0)
        {
            return ([], []);
        }

        var competitionStages =
            await LoadCompetitionStagesForUpdateAsync(competition, cancellationToken).ConfigureAwait(false);
        var allMatches = new List<Match>();
        foreach (var stage in competitionStages)
        {
            var stageMatches = await matches.ListByStageForUpdateAsync(stage.Id, cancellationToken)
                .ConfigureAwait(false);
            allMatches.AddRange(stageMatches);
        }

        return (competitionStages, allMatches);
    }

    private async Task<AttentionReadBundle> LoadAttentionReadBundleAsync(
        CompetitionId competitionId,
        CancellationToken cancellationToken)
    {
        var competition =
            await competitions.GetByIdReadOnlyAsync(competitionId, cancellationToken).ConfigureAwait(false)
            ?? throw new ApplicationFailureException(
                $"Competition '{competitionId}' was not found.",
                ApplicationErrorCodes.CompetitionNotFound);

        return await LoadAttentionReadBundleAsync(competition, cancellationToken).ConfigureAwait(false);
    }

    private async Task<AttentionReadBundle> LoadAttentionReadBundleAsync(
        Competition competition,
        CancellationToken cancellationToken)
    {
        var spec = CompetitionReadBundleSpec.Attention;
        var competitionStages = await LoadCompetitionStagesReadOnlyAsync(
            competition,
            spec.StageCapabilities,
            cancellationToken).ConfigureAwait(false);
        var matchesByStage = await LoadAttentionSlicesByStageReadOnlyAsync(
            competitionStages,
            cancellationToken).ConfigureAwait(false);
        return new AttentionReadBundle(competition, competitionStages, matchesByStage);
    }

    private async Task<ConsultationReadBundle> LoadConsultationReadBundleAsync(
        CompetitionId competitionId,
        CancellationToken cancellationToken)
    {
        var competition =
            await competitions.GetByIdReadOnlyAsync(competitionId, cancellationToken).ConfigureAwait(false)
            ?? throw new ApplicationFailureException(
                $"Competition '{competitionId}' was not found.",
                ApplicationErrorCodes.CompetitionNotFound);

        return await LoadConsultationReadBundleAsync(competition, cancellationToken).ConfigureAwait(false);
    }

    private async Task<ConsultationReadBundle> LoadConsultationReadBundleAsync(
        Competition competition,
        CancellationToken cancellationToken)
    {
        var spec = CompetitionReadBundleSpec.Consultation;
        var competitionStages = await LoadCompetitionStagesReadOnlyAsync(
            competition,
            spec.StageCapabilities,
            cancellationToken).ConfigureAwait(false);
        var matchesByStage = await LoadSummaryRowsByStageReadOnlyAsync(competitionStages, cancellationToken)
            .ConfigureAwait(false);
        return new ConsultationReadBundle(competition, competitionStages, matchesByStage);
    }

    private async Task<StructureReadBundle> LoadStructureReadBundleAsync(
        Competition competition,
        CancellationToken cancellationToken)
    {
        var spec = CompetitionReadBundleSpec.Structure;
        var competitionStagesReadOnly = await LoadCompetitionStagesReadOnlyAsync(
            competition,
            spec.StageCapabilities,
            cancellationToken).ConfigureAwait(false);
        var sheetMemberRefs = spec.MatchProfile == MatchLoadProfile.None
            ? await matches
                .ListSheetMemberRefsByCompetitionReadOnlyAsync(competition.Id, cancellationToken)
                .ConfigureAwait(false)
            : throw new InvalidOperationException("Structure bundle expects no match aggregate load.");
        return new StructureReadBundle(competition, competitionStagesReadOnly, sheetMemberRefs);
    }

    private async Task<CompetitionReadBundle> LoadCompetitionReadBundleAsync(
        CompetitionId competitionId,
        CompetitionReadBundleSpec spec,
        CancellationToken cancellationToken)
    {
        var competition =
            await competitions.GetByIdReadOnlyAsync(competitionId, cancellationToken).ConfigureAwait(false)
            ?? throw new ApplicationFailureException(
                $"Competition '{competitionId}' was not found.",
                ApplicationErrorCodes.CompetitionNotFound);

        return await LoadCompetitionReadBundleAsync(competition, spec, cancellationToken).ConfigureAwait(false);
    }

    private async Task<CompetitionReadBundle> LoadCompetitionReadBundleAsync(
        Competition competition,
        CompetitionReadBundleSpec spec,
        CancellationToken cancellationToken)
    {
        var competitionStages = await LoadCompetitionStagesReadOnlyAsync(
            competition,
            spec.StageCapabilities,
            cancellationToken).ConfigureAwait(false);
        var matchesByStage = await LoadMatchesByStageReadOnlyAsync(competitionStages, spec.MatchProfile, cancellationToken)
            .ConfigureAwait(false);
        return new CompetitionReadBundle(competition, competitionStages, matchesByStage);
    }

    private async Task<List<Stage>> LoadCompetitionStagesForUpdateAsync(
        Competition competition,
        CancellationToken cancellationToken)
    {
        var competitionStages = new List<Stage>(competition.StageIds.Count);
        foreach (var stageId in competition.StageIds)
        {
            var stage = await stages.GetByIdForUpdateAsync(stageId, cancellationToken).ConfigureAwait(false)
                        ?? throw new ApplicationFailureException(
                            $"Stage '{stageId}' was not found.",
                            ApplicationErrorCodes.StageNotFound);
            competitionStages.Add(stage);
        }

        return competitionStages;
    }

    private async Task<List<Stage>> LoadCompetitionStagesReadOnlyAsync(
        Competition competition,
        StageReadCapabilities capabilities,
        CancellationToken cancellationToken)
    {
        if (competition.StageIds.Count == 0)
        {
            return [];
        }

        var loaded = await stages.GetByIdsReadOnlyAsync(competition.StageIds, capabilities, cancellationToken)
            .ConfigureAwait(false);
        return loaded.Count != competition.StageIds.Count
            ? throw new ApplicationFailureException(
                "One or more competition stages were not found.",
                ApplicationErrorCodes.StageNotFound)
            : [.. loaded];
    }
}
