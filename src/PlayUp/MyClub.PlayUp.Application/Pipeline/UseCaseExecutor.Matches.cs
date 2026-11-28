// -----------------------------------------------------------------------
// <copyright file="UseCaseExecutor.Matches.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Matches;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Pipeline;

/// <content>
/// Match lifecycle and in-match recording commands.
/// </content>
public sealed partial class UseCaseExecutor
{
    /// <summary>
    /// Loads a match, runs <see cref="StartMatch"/>, and saves changes.
    /// </summary>
    /// <param name="matchId">Match identity.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the match is started and persisted.</returns>
    /// <exception cref="ApplicationFailureException">Thrown when the match does not exist, the competition is closed, or sportive exploitation is not open.</exception>
    public async Task StartMatchAsync(MatchId matchId, CancellationToken cancellationToken = default)
    {
        var competitionId = Guid.Empty;
        await ExecuteOnMatchCompetitionAsync(
            matchId,
            async innerToken =>
            {
                var match = await RequireMatchForOperationAsync(matchId, innerToken).ConfigureAwait(false);
                StartMatch.Execute(match, clock);
                await unitOfWork.SaveChangesAsync(innerToken).ConfigureAwait(false);
                competitionId = match.CompetitionId.Value;
            },
            cancellationToken).ConfigureAwait(false);
        LogMatchStarted(logger, matchId.Value, competitionId);
    }

    /// <summary>
    /// Loads a match, runs <see cref="FinishMatch"/>, and saves changes.
    /// </summary>
    /// <param name="matchId">Match identity.</param>
    /// <param name="result">Domain match result.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the match is finished and persisted.</returns>
    /// <exception cref="ApplicationFailureException">Thrown when the match does not exist, the competition is closed, or sportive exploitation is not open.</exception>
    public Task FinishMatchAsync(
        MatchId matchId,
        MatchResult result,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(result);

        return FinishMatchAndLogAfterCommitAsync(
            matchId,
            (_, _, _) => Task.FromResult(result),
            cancellationToken);
    }

    /// <summary>
    /// Finishes a match from HTTP entry, resolving confrontation tie shootout eligibility before building the result.
    /// </summary>
    public Task FinishMatchFromEntryAsync(
        MatchId matchId,
        MatchResultEntry entry,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);
        MatchResultEntryShape shape;
        try
        {
            shape = MatchResultFromEntry.ValidateShape(entry);
        }
        catch (Exception ex)
        {
            return Task.FromException(ex);
        }

