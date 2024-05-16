// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyClub.Domain;
using MyClub.Scorer.Domain.TeamAggregate;

namespace MyClub.Scorer.Domain.PersonAggregate
{
    public interface IManagerRepository : IRepository<Manager>
    {
        Manager Insert(Team team, string firstName, string lastName);
    }
}
