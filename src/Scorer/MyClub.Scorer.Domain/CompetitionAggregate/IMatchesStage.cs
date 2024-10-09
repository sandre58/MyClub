// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.Scheduling;
using MyClub.Scorer.Domain.TeamAggregate;

namespace MyClub.Scorer.Domain.CompetitionAggregate
{
    public interface IMatchesStage : ICompetitionStage, ISchedulable
    {
        void ScheduleAll(DateTime date);

        void Postpone(DateTime? date = null, bool propagateToMatches = true);

        Match AddMatch(DateTime date, IVirtualTeam homeTeam, IVirtualTeam awayTeam);

        Match AddMatch(IVirtualTeam homeTeam, IVirtualTeam awayTeam);
    }
}
