// -----------------------------------------------------------------------
// <copyright file="UseCaseExecutor.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

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
/// (CreateCompetition, Organisation Slice 2, PrepareStage, StartStage, ApplyProgressionOutcome,
/// PublishDraw, ApplyDraw, StartMatch, FinishMatch, PrepareCompetition, StartCompetition,
/// CompleteCompetition, ArchiveCompetition, ListCompetitions, GetWorkspaceSummary,
/// GetCompetitionDetail, GetOrganisationView, GetStageOverview, ListMatchesByStage,
/// GetMatchDetail, GetConsultation).
/// </summary>
/// <remarks>
/// Command methods load aggregates via ports, run the static use case, then commit once via <see cref="IUnitOfWork"/>.
/// Read methods load aggregates, assemble product DTOs, and never call SaveChanges.
/// PrepareStage and ApplyProgressionOutcome load all competition stages (cross-stage destinations / feeds).
/// Does not know HTTP, EF Core, or Domain Event dispatch. Not a CQRS mediator — named methods only;
/// do not introduce generic dispatch without a demonstrated need.
/// Initializes a new instance of the <see cref="UseCaseExecutor"/> class.
/// </remarks>
/// <param name="stages">Stage persistence port.</param>
/// <param name="matches">Match persistence port.</param>
/// <param name="competitions">Competition persistence port (StageIds for multi-stage load).</param>
/// <param name="unitOfWork">Unit of work for a single commit after the use case.</param>
/// <param name="clock">Clock forwarded to Domain / Application.</param>
/// <param name="mediaReferences">Port that verifies Media identities exist before storing logo refs.</param>
public sealed class UseCaseExecutor(
    IStageRepository stages,
    IMatchRepository matches,
    ICompetitionRepository competitions,
    IUnitOfWork unitOfWork,
    IClock clock,
    IMediaReferenceChecker mediaReferences)
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

        var competition = await competitions.GetByIdForUpdateAsync(stage.CompetitionId, cancellationToken).ConfigureAwait(false)
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

        var competition = await competitions.GetByIdForUpdateAsync(stage.CompetitionId, cancellationToken).ConfigureAwait(false)
            ?? throw new ApplicationFailureException(
                $"Competition '{stage.CompetitionId}' was not found.",
                ApplicationErrorCodes.CompetitionNotFound);
        EnsureCompetitionAllowsLifecycleMutation(competition);

        StartStage.Execute(stage, clock);
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

        var competition = await competitions.GetByIdForUpdateAsync(source.CompetitionId, cancellationToken).ConfigureAwait(false)
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
    public async Task<IReadOnlyList<SlotAssignmentInstruction>> ApplyQualificationAsync(
        StageId sourceStageId,
        CancellationToken cancellationToken = default)
    {
        var source = await stages.GetByIdForUpdateAsync(sourceStageId, cancellationToken).ConfigureAwait(false)
            ?? throw new ApplicationFailureException(
                $"Stage '{sourceStageId}' was not found.",
                ApplicationErrorCodes.StageNotFound);

        var competition = await competitions.GetByIdForUpdateAsync(source.CompetitionId, cancellationToken).ConfigureAwait(false)
            ?? throw new ApplicationFailureException(
                $"Competition '{source.CompetitionId}' was not found.",
                ApplicationErrorCodes.CompetitionNotFound);

        EnsureCompetitionAllowsConsequenceOperation(competition);

        var competitionStages = await LoadCompetitionStagesForUpdateAsync(competition, cancellationToken).ConfigureAwait(false);
        if (!competitionStages.Exists(candidate => candidate.Id.Equals(sourceStageId)))
        {
            throw new ApplicationFailureException(
                $"Stage '{sourceStageId}' is not part of the competition stages list.",
                ApplicationErrorCodes.StageNotInCompetition);
        }

        var canonicalSource = competitionStages.First(candidate => candidate.Id.Equals(sourceStageId));
        var stageMatches = await matches.ListByStageForUpdateAsync(sourceStageId, cancellationToken).ConfigureAwait(false);
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
    }

    /// <summary>
    /// Loads a stage, runs <see cref="ApplyDraw"/>, adds newly created Matches, and saves once.
    /// </summary>
    /// <param name="stageId">Stage that owns the draw.</param>
    /// <param name="drawId">Draw identity.</param>
    /// <param name="fixtureIds">
    /// Target fixtures for Pairing apply (one per pairing result, same order). Ignored for Slot/Group.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the draw is applied and persisted.</returns>
    /// <exception cref="ApplicationFailureException">Thrown when the stage or known matches cannot be loaded.</exception>
    public async Task ApplyDrawAsync(
        StageId stageId,
        DrawId drawId,
        IReadOnlyList<FixtureId>? fixtureIds = null,
        CancellationToken cancellationToken = default)
    {
        var stage = await stages.GetByIdForUpdateAsync(stageId, cancellationToken).ConfigureAwait(false)
            ?? throw new ApplicationFailureException(
                $"Stage '{stageId}' was not found.",
                ApplicationErrorCodes.StageNotFound);

        await EnsureCompetitionAllowsLifecycleMutationAsync(stage.CompetitionId, cancellationToken)
            .ConfigureAwait(false);

        var draw = stage.GetDraw(drawId);
        var resolvedFixtures = fixtureIds;
        if (draw.Kind == DrawResolutionKind.Pairing
            && (resolvedFixtures is null || resolvedFixtures.Count == 0))
        {
            resolvedFixtures = EnsurePairingFixtures(stage, draw, clock);
        }

        PairingApplicationContext? pairingContext = null;
        IReadOnlyList<Match> knownMatches = [];
        if (resolvedFixtures is { Count: > 0 })
        {
            pairingContext = new PairingApplicationContext(resolvedFixtures);
            knownMatches = await LoadKnownMatchesForFixturesAsync(stage, resolvedFixtures, cancellationToken)
                .ConfigureAwait(false);
        }

        var result = ApplyDraw.Execute(stage, drawId, clock, pairingContext, knownMatches);
        foreach (var created in result.CreatedMatches)
        {
            matches.Add(created);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static IReadOnlyList<FixtureId> EnsurePairingFixtures(Stage stage, Draw draw, IClock clock)
    {
        if (draw.Resolution.State != DrawResolutionState.Resolved)
        {
            throw new ApplicationFailureException(
                $"Draw '{draw.Id}' must be Resolved before Pairing apply.",
                ApplicationErrorCodes.DrawApplyFailure);
        }

        var needed = draw.Resolution.PairingResults.Count;
        var round = stage.Rounds.FirstOrDefault()
            ?? throw new ApplicationFailureException(
                "Pairing apply requires a round with fixtures on the stage.",
                ApplicationErrorCodes.DrawApplyFailure);

        while (round.Fixtures.Count < needed)
        {
            stage.AddFixture(round.Id, clock);
        }

        return [.. round.Fixtures.Take(needed).Select(fixture => fixture.Id)];
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
        var competitionStages = await LoadCompetitionStagesForUpdateAsync(competition, cancellationToken).ConfigureAwait(false);
        var matchesByStage = await LoadMatchesByStageReadOnlyAsync(competitionStages, cancellationToken).ConfigureAwait(false);
        var analysis = CompletionAnalyzer.Analyze(competition, competitionStages, matchesByStage);
        CompleteCompetition.Execute(competition, mode, analysis, clock);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
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
        return WorkspaceSummaryAssembler.Assemble(competition);
    }

    /// <summary>
    /// Adds an entry and returns the updated <see cref="OrganisationViewDto"/>.
    /// </summary>
    public async Task<OrganisationViewDto> AddEntryAsync(
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
        var presentation = shortName is null && logoMediaId is null && primaryColor is null && secondaryColor is null
            ? null
            : new EntryPresentation(
                ShortName.Create(shortName),
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
        return await AssembleOrganisationViewAsync(competition, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Updates competition presentation and returns the updated organisation view.
    /// </summary>
    public async Task<OrganisationViewDto> UpdateCompetitionPresentationAsync(
        CompetitionId competitionId,
        string? shortName,
        Guid? logoMediaId,
        CancellationToken cancellationToken = default)
    {
        var competition = await RequireCompetitionAsync(competitionId, cancellationToken).ConfigureAwait(false);
        await EnsureLogoMediaExistsAsync(logoMediaId, cancellationToken).ConfigureAwait(false);
        UpdateCompetitionPresentation.Execute(competition, shortName, logoMediaId, clock);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return await AssembleOrganisationViewAsync(competition, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sets declared competition schedule and returns the updated organisation view.
    /// </summary>
    public async Task<OrganisationViewDto> SetCompetitionScheduleAsync(
        CompetitionId competitionId,
        DateTimeOffset? scheduledStart,
        DateTimeOffset? scheduledEnd,
        CancellationToken cancellationToken = default)
    {
        var competition = await RequireCompetitionAsync(competitionId, cancellationToken).ConfigureAwait(false);
        SetCompetitionSchedule.Execute(competition, scheduledStart, scheduledEnd, clock);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return await AssembleOrganisationViewAsync(competition, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Updates entry presentation and returns the updated organisation view.
    /// </summary>
    public async Task<OrganisationViewDto> UpdateEntryPresentationAsync(
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
            shortName,
            logoMediaId,
            primaryColor,
            secondaryColor,
            clock);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return await AssembleOrganisationViewAsync(competition, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Renames an entry and returns the updated organisation view.
    /// </summary>
    public async Task<OrganisationViewDto> RenameEntryAsync(
        CompetitionId competitionId,
        EntryId entryId,
        string displayName,
        CancellationToken cancellationToken = default)
    {
        var competition = await RequireCompetitionAsync(competitionId, cancellationToken).ConfigureAwait(false);
        RenameEntry.Execute(competition, entryId, displayName, clock);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return await AssembleOrganisationViewAsync(competition, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Withdraws an entry and returns the updated organisation view.
    /// </summary>
    public async Task<OrganisationViewDto> WithdrawEntryAsync(
        CompetitionId competitionId,
        EntryId entryId,
        CancellationToken cancellationToken = default)
    {
        var competition = await RequireCompetitionAsync(competitionId, cancellationToken).ConfigureAwait(false);
        WithdrawEntry.Execute(competition, entryId, clock);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return await AssembleOrganisationViewAsync(competition, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Hard-deletes an entry and returns the updated organisation view.
    /// </summary>
    public async Task<OrganisationViewDto> DeleteEntryAsync(
        CompetitionId competitionId,
        EntryId entryId,
        CancellationToken cancellationToken = default)
    {
        var competition = await RequireCompetitionAsync(competitionId, cancellationToken).ConfigureAwait(false);
        var competitionMatches = await LoadCompetitionMatchesAsync(competition, cancellationToken).ConfigureAwait(false);
        DeleteEntry.Execute(competition, entryId, competitionMatches, clock);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return await AssembleOrganisationViewAsync(competition, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Hard-deletes several entries atomically and returns the updated organisation view.
    /// </summary>
    public async Task<OrganisationViewDto> DeleteEntriesAsync(
        CompetitionId competitionId,
        IReadOnlyList<EntryId> entryIds,
        CancellationToken cancellationToken = default)
    {
        var competition = await RequireCompetitionAsync(competitionId, cancellationToken).ConfigureAwait(false);
        var competitionMatches = await LoadCompetitionMatchesAsync(competition, cancellationToken).ConfigureAwait(false);
        DeleteEntries.Execute(competition, entryIds, competitionMatches, clock);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return await AssembleOrganisationViewAsync(competition, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Withdraws several entries atomically and returns the updated organisation view.
    /// </summary>
    public async Task<OrganisationViewDto> WithdrawEntriesAsync(
        CompetitionId competitionId,
        IReadOnlyList<EntryId> entryIds,
        CancellationToken cancellationToken = default)
    {
        var competition = await RequireCompetitionAsync(competitionId, cancellationToken).ConfigureAwait(false);
        WithdrawEntries.Execute(competition, entryIds, clock);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return await AssembleOrganisationViewAsync(competition, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Removes several declared members atomically and returns the updated organisation view.
    /// </summary>
    public async Task<OrganisationViewDto> RemoveDeclaredMembersAsync(
        CompetitionId competitionId,
        EntryId entryId,
        IReadOnlyList<MemberId> memberIds,
        CancellationToken cancellationToken = default)
    {
        var competition = await RequireCompetitionAsync(competitionId, cancellationToken).ConfigureAwait(false);
        var competitionMatches = await LoadCompetitionMatchesAsync(competition, cancellationToken).ConfigureAwait(false);
        RemoveDeclaredMembers.Execute(competition, entryId, memberIds, competitionMatches, clock);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return await AssembleOrganisationViewAsync(competition, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Adds a declared member to an entry roster and returns the updated organisation view.
    /// </summary>
    public async Task<OrganisationViewDto> AddDeclaredMemberAsync(
        CompetitionId competitionId,
        EntryId entryId,
        string displayName,
        DeclaredMemberRole role,
        CancellationToken cancellationToken = default)
    {
        var competition = await RequireCompetitionAsync(competitionId, cancellationToken).ConfigureAwait(false);
        AddDeclaredMember.Execute(competition, entryId, displayName, role, clock);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return await AssembleOrganisationViewAsync(competition, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Removes a declared member from an entry roster and returns the updated organisation view.
    /// </summary>
    public async Task<OrganisationViewDto> RemoveDeclaredMemberAsync(
        CompetitionId competitionId,
        EntryId entryId,
        MemberId memberId,
        CancellationToken cancellationToken = default)
    {
        var competition = await RequireCompetitionAsync(competitionId, cancellationToken).ConfigureAwait(false);
        var competitionMatches = await LoadCompetitionMatchesAsync(competition, cancellationToken).ConfigureAwait(false);
        RemoveDeclaredMember.Execute(competition, entryId, memberId, competitionMatches, clock);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return await AssembleOrganisationViewAsync(competition, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Renames a declared member and returns the updated organisation view.
    /// </summary>
    public async Task<OrganisationViewDto> RenameDeclaredMemberAsync(
        CompetitionId competitionId,
        EntryId entryId,
        MemberId memberId,
        string displayName,
        CancellationToken cancellationToken = default)
    {
        var competition = await RequireCompetitionAsync(competitionId, cancellationToken).ConfigureAwait(false);
        RenameDeclaredMember.Execute(competition, entryId, memberId, displayName, clock);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return await AssembleOrganisationViewAsync(competition, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Changes a declared member role and returns the updated organisation view.
    /// </summary>
    public async Task<OrganisationViewDto> ChangeDeclaredMemberRoleAsync(
        CompetitionId competitionId,
        EntryId entryId,
        MemberId memberId,
        DeclaredMemberRole role,
        CancellationToken cancellationToken = default)
    {
        var competition = await RequireCompetitionAsync(competitionId, cancellationToken).ConfigureAwait(false);
        ChangeDeclaredMemberRole.Execute(competition, entryId, memberId, role, clock);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return await AssembleOrganisationViewAsync(competition, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Replaces competition regulation and returns the updated organisation view.
    /// </summary>
    /// <param name="competitionId">Competition identity.</param>
    /// <param name="buildReplacement">
    /// Builds the replacement regulation from the current persisted regulation
    /// (so omitted disciplinary AllowedTypes can preserve existing rules).
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<OrganisationViewDto> ReplaceRegulationAsync(
        CompetitionId competitionId,
        Func<Regulation, Regulation> buildReplacement,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(buildReplacement);

        var competition = await RequireCompetitionAsync(competitionId, cancellationToken).ConfigureAwait(false);
        var regulation = buildReplacement(competition.Regulation);
        ReplaceRegulation.Execute(competition, regulation, clock);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return await AssembleOrganisationViewAsync(competition, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Configures primary stage structure from a typed intent (atomic SaveChanges).
    /// </summary>
    public async Task<OrganisationViewDto> ConfigureStructureAsync(
        CompetitionId competitionId,
        StructureIntent intent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(intent);

        var competition = await RequireCompetitionAsync(competitionId, cancellationToken).ConfigureAwait(false);
        Stage? primary = null;
        if (competition.StageIds.Count > 0)
        {
            primary = await stages.GetByIdForUpdateAsync(competition.StageIds[0], cancellationToken).ConfigureAwait(false)
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
        return await AssembleOrganisationViewAsync(competition, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Adds an additional stage to a competition (thin multi-stage authoring).
    /// </summary>
    public async Task<Stage> AddCompetitionStageAsync(
        CompetitionId competitionId,
        string name,
        CancellationToken cancellationToken = default)
    {
        var competition = await RequireCompetitionAsync(competitionId, cancellationToken).ConfigureAwait(false);
        var stage = AddCompetitionStage.Execute(competition, name, clock);
        stages.Add(stage);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return stage;
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
        var slot = AddStageSlot.Execute(stage, slotKey, clock);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return slot;
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
    /// Assembles <see cref="OrganisationViewDto"/> for the Organisation hub.
    /// </summary>
    public async Task<OrganisationViewDto> GetOrganisationViewAsync(
        CompetitionId competitionId,
        CancellationToken cancellationToken = default)
    {
        var competition = await competitions.GetByIdReadOnlyAsync(competitionId, cancellationToken).ConfigureAwait(false)
            ?? throw new ApplicationFailureException(
                $"Competition '{competitionId}' was not found.",
                ApplicationErrorCodes.CompetitionNotFound);
        return await AssembleOrganisationViewAsync(competition, cancellationToken).ConfigureAwait(false);
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
    /// Configures default draw inputs from active competition entries.
    /// </summary>
    public async Task<DrawSummaryDto> ConfigureDrawInputsAsync(
        StageId stageId,
        DrawId drawId,
        CancellationToken cancellationToken = default)
    {
        var stage = await RequireStageAsync(stageId, cancellationToken).ConfigureAwait(false);
        var competition = await RequireCompetitionAsync(stage.CompetitionId, cancellationToken).ConfigureAwait(false);
        EnsureCompetitionAllowsLifecycleMutation(competition);
        var draw = stage.GetDraw(drawId);
        var inputs = DrawInputsFactory.CreateDefault(competition, stage, draw.Kind);
        ConfigureDrawInputs.Execute(stage, drawId, inputs, clock);
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
        matchContexts = (from target in targets where !contextIds.Contains(target) select loadedMatches.FirstOrDefault(candidate => candidate.Id.Equals(target)) ?? throw new ApplicationFailureException($"Match '{target}' was not found for scheduling.", ApplicationErrorCodes.MatchNotFound)).Aggregate(matchContexts, (current, match) => [.. current, new MatchSchedulingContext(match.Id, duration, home: MatchParticipantRef.Known(match.HomeEntryId), away: MatchParticipantRef.Known(match.AwayEntryId))]);

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

    private async Task<OrganisationViewDto> AssembleOrganisationViewAsync(
        Competition competition,
        CancellationToken cancellationToken)
    {
        var bundle = await LoadCompetitionReadBundleAsync(competition, cancellationToken).ConfigureAwait(false);
        return OrganisationViewAssembler.Assemble(bundle.Competition, bundle.Stages, bundle.AllMatches);
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
        var bundle = await LoadCompetitionReadBundleAsync(competitionId, cancellationToken).ConfigureAwait(false);
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
        var bundle = await LoadCompetitionReadBundleAsync(competitionId, cancellationToken).ConfigureAwait(false);
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
        var bundle = await LoadCompetitionReadBundleAsync(competitionId, cancellationToken).ConfigureAwait(false);
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
        var bundle = await LoadCompetitionReadBundleAsync(competitionId, cancellationToken).ConfigureAwait(false);
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
        var competition = await competitions.GetByIdReadOnlyAsync(competitionId, cancellationToken).ConfigureAwait(false)
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
        var competition = await competitions.GetByIdReadOnlyAsync(competitionId, cancellationToken).ConfigureAwait(false)
            ?? throw new ApplicationFailureException(
                $"Competition '{competitionId}' was not found.",
                ApplicationErrorCodes.CompetitionNotFound);

        var stageSummaries = await stages.ListSummariesReadOnlyAsync(competition.StageIds, cancellationToken)
            .ConfigureAwait(false);
        if (stageSummaries.Count != competition.StageIds.Count)
        {
            throw new ApplicationFailureException(
                "One or more competition stages were not found.",
                ApplicationErrorCodes.StageNotFound);
        }

        return CompetitionDetailAssembler.Assemble(competition, stageSummaries);
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
        var stage = await stages.GetByIdReadOnlyAsync(stageId, StageLoadProfile.Full, cancellationToken).ConfigureAwait(false)
            ?? throw new ApplicationFailureException(
                $"Stage '{stageId}' was not found.",
                ApplicationErrorCodes.StageNotFound);

        var competition = await competitions.GetByIdReadOnlyAsync(stage.CompetitionId, cancellationToken).ConfigureAwait(false)
            ?? throw new ApplicationFailureException(
                $"Competition '{stage.CompetitionId}' was not found.",
                ApplicationErrorCodes.CompetitionNotFound);

        return StageOverviewAssembler.Assemble(stage, competition);
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
        var stage = await stages.GetByIdReadOnlyAsync(stageId, StageLoadProfile.Structure, cancellationToken).ConfigureAwait(false)
            ?? throw new ApplicationFailureException(
                $"Stage '{stageId}' was not found.",
                ApplicationErrorCodes.StageNotFound);

        var competition = await competitions.GetByIdReadOnlyAsync(stage.CompetitionId, cancellationToken).ConfigureAwait(false)
            ?? throw new ApplicationFailureException(
                $"Competition '{stage.CompetitionId}' was not found.",
                ApplicationErrorCodes.CompetitionNotFound);

        var matchRows = await matches.ListSummaryRowsByStageReadOnlyAsync(stageId, cancellationToken).ConfigureAwait(false);
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

        var competition = await competitions.GetByIdReadOnlyAsync(match.CompetitionId, cancellationToken).ConfigureAwait(false)
            ?? throw new ApplicationFailureException(
                $"Competition '{match.CompetitionId}' was not found.",
                ApplicationErrorCodes.CompetitionNotFound);

        var stage = await stages.GetByIdReadOnlyAsync(match.StageId, StageLoadProfile.Structure, cancellationToken).ConfigureAwait(false);
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

    private async Task<IReadOnlyList<Match>> LoadKnownMatchesForFixturesAsync(
        Stage stage,
        IReadOnlyList<FixtureId> fixtureIds,
        CancellationToken cancellationToken)
    {
        var loaded = new List<Match>();
        foreach (var fixtureId in fixtureIds)
        {
            var fixture = stage.FindFixture(fixtureId);
            if (fixture is null)
            {
                continue;
            }

            foreach (var attachment in fixture.Attachments)
            {
                var match = await matches.GetByIdForUpdateAsync(attachment.MatchId, cancellationToken).ConfigureAwait(false)
                    ?? throw new ApplicationFailureException(
                        $"Match '{attachment.MatchId}' was not found.",
                        ApplicationErrorCodes.MatchNotFound);
                loaded.Add(match);
            }
        }

        return loaded;
    }

    private async Task EnsureCompetitionAllowsMatchOperationAsync(
        CompetitionId competitionId,
        CancellationToken cancellationToken)
    {
        var competition = await competitions.GetByIdForUpdateAsync(competitionId, cancellationToken).ConfigureAwait(false)
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

        var competition = await competitions.GetByIdForUpdateAsync(match.CompetitionId, cancellationToken).ConfigureAwait(false)
            ?? throw new ApplicationFailureException(
                $"Competition '{match.CompetitionId}' was not found.",
                ApplicationErrorCodes.CompetitionNotFound);

        if (competition.Status is CompetitionStatus.Completed or CompetitionStatus.Archived)
        {
            throw new ApplicationFailureException(
                $"Match operations are not allowed when competition is '{competition.Status}'.",
                ApplicationErrorCodes.MatchOperationNotAllowed);
        }

        return (match, competition);
    }

    private async Task EnsureCompetitionAllowsLifecycleMutationAsync(
        CompetitionId competitionId,
        CancellationToken cancellationToken)
    {
        var competition = await competitions.GetByIdForUpdateAsync(competitionId, cancellationToken).ConfigureAwait(false)
            ?? throw new ApplicationFailureException(
                $"Competition '{competitionId}' was not found.",
                ApplicationErrorCodes.CompetitionNotFound);

        EnsureCompetitionAllowsLifecycleMutation(competition);
    }

    private async Task<Dictionary<StageId, IReadOnlyList<Match>>> LoadMatchesByStageReadOnlyAsync(
        IReadOnlyList<Stage> competitionStages,
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

        var competitionStages = await LoadCompetitionStagesForUpdateAsync(competition, cancellationToken).ConfigureAwait(false);
        var matchesByStage = await LoadMatchesByStageReadOnlyAsync(competitionStages, cancellationToken).ConfigureAwait(false);
        return [.. matchesByStage.Values.SelectMany(stageMatches => stageMatches)];
    }

    private async Task<CompetitionReadBundle> LoadCompetitionReadBundleAsync(
        CompetitionId competitionId,
        CancellationToken cancellationToken)
    {
        var competition = await competitions.GetByIdReadOnlyAsync(competitionId, cancellationToken).ConfigureAwait(false)
            ?? throw new ApplicationFailureException(
                $"Competition '{competitionId}' was not found.",
                ApplicationErrorCodes.CompetitionNotFound);

        return await LoadCompetitionReadBundleAsync(competition, cancellationToken).ConfigureAwait(false);
    }

    private async Task<CompetitionReadBundle> LoadCompetitionReadBundleAsync(
        Competition competition,
        CancellationToken cancellationToken)
    {
        var stages = await LoadCompetitionStagesReadOnlyAsync(
            competition,
            StageLoadProfile.Full,
            cancellationToken).ConfigureAwait(false);
        var matchesByStage = await LoadMatchesByStageReadOnlyAsync(stages, cancellationToken).ConfigureAwait(false);
        return new CompetitionReadBundle(competition, stages, matchesByStage);
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
        StageLoadProfile profile,
        CancellationToken cancellationToken)
    {
        if (competition.StageIds.Count == 0)
        {
            return [];
        }

        var loaded = await stages.GetByIdsReadOnlyAsync(competition.StageIds, profile, cancellationToken)
            .ConfigureAwait(false);
        if (loaded.Count != competition.StageIds.Count)
        {
            throw new ApplicationFailureException(
                "One or more competition stages were not found.",
                ApplicationErrorCodes.StageNotFound);
        }

        return [.. loaded];
    }
}
