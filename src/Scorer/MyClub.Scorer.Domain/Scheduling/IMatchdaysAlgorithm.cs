// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using MyClub.Scorer.Domain.TeamAggregate;

namespace MyClub.Scorer.Domain.Scheduling
{
    public interface IMatchdaysAlgorithm
    {
        IEnumerable<IEnumerable<(ITeam home, ITeam away)>> Build(IEnumerable<ITeam> teams);

        int NumberOfMatchdays(int teamsCount);

        int NumberOfMatchesByMatchday(int teamsCount);
    }
}
