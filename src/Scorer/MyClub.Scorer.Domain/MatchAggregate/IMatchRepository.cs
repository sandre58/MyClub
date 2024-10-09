// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using MyClub.Domain;
using MyClub.Scorer.Domain.TeamAggregate;
using MyNet.Utilities.DateTimes;

namespace MyClub.Scorer.Domain.MatchAggregate
{
    public interface IMatchRepository : IRepository<Match>
    {
        Match Insert(Guid stageId, DateTime date, IVirtualTeam homeTeam, IVirtualTeam awayTeam);

        IEnumerable<Match> GetByPeriod(Period period);

        IEnumerable<Match> GetMatchesInStadium(Guid stadiumId, Period? period = null);

        IEnumerable<Match> GetMatchesOfTeams(IEnumerable<Guid> teamIds, Period? period = null);
    }
}
