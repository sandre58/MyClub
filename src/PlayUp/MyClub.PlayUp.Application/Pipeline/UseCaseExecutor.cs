// -----------------------------------------------------------------------
// <copyright file="UseCaseExecutor.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Matches;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Pipeline;

/// <summary>
/// Minimal persistence orchestration for Application use cases
/// (named methods: PrepareStage, ApplyProgressionOutcome, PublishDraw, StartMatch, FinishMatch).
/// </summary>
/// <remarks>
/// Loads aggregates via ports, runs the static use case, then commits once via <see cref="IUnitOfWork"/>.
/// Does not know HTTP, EF Core, or Domain Event dispatch. Not a CQRS mediator — named methods only;
/// do not introduce generic dispatch without a demonstrated need.
/// Initializes a new instance of the <see cref="UseCaseExecutor"/> class.
/// </remarks>
/// <param name="stages">Stage persistence port.</param>
/// <param name="matches">Match persistence port.</param>
/// <param name="competitions">Competition persistence port (StageIds for multi-stage load).</param>
/// <param name="unitOfWork">Unit of work for a single commit after the use case.</param>
/// <param name="clock">Clock forwarded to Domain / Application.</param>
public sealed class UseCaseExecutor(
    IStageRepository stages,
    IMatchRepository matches,
    ICompetitionRepository competitions,
    IUnitOfWork unitOfWork,
    IClock clock)
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
        var stage = await stages.GetByIdAsync(stageId, cancellationToken).ConfigureAwait(false)
            ?? throw new ApplicationFailureException(
                $"Stage '{stageId}' was not found.",
                ApplicationErrorCodes.StageNotFound);

        // R1 mono-stage: competition stage list is the target alone (no slots / cross-stage feeds).
        PrepareStage.Execute(stage, [stage], clock);
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
        var source = await stages.GetByIdAsync(sourceStageId, cancellationToken).ConfigureAwait(false)
            ?? throw new ApplicationFailureException(
                $"Stage '{sourceStageId}' was not found.",
                ApplicationErrorCodes.StageNotFound);

        var competition = await competitions.GetByIdAsync(source.CompetitionId, cancellationToken).ConfigureAwait(false)
            ?? throw new ApplicationFailureException(
                $"Competition '{source.CompetitionId}' was not found.",
                ApplicationErrorCodes.CompetitionNotFound);

        var competitionStages = new List<Stage>(competition.StageIds.Count);
        foreach (var stageId in competition.StageIds)
        {
            var stage = await stages.GetByIdAsync(stageId, cancellationToken).ConfigureAwait(false)
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
            var match = await matches.GetByIdAsync(attachment.MatchId, cancellationToken).ConfigureAwait(false)
                ?? throw new ApplicationFailureException(
                    $"Match '{attachment.MatchId}' was not found.",
                    ApplicationErrorCodes.MatchNotFound);
            loadedMatches.Add(match);
        }

        ApplyProgressionOutcome.Execute(source, fixtureId, loadedMatches, competitionStages, clock);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
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
        var stage = await stages.GetByIdAsync(stageId, cancellationToken).ConfigureAwait(false)
            ?? throw new ApplicationFailureException(
                $"Stage '{stageId}' was not found.",
                ApplicationErrorCodes.StageNotFound);

        PublishDraw.Execute(stage, drawId, clock);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Loads a match, runs <see cref="StartMatch"/>, and saves changes.
    /// </summary>
    /// <param name="matchId">Match identity.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the match is started and persisted.</returns>
    /// <exception cref="ApplicationFailureException">Thrown when the match does not exist.</exception>
    public async Task StartMatchAsync(MatchId matchId, CancellationToken cancellationToken = default)
    {
        var match = await matches.GetByIdAsync(matchId, cancellationToken).ConfigureAwait(false)
            ?? throw new ApplicationFailureException(
                $"Match '{matchId}' was not found.",
                ApplicationErrorCodes.MatchNotFound);

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
    /// <exception cref="ApplicationFailureException">Thrown when the match does not exist.</exception>
    public async Task FinishMatchAsync(
        MatchId matchId,
        MatchResult result,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(result);

        var match = await matches.GetByIdAsync(matchId, cancellationToken).ConfigureAwait(false)
            ?? throw new ApplicationFailureException(
                $"Match '{matchId}' was not found.",
                ApplicationErrorCodes.MatchNotFound);

        FinishMatch.Execute(match, result, clock);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
