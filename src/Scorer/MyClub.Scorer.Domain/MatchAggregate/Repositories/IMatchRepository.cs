// -----------------------------------------------------------------------
// <copyright file="IMatchRepository.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.Shared.Domain.Matches;
using MyClub.Shared.Kernel.Repositories;

namespace MyClub.Scorer.Domain.MatchAggregate.Repositories;

/// <summary>
/// Repository interface for managing Match aggregate roots.
/// Provides specialized data access operations for Match entities including their events, scores, and metadata.
/// This interface handles the complex Match aggregate which is central to football competition management.
/// </summary>
public interface IMatchRepository : IRepository<Match, MatchId>;
