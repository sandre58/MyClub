// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.ObjectModel;
using MyClub.Domain;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.Scheduling;
using MyClub.Scorer.Domain.TeamAggregate;

namespace MyClub.Scorer.Domain.CompetitionAggregate
{
    public interface IMatchesProvider : IEntity, IMatchFormatProvider, ISchedulingParametersProvider
    {
        ReadOnlyObservableCollection<Match> Matches { get; }

        Match AddMatch(DateTime date, ITeam homeTeam, ITeam awayTeam);

        bool RemoveMatch(Match item);
    }
}
