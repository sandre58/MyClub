// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.ObjectModel;
using MyNet.Utilities;
using MyClub.Teamup.Domain.TeamAggregate;

namespace MyClub.Teamup.Domain.MatchAggregate
{
    public interface IHasMatches : IIdentifiable<Guid>
    {
        ReadOnlyObservableCollection<Match> Matches { get; }

        Match AddMatch(DateTime date, Team homeTeam, Team awayTeam);

        Match AddMatch(Match match);

        bool RemoveMatch(Match match);

        MatchFormat MatchFormat { get; }
    }
}
