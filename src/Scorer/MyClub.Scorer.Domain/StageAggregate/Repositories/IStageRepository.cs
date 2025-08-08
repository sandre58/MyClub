// -----------------------------------------------------------------------
// <copyright file="IStageRepository.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.Shared.Kernel.Repositories;

namespace MyClub.Scorer.Domain.StageAggregate.Repositories;

/// <summary>
/// Repository interface for managing Stage aggregate roots.
/// Provides specialized data access operations for Stage entities including group stages,
/// knockout stages, and championship stages within multiphase tournament competitions.
/// </summary>
public interface IStageRepository : IRepository<Stage, StageId>;
