// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using DynamicData;
using DynamicData.Binding;
using MyClub.Scorer.Application.Services;
using MyClub.Scorer.Domain.Extensions;
using MyClub.Scorer.Wpf.Services.Managers;
using MyClub.Scorer.Wpf.Services.Providers;
using MyClub.Scorer.Wpf.ViewModels.Edition;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyNet.Observable.Collections.Providers;
using MyNet.UI.Dialogs;
using MyNet.UI.ViewModels.Edition;
using MyNet.Utilities;

namespace MyClub.Scorer.Wpf.ViewModels.BuildAssistant
{

    internal class LeagueBuildAssistantViewModel : EditionViewModel
    {
        private readonly LeagueService _leagueService;
        private readonly MatchesProvider _matchesProvider;
        private readonly TeamsProvider _teamsProvider;
        private readonly CompetitionInfoProvider _competitionInfoProvider;
        private readonly ConflictsManager _conflictsManager;

        public LeagueBuildAssistantViewModel(LeagueService leagueService,
                                             CompetitionInfoProvider competitionInfoProvider,
                                             MatchesProvider matchesProvider,
                                             StadiumsProvider stadiumsProvider,
                                             TeamsProvider teamsProvider,
                                             ConflictsManager conflictsManager)
        {
            _leagueService = leagueService;
            _matchesProvider = matchesProvider;
            _teamsProvider = teamsProvider;
            _competitionInfoProvider = competitionInfoProvider;
            _conflictsManager = conflictsManager;
            BuildParameters = new(new ObservableSourceProvider<IEditableStadiumViewModel>(stadiumsProvider.Items.ToObservableChangeSet().Transform(x => (IEditableStadiumViewModel)x)));

            Reset();

            _competitionInfoProvider.LoadRunner.RegisterOnEnd(this, x => x.IfIs<LeagueViewModel>(_ => Reset()));
        }

        public LeagueBuildAssistantParametersViewModel BuildParameters { get; }

        protected override void ResetCore()
        {
            BuildParameters.Reset();
            BuildParameters.Load(_leagueService.GetMatchFormat(), _leagueService.GetSchedulingParameters(), _teamsProvider.Count);
        }

        protected override void RefreshCore() => BuildParameters.Refresh(_teamsProvider.Count);

        protected override void SaveCore()
        {
            using (_conflictsManager.Defer())
            using (_competitionInfoProvider.GetLeague().DeferRefreshRankings())
            using (_matchesProvider.DeferReload())
            {
                var parameters = BuildParameters.ToBuildParameters();
                _leagueService.Build(parameters);

                var schedulingParameters = _leagueService.GetSchedulingParameters();
                if (BuildParameters.AutomaticStartDate)
                    BuildParameters.StartDate = schedulingParameters.GetCurrentStartDate();

                if (BuildParameters.AutomaticEndDate)
                    BuildParameters.EndDate = schedulingParameters.GetCurrentEndDate();
            }
        }

        protected override Task<bool> CanCancelAsync() => Task.FromResult(true);

        protected override Task<MessageBoxResult> SavingRequestAsync() => Task.FromResult(MessageBoxResult.No);

        protected override void Cleanup()
        {
            _competitionInfoProvider.LoadRunner.Unregister(this);
            base.Cleanup();
        }
    }
}
