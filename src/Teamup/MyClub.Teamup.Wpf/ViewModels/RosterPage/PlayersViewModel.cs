// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using MyNet.UI.Commands;
using MyNet.UI.ViewModels.List;
using MyNet.Utilities;
using MyClub.Teamup.Domain.Enums;
using MyClub.Teamup.Wpf.Services;
using MyClub.Teamup.Wpf.Services.Providers;
using MyClub.Teamup.Wpf.ViewModels.Entities;
using MyNet.Observable.Attributes;

namespace MyClub.Teamup.Wpf.ViewModels.RosterPage
{
    internal class PlayersViewModel : SelectionListViewModel<PlayerViewModel>
    {
        private readonly PlayerPresentationService _playerPresentationService;

        public bool CanMoveSelectedItems => SelectedItems.Any(x => x.TeamId == null || x.OtherTeams.Any());

        [CanSetIsModified(false)]
        [CanBeValidated(false)]
        public bool HasImportSources { get; private set; }

        public ICommand MoveSelectedItemsCommand { get; private set; }

        public ICommand AddAbsenceToSelectedItemsCommand { get; private set; }

        public ICommand OpenMailClientSelectedItemsCommand { get; private set; }

        public ICommand OpenCommunicationCommand { get; private set; }

        public ICommand ExportCommand { get; private set; }

        public ICommand ImportCommand { get; private set; }

        public PlayersViewModel(
            PlayersProvider playersProvider,
            PlayerPresentationService playerPresentationService)
            : base(source: playersProvider.Connect(),
                   parametersProvider: new PlayersListParametersProvider())
        {
            _playerPresentationService = playerPresentationService;
            HasImportSources = _playerPresentationService.HasImportSources();

            MoveSelectedItemsCommand = CommandsManager.Create<TeamViewModel>(async x => await MoveAsync(SelectedItems, x).ConfigureAwait(false), x => SelectedItems.Any(y => y.TeamId != x?.Id));
            AddAbsenceToSelectedItemsCommand = CommandsManager.Create<AbsenceType>(async x => await AddAbsenceAsync(SelectedItems, x).ConfigureAwait(false), x => SelectedItems.Any());
            OpenMailClientSelectedItemsCommand = CommandsManager.Create(() => MailCommandsService.OpenMailClient(SelectedItems.Select(x => x.Email?.Value).NotNull()), () => SelectedItems.Any(y => y.Email is not null));
            OpenCommunicationCommand = CommandsManager.Create(async () => await NavigationCommandsService.NavigateToCommunicationPageAsync(SelectedItems.Select(x => x.Email?.Value).NotNull(), string.Empty, string.Empty, true).ConfigureAwait(false), () => SelectedItems.Any(y => y.Email is not null));
            ExportCommand = CommandsManager.Create(async () => await ExportAsync().ConfigureAwait(false), () => Items.Any());
            ImportCommand = CommandsManager.Create(async () => await ImportAsync().ConfigureAwait(false), () => HasImportSources);
        }

        protected override async Task<PlayerViewModel?> CreateNewItemAsync()
        {
            var id = await _playerPresentationService.AddAsync().ConfigureAwait(false);

            return Source.GetByIdOrDefault(id.GetValueOrDefault());
        }

        protected override void OnAddCompleted(PlayerViewModel item)
        {
            if (Items.Contains(item))
                Collection.SetSelection([item]);
        }

        protected override async Task<PlayerViewModel?> UpdateItemAsync(PlayerViewModel oldItem)
        {
            await _playerPresentationService.EditAsync(oldItem).ConfigureAwait(false);

            return null;
        }

        protected override async Task<IEnumerable<PlayerViewModel>> UpdateRangeAsync(IEnumerable<PlayerViewModel> oldItems)
        {
            if (oldItems.Count() == 1)
                await _playerPresentationService.EditAsync(oldItems.First()).ConfigureAwait(false);
            else if (oldItems.Count() > 1)
                await _playerPresentationService.EditMultipleAsync(oldItems).ConfigureAwait(false);

            return [];
        }

        public override async Task RemoveRangeAsync(IEnumerable<PlayerViewModel> oldItems) => await _playerPresentationService.RemoveAsync(oldItems).ConfigureAwait(false);

        private async Task MoveAsync(IEnumerable<PlayerViewModel> players, TeamViewModel? team) => await _playerPresentationService.MoveAsync(players, team).ConfigureAwait(false);

        private async Task AddAbsenceAsync(IEnumerable<PlayerViewModel> players, AbsenceType absenceType) => await _playerPresentationService.AddAbsenceAsync(players, absenceType).ConfigureAwait(false);

        protected override void OnSelectionChanged()
        {
            base.OnSelectionChanged();

            RaisePropertyChanged(nameof(CanMoveSelectedItems));
        }

        private async Task ExportAsync() => await _playerPresentationService.ExportAsync(Items).ConfigureAwait(false);

        private async Task ImportAsync() => await _playerPresentationService.LaunchImportAsync().ConfigureAwait(false);
    }
}
