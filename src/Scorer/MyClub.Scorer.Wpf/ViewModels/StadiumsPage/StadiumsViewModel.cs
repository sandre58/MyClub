// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Spreadsheet;
using MyNet.UI.Commands;
using MyNet.UI.ViewModels.List;
using MyNet.Utilities;
using MyClub.Scorer.Wpf.Services;
using MyClub.Scorer.Wpf.Services.Providers;
using MyClub.Scorer.Wpf.ViewModels.Entities;

namespace MyClub.Scorer.Wpf.ViewModels.StadiumsPage
{
    internal class StadiumsViewModel : SelectionListViewModel<StadiumViewModel>
    {
        private readonly StadiumPresentationService _stadiumPresentationService;

        public StadiumDetailsViewModel DetailsViewModel { get; private set; }

        public ICommand ExportCommand { get; private set; }

        public ICommand ImportCommand { get; private set; }

        public StadiumsViewModel(
            StadiumsProvider stadiumsProvider,
            TeamsProvider teamsProvider,
            StadiumPresentationService stadiumPresentationService)
            : base(source: stadiumsProvider.Connect(),
                   parametersProvider: new StadiumsListParametersProvider())
        {
            _stadiumPresentationService = stadiumPresentationService;

            DetailsViewModel = new(teamsProvider);

            ExportCommand = CommandsManager.Create(async () => await ExportAsync().ConfigureAwait(false), () => Items.Any());
            ImportCommand = CommandsManager.Create(async () => await ImportAsync().ConfigureAwait(false));
        }

        protected override async Task<StadiumViewModel?> CreateNewItemAsync()
        {
            var id = await _stadiumPresentationService.AddAsync().ConfigureAwait(false);

            return Source.GetByIdOrDefault(id.GetValueOrDefault());
        }

        protected override void OnAddCompleted(StadiumViewModel item)
        {
            if (Items.Contains(item))
                Collection.SetSelection([item]);
        }

        protected override async Task<StadiumViewModel?> UpdateItemAsync(StadiumViewModel oldItem)
        {
            await _stadiumPresentationService.EditAsync(oldItem).ConfigureAwait(false);

            return null;
        }

        public override async Task RemoveRangeAsync(IEnumerable<StadiumViewModel> oldItems) => await _stadiumPresentationService.RemoveAsync(oldItems).ConfigureAwait(false);

        private async Task ExportAsync() => await _stadiumPresentationService.ExportAsync(Items).ConfigureAwait(false);

        private async Task ImportAsync() => await _stadiumPresentationService.ImportAsync().ConfigureAwait(false);

        protected override void OnSelectionChanged()
        {
            base.OnSelectionChanged();

            DetailsViewModel.SetItem(SelectedItem);
        }
    }
}
