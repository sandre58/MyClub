// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using MyNet.Utilities.DateTimes;
using MyClub.Domain;
using MyClub.Scorer.Domain.CompetitionAggregate;
using MyClub.Scorer.Domain.TeamAggregate;

namespace MyClub.Scorer.Domain.MatchAggregate
{
    public interface IMatchRepository : IRepository<Match>
    {
        Match Insert(IMatchesProvider parent, DateTime date, ITeam homeTeam, ITeam awayTeam);

        IEnumerable<Match> GetByPeriod(Period period);
    }
}