        return FinishMatchAndLogAfterCommitAsync(
            matchId,
            async (match, stage, innerToken) => await MatchResultFromEntry
                .BuildAsync(stage, match, shape, matches, innerToken)
                .ConfigureAwait(false),
            cancellationToken);
    }

    private async Task FinishMatchAndLogAfterCommitAsync(
        MatchId matchId,
        Func<Match, Stage, CancellationToken, Task<MatchResult>> buildResult,
        CancellationToken cancellationToken)
    {
        var competitionId = Guid.Empty;
        await ExecuteOnMatchCompetitionAsync(
            matchId,
            async innerToken =>
            {
                var (match, stage) = await RequireMatchAndStageForOperationAsync(matchId, innerToken).ConfigureAwait(false);
                var result = await buildResult(match, stage, innerToken).ConfigureAwait(false);
                NonFinalLegEtTabEntryGuard.EnsureAllowed(stage, match.Id, result);
                FinishMatch.Execute(match, result, clock);
                await unitOfWork.SaveChangesAsync(innerToken).ConfigureAwait(false);
                competitionId = match.CompetitionId.Value;
            },
            cancellationToken).ConfigureAwait(false);
        LogMatchFinished(logger, matchId.Value, competitionId);
    }

    /// <summary>
    /// Loads a finished match, runs <see cref="CorrectMatchResult"/>, and saves changes.
    /// </summary>
    public Task CorrectMatchResultAsync(
        MatchId matchId,
        MatchResult result,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(result);

        return CorrectMatchResultAndCommitAsync(
            matchId,
            (_, _, _) => Task.FromResult(result),
            cancellationToken);
    }

    /// <summary>
    /// Corrects a finished match from HTTP entry, resolving confrontation tie shootout eligibility before building the result.
    /// </summary>
    public Task CorrectMatchResultFromEntryAsync(
        MatchId matchId,
        MatchResultEntry entry,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);
        MatchResultEntryShape shape;
        try
        {
            shape = MatchResultFromEntry.ValidateShape(entry);
        }
        catch (Exception ex)
        {
            return Task.FromException(ex);
        }

        return CorrectMatchResultAndCommitAsync(
            matchId,
            async (match, stage, innerToken) => await MatchResultFromEntry
                .BuildAsync(stage, match, shape, matches, innerToken)
                .ConfigureAwait(false),
            cancellationToken);
    }

    private Task CorrectMatchResultAndCommitAsync(
        MatchId matchId,
        Func<Match, Stage, CancellationToken, Task<MatchResult>> buildResult,
        CancellationToken cancellationToken) =>
        ExecuteOnMatchCompetitionAsync(
            matchId,
            async innerToken =>
            {
                var (match, stage) = await RequireMatchAndStageForOperationAsync(matchId, innerToken).ConfigureAwait(false);
                var result = await buildResult(match, stage, innerToken).ConfigureAwait(false);
                NonFinalLegEtTabEntryGuard.EnsureAllowed(stage, match.Id, result);
                CorrectMatchResult.Execute(match, result, clock);
                await unitOfWork.SaveChangesAsync(innerToken).ConfigureAwait(false);
            },
            cancellationToken);

    /// <summary>
    /// Adds an eligible player to the match composition sheet and saves changes.
    /// </summary>
    public Task AddDeclaredParticipationAsync(
        MatchId matchId,
        MemberId memberId,
        Side side,
        CompositionStatus compositionStatus,
        int? jerseyNumber = null,
        CancellationToken cancellationToken = default) =>
        ExecuteOnMatchCompetitionAsync(
            matchId,
            async innerToken =>
            {
                var (match, competition) = await RequireMatchWithCompetitionForOperationAsync(matchId, innerToken)
                    .ConfigureAwait(false);
                AddDeclaredParticipation.Execute(match, competition, memberId, side, compositionStatus, clock, jerseyNumber);
                await unitOfWork.SaveChangesAsync(innerToken).ConfigureAwait(false);
            },
            cancellationToken);

    /// <summary>
    /// Removes a declared participation from the match sheet and saves changes.
    /// </summary>
    public Task RemoveDeclaredParticipationAsync(
        MatchId matchId,
        MemberId memberId,
        CancellationToken cancellationToken = default) =>
        ExecuteOnMatchCompetitionAsync(
            matchId,
            async innerToken =>
            {
                var match = await RequireMatchForOperationAsync(matchId, innerToken).ConfigureAwait(false);
                RemoveDeclaredParticipation.Execute(match, memberId, clock);
                await unitOfWork.SaveChangesAsync(innerToken).ConfigureAwait(false);
            },
            cancellationToken);

    /// <summary>
    /// Changes starter/bench status on the match sheet and saves changes.
    /// </summary>
    public Task ChangeDeclaredParticipationCompositionStatusAsync(
        MatchId matchId,
        MemberId memberId,
        CompositionStatus compositionStatus,
        CancellationToken cancellationToken = default) =>
        ExecuteOnMatchCompetitionAsync(
            matchId,
            async innerToken =>
            {
                var match = await RequireMatchForOperationAsync(matchId, innerToken).ConfigureAwait(false);
                ChangeDeclaredParticipationCompositionStatus.Execute(match, memberId, compositionStatus, clock);
                await unitOfWork.SaveChangesAsync(innerToken).ConfigureAwait(false);
            },
            cancellationToken);

    /// <summary>
    /// Sets or clears a jersey number on the match sheet and saves changes.
    /// </summary>
    public Task SetDeclaredParticipationJerseyNumberAsync(
        MatchId matchId,
        MemberId memberId,
        int? jerseyNumber,
        CancellationToken cancellationToken = default) =>
        ExecuteOnMatchCompetitionAsync(
            matchId,
            async innerToken =>
            {
                var match = await RequireMatchForOperationAsync(matchId, innerToken).ConfigureAwait(false);
                SetDeclaredParticipationJerseyNumber.Execute(match, memberId, jerseyNumber, clock);
                await unitOfWork.SaveChangesAsync(innerToken).ConfigureAwait(false);
            },
            cancellationToken);

    /// <summary>
    /// Replaces the observed Live running score and saves changes.
    /// </summary>
    public Task SetRunningScoreAsync(
        MatchId matchId,
        int homeGoals,
        int awayGoals,
        CancellationToken cancellationToken = default) =>
        ExecuteOnMatchCompetitionAsync(
            matchId,
            async innerToken =>
            {
                var match = await RequireMatchForOperationAsync(matchId, innerToken).ConfigureAwait(false);
                SetRunningScore.Execute(match, homeGoals, awayGoals, clock);
                await unitOfWork.SaveChangesAsync(innerToken).ConfigureAwait(false);
            },
            cancellationToken);

    /// <summary>
    /// Records a nominative goal and saves changes.
    /// </summary>
    public Task RecordGoalAsync(
        MatchId matchId,
        MemberId scorerMemberId,
        Side creditedSide,
        MemberId? assisterMemberId = null,
        CancellationToken cancellationToken = default) =>
        ExecuteOnMatchCompetitionAsync(
            matchId,
            async innerToken =>
            {
                var match = await RequireMatchForOperationAsync(matchId, innerToken).ConfigureAwait(false);
                RecordGoal.Execute(match, scorerMemberId, creditedSide, clock, assisterMemberId);
                await unitOfWork.SaveChangesAsync(innerToken).ConfigureAwait(false);
            },
            cancellationToken);

    /// <summary>
    /// Corrects a nominative goal and saves changes.
    /// </summary>
    public Task CorrectRecordedGoalAsync(
        MatchId matchId,
        GoalId goalId,
        MemberId scorerMemberId,
        Side creditedSide,
        MemberId? assisterMemberId = null,
        CancellationToken cancellationToken = default) =>
        ExecuteOnMatchCompetitionAsync(
            matchId,
            async innerToken =>
            {
                var match = await RequireMatchForOperationAsync(matchId, innerToken).ConfigureAwait(false);
                CorrectRecordedGoal.Execute(match, goalId, scorerMemberId, creditedSide, clock, assisterMemberId);
                await unitOfWork.SaveChangesAsync(innerToken).ConfigureAwait(false);
            },
            cancellationToken);

    /// <summary>
    /// Removes a nominative goal and saves changes.
    /// </summary>
    public Task RemoveRecordedGoalAsync(
        MatchId matchId,
        GoalId goalId,
        CancellationToken cancellationToken = default) =>
        ExecuteOnMatchCompetitionAsync(
            matchId,
            async innerToken =>
            {
                var match = await RequireMatchForOperationAsync(matchId, innerToken).ConfigureAwait(false);
                RemoveRecordedGoal.Execute(match, goalId, clock);
                await unitOfWork.SaveChangesAsync(innerToken).ConfigureAwait(false);
            },
            cancellationToken);

    /// <summary>
    /// Records a substitution fact and saves changes.
    /// </summary>
    public Task RecordSubstitutionAsync(
        MatchId matchId,
        MemberId outMemberId,
        MemberId inMemberId,
        Side side,
        CancellationToken cancellationToken = default) =>
        ExecuteOnMatchCompetitionAsync(
            matchId,
            async innerToken =>
            {
                var match = await RequireMatchForOperationAsync(matchId, innerToken).ConfigureAwait(false);
                RecordSubstitution.Execute(match, outMemberId, inMemberId, side, clock);
                await unitOfWork.SaveChangesAsync(innerToken).ConfigureAwait(false);
            },
            cancellationToken);

    /// <summary>
    /// Corrects a substitution fact and saves changes.
    /// </summary>
    public Task CorrectRecordedSubstitutionAsync(
        MatchId matchId,
        SubstitutionId substitutionId,
        MemberId outMemberId,
        MemberId inMemberId,
        Side side,
        CancellationToken cancellationToken = default) =>
        ExecuteOnMatchCompetitionAsync(
            matchId,
            async innerToken =>
            {
                var match = await RequireMatchForOperationAsync(matchId, innerToken).ConfigureAwait(false);
                CorrectRecordedSubstitution.Execute(match, substitutionId, outMemberId, inMemberId, side, clock);
                await unitOfWork.SaveChangesAsync(innerToken).ConfigureAwait(false);
            },
            cancellationToken);

    /// <summary>
    /// Removes a substitution fact and saves changes.
    /// </summary>
    public Task RemoveRecordedSubstitutionAsync(
        MatchId matchId,
        SubstitutionId substitutionId,
        CancellationToken cancellationToken = default) =>
        ExecuteOnMatchCompetitionAsync(
            matchId,
            async innerToken =>
            {
                var match = await RequireMatchForOperationAsync(matchId, innerToken).ConfigureAwait(false);
                RemoveRecordedSubstitution.Execute(match, substitutionId, clock);
                await unitOfWork.SaveChangesAsync(innerToken).ConfigureAwait(false);
            },
            cancellationToken);

    /// <summary>
    /// Records a disciplinary fact when authorized by competition rules and saves changes.
    /// </summary>
    public Task RecordDisciplinaryEventAsync(
        MatchId matchId,
        MemberId memberId,
        DisciplinaryType type,
        CancellationToken cancellationToken = default) =>
        ExecuteOnMatchCompetitionAsync(
            matchId,
            async innerToken =>
            {
                var (match, competition) = await RequireMatchWithCompetitionForOperationAsync(matchId, innerToken)
                    .ConfigureAwait(false);
                RecordDisciplinaryEvent.Execute(match, competition, memberId, type, clock);
                await unitOfWork.SaveChangesAsync(innerToken).ConfigureAwait(false);
            },
            cancellationToken);

    /// <summary>
    /// Corrects a disciplinary fact when authorized by competition rules and saves changes.
    /// </summary>
    public Task CorrectRecordedDisciplinaryEventAsync(
        MatchId matchId,
        DisciplinaryEventId disciplinaryEventId,
        MemberId memberId,
        DisciplinaryType type,
        CancellationToken cancellationToken = default) =>
        ExecuteOnMatchCompetitionAsync(
            matchId,
            async innerToken =>
            {
                var (match, competition) = await RequireMatchWithCompetitionForOperationAsync(matchId, innerToken)
                    .ConfigureAwait(false);
                CorrectRecordedDisciplinaryEvent.Execute(match, competition, disciplinaryEventId, memberId, type, clock);
                await unitOfWork.SaveChangesAsync(innerToken).ConfigureAwait(false);
            },
            cancellationToken);

    /// <summary>
    /// Removes a disciplinary fact and saves changes.
    /// </summary>
    public Task RemoveRecordedDisciplinaryEventAsync(
        MatchId matchId,
        DisciplinaryEventId disciplinaryEventId,
        CancellationToken cancellationToken = default) =>
        ExecuteOnMatchCompetitionAsync(
            matchId,
            async innerToken =>
            {
                var match = await RequireMatchForOperationAsync(matchId, innerToken).ConfigureAwait(false);
                RemoveRecordedDisciplinaryEvent.Execute(match, disciplinaryEventId, clock);
                await unitOfWork.SaveChangesAsync(innerToken).ConfigureAwait(false);
            },
            cancellationToken);
}
