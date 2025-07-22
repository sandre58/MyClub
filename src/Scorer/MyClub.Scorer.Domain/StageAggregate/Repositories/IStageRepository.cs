// -----------------------------------------------------------------------
// <copyright file="IStageRepository.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.Shared.Kernel.Repositories;

namespace MyClub.Scorer.Domain.StageAggregate.Repositories;

public interface IStageRepository : IRepository<Stage, StageId>;
