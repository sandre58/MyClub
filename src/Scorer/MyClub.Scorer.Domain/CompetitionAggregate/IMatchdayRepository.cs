// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyClub.Domain;

namespace MyClub.Scorer.Domain.CompetitionAggregate
{
    public interface IMatchdayRepository : IRepository<Matchday>
    {
        Matchday Insert(IMatchdaysProvider parent, DateTime date, string name, string? shortName = null);
    }
}
