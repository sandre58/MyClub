// -----------------------------------------------------------------------
// <copyright file="UseCaseExecutor.Helpers.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Scheduling;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Pipeline;

/// <content>
/// Private loaders, guards, and mapping helpers shared by command/read partials.
/// </content>
public sealed partial class UseCaseExecutor
{
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
