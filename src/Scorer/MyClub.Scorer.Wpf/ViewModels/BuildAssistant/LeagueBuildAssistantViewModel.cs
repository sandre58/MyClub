// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyClub.Scorer.Application.Dtos;
using MyClub.Scorer.Application.Services;
using MyNet.UI.ViewModels.Edition;

namespace MyClub.Scorer.Wpf.ViewModels.BuildAssistant
{
    internal class LeagueBuildAssistantViewModel : EditionViewModel
    {
        private readonly LeagueService _leagueService;

        public LeagueBuildAssistantViewModel(LeagueService leagueService) => _leagueService = leagueService;

        public bool UseRoundRobin { get; set; }

        public int NumberOfMatchesByTeam { get; set; }

        public int NumberOfMatchesBetweenTeams { get; set; }

        public int UseHomeTeamVenues { get; set; }

        protected override void SaveCore() => _leagueService.Build(new BuildParametersDto()
        {

        });
    }
}
