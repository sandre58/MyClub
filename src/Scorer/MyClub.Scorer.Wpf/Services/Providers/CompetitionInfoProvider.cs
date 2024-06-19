// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using DynamicData.Binding;
using MyClub.Scorer.Application.Services;
using MyClub.Scorer.Domain.ProjectAggregate;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces;
using MyNet.Observable;

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
        {
            switch (project)
            {
                case LeagueProject leagueProject:
                    var league = new LeagueViewModel(leagueProject.Competition,
                                                     new SchedulingParametersViewModel(leagueProject.WhenChanged(x => x.SchedulingParameters, (x, y) => y)),
                                                     _matchdayPresentationService,
                                                     _matchPresentationService,
                                                     _stadiumsProvider,
                                                     _teamsProvider,
                                                     _leagueService);

                    _currentCompetition = league;
                    break;
                case CupProject cupProject:
                    _currentCompetition = new CupViewModel(cupProject.Competition);
                    break;
                case TournamentProject _:
                    _currentCompetition = null;
                    break;
                default:
                    break;
            }
        }

        protected override void Cleanup()
        {
            base.Cleanup();
            _currentCompetition?.Dispose();
        }
    }
}
