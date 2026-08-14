// -----------------------------------------------------------------------
// <copyright file="UseCaseExecutor.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Application.Pipeline;

/// <summary>
/// Minimal persistence orchestration for Application use cases (Phase 10.0: PrepareStage only).
/// </summary>
/// <remarks>
/// Loads aggregates via ports, runs the static use case, then commits once via <see cref="IUnitOfWork"/>.
/// Does not know HTTP, EF Core, or Domain Event dispatch. Not a CQRS mediator — do not generalize
/// to other use cases in Phase 10.0.
/// Initializes a new instance of the <see cref="UseCaseExecutor"/> class.
/// </remarks>
/// <param name="stages">Stage persistence port.</param>
/// <param name="unitOfWork">Unit of work for a single commit after the use case.</param>
/// <param name="clock">Clock forwarded to Domain / Application.</param>
public sealed class UseCaseExecutor(
    IStageRepository stages,
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
}
