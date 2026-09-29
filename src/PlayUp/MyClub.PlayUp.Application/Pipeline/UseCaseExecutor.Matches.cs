// -----------------------------------------------------------------------
// <copyright file="UseCaseExecutor.Matches.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Matches;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Rules;

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
}
