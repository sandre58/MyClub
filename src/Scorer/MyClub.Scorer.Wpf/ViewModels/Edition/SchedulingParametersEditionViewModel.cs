// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyClub.Scorer.Application.Services;
using MyClub.Scorer.Wpf.Services.Providers;
using MyNet.Observable.Attributes;
using MyNet.UI.ViewModels.Edition;

namespace MyClub.Scorer.Wpf.ViewModels.Edition
{
    internal class SchedulingParametersEditionViewModel : EditionViewModel
    {
        private readonly LeagueService _leagueService;

        public SchedulingParametersEditionViewModel(LeagueService leagueService, StadiumsProvider stadiumsProvider)
        {
            _leagueService = leagueService;
            SchedulingParameters = new(stadiumsProvider);
        }

        public EditableSchedulingParametersViewModel SchedulingParameters { get; }

        public EditableMatchFormatViewModel MatchFormat { get; } = new();

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public bool CanEditMatchFormat { get; private set; }

        protected override void RefreshCore()
        {
            var matchFormat = _leagueService.GetMatchFormat();
            var schedulingParameters = _leagueService.GetSchedulingParameters();

            CanEditMatchFormat = !_leagueService.HasMatches();
            MatchFormat.Load(matchFormat);
            SchedulingParameters.Load(schedulingParameters);
        }

        protected override void SaveCore()
        {
            if (CanEditMatchFormat)
                _leagueService.UpdateMatchFormat(MatchFormat.Create());

            _leagueService.UpdateSchedulingParameters(SchedulingParameters.Create());
        }
    }
}
