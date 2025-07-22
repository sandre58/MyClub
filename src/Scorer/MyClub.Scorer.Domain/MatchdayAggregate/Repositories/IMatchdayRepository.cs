// -----------------------------------------------------------------------
// <copyright file="IMatchdayRepository.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.Shared.Kernel.Repositories;

namespace MyClub.Scorer.Domain.MatchdayAggregate.Repositories;

public interface IMatchdayRepository : IRepository<Matchday, MatchdayId>;
