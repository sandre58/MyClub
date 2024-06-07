// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using MyClub.Scorer.Wpf.Filters;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyNet.UI.ViewModels.List.Filtering;
using MyNet.UI.ViewModels.List.Filtering.Filters;

namespace MyClub.Scorer.Wpf.ViewModels.SchedulingAssistant
{
    internal class SchedulingAssistantSpeedFiltersViewModel : SpeedFiltersViewModel
    {
        public SchedulingAssistantSpeedFiltersViewModel(IEnumerable<TeamViewModel> teams, IEnumerable<StadiumViewModel> stadiums)
        {
            TeamFilter = new MatchTeamFilterViewModel(teams);
            StadiumFilter = new(nameof(MatchViewModel.Stadium), stadiums);
            AddRange([TeamFilter, StadiumFilter]);
        }

        public MatchTeamFilterViewModel TeamFilter { get; }

        public SelectedValuesFilterViewModel<StadiumViewModel> StadiumFilter { get; }
    }
}
