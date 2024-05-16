// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using DynamicData;
using MyNet.Utilities;
using MyClub.Teamup.Wpf.Services;
using MyClub.Teamup.Wpf.Services.Providers;
using MyClub.Teamup.Wpf.ViewModels.Entities;
using MyClub.Teamup.Wpf.ViewModels.Entities.Interfaces;

namespace MyClub.Teamup.Wpf.ViewModels.CompetitionPage
{
    internal class LeaguePageMatchdaysViewModel : CompetitionPageMatchdaysViewModel
    {
        private readonly MatchdayPresentationService _matchdayPresentationService;

        public LeaguePageMatchdaysViewModel(MatchdayPresentationService matchdayPresentationService, MatchPresentationService matchPresentationService, HolidaysProvider holidaysProvider, Subject<LeagueViewModel?> leagueChanged)
            : base(matchPresentationService, holidaysProvider, collection: new MatchdaysCollection(new MatchdaysByLeagueSourceProvider(leagueChanged)))
        {
            _matchdayPresentationService = matchdayPresentationService;

            Disposables.AddRange(
            [
                leagueChanged.Subscribe(x =>
                {
                    League = x;
                    StartDate = x?.StartDate ?? DateTime.Today.BeginningOfYear();
                    EndDate = x?.EndDate ?? DateTime.Today.EndOfYear();
                })
            ]);
        }

        public LeagueViewModel? League { get; private set; }

        protected override async Task<IMatchdayViewModel?> CreateNewItemAsync()
        {
            if (League is not null)
                await _matchdayPresentationService.AddAsync(League).ConfigureAwait(false);

            return null;
        }

        protected override async Task<IMatchdayViewModel?> UpdateItemAsync(IMatchdayViewModel oldItem)
        {
            await _matchdayPresentationService.EditAsync(oldItem.CastIn<MatchdayViewModel>()).ConfigureAwait(false);

            return null;
        }

        protected override async Task AddToDateAsync(DateTime date)
        {
            if (League is not null)
                await _matchdayPresentationService.AddAsync(League, date.AddFluentTimeSpan(League.Rules.MatchTime)).ConfigureAwait(false);
        }

        public override async Task AddMultipleAsync()
        {
            if (League is not null)
                await _matchdayPresentationService.AddMultipleAsync(League, GetSelectedDates()).ConfigureAwait(false);
        }

        public override async Task DuplicateAsync()
        {
            if (League is not null && SelectedItems.Any())
                await _matchdayPresentationService.AddMultipleAsync(League, SelectedItems.OfType<MatchdayViewModel>(), GetSelectedDates()).ConfigureAwait(false);
        }

        public override async Task PostponeAsync()
        {
            if (League is not null && SelectedItems.Any())
                await _matchdayPresentationService.PostponeAsync(SelectedItems.OfType<MatchdayViewModel>()).ConfigureAwait(false);
        }

        public override async Task RemoveRangeAsync(IEnumerable<IMatchdayViewModel> oldItems) => await _matchdayPresentationService.RemoveAsync(oldItems.OfType<MatchdayViewModel>()).ConfigureAwait(false);
    }
}
