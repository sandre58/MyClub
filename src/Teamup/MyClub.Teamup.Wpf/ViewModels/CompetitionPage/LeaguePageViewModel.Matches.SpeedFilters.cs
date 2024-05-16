// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Reactive.Subjects;
using MyNet.UI.ViewModels.List.Filtering;
using MyNet.UI.ViewModels.List.Filtering.Filters;
using MyNet.Utilities.Comparaison;
using MyClub.Domain.Enums;
using MyClub.Teamup.Wpf.Filters;
using MyClub.Teamup.Wpf.ViewModels.Entities;

namespace MyClub.Teamup.Wpf.ViewModels.CompetitionPage
{
    internal sealed class LeagueMatchesSpeedFiltersViewModel : SpeedFiltersViewModel
    {
        public LeagueMatchesSpeedFiltersViewModel(Subject<LeagueViewModel?> leagueChanged)
        {
            AddRange([DateFilter, TeamFilter, MatchdayFilter, StateFilter]);
            Disposables.Add(leagueChanged.Subscribe(x =>
            {
                if (x is not null)
                {
                    TeamFilter.Initialize(x.Teams);
                    MatchdayFilter.Initialize(x.Matchdays);
                }
            }));
        }

        public DateFilterViewModel DateFilter { get; } = new(nameof(MatchViewModel.Date), ComplexComparableOperator.IsBetween, null, null);

        public MatchTeamFilterViewModel TeamFilter { get; } = new MatchTeamFilterViewModel();

        public MatchdayFilterViewModel MatchdayFilter { get; } = new MatchdayFilterViewModel(nameof(MatchViewModel.Parent));

        public EnumValuesFilterViewModel<MatchState> StateFilter { get; } = new(nameof(MatchViewModel.State));
    }
}
