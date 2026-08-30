// -----------------------------------------------------------------------
// <copyright file="Match.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches.Events;

namespace MyClub.PlayUp.Domain.Matches;

/// <summary>
/// Aggregate root for a sporting encounter between two competition entries.
/// </summary>
[DebuggerDisplay("{HomeEntryId} vs {AwayEntryId} ({Status})")]
public sealed class Match : AggregateRoot<MatchId>
{
    private readonly List<DeclaredParticipation> _declaredParticipations = [];
    private readonly List<RecordedGoal> _recordedGoals = [];

    private Match(
        MatchId id,
        CompetitionId competitionId,
        StageId stageId,
        EntryId homeEntryId,
        EntryId awayEntryId)
        : base(id)
    {
        CompetitionId = competitionId;
        StageId = stageId;
        HomeEntryId = homeEntryId;
        AwayEntryId = awayEntryId;
        Status = MatchStatus.Scheduled;
    }

    /// <summary>
    /// Gets the owning competition identity (immutable).
    /// </summary>
    public CompetitionId CompetitionId { get; }

    /// <summary>
    /// Gets the stage identity (immutable).
    /// </summary>
    public StageId StageId { get; }

    /// <summary>
    /// Gets the home entry identity (immutable).
    /// </summary>
    public EntryId HomeEntryId { get; }

    /// <summary>
    /// Gets the away entry identity (immutable).
    /// </summary>
    public EntryId AwayEntryId { get; }

    /// <summary>
    /// Gets the match lifecycle status.
    /// </summary>
    public MatchStatus Status { get; private set; }

    /// <summary>
    /// Gets the match result when finished; otherwise <see langword="null"/>.
    /// </summary>
    public MatchResult? Result { get; private set; }

    /// <summary>
    /// Gets the current running score when the match has been started; otherwise <see langword="null"/>.
    /// </summary>
    /// <remarks>
    /// Null exclusively means the match was never started. Distinct from <see cref="MatchResult.Score"/>.
    /// </remarks>
    public RunningScore? RunningScore { get; private set; }

    /// <summary>
    /// Gets the declared composition for this match (not live on-field presence).
    /// </summary>
    public IReadOnlyList<DeclaredParticipation> DeclaredParticipations => _declaredParticipations.AsReadOnly();

    /// <summary>
    /// Gets the nominative goal attributions recorded on this match (distinct from running / official score).
    /// </summary>
    public IReadOnlyList<RecordedGoal> RecordedGoals => _recordedGoals.AsReadOnly();

    /// <summary>
    /// Creates a new match in Scheduled status.
    /// </summary>
    /// <param name="competitionId">The competition identity.</param>
    /// <param name="stageId">The stage identity.</param>
    /// <param name="homeEntryId">The home entry identity.</param>
    /// <param name="awayEntryId">The away entry identity.</param>
    /// <param name="clock">The clock used for domain events.</param>
    /// <returns>The created match.</returns>
    public static Match Create(
        CompetitionId competitionId,
        StageId stageId,
        EntryId homeEntryId,
        EntryId awayEntryId,
        IClock clock) =>
        Create(competitionId, stageId, homeEntryId, awayEntryId, MatchId.New(), clock);

    /// <summary>
    /// Creates a new match in Scheduled status with an explicit identity.
    /// </summary>
    /// <param name="competitionId">The competition identity.</param>
    /// <param name="stageId">The stage identity.</param>
    /// <param name="homeEntryId">The home entry identity.</param>
    /// <param name="awayEntryId">The away entry identity.</param>
    /// <param name="id">The match identity (must not be empty).</param>
    /// <param name="clock">The clock used for domain events.</param>
    /// <returns>The created match.</returns>
    public static Match Create(
        CompetitionId competitionId,
        StageId stageId,
        EntryId homeEntryId,
        EntryId awayEntryId,
        MatchId id,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);

        if (homeEntryId.Equals(awayEntryId))
        {
            throw new DomainException(
                "Home and away entries must be different.",
                MatchErrorCodes.SameParticipant);
        }

