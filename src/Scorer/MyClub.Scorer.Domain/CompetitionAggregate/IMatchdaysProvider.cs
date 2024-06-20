// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.ObjectModel;
using MyClub.Domain;
using MyClub.Scorer.Domain.TeamAggregate;

namespace MyClub.Scorer.Domain.CompetitionAggregate
{
    public interface IMatchdaysProvider : IMatchFormatProvider, IEntity
    {
        ReadOnlyObservableCollection<ITeam> Teams { get; }

        ReadOnlyObservableCollection<Matchday> Matchdays { get; }

        Matchday AddMatchday(DateTime date, string name, string? shortName = null);

        Matchday AddMatchday(Matchday matchday);

        bool RemoveMatchday(Matchday item);

        void Clear();
    }
}
