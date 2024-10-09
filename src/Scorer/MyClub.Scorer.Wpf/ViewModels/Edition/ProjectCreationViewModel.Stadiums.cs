// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using MyClub.CrossCutting.Localization;
using MyClub.Scorer.Application.Services;
using MyClub.Scorer.Plugins.Contracts;
using MyClub.Scorer.Wpf.Services;
using MyClub.Scorer.Wpf.ViewModels.Import;
using MyClub.Scorer.Wpf.ViewModels.StadiumsPage;
using MyNet.UI.Dialogs;
using MyNet.UI.ViewModels.Import;
using MyNet.Utilities;

namespace MyClub.Scorer.Wpf.ViewModels.Edition
{
    internal class ProjectCreationStadiumsViewModel : StadiumsViewModelBase<EditableStadiumViewModel>
    {
        private readonly PluginsService _pluginsService;
        private readonly AddressService _addressService;

        public ProjectCreationStadiumsViewModel(PluginsService pluginsService, AddressService addressService) : base(pluginsService.HasPlugin<IImportStadiumsSourcePlugin>())
        {
            _pluginsService = pluginsService;
            _addressService = addressService;
        }

        protected override Task AddItemAsync(string name)
        {
            var item = new EditableStadiumViewModel
            {
                Name = name
            };
            Collection.Add(item);

            return Task.CompletedTask;
        }

        protected override async Task<EditableStadiumViewModel?> CreateNewItemAsync()
        {
            using var vm = new EditableStadiumEditionViewModel(_addressService);
            vm.New(MyClubResources.Stadium.Increment(Items.Select(y => y.Name).ToList(), format: " #"));

            var result = await DialogManager.ShowDialogAsync(vm).ConfigureAwait(false);

            return result.IsTrue()
                ? new EditableStadiumViewModel
                {
                    Name = vm.Name.OrEmpty(),
                    Ground = vm.Ground,
                    Address = vm.Address.Create()
                }
                : null;
        }

        protected override async Task<EditableStadiumViewModel?> UpdateItemAsync(EditableStadiumViewModel oldItem)
        {
            using var vm = new EditableStadiumEditionViewModel(_addressService);
            vm.Load(oldItem);

            var result = await DialogManager.ShowDialogAsync(vm).ConfigureAwait(false);

            if (result.IsTrue())
            {
                oldItem.Name = vm.Name.OrEmpty();
                oldItem.Ground = vm.Ground;
                oldItem.Address = vm.Address.Create();
            }

            return null;
        }

        public override async Task RemoveRangeAsync(IEnumerable<EditableStadiumViewModel> oldItems) => await ExecuteAsync(() => Collection.RemoveMany(oldItems)).ConfigureAwait(false);

        protected override Task ExportAsync() => throw new NotImplementedException();

        protected override async Task ImportAsync()
        {
            using var vm = new StadiumsImportBySourcesDialogViewModel(new StadiumsImportBySourcesProvider(_pluginsService, (x, y) => Items.Any(z => z.Name.Equals(x, StringComparison.OrdinalIgnoreCase) && z.Address?.City == y)));

            if (vm.Sources.Count == 0) return;

            var result = await DialogManager.ShowDialogAsync(vm).ConfigureAwait(false);

            if (result.IsFalse()) return;

            // Update stadiums
            vm.List.ImportItems.Where(x => x.Mode == ImportMode.Update).ForEach(x =>
            {
                var similarStadium = Items.FirstOrDefault(y => y.Name.Equals(x.Name, StringComparison.OrdinalIgnoreCase));

                if (similarStadium != null)
                {
                    similarStadium.Name = x.Name;
                    similarStadium.Ground = x.Ground;
                    similarStadium.Address = x.GetAddress();
                }
            });

            // Add stadiums
            Collection.AddRange(vm.List.ImportItems.Where(x => x.Mode == ImportMode.Add).Select(x => new EditableStadiumViewModel
            {
                Name = x.Name,
                Ground = x.Ground,
                Address = x.GetAddress(),
            }));
        }

        protected override Task OnRemovingRequestedAsync(IEnumerable<EditableStadiumViewModel> oldItems, CancelEventArgs cancelEventArgs) => Task.CompletedTask;

        protected override void ResetCore()
        {
            base.ResetCore();
            Collection.Clear();
        }

        public void Clear() => Collection.Clear();
    }
}
