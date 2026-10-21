// -----------------------------------------------------------------------
// <copyright file="UseCaseExecutor.Structure.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Pipeline;

/// <content>
/// Structure configure / add-stage / rebuild / remove-stage commands.
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

        var competition = await RequireCompetitionAsync(competitionId, cancellationToken).ConfigureAwait(false);
        Stage? primary = null;
        if (competition.StageIds.Count > 0)
        {
            primary = await stages.GetByIdForUpdateAsync(competition.StageIds[0], cancellationToken)
                          .ConfigureAwait(false)
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
        var view = await AssembleStructureViewAsync(competition, cancellationToken).ConfigureAwait(false);
        return (result, view);
    }

    /// <summary>
    /// Atomic stage birth: identity + skeleton in one SaveChanges.
    /// </summary>
    public async Task<(Stage Stage, StructureViewDto View)> AddCompetitionStageAsync(
        CompetitionId competitionId,
        StructureIntent intent,
        CancellationToken cancellationToken = default)
    {
        var competition = await RequireCompetitionAsync(competitionId, cancellationToken).ConfigureAwait(false);
        var stage = AddCompetitionStage.Execute(competition, intent, clock);
        stages.Add(stage);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        var view = await AssembleStructureViewAsync(competition, cancellationToken).ConfigureAwait(false);
        return (stage, view);
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
        var stage = await RequireStageAsync(stageId, cancellationToken).ConfigureAwait(false);
        var competition = await RequireCompetitionAsync(stage.CompetitionId, cancellationToken)
            .ConfigureAwait(false);
        var intent = buildIntent(stage.Name.Value);
        var impact = RebuildStageStructure.Execute(competition, stage, intent, clock);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        var view = await AssembleStructureViewAsync(competition, cancellationToken).ConfigureAwait(false);
        return (impact, view);
    }

    /// <summary>
    /// Removes a stage and scrubs peer Qualif/Prog paths that targeted it.
    /// </summary>
    public async Task<(RemoveCompetitionStageResult Impact, StructureViewDto View)> RemoveCompetitionStageAsync(
        CompetitionId competitionId,
        StageId stageId,
        CancellationToken cancellationToken = default)
    {
        var competition = await RequireCompetitionAsync(competitionId, cancellationToken).ConfigureAwait(false);
        var peerStages = await LoadCompetitionStagesForUpdateAsync(competition, cancellationToken)
            .ConfigureAwait(false);
        var target = peerStages.FirstOrDefault(stage => stage.Id.Equals(stageId))
            ?? throw new ApplicationFailureException(
                $"Stage '{stageId}' was not found.",
                ApplicationErrorCodes.StageNotFound);

        var impact = RemoveCompetitionStage.Execute(competition, target, peerStages, clock);
        stages.Remove(target);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        var view = await AssembleStructureViewAsync(competition, cancellationToken).ConfigureAwait(false);
        return (impact, view);
    }
}
