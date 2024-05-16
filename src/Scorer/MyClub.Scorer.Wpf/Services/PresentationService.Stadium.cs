// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MyClub.DatabaseContext.Application.Services;
using MyClub.Scorer.Application.Dtos;
using MyClub.Scorer.Application.Services;
using MyClub.Scorer.Wpf.ViewModels.Edition;
using MyClub.Scorer.Wpf.ViewModels.Entities;
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
    internal class StadiumPresentationService(StadiumService service, DatabaseService databaseService, IViewModelLocator viewModelLocator) : PresentationServiceBase<StadiumViewModel, StadiumEditionViewModel, StadiumService>(service, viewModelLocator)
    {
        private readonly DatabaseService _databaseService = databaseService;

        public async Task OpenAsync(StadiumViewModel item) => await EditAsync(item).ConfigureAwait(false);

        public async Task ExportAsync(IEnumerable<StadiumViewModel> items)
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
                        var players = list.Select(x => new StadiumExportDto
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
                        await ExportService.ExportAsCsvOrExcelAsync(players, vm.Columns.Where(x => x.IsSelected).Select(x => x.Item).ToList(), filepath, vm.ShowHeaderColumnTraduction).ConfigureAwait(false);

                        Messenger.Default.Send(new FileExportedMessage(filepath, ProcessHelper.OpenInExcel));
                    });
                }
                catch (DirectoryNotFoundException)
                {
                    throw new TranslatableException(MessageResources.FileXNotFoundError.FormatWith(filepath));
                }
            }
        }

        public async Task ImportAsync()
        {
            var vm = ViewModelLocator.Get<StadiumsImportViewModel>();
            var result = await DialogManager.ShowDialogAsync(vm).ConfigureAwait(false);

            if (result.IsFalse()) return;

            var itemsToImport = vm.Items.Where(x => x.Import).Select(x => new
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
        }

        public async Task<Guid?> ImportFromDatabaseAsync()
        {
            var vm = new StadiumsDatabaseImportViewModel(_databaseService, Service, x => !Service.GetSimilarStadiums(x.Name, x.City).Any());

            var result = await DialogManager.ShowDialogAsync(vm).ConfigureAwait(false);

            if (result.IsTrue() && vm.SelectedItem is not null)
            {
                var item = Service.Save(new StadiumDto
                {
                    Name = vm.SelectedItem.Name,
                    Ground = vm.SelectedItem.Ground,
                    Address = vm.SelectedItem.GetAddress()
                });

                return item.Id;
            }

            return null;
        }
    }
}
