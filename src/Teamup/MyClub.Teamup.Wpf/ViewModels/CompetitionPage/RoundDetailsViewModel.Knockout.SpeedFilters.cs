// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyNet.UI.ViewModels.List.Filtering;
using MyNet.UI.ViewModels.List.Filtering.Filters;
using MyClub.Domain.Enums;
using MyClub.Teamup.Wpf.Filters;
using MyClub.Teamup.Wpf.ViewModels.Entities;

namespace MyClub.Teamup.Wpf.ViewModels.CompetitionPage
{
    internal sealed class KnockoutMatchesSpeedFiltersViewModel : SpeedFiltersViewModel
    {
        public KnockoutMatchesSpeedFiltersViewModel(KnockoutViewModel knockout)
        {
            AddRange([TeamFilter, StateFilter]);
            TeamFilter.Initialize(knockout.Teams);
        }

        public MatchTeamFilterViewModel TeamFilter { get; } = new MatchTeamFilterViewModel();

        public EnumValuesFilterViewModel<MatchState> StateFilter { get; } = new(nameof(MatchViewModel.State));
    }
}
