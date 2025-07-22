// -----------------------------------------------------------------------
// <copyright file="IMatchRepository.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.Shared.Domain.Matchs;
using MyClub.Shared.Kernel.Repositories;

namespace MyClub.Scorer.Domain.MatchAggregate.Repositories;

public interface IMatchRepository : IRepository<Match, MatchId>;
