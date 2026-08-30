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
    private readonly List<RecordedSubstitution> _recordedSubstitutions = [];

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
    /// Gets the observed Live running score when MyClub opened Live; otherwise <see langword="null"/>.
    /// </summary>
    /// <remarks>
    /// Null means no Live was opened in Domain — not that the match was never played.
    /// Distinct from <see cref="MatchResult.Score"/>. Legitimate on Finished when the result was entered without Live.
    /// </remarks>
    public RunningScore? RunningScore { get; private set; }

    /// <summary>
    /// Gets a value indicating whether MyClub opened a Live for this match (<see cref="RunningScore"/> is not null).
    /// </summary>
    /// <remarks>
    /// Derived convenience only — not persisted. Means observed Live, not “the match was played in reality”.
    /// </remarks>
    public bool HasObservedLive => RunningScore is not null;

    /// <summary>
    /// Gets the declared composition for this match (not live on-field presence).
    /// </summary>
    public IReadOnlyList<DeclaredParticipation> DeclaredParticipations => _declaredParticipations.AsReadOnly();

    /// <summary>
    /// Gets the nominative goal attributions recorded on this match (distinct from running / official score).
    /// </summary>
    public IReadOnlyList<RecordedGoal> RecordedGoals => _recordedGoals.AsReadOnly();

    /// <summary>
    /// Gets the ordered substitution facts recorded on this match (distinct from declared composition).
    /// </summary>
    /// <remarks>
    /// Order is the business recording order preserved by the Domain and used to derive on-field presence.
    /// </remarks>
    public IReadOnlyList<RecordedSubstitution> RecordedSubstitutions => _recordedSubstitutions.AsReadOnly();

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
    /// Finishes the match with an official result (Scheduled, Postponed, or Live → Finished).
    /// </summary>
    /// <remarks>
    /// Does not open Live, does not create <see cref="RunningScore"/>.
    /// Finished describes business state, not whether MyClub observed play.
    /// </remarks>
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

        if (Status is not (MatchStatus.Scheduled or MatchStatus.Postponed or MatchStatus.Live))
        {
            throw new DomainException(
                $"Match can only be finished from Scheduled, Postponed, or Live (current: '{Status}').",
                MatchErrorCodes.InvalidTransition);
        }

        Status = MatchStatus.Finished;
        Result = result;
        Raise(new MatchFinished(Id, result.Type, result.Score, clock));
    }

    /// <summary>
    /// Replaces the official result of a finished match (administrative correction).
    /// </summary>
    /// <remarks>
    /// No-op when the new value equals the current result. Does not change Status,
    /// <see cref="RunningScore"/>, or <see cref="RecordedGoals"/>.
    /// </remarks>
    public void CorrectMatchResult(MatchResult result, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(clock);

        if (Status != MatchStatus.Finished || Result is null)
        {
            throw new DomainException(
                $"Match result can only be corrected when status is Finished (current: '{Status}').",
                MatchErrorCodes.InvalidTransition);
        }

        if (Result.Equals(result))
        {
            return;
        }

        Result = result;
        Raise(new MatchResultCorrected(Id, result, clock));
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
        if (_recordedGoals.Exists(goal =>
                goal.ScorerMemberId.Equals(memberId)
                || (goal.AssisterMemberId is { } assister && assister.Equals(memberId))))
        {
            throw new DomainException(
                $"Declared participation '{memberId}' is referenced by a recorded goal on match '{Id}'.",
                MatchErrorCodes.ParticipationReferencedByRecordedGoal);
        }

        if (_recordedSubstitutions.Exists(substitution =>
                substitution.OutMemberId.Equals(memberId)
                || substitution.InMemberId.Equals(memberId)))
        {
            throw new DomainException(
                $"Declared participation '{memberId}' is referenced by a recorded substitution on match '{Id}'.",
                MatchErrorCodes.ParticipationReferencedBySubstitution);
        }

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
    /// Records a nominative goal attribution.
    /// Does not mutate <see cref="RunningScore"/> or <see cref="Result"/>.
    /// </summary>
    /// <remarks>
    /// Allowed when Scheduled, Postponed, Live, or Finished without observed Live.
    /// Forbidden when Finished with observed Live, or Cancelled.
    /// </remarks>
    public RecordedGoal RecordGoal(
        MemberId scorerMemberId,
        Side creditedSide,
        IClock clock,
        MemberId? assisterMemberId = null) =>
        RecordGoal(GoalId.New(), scorerMemberId, creditedSide, clock, assisterMemberId);

    /// <summary>
    /// Records a nominative goal attribution with an explicit identity.
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
        EnsureRecordedGoalCreateOrRemoveAllowed();
        EnsureValidRecordedGoalAttribution(scorerMemberId, creditedSide, assisterMemberId);

        var goal = new RecordedGoal(goalId, scorerMemberId, creditedSide, assisterMemberId);
        _recordedGoals.Add(goal);
        Raise(new MatchRecordedGoalAdded(
            Id, goal.Id, scorerMemberId, creditedSide, assisterMemberId, clock));
        return goal;
    }

    /// <summary>
    /// Corrects an existing nominative goal attribution.
    /// Does not mutate <see cref="RunningScore"/> or <see cref="Result"/>.
    /// </summary>
    /// <remarks>
    /// Allowed while Scheduled, Postponed, Live, Finished (with or without observed Live).
    /// Forbidden when Cancelled.
    /// </remarks>
    public void CorrectRecordedGoal(
        GoalId goalId,
        MemberId scorerMemberId,
        Side creditedSide,
        IClock clock,
        MemberId? assisterMemberId = null)
    {
        ArgumentNullException.ThrowIfNull(clock);
        EnsureRecordedGoalCorrectAllowed();
        ApplyRecordedGoalCorrection(goalId, scorerMemberId, creditedSide, assisterMemberId, clock);
    }

    /// <summary>
    /// Removes a nominative goal attribution.
    /// Does not mutate <see cref="RunningScore"/> or <see cref="Result"/>.
    /// </summary>
    /// <remarks>
    /// Allowed when Scheduled, Postponed, Live, or Finished without observed Live.
    /// Forbidden when Finished with observed Live, or Cancelled.
    /// </remarks>
    public void RemoveRecordedGoal(GoalId goalId, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        EnsureRecordedGoalCreateOrRemoveAllowed();

        var goal = GetRecordedGoal(goalId);
        _recordedGoals.Remove(goal);
        Raise(new MatchRecordedGoalRemoved(Id, goalId, clock));
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

    /// <summary>
    /// Records an ordered substitution fact (player out / player in).
    /// Does not mutate declared composition, <see cref="RunningScore"/>, or <see cref="Result"/>.
    /// </summary>
    public RecordedSubstitution RecordSubstitution(
        MemberId outMemberId,
        MemberId inMemberId,
        Side side,
        IClock clock) =>
        RecordSubstitution(SubstitutionId.New(), outMemberId, inMemberId, side, clock);

    /// <summary>
    /// Records an ordered substitution fact with an explicit identity.
    /// </summary>
    public RecordedSubstitution RecordSubstitution(
        SubstitutionId substitutionId,
        MemberId outMemberId,
        MemberId inMemberId,
        Side side,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        EnsureSubstitutionCreateOrRemoveAllowed();
        EnsureValidSubstitutionFact(outMemberId, inMemberId, side);

        var tentative = _recordedSubstitutions.ToList();
        tentative.Add(new RecordedSubstitution(substitutionId, side, outMemberId, inMemberId));
        EnsureSubstitutionSequenceValid(tentative);

        var substitution = tentative[^1];
        _recordedSubstitutions.Add(substitution);
        Raise(new MatchRecordedSubstitutionAdded(
            Id, substitution.Id, side, outMemberId, inMemberId, clock));
        return substitution;
    }

    /// <summary>
    /// Corrects an existing substitution fact (atomic Side / Out / In replace).
    /// </summary>
    public void CorrectRecordedSubstitution(
        SubstitutionId substitutionId,
        MemberId outMemberId,
        MemberId inMemberId,
        Side side,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        EnsureSubstitutionCorrectAllowed();
        EnsureValidSubstitutionFact(outMemberId, inMemberId, side);

        var index = _recordedSubstitutions.FindIndex(item => item.Id.Equals(substitutionId));
        if (index < 0)
        {
            throw new DomainException(
                $"Recorded substitution '{substitutionId}' was not found on match '{Id}'.",
                MatchErrorCodes.SubstitutionNotFound);
        }

        var existing = _recordedSubstitutions[index];
        if (existing.Side == side
            && existing.OutMemberId.Equals(outMemberId)
            && existing.InMemberId.Equals(inMemberId))
        {
            return;
        }

        var tentative = _recordedSubstitutions.ToList();
        tentative[index] = new RecordedSubstitution(substitutionId, side, outMemberId, inMemberId);
        EnsureSubstitutionSequenceValid(tentative);

        existing.Correct(side, outMemberId, inMemberId);
        Raise(new MatchRecordedSubstitutionChanged(
            Id, substitutionId, side, outMemberId, inMemberId, clock));
    }

    /// <summary>
    /// Removes a substitution fact.
    /// </summary>
    public void RemoveRecordedSubstitution(SubstitutionId substitutionId, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        EnsureSubstitutionCreateOrRemoveAllowed();

        var index = _recordedSubstitutions.FindIndex(item => item.Id.Equals(substitutionId));
        if (index < 0)
        {
            throw new DomainException(
                $"Recorded substitution '{substitutionId}' was not found on match '{Id}'.",
                MatchErrorCodes.SubstitutionNotFound);
        }

        var tentative = _recordedSubstitutions.ToList();
        tentative.RemoveAt(index);
        EnsureSubstitutionSequenceValid(tentative);

        _recordedSubstitutions.RemoveAt(index);
        Raise(new MatchRecordedSubstitutionRemoved(Id, substitutionId, clock));
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

        if (assisterMemberId is not { } assister) return;
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

    private bool CanMutateRecordedGoalsFreely =>
        Status is MatchStatus.Scheduled or MatchStatus.Postponed or MatchStatus.Live
        || (Status == MatchStatus.Finished && !HasObservedLive);

    private void EnsureRecordedGoalCreateOrRemoveAllowed()
    {
        if (!CanMutateRecordedGoalsFreely)
        {
            throw new DomainException(
                $"Recorded goal create/remove is not allowed when status is '{Status}'"
                + (HasObservedLive ? " with an observed Live." : "."),
                MatchErrorCodes.RecordedGoalMutationNotAllowed);
        }
    }

    private void EnsureRecordedGoalCorrectAllowed()
    {
        if (CanMutateRecordedGoalsFreely
            || (Status == MatchStatus.Finished && HasObservedLive))
        {
            return;
        }

        throw new DomainException(
            $"Recorded goal correction is not allowed when status is '{Status}'.",
            MatchErrorCodes.RecordedGoalMutationNotAllowed);
    }

    private RecordedGoal GetRecordedGoal(GoalId goalId) =>
        _recordedGoals.FirstOrDefault(goal => goal.Id.Equals(goalId))
        ?? throw new DomainException(
            $"Recorded goal '{goalId}' was not found on match '{Id}'.",
            MatchErrorCodes.RecordedGoalNotFound);

    private bool CanMutateSubstitutionsFreely =>
        Status == MatchStatus.Live
        || (Status == MatchStatus.Finished && !HasObservedLive);

    private void EnsureSubstitutionCreateOrRemoveAllowed()
    {
        if (!CanMutateSubstitutionsFreely)
        {
            throw new DomainException(
                $"Substitution create/remove is not allowed when status is '{Status}'"
                + (HasObservedLive ? " with an observed Live." : "."),
                MatchErrorCodes.SubstitutionMutationNotAllowed);
        }
    }

    private void EnsureSubstitutionCorrectAllowed()
    {
        if (CanMutateSubstitutionsFreely
            || (Status == MatchStatus.Finished && HasObservedLive))
        {
            return;
        }

        throw new DomainException(
            $"Substitution correction is not allowed when status is '{Status}'.",
            MatchErrorCodes.SubstitutionMutationNotAllowed);
    }

    private void EnsureValidSubstitutionFact(MemberId outMemberId, MemberId inMemberId, Side side)
    {
        if (!Enum.IsDefined(side))
        {
            throw new DomainException(
                $"Unknown match side '{side}'.",
                MatchErrorCodes.InvalidSide);
        }

        if (outMemberId.Equals(inMemberId))
        {
            throw new DomainException(
                "Substitution out and in members must be different.",
                MatchErrorCodes.SubstitutionSameMember);
        }

        var outParticipation = GetDeclaredParticipation(outMemberId);
        var inParticipation = GetDeclaredParticipation(inMemberId);

        if (outParticipation.Side != side || inParticipation.Side != side)
        {
            throw new DomainException(
                $"Substitution members must both belong to side '{side}'.",
                MatchErrorCodes.SubstitutionSideMismatch);
        }
    }

    /// <summary>
    /// Validates that a substitution sequence is consistent with derived presence
    /// (Starter baseline on-field, Bench off-field, then ordered replay).
    /// </summary>
    private void EnsureSubstitutionSequenceValid(IReadOnlyList<RecordedSubstitution> sequence)
    {
        var onFieldBySide = new Dictionary<Side, HashSet<MemberId>>
        {
            [Side.Home] = [],
            [Side.Away] = []
        };

        foreach (var participation in _declaredParticipations)
        {
            if (participation.CompositionStatus == CompositionStatus.Starter)
            {
                onFieldBySide[participation.Side].Add(participation.Id);
            }
        }

        foreach (var substitution in sequence)
        {
            var onField = onFieldBySide[substitution.Side];
            if (!onField.Contains(substitution.OutMemberId) || onField.Contains(substitution.InMemberId))
            {
                throw new DomainException(
                    "Substitution is inconsistent with derived on-field presence.",
                    MatchErrorCodes.SubstitutionPresenceInvalid);
            }

            onField.Remove(substitution.OutMemberId);
            onField.Add(substitution.InMemberId);
        }
    }

    private DeclaredParticipation GetDeclaredParticipation(MemberId memberId) =>
        _declaredParticipations.FirstOrDefault(participation => participation.Id.Equals(memberId))
        ?? throw new DomainException(
            $"Declared participation '{memberId}' was not found on match '{Id}'.",
            MatchErrorCodes.ParticipationNotFound);

    private void EnsureCompositionMutable()
    {
        switch (Status)
        {
            case MatchStatus.Scheduled or MatchStatus.Postponed:
            case MatchStatus.Finished when !HasObservedLive:
                return;
            default:
                throw new DomainException(
                    $"Match composition cannot be mutated when status is '{Status}'"
                    + (HasObservedLive ? " with an observed Live." : "."),
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
