// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using MyNet.UI.Commands;
using MyNet.UI.Dialogs;
using MyNet.UI.Services;
using MyNet.UI.ViewModels.List;
using MyNet.Utilities;
using MyNet.Humanizer;
using MyNet.UI.Resources;
using MyClub.Teamup.Wpf.Services;
using MyClub.Teamup.Wpf.Services.Providers;
using MyClub.Teamup.Wpf.ViewModels.Entities;
using MyNet.Observable.Attributes;

namespace MyClub.Teamup.Wpf.ViewModels.CompetitionsPage.CompetitionsTab
{
    internal class CompetitionsViewModel : SelectionListViewModel<CompetitionViewModel>
    {
        private enum CompetitionType
        {
            Friendly,

            League,

            Cup
        }

        private readonly FriendlyPresentationService _friendlyPresentationService;
        private readonly LeaguePresentationService _leaguePresentationService;
        private readonly CupPresentationService _cupPresentationService;
        private readonly CompetitionPresentationService _competitionPresentationService;
        private CompetitionType? _currentType;

        public CompetitionsViewModel(CompetitionsProvider competitionsProvider,
                                     FriendlyPresentationService friendlyPresentationService,
                                     LeaguePresentationService leaguePresentationService,
                                     CupPresentationService cupPresentationService,
                                     CompetitionPresentationService competitionPresentationService)
            : base(source: competitionsProvider.Connect(),
                  parametersProvider: new CompetitionsListParametersProvider())
        {
            _friendlyPresentationService = friendlyPresentationService;
            _leaguePresentationService = leaguePresentationService;
            _cupPresentationService = cupPresentationService;
            _competitionPresentationService = competitionPresentationService;
            HasImportSources = _competitionPresentationService.HasImportSources();

            DuplicateSelectedItemCommand = CommandsManager.Create(async () => await SelectedItem!.DuplicateAsync().ConfigureAwait(false), () => SelectedItems.Count() == 1);
            AddLeagueCommand = CommandsManager.Create(async () => await AddLeagueAsync().ConfigureAwait(false));
            AddCupCommand = CommandsManager.Create(async () => await AddCupAsync().ConfigureAwait(false));
            AddFriendlyCommand = CommandsManager.Create(async () => await AddFriendlyAsync().ConfigureAwait(false));
            ExportCommand = CommandsManager.Create(async () => await ExportAsync().ConfigureAwait(false), () => Items.Any());
            ImportCommand = CommandsManager.Create(async () => await ImportAsync().ConfigureAwait(false), () => HasImportSources);
        }

        [CanSetIsModified(false)]
        [CanBeValidated(false)]
        public bool HasImportSources { get; private set; }

        public ICommand AddLeagueCommand { get; }

        public ICommand AddCupCommand { get; }

        public ICommand AddFriendlyCommand { get; }

        public ICommand DuplicateSelectedItemCommand { get; }

        public ICommand ExportCommand { get; }

        public ICommand ImportCommand { get; }

        protected override async Task<CompetitionViewModel?> CreateNewItemAsync()
        {
            if (_currentType is null) return null;

            Guid? id = null;
            switch (_currentType)
            {
                case CompetitionType.Friendly:
                    id = await _friendlyPresentationService.AddAsync().ConfigureAwait(false);
                    break;
                case CompetitionType.League:
                    id = await _leaguePresentationService.AddAsync().ConfigureAwait(false);
                    break;
                case CompetitionType.Cup:
                    id = await _cupPresentationService.AddAsync().ConfigureAwait(false);
                    break;
            }

            return Source.GetByIdOrDefault(id.GetValueOrDefault());
        }

        protected override void OnAddCompleted(CompetitionViewModel item)
        {
            if (Items.Contains(item))
                Collection.SetSelection([item]);
        }

        protected override async Task<CompetitionViewModel?> UpdateItemAsync(CompetitionViewModel oldItem)
        {
            switch (oldItem)
            {
                case FriendlyViewModel friendly:
                    await _friendlyPresentationService.EditAsync(friendly).ConfigureAwait(false);
                    break;
                case LeagueViewModel league:
                    await _leaguePresentationService.EditAsync(league).ConfigureAwait(false);
                    break;
                case CupViewModel cup:
                    await _cupPresentationService.EditAsync(cup).ConfigureAwait(false);
                    break;
            }

            return null;
        }

        public override async Task RemoveRangeAsync(IEnumerable<CompetitionViewModel> oldItems)
        {
            if (!oldItems.Any()) return;

            var cancel = await DialogManager.ShowQuestionAsync(nameof(MessageResources.XItemsRemovingQuestion).TranslateWithCountAndOptionalFormat(oldItems.Count())!, UiResources.Removing).ConfigureAwait(false) != MessageBoxResult.Yes;

            if (!cancel)
            {

                await AppBusyManager.WaitAsync(() =>
                {
                    var friendlies = oldItems.OfType<FriendlyViewModel>().ToList();
                    var leagues = oldItems.OfType<LeagueViewModel>().ToList();
                    var cups = oldItems.OfType<CupViewModel>().ToList();

                    if (friendlies.Count != 0)
                        _friendlyPresentationService.Remove(friendlies);

                    if (leagues.Count != 0)
                        _leaguePresentationService.Remove(leagues);

                    if (cups.Count != 0)
                        _cupPresentationService.Remove(cups);
                });
            }
        }

        private async Task AddLeagueAsync() => await AddCompetitionAsync(CompetitionType.League).ConfigureAwait(false);

        private async Task AddCupAsync() => await AddCompetitionAsync(CompetitionType.Cup).ConfigureAwait(false);

        private async Task AddFriendlyAsync() => await AddCompetitionAsync(CompetitionType.Friendly).ConfigureAwait(false);

        private async Task AddCompetitionAsync(CompetitionType type)
        {
            _currentType = type;
            await AddAsync().ConfigureAwait(false);
        }

        private async Task ExportAsync() => await _competitionPresentationService.ExportAsync(Items).ConfigureAwait(false);

        private async Task ImportAsync() => await _competitionPresentationService.LaunchImportAsync().ConfigureAwait(false);
    }
}
