// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using MyClub.Scorer.Domain.TeamAggregate;

namespace MyClub.Scorer.Domain.BracketComputing
{
    public record struct BracketMatch(IVirtualTeam Team1, IVirtualTeam Team2)
    {
    }

    public interface IMatchdaysAlgorithm
    {
        IEnumerable<IEnumerable<BracketMatch>> Compute(IEnumerable<IVirtualTeam> teams);

        int NumberOfMatchdays(int teamsCount);

        int NumberOfMatchesByMatchday(int teamsCount);
    }
}
