// -----------------------------------------------------------------------
// <copyright file="UseCaseExecutor.Structure.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Pipeline;

/// <content>
/// Structure configure / add-stage / insert-stage / reorder / rebuild / remove-stage commands.
/// </content>
public sealed partial class UseCaseExecutor
{
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

        Competition? loaded = null;
        ConfigureStructureResult? result = null;
        await ExecuteOnCompetitionAsync(
            competitionId,
            async innerToken =>
            {
                loaded = await RequireCompetitionAsync(competitionId, innerToken).ConfigureAwait(false);
                Stage? primary = null;
                if (loaded.StageIds.Count > 0)
                {
                    primary = await stages.GetByIdForUpdateAsync(loaded.StageIds[0], innerToken)
                                  .ConfigureAwait(false)
                              ?? throw new ApplicationFailureException(
                                  $"Stage '{loaded.StageIds[0]}' was not found.",
                                  ApplicationErrorCodes.StageNotFound);
                }

                result = ConfigureStructure.Execute(loaded, primary, intent, clock);
                if (result.StageCreated)
                {
                    stages.Add(result.Stage);
                }

                await unitOfWork.SaveChangesAsync(innerToken).ConfigureAwait(false);
            },
            cancellationToken).ConfigureAwait(false);
        var view = await AssembleStructureViewAsync(loaded!, cancellationToken).ConfigureAwait(false);
        return (result!, view);
    }

    /// <summary>
    /// Atomic stage birth: identity + skeleton in one SaveChanges.
    /// </summary>
    public async Task<(Stage Stage, StructureViewDto View)> AddCompetitionStageAsync(
        CompetitionId competitionId,
        StructureIntent intent,
        CancellationToken cancellationToken = default)
    {
        Stage? stage = null;
        Competition? competition = null;
        await ExecuteOnCompetitionAsync(
            competitionId,
            async innerToken =>
            {
                competition = await RequireCompetitionAsync(competitionId, innerToken).ConfigureAwait(false);
                stage = AddCompetitionStage.Execute(competition, intent, clock);
                stages.Add(stage);
                await unitOfWork.SaveChangesAsync(innerToken).ConfigureAwait(false);
            },
            cancellationToken).ConfigureAwait(false);
        var view = await AssembleStructureViewAsync(competition!, cancellationToken).ConfigureAwait(false);
        return (stage!, view);
    }

    /// <summary>
    /// Positioned stage birth: identity + skeleton + order in one SaveChanges.
    /// </summary>
    /// <param name="competitionId">Owning competition.</param>
    /// <param name="intent">Validated format intent.</param>
    /// <param name="anchorStageId">Existing stage the new stage is placed against.</param>
    /// <param name="position">Before or after the anchor.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The new stage and the refreshed structure read model.</returns>
    public async Task<(Stage Stage, StructureViewDto View)> InsertCompetitionStageAsync(
        CompetitionId competitionId,
        StructureIntent intent,
        StageId anchorStageId,
        StageInsertPosition position,
        CancellationToken cancellationToken = default)
    {
        Stage? stage = null;
        Competition? competition = null;
        await ExecuteOnCompetitionAsync(
            competitionId,
            async innerToken =>
            {
                competition = await RequireCompetitionAsync(competitionId, innerToken).ConfigureAwait(false);
                stage = InsertCompetitionStage.Execute(competition, intent, anchorStageId, position, clock);
                stages.Add(stage);
                await unitOfWork.SaveChangesAsync(innerToken).ConfigureAwait(false);
            },
            cancellationToken).ConfigureAwait(false);
        var view = await AssembleStructureViewAsync(competition!, cancellationToken).ConfigureAwait(false);
        return (stage!, view);
    }

    /// <summary>
    /// Reorders stages. <paramref name="orderedStageIds"/> is a permutation of the current ids.
    /// </summary>
    /// <param name="competitionId">Owning competition.</param>
    /// <param name="orderedStageIds">Desired stage order.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The refreshed structure read model.</returns>
    public async Task<StructureViewDto> SetCompetitionStageOrderAsync(
        CompetitionId competitionId,
        IReadOnlyList<StageId> orderedStageIds,
        CancellationToken cancellationToken = default)
    {
        Competition? competition = null;
        await ExecuteOnCompetitionAsync(
            competitionId,
            async innerToken =>
            {
                competition = await RequireCompetitionAsync(competitionId, innerToken).ConfigureAwait(false);
                SetCompetitionStageOrder.Execute(competition, orderedStageIds, clock);
                await unitOfWork.SaveChangesAsync(innerToken).ConfigureAwait(false);
            },
            cancellationToken).ConfigureAwait(false);
        return await AssembleStructureViewAsync(competition!, cancellationToken).ConfigureAwait(false);
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
        Competition? loaded = null;
        StructureRebuildImpact? impact = null;
        await ExecuteOnStageCompetitionAsync(
            stageId,
            async innerToken =>
            {
                var stage = await RequireStageAsync(stageId, innerToken).ConfigureAwait(false);
                loaded = await RequireCompetitionAsync(stage.CompetitionId, innerToken).ConfigureAwait(false);
                var intent = buildIntent(stage.Name.Value);
                impact = RebuildStageStructure.Execute(loaded, stage, intent, clock);
                await unitOfWork.SaveChangesAsync(innerToken).ConfigureAwait(false);
            },
            cancellationToken).ConfigureAwait(false);
        var view = await AssembleStructureViewAsync(loaded!, cancellationToken).ConfigureAwait(false);
        return (impact!, view);
    }

    /// <summary>
    /// Removes a stage and scrubs peer Qualif/Prog paths that targeted it.
    /// </summary>
    public async Task<(RemoveCompetitionStageResult Impact, StructureViewDto View)> RemoveCompetitionStageAsync(
        CompetitionId competitionId,
        StageId stageId,
        CancellationToken cancellationToken = default)
    {
        Competition? loaded = null;
        RemoveCompetitionStageResult? impact = null;
        await ExecuteOnCompetitionAsync(
            competitionId,
            async innerToken =>
            {
                loaded = await RequireCompetitionAsync(competitionId, innerToken).ConfigureAwait(false);
                var peerStages = await LoadCompetitionStagesForUpdateAsync(loaded, innerToken)
                    .ConfigureAwait(false);
                var target = peerStages.FirstOrDefault(stage => stage.Id.Equals(stageId))
                    ?? throw new ApplicationFailureException(
                        $"Stage '{stageId}' was not found.",
                        ApplicationErrorCodes.StageNotFound);

                impact = RemoveCompetitionStage.Execute(loaded, target, peerStages, clock);
                stages.Remove(target);
                await unitOfWork.SaveChangesAsync(innerToken).ConfigureAwait(false);
            },
            cancellationToken).ConfigureAwait(false);
        var view = await AssembleStructureViewAsync(loaded!, cancellationToken).ConfigureAwait(false);
        return (impact!, view);
    }
}
