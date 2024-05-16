// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyClub.Domain;

namespace MyClub.Teamup.Domain.CompetitionAggregate
{
    public interface IMatchdayRepository : IRepository<Matchday>
    {
        Matchday Insert(IHasMatchdays parent, Matchday item);

        Matchday Insert(IHasMatchdays parent, string name, DateTime date, string? shortName = null);
    }
}