        var match = new Match(id, competitionId, stageId, homeEntryId, awayEntryId);
        match.Raise(new MatchCreated(
            match.Id,
            competitionId,
            stageId,
            homeEntryId,
            awayEntryId,
            clock));
        return match;
    }

    /// <summary>
    /// Starts the match (Scheduled to Live). Composition may be empty. Initializes running score to 0–0.
    /// </summary>
    /// <param name="clock">The clock used for domain events.</param>
    public void Start(IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        EnsureStatus(MatchStatus.Scheduled, "Match can only be started from Scheduled.");
        Status = MatchStatus.Live;
        RunningScore = new RunningScore(0, 0);
        Raise(new MatchStarted(Id, clock));
    }

    /// <summary>
    /// Replaces the running score while the match is Live.
    /// </summary>
    /// <param name="runningScore">The absolute current running score.</param>
    /// <param name="clock">The clock used for domain events.</param>
    public void SetRunningScore(RunningScore runningScore, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);

        if (Status != MatchStatus.Live)
        {
            throw new DomainException(
                $"Running score cannot be mutated when status is '{Status}'.",
                MatchErrorCodes.RunningScoreImmutable);
        }

        if (RunningScore == runningScore)
        {
            return;
        }

        RunningScore = runningScore;
        Raise(new MatchRunningScoreChanged(Id, runningScore, clock));
    }

    /// <summary>
    /// Finishes the match with a result (Live to Finished).
    /// </summary>
    /// <param name="result">The match result.</param>
    /// <param name="clock">The clock used for domain events.</param>
    public void Finish(MatchResult result, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(clock);

        if (Status == MatchStatus.Finished || Result is not null)
        {
            throw new DomainException(
                "Match result is already recorded.",
                MatchErrorCodes.ResultAlreadyRecorded);
        }

        EnsureStatus(MatchStatus.Live, "Match can only be finished from Live.");

        Status = MatchStatus.Finished;
        Result = result;
        Raise(new MatchFinished(Id, result.Type, result.Score, clock));
    }

    /// <summary>
    /// Postpones the match (Scheduled to Postponed).
    /// </summary>
    /// <param name="clock">The clock used for domain events.</param>
    public void Postpone(IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        EnsureStatus(MatchStatus.Scheduled, "Match can only be postponed from Scheduled.");
        Status = MatchStatus.Postponed;
        Raise(new MatchPostponed(Id, clock));
    }

    /// <summary>
    /// Returns a postponed match to Scheduled.
    /// </summary>
    /// <param name="clock">The clock used for domain events.</param>
    public void ResumeSchedule(IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        EnsureStatus(MatchStatus.Postponed, "Match can only resume schedule from Postponed.");
        Status = MatchStatus.Scheduled;
        Raise(new MatchResumed(Id, clock));
    }

    /// <summary>
    /// Cancels the match.
    /// </summary>
    /// <param name="clock">The clock used for domain events.</param>
    public void Cancel(IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        if (Status is not (MatchStatus.Scheduled or MatchStatus.Live or MatchStatus.Postponed))
        {
            throw new DomainException(
                $"Match cannot be cancelled from '{Status}'.",
                MatchErrorCodes.InvalidTransition);
        }

        Status = MatchStatus.Cancelled;
        Raise(new MatchCancelled(Id, clock));
    }

    /// <summary>
    /// Adds a declared participation to the match composition.
    /// </summary>
    /// <remarks>
    /// Eligibility (member declared as Player on the side's entry, etc.) is an Application orchestration rule — not verified here.
    /// </remarks>
    public DeclaredParticipation AddDeclaredParticipation(
        MemberId memberId,
        Side side,
        CompositionStatus compositionStatus,
        IClock clock,
        int? jerseyNumber = null)
    {
        ArgumentNullException.ThrowIfNull(clock);
        EnsureCompositionMutable();

        if (_declaredParticipations.Exists(participation => participation.Id.Equals(memberId)))
        {
            throw new DomainException(
                $"Declared participation '{memberId}' already exists on match '{Id}'.",
                MatchErrorCodes.DuplicateParticipation);
        }

        EnsureJerseyAvailable(side, jerseyNumber, excludingMemberId: null);

        var participation = new DeclaredParticipation(memberId, side, compositionStatus, jerseyNumber);
        _declaredParticipations.Add(participation);
        Raise(new MatchDeclaredParticipationAdded(
            Id, memberId, side, compositionStatus, jerseyNumber, clock));
        return participation;
    }

    /// <summary>
    /// Removes a declared participation from the match composition.
    /// </summary>
    public void RemoveDeclaredParticipation(MemberId memberId, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        EnsureCompositionMutable();

        var participation = GetDeclaredParticipation(memberId);
        _declaredParticipations.Remove(participation);
        Raise(new MatchDeclaredParticipationRemoved(Id, memberId, clock));
    }

    /// <summary>
    /// Changes the composition status (starter / bench) of a declared participation.
    /// </summary>
    public void ChangeDeclaredParticipationCompositionStatus(
        MemberId memberId,
        CompositionStatus compositionStatus,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        EnsureCompositionMutable();

        var participation = GetDeclaredParticipation(memberId);
        participation.ChangeCompositionStatus(compositionStatus);
        Raise(new MatchDeclaredParticipationCompositionStatusChanged(
            Id, memberId, compositionStatus, clock));
    }

    /// <summary>
    /// Sets or clears the jersey number of a declared participation.
    /// </summary>
    public void SetDeclaredParticipationJerseyNumber(MemberId memberId, int? jerseyNumber, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        EnsureCompositionMutable();

        var participation = GetDeclaredParticipation(memberId);
        EnsureJerseyAvailable(participation.Side, jerseyNumber, excludingMemberId: memberId);
        participation.SetJerseyNumber(jerseyNumber);
        Raise(new MatchDeclaredParticipationJerseyNumberChanged(Id, memberId, jerseyNumber, clock));
    }

    /// <summary>
    /// Returns whether the match composition references the given member.
    /// </summary>
    public bool HasDeclaredParticipation(MemberId memberId) =>
        _declaredParticipations.Exists(participation => participation.Id.Equals(memberId));

    /// <summary>
    /// Records a nominative goal attribution while the match is Live.
    /// Does not mutate <see cref="RunningScore"/> or <see cref="Result"/>.
    /// </summary>
    public RecordedGoal RecordGoal(
        MemberId scorerMemberId,
        Side creditedSide,
        IClock clock,
        MemberId? assisterMemberId = null) =>
        RecordGoal(GoalId.New(), scorerMemberId, creditedSide, clock, assisterMemberId);

    /// <summary>
    /// Records a nominative goal attribution while the match is Live, with an explicit identity.
    /// Does not mutate <see cref="RunningScore"/> or <see cref="Result"/>.
    /// </summary>
    public RecordedGoal RecordGoal(
        GoalId goalId,
        MemberId scorerMemberId,
        Side creditedSide,
        IClock clock,
        MemberId? assisterMemberId = null)
    {
        ArgumentNullException.ThrowIfNull(clock);
        EnsureLiveRecordedGoalMutation();
        EnsureValidRecordedGoalAttribution(scorerMemberId, creditedSide, assisterMemberId);

        var goal = new RecordedGoal(goalId, scorerMemberId, creditedSide, assisterMemberId);
        _recordedGoals.Add(goal);
        Raise(new MatchRecordedGoalAdded(
            Id, goal.Id, scorerMemberId, creditedSide, assisterMemberId, clock));
        return goal;
    }

    /// <summary>
    /// Corrects an existing nominative goal attribution while the match is Live.
    /// Does not mutate <see cref="RunningScore"/> or <see cref="Result"/>.
    /// </summary>
    public void CorrectRecordedGoal(
        GoalId goalId,
        MemberId scorerMemberId,
        Side creditedSide,
        IClock clock,
        MemberId? assisterMemberId = null)
    {
        ArgumentNullException.ThrowIfNull(clock);
        EnsureLiveRecordedGoalMutation();
        ApplyRecordedGoalCorrection(goalId, scorerMemberId, creditedSide, assisterMemberId, clock);
    }

    /// <summary>
    /// Removes a nominative goal attribution while the match is Live.
    /// Does not mutate <see cref="RunningScore"/> or <see cref="Result"/>.
    /// </summary>
    public void RemoveRecordedGoal(GoalId goalId, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        EnsureLiveRecordedGoalMutation();

        var goal = GetRecordedGoal(goalId);
        _recordedGoals.Remove(goal);
        Raise(new MatchRecordedGoalRemoved(Id, goalId, clock));
    }

    /// <summary>
    /// Corrects an existing nominative goal attribution after Finish.
    /// Explicit post-finish operation only — does not allow create or remove; does not mutate scores.
    /// </summary>
    public void CorrectRecordedGoalAfterFinish(
        GoalId goalId,
        MemberId scorerMemberId,
        Side creditedSide,
        IClock clock,
        MemberId? assisterMemberId = null)
    {
        ArgumentNullException.ThrowIfNull(clock);

        if (Status != MatchStatus.Finished)
        {
            throw new DomainException(
                $"Post-finish recorded goal correction is only allowed when status is Finished (current: '{Status}').",
                MatchErrorCodes.RecordedGoalMutationNotAllowed);
        }

        ApplyRecordedGoalCorrection(goalId, scorerMemberId, creditedSide, assisterMemberId, clock);
    }

    /// <summary>
    /// Returns whether the recorded goal is an own goal (scorer's sheet side differs from credited side).
    /// Derived — not stored on <see cref="RecordedGoal"/>.
    /// </summary>
    public bool IsOwnGoal(RecordedGoal goal)
    {
        ArgumentNullException.ThrowIfNull(goal);
        return GetDeclaredParticipation(goal.ScorerMemberId).Side != goal.CreditedSide;
    }

    private void ApplyRecordedGoalCorrection(
        GoalId goalId,
        MemberId scorerMemberId,
        Side creditedSide,
        MemberId? assisterMemberId,
        IClock clock)
    {
        EnsureValidRecordedGoalAttribution(scorerMemberId, creditedSide, assisterMemberId);

        var goal = GetRecordedGoal(goalId);
        if (goal.ScorerMemberId.Equals(scorerMemberId)
            && goal.CreditedSide == creditedSide
            && Equals(goal.AssisterMemberId, assisterMemberId))
        {
            return;
        }

        goal.Correct(scorerMemberId, creditedSide, assisterMemberId);
        Raise(new MatchRecordedGoalChanged(
            Id, goal.Id, scorerMemberId, creditedSide, assisterMemberId, clock));
    }

    private void EnsureValidRecordedGoalAttribution(
        MemberId scorerMemberId,
        Side creditedSide,
        MemberId? assisterMemberId)
    {
        if (!Enum.IsDefined(creditedSide))
        {
            throw new DomainException(
                $"Unknown match side '{creditedSide}'.",
                MatchErrorCodes.InvalidSide);
        }

        var scorerParticipation = GetDeclaredParticipation(scorerMemberId);

        if (assisterMemberId is { } assister)
        {
            if (assister.Equals(scorerMemberId))
            {
                throw new DomainException(
                    "Assister cannot be the same member as the scorer.",
                    MatchErrorCodes.AssisterSameAsScorer);
            }

            _ = GetDeclaredParticipation(assister);

            if (scorerParticipation.Side != creditedSide)
            {
                throw new DomainException(
                    "Assister is not allowed on an own goal (credited side differs from scorer sheet side).",
                    MatchErrorCodes.AssisterNotAllowedOnOwnGoal);
            }
        }
    }

    private void EnsureLiveRecordedGoalMutation()
    {
        if (Status != MatchStatus.Live)
        {
            throw new DomainException(
                $"Recorded goal create/correct/remove is only allowed while Live (current: '{Status}').",
                MatchErrorCodes.RecordedGoalMutationNotAllowed);
        }
    }

    private RecordedGoal GetRecordedGoal(GoalId goalId) =>
        _recordedGoals.FirstOrDefault(goal => goal.Id.Equals(goalId))
        ?? throw new DomainException(
            $"Recorded goal '{goalId}' was not found on match '{Id}'.",
            MatchErrorCodes.RecordedGoalNotFound);

    private DeclaredParticipation GetDeclaredParticipation(MemberId memberId) =>
        _declaredParticipations.FirstOrDefault(participation => participation.Id.Equals(memberId))
        ?? throw new DomainException(
            $"Declared participation '{memberId}' was not found on match '{Id}'.",
            MatchErrorCodes.ParticipationNotFound);

    private void EnsureCompositionMutable()
    {
        if (Status is not (MatchStatus.Scheduled or MatchStatus.Postponed))
        {
            throw new DomainException(
                $"Match composition cannot be mutated when status is '{Status}'.",
                MatchErrorCodes.CompositionImmutable);
        }
    }

    private void EnsureJerseyAvailable(Side side, int? jerseyNumber, MemberId? excludingMemberId)
    {
        if (jerseyNumber is null)
        {
            return;
        }

        var duplicate = _declaredParticipations.Exists(participation =>
            participation.Side == side
            && participation.JerseyNumber == jerseyNumber
            && (excludingMemberId is null || !participation.Id.Equals(excludingMemberId.Value)));

        if (duplicate)
        {
            throw new DomainException(
                $"Jersey number '{jerseyNumber}' is already used on the {side} side of match '{Id}'.",
                MatchErrorCodes.DuplicateJerseyNumber);
        }
    }

    private void EnsureStatus(MatchStatus expected, string message)
    {
        if (Status != expected)
        {
            throw new DomainException(message, MatchErrorCodes.InvalidTransition);
        }
    }
}
