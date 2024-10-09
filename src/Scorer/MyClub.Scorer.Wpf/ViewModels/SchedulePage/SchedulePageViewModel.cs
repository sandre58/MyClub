// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using DynamicData;
using MyClub.Scorer.Application.Services;
using MyClub.Scorer.Wpf.Services;
using MyClub.Scorer.Wpf.Services.Providers;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyNet.UI.Navigation.Models;
using MyNet.UI.ViewModels.Display;
using MyNet.Utilities;

namespace MyClub.Scorer.Wpf.ViewModels.SchedulePage
{
    internal class SchedulePageViewModel : PageViewModel
    {
        private readonly CompetitionStagesProvider _competitionStagesProvider;

        public SchedulePageViewModel(CompetitionInfoProvider competitionInfoProvider,
                                     MatchesProvider matchesProvider,
                                     CompetitionStagesProvider competitionStagesProvider,
                                     TeamsProvider teamsProvider,
                                     StadiumsProvider stadiumsProvider,
                                     MatchPresentationService matchPresentationService,
                                     AvailibilityCheckingService availibilityCheckingService)
        {
            _competitionStagesProvider = competitionStagesProvider;
            competitionStagesProvider.LoadRunner.RegisterOnEnd(this, _ => MatchesPlanningViewModel?.Filters.Reset());

            Disposables.AddRange(
            [
                competitionInfoProvider.LoadRunner.WhenEnd().Subscribe(x => MatchesPlanningViewModel = x switch
                {
                    LeagueViewModel league => new(league.SchedulingParameters,
                                                  matchesProvider,
                                                  competitionStagesProvider,
                                                  teamsProvider,
                                                  stadiumsProvider,
                                                  matchPresentationService,
                                                  availibilityCheckingService),
                    CupViewModel cup => new(cup.SchedulingParameters,
                                            matchesProvider,
                                            competitionStagesProvider,
                                            teamsProvider,
                                            stadiumsProvider,
                                            matchPresentationService,
                                            availibilityCheckingService),
                    _ => throw new NotImplementedException()
                }),
                competitionInfoProvider.UnloadRunner.WhenStart().Subscribe(_ =>
                {
                    MatchesPlanningViewModel?.Dispose();
                    MatchesPlanningViewModel = null;
                })
           ]);
        }

        public MatchesPlanningViewModel? MatchesPlanningViewModel { get; private set; }

        public override void LoadParameters(INavigationParameters? parameters)
        {
            base.LoadParameters(parameters);

            if (parameters?.Get<string>(NavigationCommandsService.DisplayModeParameterKey) is string displayMode && !string.IsNullOrEmpty(displayMode))
            {
                switch (displayMode)
                {
                    case nameof(DisplayModeByDate):
                    case nameof(DisplayModeByStage):
                    case nameof(DisplayModeDay):
                    case nameof(DisplayModeList):
                        MatchesPlanningViewModel?.Load(displayMode, parameters?.Get<object>(NavigationCommandsService.FilterParameterKey));
                        break;
                    default:
                        break;
                }
            }
        }

        protected override void Cleanup()
        {
            _competitionStagesProvider.LoadRunner.Unregister(this);
            base.Cleanup();
        }
    }
}
