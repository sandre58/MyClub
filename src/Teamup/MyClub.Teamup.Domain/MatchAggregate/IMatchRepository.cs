// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyClub.Domain;
using MyClub.Teamup.Domain.TeamAggregate;

namespace MyClub.Teamup.Domain.MatchAggregate
{
    public interface IMatchRepository : IRepository<Match>
    {
        Match Insert(IHasMatches parent, DateTime date, Team homeTeam, Team awayTeam);
    }
}
