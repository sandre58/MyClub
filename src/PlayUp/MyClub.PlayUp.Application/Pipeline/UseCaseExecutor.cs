// -----------------------------------------------------------------------
// <copyright file="UseCaseExecutor.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Matches;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Pipeline;

/// <summary>
/// Minimal persistence orchestration for Application use cases and named read methods
/// (PrepareStage, ApplyProgressionOutcome, PublishDraw, ApplyDraw, StartMatch, FinishMatch,
/// GetCompetitionOverview, GetStageOverview, ListMatchesByStage, GetMatchDetail).
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

        var competition = await competitions.GetByIdAsync(stage.CompetitionId, cancellationToken).ConfigureAwait(false)
            ?? throw new ApplicationFailureException(
                $"Competition '{stage.CompetitionId}' was not found.",
                ApplicationErrorCodes.CompetitionNotFound);

        var competitionStages = new List<Stage>(competition.StageIds.Count);
        foreach (var competitionStageId in competition.StageIds)
        {
            var loaded = await stages.GetByIdAsync(competitionStageId, cancellationToken).ConfigureAwait(false)
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
        var stage = await stages.GetByIdAsync(stageId, cancellationToken).ConfigureAwait(false)
            ?? throw new ApplicationFailureException(
                $"Stage '{stageId}' was not found.",
                ApplicationErrorCodes.StageNotFound);

        PairingApplicationContext? pairingContext = null;
        IReadOnlyList<Match> knownMatches = [];
        if (fixtureIds is { Count: > 0 })
        {
            pairingContext = new PairingApplicationContext(fixtureIds);
            knownMatches = await LoadKnownMatchesForFixturesAsync(stage, fixtureIds, cancellationToken)
                .ConfigureAwait(false);
        }

        var result = ApplyDraw.Execute(stage, drawId, clock, pairingContext, knownMatches);
        foreach (var created in result.CreatedMatches)
        {
            matches.Add(created);
        }

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

    /// <summary>
    /// Loads a competition and its stages, then assembles <see cref="CompetitionOverviewDto"/>.
    /// </summary>
    /// <param name="competitionId">Competition identity.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The competition overview.</returns>
    /// <exception cref="ApplicationFailureException">Thrown when the competition or a referenced stage is missing.</exception>
    public async Task<CompetitionOverviewDto> GetCompetitionOverviewAsync(
        CompetitionId competitionId,
        CancellationToken cancellationToken = default)
    {
        var competition = await competitions.GetByIdAsync(competitionId, cancellationToken).ConfigureAwait(false)
            ?? throw new ApplicationFailureException(
                $"Competition '{competitionId}' was not found.",
                ApplicationErrorCodes.CompetitionNotFound);

        var loadedStages = new List<Stage>(competition.StageIds.Count);
        foreach (var stageId in competition.StageIds)
        {
            var stage = await stages.GetByIdAsync(stageId, cancellationToken).ConfigureAwait(false)
                ?? throw new ApplicationFailureException(
                    $"Stage '{stageId}' was not found.",
                    ApplicationErrorCodes.StageNotFound);
            loadedStages.Add(stage);
        }

        return CompetitionOverviewAssembler.Assemble(competition, loadedStages);
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
        var stage = await stages.GetByIdAsync(stageId, cancellationToken).ConfigureAwait(false)
            ?? throw new ApplicationFailureException(
                $"Stage '{stageId}' was not found.",
                ApplicationErrorCodes.StageNotFound);

        var competition = await competitions.GetByIdAsync(stage.CompetitionId, cancellationToken).ConfigureAwait(false)
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
        var stage = await stages.GetByIdAsync(stageId, cancellationToken).ConfigureAwait(false)
            ?? throw new ApplicationFailureException(
                $"Stage '{stageId}' was not found.",
                ApplicationErrorCodes.StageNotFound);

        var competition = await competitions.GetByIdAsync(stage.CompetitionId, cancellationToken).ConfigureAwait(false)
            ?? throw new ApplicationFailureException(
                $"Competition '{stage.CompetitionId}' was not found.",
                ApplicationErrorCodes.CompetitionNotFound);

        var matchList = await matches.ListByStageAsync(stageId, cancellationToken).ConfigureAwait(false);
        return MatchReadAssembler.AssembleSummaries(stage, competition, matchList);
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
        var match = await matches.GetByIdAsync(matchId, cancellationToken).ConfigureAwait(false)
            ?? throw new ApplicationFailureException(
                $"Match '{matchId}' was not found.",
                ApplicationErrorCodes.MatchNotFound);

        var competition = await competitions.GetByIdAsync(match.CompetitionId, cancellationToken).ConfigureAwait(false)
            ?? throw new ApplicationFailureException(
                $"Competition '{match.CompetitionId}' was not found.",
                ApplicationErrorCodes.CompetitionNotFound);

        var stage = await stages.GetByIdAsync(match.StageId, cancellationToken).ConfigureAwait(false);
        return MatchReadAssembler.AssembleDetail(match, competition, stage);
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
                var match = await matches.GetByIdAsync(attachment.MatchId, cancellationToken).ConfigureAwait(false)
                    ?? throw new ApplicationFailureException(
                        $"Match '{attachment.MatchId}' was not found.",
                        ApplicationErrorCodes.MatchNotFound);
                loaded.Add(match);
            }
        }

        return loaded;
    }
}
