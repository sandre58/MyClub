// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyNet.UI.Commands;
using MyNet.UI.ViewModels.List;
using MyNet.Utilities;
using MyClub.Scorer.Wpf.Services;
using MyClub.Scorer.Wpf.Services.Providers;
using MyClub.Scorer.Wpf.ViewModels.Entities;

namespace MyClub.Scorer.Wpf.ViewModels.TeamsPage
{
    internal class TeamsViewModel : SelectionListViewModel<TeamViewModel>
    {
        private readonly TeamPresentationService _teamPresentationService;

        public TeamDetailsViewModel DetailsViewModel { get; private set; }

        public ICommand ExportCommand { get; private set; }

        public ICommand ImportCommand { get; private set; }

        public TeamsViewModel(
            TeamsProvider teamsProvider,
            TeamPresentationService teamPresentationService)
            : base(source: teamsProvider.Connect(),
                   parametersProvider: new TeamsListParametersProvider())
        {
            _teamPresentationService = teamPresentationService;

            DetailsViewModel = new(teamPresentationService);

            ExportCommand = CommandsManager.Create(async () => await ExportAsync().ConfigureAwait(false), () => Items.Any());
            ImportCommand = CommandsManager.Create(async () => await ImportAsync().ConfigureAwait(false));
        }

        protected override async Task<TeamViewModel?> CreateNewItemAsync()
        {
            var id = await _teamPresentationService.AddAsync().ConfigureAwait(false);

            return Source.GetByIdOrDefault(id.GetValueOrDefault());
        }

        protected override void OnAddCompleted(TeamViewModel item)
        {
            if (Items.Contains(item))
                Collection.SetSelection([item]);
        }

        protected override async Task<TeamViewModel?> UpdateItemAsync(TeamViewModel oldItem)
        {
            await _teamPresentationService.EditAsync(oldItem).ConfigureAwait(false);

            return null;
        }

        public override async Task RemoveRangeAsync(IEnumerable<TeamViewModel> oldItems) => await _teamPresentationService.RemoveAsync(oldItems).ConfigureAwait(false);

        private async Task ExportAsync() => await _teamPresentationService.ExportAsync(Items).ConfigureAwait(false);

        private async Task ImportAsync() => await _teamPresentationService.ImportAsync().ConfigureAwait(false);

        protected override void OnSelectionChanged()
        {
            base.OnSelectionChanged();

            DetailsViewModel.SetItem(SelectedItem);
        }
    }
}
