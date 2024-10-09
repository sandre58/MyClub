// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;
using MyClub.Scorer.Wpf.Services;
using MyClub.Scorer.Wpf.Services.Providers;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyNet.Utilities;

namespace MyClub.Scorer.Wpf.ViewModels.StadiumsPage
{
    internal class StadiumsViewModel : StadiumsViewModelBase<StadiumViewModel>
    {
        private readonly StadiumPresentationService _stadiumPresentationService;

        public StadiumDetailsViewModel DetailsViewModel { get; private set; }

        public StadiumsViewModel(StadiumsProvider stadiumsProvider,
                                 TeamsProvider teamsProvider,
                                 StadiumPresentationService stadiumPresentationService)
            : base(stadiumsProvider, stadiumPresentationService.HasImportSources(), new StadiumsListParametersProvider())
        {
            DetailsViewModel = new(teamsProvider);

            _stadiumPresentationService = stadiumPresentationService;
        }

        protected override async Task AddItemAsync(string name) => await _stadiumPresentationService.AddAsync(name).ConfigureAwait(false);

        protected override async Task<StadiumViewModel?> CreateNewItemAsync()
        {
            var id = await _stadiumPresentationService.AddAsync().ConfigureAwait(false);

            return Source.GetByIdOrDefault(id.GetValueOrDefault());
        }

        protected override async Task<StadiumViewModel?> UpdateItemAsync(StadiumViewModel oldItem)
        {
            await _stadiumPresentationService.EditAsync(oldItem).ConfigureAwait(false);

            return default;
        }

        public override async Task RemoveRangeAsync(IEnumerable<StadiumViewModel> oldItems) => await _stadiumPresentationService.RemoveAsync(oldItems).ConfigureAwait(false);

        protected override async Task ExportAsync() => await _stadiumPresentationService.ExportAsync(Items).ConfigureAwait(false);

        protected override async Task ImportAsync() => await _stadiumPresentationService.LauchImportAsync().ConfigureAwait(false);

        protected override void OnSelectionChanged()
        {
            base.OnSelectionChanged();

            DetailsViewModel.SetItem(SelectedItem);
        }
    }
}
