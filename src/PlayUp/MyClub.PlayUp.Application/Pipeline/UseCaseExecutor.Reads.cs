// -----------------------------------------------------------------------
// <copyright file="UseCaseExecutor.Reads.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Pipeline;

/// <content>
/// Named read methods (assemble DTOs; never SaveChanges).
/// </content>
public sealed partial class UseCaseExecutor
{
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
}
