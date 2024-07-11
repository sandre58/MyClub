// Copyright (c) Stéphane ANDRE. All Right Reserved.
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
        public BracketPageViewModel(CompetitionInfoProvider competitionInfoProvider,
                                    MatchdaysProvider matchdaysProvider,
                                    MatchdayPresentationService matchdayPresentationService)
        => competitionInfoProvider.WhenCompetitionChanged(x => MatchParentsViewModel = x switch
        {
            LeagueViewModel league => new MatchdaysViewModel(league, matchdaysProvider, matchdayPresentationService),
            _ => throw new NotImplementedException(),
        }, _ =>
        {
            MatchParentsViewModel?.Dispose();
            MatchParentsViewModel = null;
        });

        public IListViewModel? MatchParentsViewModel { get; private set; }
    }
}
