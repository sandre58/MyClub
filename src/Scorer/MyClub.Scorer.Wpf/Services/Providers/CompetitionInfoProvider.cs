// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyNet.Observable;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces;
using MyClub.Scorer.Application.Services;
using MyClub.Scorer.Domain.CompetitionAggregate;
using MyClub.Scorer.Domain.ProjectAggregate;

namespace MyClub.Scorer.Wpf.Services.Providers
{
    internal class CompetitionInfoProvider : ObservableObject
    {
        private readonly MatchdayPresentationService _matchdayPresentationService;
        private readonly MatchPresentationService _matchPresentationService;
        private readonly StadiumsProvider _stadiumsProvider;
        private readonly TeamsProvider _teamsProvider;
        private readonly LeagueService _leagueService;
        private ICompetitionViewModel? _currentCompetition;

        public CompetitionInfoProvider(ProjectInfoProvider projectInfoProvider,
                                       MatchdayPresentationService matchdayPresentationService,
                                       MatchPresentationService matchPresentationService,
                                       StadiumsProvider stadiumsProvider,
                                       TeamsProvider teamsProvider,
                                       LeagueService leagueService)
        {
            _matchdayPresentationService = matchdayPresentationService;
            _matchPresentationService = matchPresentationService;
            _stadiumsProvider = stadiumsProvider;
            _teamsProvider = teamsProvider;
            _leagueService = leagueService;

            projectInfoProvider.WhenProjectClosing(Clear);
            projectInfoProvider.WhenProjectLoaded(Reload);
        }

        public ICompetitionViewModel GetCompetition() => GetCompetition<ICompetitionViewModel>();

        public T GetCompetition<T>() where T : ICompetitionViewModel => (T)_currentCompetition! ?? throw new InvalidCastException($"Competition is not of type {typeof(T)}");

        private void Clear() => _currentCompetition?.Dispose();

        protected void Reload(IProject project)
            => _currentCompetition = project.Competition switch
            {
                League league => new LeagueViewModel(league, _matchdayPresentationService, _matchPresentationService, _stadiumsProvider, _teamsProvider, _leagueService),
                Cup cup => new CupViewModel(cup),
                _ => null,
            };

        protected override void Cleanup() => _currentCompetition?.Dispose();
    }
}
