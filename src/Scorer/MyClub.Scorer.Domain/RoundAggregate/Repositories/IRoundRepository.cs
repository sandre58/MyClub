// -----------------------------------------------------------------------
// <copyright file="IRoundRepository.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.Shared.Kernel.Repositories;

namespace MyClub.Scorer.Domain.RoundAggregate.Repositories;

public interface IRoundRepository : IRepository<Round, RoundId>;
