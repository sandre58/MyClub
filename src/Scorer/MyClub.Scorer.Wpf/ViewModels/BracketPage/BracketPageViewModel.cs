// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyNet.UI.ViewModels;
using MyClub.Scorer.Wpf.Services;
using MyClub.Scorer.Wpf.Services.Providers;
using MyClub.Scorer.Domain.Enums;
using MyClub.Scorer.Wpf.ViewModels.Entities;

namespace MyClub.Scorer.Wpf.ViewModels.BracketPage
{
    internal class BracketPageViewModel : PageViewModel
    {
        public BracketPageViewModel(ProjectInfoProvider projectInfoProvider,
                                    CompetitionInfoProvider competitionInfoProvider,
                                    MatchdaysProvider matchdaysProvider,
                                    MatchdayPresentationService matchdayPresentationService)
        {
            projectInfoProvider.WhenProjectClosing(() =>
            {
                MatchParentsViewModel?.Dispose();
                MatchParentsViewModel = null;
            });

            projectInfoProvider.WhenProjectLoaded(x => MatchParentsViewModel = x.Type switch
            {
                CompetitionType.League => new MatchdaysViewModel(competitionInfoProvider.GetCompetition<LeagueViewModel>(), matchdaysProvider, matchdayPresentationService),
                _ => throw new NotImplementedException(),
            });
        }

        public IListViewModel? MatchParentsViewModel { get; private set; }
    }
}
