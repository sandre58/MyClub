﻿// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyClub.Scorer.Wpf.Services;
using MyClub.Scorer.Wpf.Services.Providers;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyNet.UI.ViewModels;

namespace MyClub.Scorer.Wpf.ViewModels.BracketPage
{
    internal class BracketPageViewModel : PageViewModel
    {
        private readonly CompetitionInfoProvider _competitionInfoProvider;
        public BracketPageViewModel(CompetitionInfoProvider competitionInfoProvider,
                                    MatchdayPresentationService matchdayPresentationService,
                                    RoundPresentationService roundPresentationService,
                                    CompetitionCommandsService competitionCommandsService)
        {
            _competitionInfoProvider = competitionInfoProvider;
            competitionInfoProvider.LoadRunner.RegisterOnEnd(this, x => StagesViewModel = x switch
            {
                LeagueViewModel league => new MatchdaysViewModel(league, matchdayPresentationService, competitionCommandsService),
                CupViewModel cup => new RoundsViewModel(cup, roundPresentationService, competitionCommandsService),
                _ => throw new NotImplementedException(),
            });

            competitionInfoProvider.UnloadRunner.RegisterOnStart(this, () =>
            {
                StagesViewModel?.Dispose();
                StagesViewModel = null;
            });
        }

        public IListViewModel? StagesViewModel { get; private set; }

        protected override void Cleanup()
        {
            _competitionInfoProvider.LoadRunner.Unregister(this);
            _competitionInfoProvider.UnloadRunner.Unregister(this);
            base.Cleanup();
        }
    }
}
