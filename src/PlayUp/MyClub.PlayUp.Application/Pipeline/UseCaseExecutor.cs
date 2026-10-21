// -----------------------------------------------------------------------
// <copyright file="UseCaseExecutor.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Application.Pipeline;

/// <summary>
/// Minimal persistence orchestration for Application use cases and named read methods.
/// Split across partials: Competitions, Structure, Stages, Matches, Reads, Helpers, Logging.
/// </summary>
/// <remarks>
/// Command methods load aggregates via ports, run the static use case, then commit once via <see cref="IUnitOfWork"/>.
/// Read methods load aggregates, assemble product DTOs, and never call SaveChanges.
/// PrepareStage and ApplyProgressionOutcome load all competition stages (cross-stage destinations / feeds).
/// Does not know HTTP, EF Core, or Domain Event dispatch. Not a CQRS mediator — named methods only;
/// do not introduce generic dispatch without a demonstrated need.
/// Lifecycle commands emit structured Information logs (IDs only); Domain has no logging;
/// mapped HTTP 4xx stay silent in Host handlers.
/// </remarks>
/// <param name="stages">Stage persistence port.</param>
/// <param name="matches">Match persistence port.</param>
/// <param name="competitions">Competition persistence port (StageIds for multi-stage load).</param>
/// <param name="unitOfWork">Unit of work for a single commit after the use case.</param>
/// <param name="clock">Clock forwarded to Domain / Application.</param>
/// <param name="mediaReferences">Port that verifies Media identities exist before storing logo refs.</param>
/// <param name="logger">Structured logger for lifecycle milestones.</param>
public sealed partial class UseCaseExecutor(
    IStageRepository stages,
    IMatchRepository matches,
    ICompetitionRepository competitions,
    IUnitOfWork unitOfWork,
    IClock clock,
    IMediaReferenceChecker mediaReferences,
    ILogger<UseCaseExecutor> logger);
