// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.ObjectModel;
using MyNet.Utilities;
using MyClub.Teamup.Domain.TeamAggregate;

namespace MyClub.Teamup.Domain.CompetitionAggregate
{
    public interface IHasMatchdays : IIdentifiable<Guid>
    {
        ChampionshipRules Rules { get; }

        ReadOnlyObservableCollection<Team> Teams { get; }

        ReadOnlyObservableCollection<Matchday> Matchdays { get; }

        Matchday AddMatchday(string name, DateTime date, string? shortName = null);

        Matchday AddMatchday(Matchday matchday);

        bool RemoveMatchday(Matchday matchday);

        bool RemoveMatchday(Guid matchdayId);

        void ClearMatchdays();
    }
}
