// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Linq;
using MyNet.Utilities;
using MyClub.Teamup.Wpf.Collections;
using MyClub.Teamup.Wpf.Filters;
using MyClub.Teamup.Wpf.Services.Providers;
using MyClub.Teamup.Wpf.ViewModels.Entities;
using MyClub.Teamup.Domain.Extensions;
using MyNet.UI.ViewModels.List.Filtering;

namespace MyClub.Teamup.Wpf.ViewModels.TrainingPage.SessionsTab
{
    internal class TrainingStatisticsSpeedFiltersViewModel : SpeedFiltersViewModel
    {
        private readonly ProjectInfoProvider _projectInfoProvider;

        public TrainingStatisticsSpeedFiltersViewModel(ProjectInfoProvider projectInfoProvider)
        {
            _projectInfoProvider = projectInfoProvider;
            AddRange([_teamsFilter]);
        }

        private readonly TeamsFilterViewModel _teamsFilter = new($"{nameof(PlayerTrainingStatisticsViewModel.Player)}.{nameof(PlayerViewModel.TeamId)}", nameof(TeamViewModel.Id));

        public bool OnlyMyPlayers { get; set; } = true;

        protected virtual void OnOnlyMyPlayersChanged()
            => _teamsFilter.Values = (OnlyMyPlayers ? _projectInfoProvider.GetCurrentProject().OrThrow().GetMainTeams().Select(x => x.Id) : TeamsCollection.MyTeams.Select(x => x.Id)).ToList();
    }
}
