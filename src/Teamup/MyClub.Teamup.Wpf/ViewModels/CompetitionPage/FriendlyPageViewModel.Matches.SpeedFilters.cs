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
    internal sealed class FriendlyMatchesSpeedFiltersViewModel : SpeedFiltersViewModel
    {
        public FriendlyMatchesSpeedFiltersViewModel(Subject<FriendlyViewModel?> friendlyChanged)
        {
            AddRange([DateFilter, TeamFilter, StateFilter]);
            Disposables.Add(friendlyChanged.Subscribe(x =>
            {
                if (x is not null)
                {
                    TeamFilter.Initialize(x.Teams);
                }
            }));
        }

        public DateFilterViewModel DateFilter { get; } = new(nameof(MatchViewModel.Date), ComplexComparableOperator.IsBetween, null, null);

        public MatchTeamFilterViewModel TeamFilter { get; } = new MatchTeamFilterViewModel();

        public EnumValuesFilterViewModel<MatchState> StateFilter { get; } = new(nameof(MatchViewModel.State));
    }
}
