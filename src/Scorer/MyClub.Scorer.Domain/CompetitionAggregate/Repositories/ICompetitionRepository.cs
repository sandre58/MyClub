// -----------------------------------------------------------------------
// <copyright file="ICompetitionRepository.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.Shared.Kernel.Repositories;

namespace MyClub.Scorer.Domain.CompetitionAggregate.Repositories;

public interface ICompetitionRepository : IRepository<Competition, CompetitionId>;
