// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using DynamicData;
using MyNet.UI.Commands;
using MyNet.Utilities;
using MyClub.Teamup.Wpf.Services;
using MyClub.Teamup.Wpf.Services.Providers;
using MyClub.Teamup.Wpf.ViewModels.Entities;
using MyClub.Teamup.Wpf.ViewModels.Entities.Interfaces;

namespace MyClub.Teamup.Wpf.ViewModels.CompetitionPage
{
    internal class CupPageMatchdaysViewModel : CompetitionPageMatchdaysViewModel
    {
        private enum RoundType
        {
            Knockout,

            GroupStage
        }

        private readonly RoundPresentationService _roundPresentationService;
        private readonly MatchdayPresentationService _matchdayPresentationService;
        private RoundType? _currentType;

        public CupPageMatchdaysViewModel(RoundPresentationService roundPresentationService, MatchdayPresentationService matchdayPresentationService, MatchPresentationService matchPresentationService, HolidaysProvider holidaysProvider, Subject<CupViewModel?> cupChanged)
            : base(matchPresentationService, holidaysProvider, collection: new MatchdaysCollection(new MatchdaysByCupSourceProvider(cupChanged)))
        {
            _roundPresentationService = roundPresentationService;
            _matchdayPresentationService = matchdayPresentationService;

            AddKnockoutCommand = CommandsManager.Create(async () => await AddKnockoutAsync().ConfigureAwait(false));
            AddGroupStageCommand = CommandsManager.Create(async () => await AddGroupStageAsync().ConfigureAwait(false));
            EditGroupStageCommand = CommandsManager.Create(async () => await EditGroupStageAsync().ConfigureAwait(false), () => SelectedItems.Select(x => x.Parent).Distinct().Count() == 1 && SelectedItems.Select(x => x.Parent).Distinct().First() is GroupStageViewModel);

            Disposables.AddRange(
            [
                cupChanged.Subscribe(x =>
                {
                    Cup = x;
                    StartDate = x?.StartDate ?? DateTime.Today.BeginningOfYear();
                    EndDate = x?.EndDate ?? DateTime.Today.EndOfYear();
                })
            ]);
        }

        public override bool CanDuplicate => SelectedItems.OfType<MatchdayViewModel>().Select(x => x.Parent).Distinct().Count() == 1;

        public CupViewModel? Cup { get; private set; }

        public ICommand AddKnockoutCommand { get; }

        public ICommand AddGroupStageCommand { get; }

        public ICommand EditGroupStageCommand { get; }

        protected override async Task<IMatchdayViewModel?> CreateNewItemAsync()
        {
            if (Cup is null || _currentType is null) return null;

            switch (_currentType)
            {
                case RoundType.Knockout:
                    await _roundPresentationService.AddKnockoutAsync(Cup, GetSelectedDates().FirstOrDefault().AddFluentTimeSpan(Cup.Rules.MatchTime)).ConfigureAwait(false);
                    break;
                case RoundType.GroupStage:
                    await _roundPresentationService.AddGroupStageAsync(Cup, GetSelectedDates().MinOrDefault(), GetSelectedDates().MaxOrDefault()).ConfigureAwait(false);
                    break;
            }

            return null;
        }

        protected override async Task<IMatchdayViewModel?> UpdateItemAsync(IMatchdayViewModel oldItem)
        {
            await _roundPresentationService.EditAsync(oldItem).ConfigureAwait(false);

            return null;
        }

        protected override async Task AddToDateAsync(DateTime date)
        {
            if (Cup is not null)
                await _roundPresentationService.AddKnockoutAsync(Cup, date.AddFluentTimeSpan(Cup.Rules.MatchTime)).ConfigureAwait(false);
        }

        public override async Task AddMultipleAsync()
        {
            if (Cup is not null)
                await _roundPresentationService.AddKnockoutAsync(Cup, GetSelectedDates().FirstOrDefault().AddFluentTimeSpan(Cup.Rules.MatchTime)).ConfigureAwait(false);
        }

        public override async Task DuplicateAsync()
        {
            var selectedItems = SelectedItems.OfType<MatchdayViewModel>();
            var parents = selectedItems.Select(x => x.Parent).Distinct();

            if (parents.Count() != 1) return;

            await _matchdayPresentationService.AddMultipleAsync(parents.First(), SelectedItems.OfType<MatchdayViewModel>(), GetSelectedDates()).ConfigureAwait(false);
        }

        public override async Task PostponeAsync()
        {
            if (SelectedItems.Any())
                await _roundPresentationService.PostponeAsync(SelectedItems).ConfigureAwait(false);
        }

        public override async Task RemoveRangeAsync(IEnumerable<IMatchdayViewModel> oldItems) => await _roundPresentationService.RemoveAsync(oldItems).ConfigureAwait(false);

        private async Task AddKnockoutAsync() => await AddRoundAsync(RoundType.Knockout).ConfigureAwait(false);

        private async Task AddGroupStageAsync() => await AddRoundAsync(RoundType.GroupStage).ConfigureAwait(false);

        private async Task AddRoundAsync(RoundType type)
        {
            _currentType = type;
            await AddAsync().ConfigureAwait(false);
        }

        private async Task EditGroupStageAsync()
        {
            var parents = SelectedItems.Select(x => x.Parent).OfType<GroupStageViewModel>().Distinct();

            if (parents.Count() != 1) return;

            await _roundPresentationService.EditAsync(parents.First()).ConfigureAwait(false);
        }
    }
}
