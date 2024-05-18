// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;
using MyClub.Teamup.Application.Contracts;
using MyClub.Teamup.Application.Dtos;
using MyClub.Teamup.Application.Services;
using MyClub.Teamup.Wpf.Services.Providers;
using MyClub.Teamup.Wpf.ViewModels.Edition;
using MyClub.Teamup.Wpf.ViewModels.Entities;
using MyClub.Teamup.Wpf.ViewModels.Import;
using MyClub.Teamup.Wpf.ViewModels.Selection;
using MyNet.UI.Dialogs;
using MyNet.UI.Extensions;
using MyNet.UI.Locators;
using MyNet.UI.Selection;
using MyNet.Utilities;
using MyNet.Utilities.Providers;

namespace MyClub.Teamup.Wpf.Services
{
    internal class StadiumPresentationService(StadiumService stadiumService,
                                              ImportStadiumsProvider importStadiumsProvider,
                                              IViewModelLocator viewModelLocator)
    {
        private readonly IViewModelLocator _viewModelLocator = viewModelLocator;
        private readonly StadiumService _stadiumService = stadiumService;
        private readonly ImportStadiumsProvider _importStadiumsProvider = importStadiumsProvider;

        public async Task<EditableStadiumViewModel?> CreateAsync(string? name = null)
        {
            var vm = _viewModelLocator.Get<StadiumEditionViewModel>();
            vm.New(name);

            var result = await DialogManager.ShowDialogAsync(vm).ConfigureAwait(false);

            if (result.IsTrue())
            {
                var item = new EditableStadiumViewModel(vm.Id)
                {
                    Name = vm.Name,
                    Ground = vm.Ground,
                    Address = vm.Address.Create()
                };

                return item;
            }

            return null;
        }

        public async Task<bool?> UpdateAsync(EditableStadiumViewModel oldItem)
        {
            var vm = _viewModelLocator.Get<StadiumEditionViewModel>();
            vm.Load(oldItem);

            var result = await DialogManager.ShowDialogAsync(vm).ConfigureAwait(false);

            if (result.IsTrue())
            {
                oldItem.Name = vm.Name;
                oldItem.Ground = vm.Ground;
                oldItem.Address = vm.Address.Create();
            }

            return result;
        }

        public async Task EditAsync(StadiumViewModel item)
        {
            var vm = _viewModelLocator.Get<StadiumEditionViewModel>();
            vm.Load(new EditableStadiumViewModel(item.Id)
            {
                Address = item.Address,
                Ground = item.Ground,
                Name = item.Name,
            });

            var result = await DialogManager.ShowDialogAsync(vm).ConfigureAwait(false);

            if (result.IsTrue())
            {
                _stadiumService.Update(new StadiumDto
                {
                    Id = vm.ItemId,
                    Address = vm.Address.Create(),
                    Ground = vm.Ground,
                    Name = vm.Name
                });
            }
        }

        public async Task<EditableStadiumViewModel?> ImportAsync(IEnumerable<(string name, string? city)> excludeStadiumNamesAndCity)
        {
            var vm = new StadiumsSelectionViewModel(new PredicateItemsProvider<StadiumImportableViewModel>(_importStadiumsProvider, x => !excludeStadiumNamesAndCity.Contains((x.Name, x.City))), SelectionMode.Single);

            var result = await DialogManager.ShowDialogAsync(vm).ConfigureAwait(false);

            if (result.IsTrue() && vm.SelectedItem is not null)
            {
                var item = new EditableStadiumViewModel
                {
                    Address = vm.SelectedItem.GetAddress(),
                    Ground = vm.SelectedItem.Ground,
                    Name = vm.SelectedItem.Name,
                };

                return item;
            }

            return null;
        }

        public bool CanImport() => _importStadiumsProvider.CanImport();
    }
}
