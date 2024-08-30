// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MyClub.Scorer.Application.Dtos;
using MyClub.Scorer.Application.Services;
using MyClub.Scorer.Plugins.Contracts;
using MyClub.Scorer.Wpf.ViewModels.Edition;
using MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces;
using MyClub.Scorer.Wpf.ViewModels.Export;
using MyClub.Scorer.Wpf.ViewModels.Import;
using MyNet.UI.Dialogs;
using MyNet.UI.Extensions;
using MyNet.UI.Locators;
using MyNet.UI.Messages;
using MyNet.UI.Resources;
using MyNet.UI.Services;
using MyNet.UI.ViewModels.Import;
using MyNet.Utilities;
using MyNet.Utilities.Exceptions;
using MyNet.Utilities.Helpers;
using MyNet.Utilities.Messaging;

namespace MyClub.Scorer.Wpf.Services
{
    internal class StadiumPresentationService(StadiumService service,
                                              PluginsService pluginsService,
                                              IViewModelLocator viewModelLocator) : PresentationServiceBase<IStadiumViewModel, StadiumEditionViewModel, StadiumService>(service, viewModelLocator)
    {
        private readonly PluginsService _pluginsService = pluginsService;


        public async Task AddAsync(string name) => await AppBusyManager.WaitAsync(() => Service.Add(name)).ConfigureAwait(false);

        public async Task OpenAsync(IStadiumViewModel item) => await EditAsync(item).ConfigureAwait(false);

        public async Task ExportAsync(IEnumerable<IStadiumViewModel> items)
        {
            var vm = ViewModelLocator.Get<StadiumsExportViewModel>();
            var list = items.ToList();
            vm.Load(list);

            var result = await DialogManager.ShowDialogAsync(vm).ConfigureAwait(false);
            if (result.IsTrue())
            {
                var filepath = vm.Destination!;

                try
                {
                    await AppBusyManager.BackgroundAsync(async () =>
                    {
                        var items = list.Select(x => new StadiumExportDto
                        {
                            Name = x.Name,
                            Ground = x.Ground,
                            Street = x.Address?.Street,
                            PostalCode = x.Address?.PostalCode,
                            City = x.Address?.City,
                            Country = x.Address?.Country,
                            Longitude = x.Address?.Longitude,
                            Latitude = x.Address?.Latitude
                        }).ToList();
                        await ExportService.ExportAsCsvOrExcelAsync(items, vm.Columns.Where(x => x.IsSelected).Select(x => x.Item.ColumnMapping).ToList(), filepath, vm.ShowHeaderColumnTraduction).ConfigureAwait(false);

                        Messenger.Default.Send(new FileExportedMessage(filepath, ProcessHelper.OpenInExcel));
                    });
                }
                catch (DirectoryNotFoundException)
                {
                    throw new TranslatableException(MessageResources.FileXNotFoundError.FormatWith(filepath));
                }
            }
        }

        public async Task LauchImportAsync()
        {
            var vm = ViewModelLocator.Get<StadiumsImportBySourcesDialogViewModel>();
            var result = await DialogManager.ShowDialogAsync(vm).ConfigureAwait(false);

            if (result.IsFalse()) return;

            var itemsToImport = vm.List.ImportItems.Select(x => new
            {
                x.Mode,
                Item = new StadiumDto
                {
                    Name = x.Name,
                    Ground = x.Ground,
                    Address = x.GetAddress()
                }
            }).ToList();

            await AppBusyManager.WaitAsync(() => Service.Import(itemsToImport.Where(x => x.Mode == ImportMode.Add).Select(x => x.Item).ToList(), itemsToImport.Where(x => x.Mode == ImportMode.Update).Select(x => x.Item).ToList())).ConfigureAwait(false);
            await vm.ResetAsync().ConfigureAwait(false);
        }

        public async Task<Guid?> ImportAsync()
        {
            var plugin = _pluginsService.GetPlugin<IImportStadiumsPlugin>();

            if (plugin is null) return null;

            var vm = new StadiumsImportDialogViewModel(plugin, Service, x => !Service.GetSimilarStadiums(x.Name, x.City).Any());

            var result = await DialogManager.ShowDialogAsync(vm).ConfigureAwait(false);

            var selectedItem = vm.List.SelectedItem;
            if (result.IsTrue() && selectedItem is not null)
            {
                var item = Service.Save(new StadiumDto
                {
                    Name = selectedItem.Name,
                    Ground = selectedItem.Ground,
                    Address = selectedItem.GetAddress()
                });

                return item.Id;
            }

            return null;
        }

        public bool CanImport() => _pluginsService.GetPlugin<IImportStadiumsPlugin>()?.IsEnabled() ?? false;

        public bool HasImportSources() => _pluginsService.HasPlugin<IImportStadiumsSourcePlugin>();
    }
}
