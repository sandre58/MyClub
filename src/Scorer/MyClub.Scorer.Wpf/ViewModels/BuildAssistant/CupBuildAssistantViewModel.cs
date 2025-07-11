// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using DynamicData;
using DynamicData.Binding;
using MyClub.Scorer.Application.Services;
using MyClub.Scorer.Wpf.Services.Managers;
using MyClub.Scorer.Wpf.Services.Providers;
using MyClub.Scorer.Wpf.ViewModels.Edition;
using MyNet.Observable.Collections.Providers;
using MyNet.UI.Dialogs;
using MyNet.UI.ViewModels.Edition;
using MyClub.Scorer.Domain.Extensions;
using MyNet.Utilities;
using MyClub.Scorer.Wpf.ViewModels.Entities;

namespace MyClub.Scorer.Wpf.ViewModels.BuildAssistant
{

    internal class CupBuildAssistantViewModel : EditionViewModel
    {
        private readonly MatchesProvider _matchesProvider;
        private readonly TeamsProvider _teamsProvider;
        private readonly CompetitionInfoProvider _competitionInfoProvider;
        private readonly ConflictsManager _conflictsManager;
        private readonly CupService _cupService;

        public CupBuildAssistantViewModel(CupService cupService,
                                          CompetitionInfoProvider competitionInfoProvider,
                                          MatchesProvider matchesProvider,
                                          StadiumsProvider stadiumsProvider,
                                          TeamsProvider teamsProvider,
                                          ConflictsManager conflictsManager)
        {
            _cupService = cupService;
            _matchesProvider = matchesProvider;
            _teamsProvider = teamsProvider;
            _competitionInfoProvider = competitionInfoProvider;
            _conflictsManager = conflictsManager;
            BuildParameters = new(new ObservableSourceProvider<IEditableStadiumViewModel>(stadiumsProvider.Items.ToObservableChangeSet().Transform(x => (IEditableStadiumViewModel)x)));
            BuildRounds = new();

            Reset();
            _competitionInfoProvider.LoadRunner.RegisterOnEnd(this, x => x.IfIs<CupViewModel>(_ => Reset()));

            AddSubWorkspaces([BuildRounds, BuildParameters]);
        }

        public CupBuildAssistantParametersViewModel BuildParameters { get; }

        public CupBuildAssistantRoundsViewModel BuildRounds { get; }

        protected override void ResetCore()
        {
            BuildParameters.Reset();
            BuildParameters.Load(_cupService.GetMatchFormat(), _cupService.GetSchedulingParameters(), _teamsProvider.Count);
        }

        protected override void RefreshCore() => BuildParameters.Refresh(_teamsProvider.Count);

        protected override void SaveCore()
        {
            using (_conflictsManager.Defer())
            using (_competitionInfoProvider.GetLeague().DeferRefreshRankings())
            using (_matchesProvider.DeferReload())
            {
                var parameters = BuildParameters.ToBuildParameters();
                _cupService.Build(parameters);

                var schedulingParameters = _cupService.GetSchedulingParameters();
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
