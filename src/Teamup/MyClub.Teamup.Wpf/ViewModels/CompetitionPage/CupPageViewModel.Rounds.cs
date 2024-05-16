// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using MyNet.UI.Commands;
using MyNet.UI.ViewModels.List;
using MyNet.Utilities;
using MyClub.Teamup.Wpf.Services;
using MyClub.Teamup.Wpf.Services.Providers;
using MyClub.Teamup.Wpf.ViewModels.Entities;

namespace MyClub.Teamup.Wpf.ViewModels.CompetitionPage
{
    internal class CupPageRoundsViewModel : ListViewModel<RoundDetailsViewModel>
    {
        private enum RoundType
        {
            Knockout,

            GroupStage
        }

        private readonly RoundPresentationService _roundPresentationService;
        private RoundType? _currentType;

        public CupPageRoundsViewModel(RoundPresentationService roundPresentationService, MatchdayPresentationService matchdayPresentationService, MatchPresentationService matchPresentationService, Subject<CupViewModel?> cupChanged)
            : base(new RoundDetailsByCupSourceProvider(matchdayPresentationService, matchPresentationService, cupChanged).Connect(), parametersProvider: new CupRoundsListParametersProvider())
        {
            _roundPresentationService = roundPresentationService;

            AddKnockoutCommand = CommandsManager.Create(async () => await AddKnockoutAsync().ConfigureAwait(false));
            AddGroupStageCommand = CommandsManager.Create(async () => await AddGroupStageAsync().ConfigureAwait(false));
            NextFixturesCommand = CommandsManager.Create(() => SelectedItem = GetNextFixtures(), () => GetNextFixtures() is not null);
            LatestResultsCommand = CommandsManager.Create(() => SelectedItem = GetLatestResults(), () => GetLatestResults() is not null);

            Disposables.AddRange(
            [
                cupChanged.Subscribe(x =>
                {
                    Cup = x;
                    SelectedItem = GetLatestResults() ?? GetNextFixtures();
                }),
            ]);
        }

        public CupViewModel? Cup { get; private set; }

        public ICommand AddKnockoutCommand { get; }

        public ICommand AddGroupStageCommand { get; }

        public ICommand NextFixturesCommand { get; private set; }

        public ICommand LatestResultsCommand { get; private set; }

        protected override async Task<RoundDetailsViewModel?> CreateNewItemAsync()
        {
            if (Cup is null || _currentType is null) return null;

            switch (_currentType)
            {
                case RoundType.Knockout:
                    await _roundPresentationService.AddKnockoutAsync(Cup).ConfigureAwait(false);
                    break;
                case RoundType.GroupStage:
                    await _roundPresentationService.AddGroupStageAsync(Cup).ConfigureAwait(false);
                    break;
            }

            return null;
        }

        protected override async Task<RoundDetailsViewModel?> UpdateItemAsync(RoundDetailsViewModel oldItem)
        {
            await _roundPresentationService.EditAsync(oldItem.Item).ConfigureAwait(false);

            return null;
        }

        private async Task AddKnockoutAsync() => await AddRoundAsync(RoundType.Knockout).ConfigureAwait(false);

        private async Task AddGroupStageAsync() => await AddRoundAsync(RoundType.GroupStage).ConfigureAwait(false);

        private async Task AddRoundAsync(RoundType type)
        {
            _currentType = type;
            await AddAsync().ConfigureAwait(false);
        }

        private RoundDetailsViewModel? GetNextFixtures() => Items.FirstOrDefault(x => x.Item.StartDate.IsAfter(DateTime.Now));

        private RoundDetailsViewModel? GetLatestResults() => Items.LastOrDefault(x => x.Item.StartDate.IsBefore(DateTime.Now));

        public void SelectRound(RoundViewModel item) => SelectedItem = Items.FirstOrDefault(x => x.Item == item);
    }
}
