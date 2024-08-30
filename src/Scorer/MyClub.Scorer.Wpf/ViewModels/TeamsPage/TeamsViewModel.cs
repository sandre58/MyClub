// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;
using MyClub.Scorer.Wpf.Services;
using MyClub.Scorer.Wpf.Services.Providers;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces;
using MyNet.Utilities;

namespace MyClub.Scorer.Wpf.ViewModels.TeamsPage
{
    internal class TeamsViewModel : TeamsViewModelBase<ITeamViewModel>
    {
        private readonly TeamPresentationService _teamPresentationService;

        public TeamDetailsViewModel DetailsViewModel { get; private set; }

        public TeamsViewModel(TeamsProvider teamsProvider,
                              TeamPresentationService teamPresentationService)
            : base(teamsProvider, teamPresentationService.HasImportSources(), new TeamsListParametersProvider())
        {
            _teamPresentationService = teamPresentationService;

            DetailsViewModel = new(teamPresentationService);
        }

        protected override async Task AddItemAsync(string name) => await _teamPresentationService.AddAsync(name).ConfigureAwait(false);

        protected override async Task<ITeamViewModel?> CreateNewItemAsync()
        {
            var id = await _teamPresentationService.AddAsync().ConfigureAwait(false);

            return Source.GetByIdOrDefault(id.GetValueOrDefault());
        }

        protected override async Task<ITeamViewModel?> UpdateItemAsync(ITeamViewModel oldItem)
        {
            await _teamPresentationService.EditAsync(oldItem).ConfigureAwait(false);

            return null;
        }

        public override async Task RemoveRangeAsync(IEnumerable<ITeamViewModel> oldItems) => await _teamPresentationService.RemoveAsync(oldItems).ConfigureAwait(false);

        protected override void OnAddCompleted(ITeamViewModel item)
        {
            if (Items.Contains(item))
                Collection.SetSelection([item]);
        }

        protected override async Task ExportAsync() => await _teamPresentationService.ExportAsync(Items).ConfigureAwait(false);

        protected override async Task ImportAsync() => await _teamPresentationService.LaunchImportAsync().ConfigureAwait(false);

        protected override void OnSelectionChanged()
        {
            base.OnSelectionChanged();

            DetailsViewModel.SetItem((TeamViewModel?)SelectedItem);
        }
    }
}
