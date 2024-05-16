// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using DynamicData.Binding;
using MyNet.UI.Commands;
using MyNet.UI.ViewModels.List.Filtering;
using MyNet.Utilities;
using MyClub.Teamup.Wpf.Filters;
using MyClub.Teamup.Wpf.Services;
using MyClub.Teamup.Wpf.Services.Providers;
using MyClub.Teamup.Wpf.ViewModels.Entities;

namespace MyClub.Teamup.Wpf.ViewModels.CompetitionPage
{
    internal class LeaguePageMatchesViewModel : MatchesViewModel
    {
        private readonly MatchdayPresentationService _matchdayPresentationService;

        public LeaguePageMatchesViewModel(MatchdayPresentationService matchdayPresentationService, MatchPresentationService matchPresentationService, Subject<LeagueViewModel?> leagueChanged)
            : base(new MatchesByCompetitionSourceProvider<LeagueViewModel>(leagueChanged), matchPresentationService, new LeagueMatchesListParametersProvider(leagueChanged))
        {
            _matchdayPresentationService = matchdayPresentationService;

            AddMatchdaysCommand = CommandsManager.Create(async () => await AddMatchdaysAsync().ConfigureAwait(false));

            var matchdayFilter = new MatchdayFilterViewModel(nameof(MatchViewModel.Parent));

            Disposables.AddRange(
            [
                leagueChanged.Subscribe(x => x.IfNotNull(y =>
                {
                    League = y;
                    matchdayFilter.Initialize(y.Matchdays);

                    if (Filters is FiltersViewModel filtersViewModel)
                        MyNet.Observable.Threading.Scheduler.UI.Schedule(_ => filtersViewModel.Reset());
                })),
                GetMatchdayFilter().WhenPropertyChanged(x => x.Value).Subscribe(_ =>
                {
                    Parent = SelectedMatchday;
                    RaisePropertyChanged(nameof(SelectedMatchday));
                    RaisePropertyChanged(nameof(CanAdd));
                })
            ]);

            if (Filters is ExtendedFiltersViewModel filtersViewModel)
                filtersViewModel.SetDefaultFilters([matchdayFilter]);
        }

        public LeagueViewModel? League { get; private set; }

        public MatchdayViewModel? SelectedMatchday => GetMatchdayFilter().Value;

        public ICommand AddMatchdaysCommand { get; }

        public MatchdayFilterViewModel GetMatchdayFilter() => ((LeagueMatchesSpeedFiltersViewModel)Filters).MatchdayFilter;

        public void SelectMatchday(MatchdayViewModel item) => GetMatchdayFilter().Value = item;

        public async Task AddMatchdaysAsync()
        {
            if (League is not null)
            {
                var ids = await _matchdayPresentationService.AddMultipleAsync(League, [DateTime.Today]).ConfigureAwait(false);

                if (ids is not null && ids.Any())
                {
                    var newMatchday = GetMatchdayFilter().AvailableValues?.GetById(ids.First());

                    if (newMatchday is not null)
                        SelectMatchday(newMatchday);
                }
            }
        }
    }
}
