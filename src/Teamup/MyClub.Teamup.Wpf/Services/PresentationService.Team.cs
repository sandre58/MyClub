// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MyNet.UI.Dialogs;
using MyNet.UI.Locators;
using MyNet.UI.Extensions;
using MyNet.UI.Messages;
using MyNet.UI.Services;
using MyNet.UI.Selection;
using MyNet.Wpf.Extensions;
using MyNet.Utilities;
using MyNet.Utilities.Exceptions;
using MyNet.Utilities.Helpers;
using MyNet.Utilities.Messaging;
using MyNet.UI.Resources;
using MyClub.Teamup.Application.Dtos;
using MyClub.Teamup.Application.Services;
using MyClub.DatabaseContext.Application.Services;
using MyClub.Domain.Enums;
using MyClub.Teamup.Wpf.Services.Providers;
using MyClub.Teamup.Wpf.ViewModels.Edition;
using MyClub.Teamup.Wpf.ViewModels.Entities;
using MyClub.Teamup.Wpf.ViewModels.Export;
using MyClub.Teamup.Wpf.ViewModels.Import;
using MyClub.Teamup.Wpf.ViewModels.Selection;

namespace MyClub.Teamup.Wpf.Services
{
    internal class TeamPresentationService(TeamService teamService, DatabaseService databaseService, IViewModelLocator viewModelLocator)
    {
        private readonly IViewModelLocator _viewModelLocator = viewModelLocator;
        private readonly TeamService _teamService = teamService;
        private readonly DatabaseService _databaseService = databaseService;

        public async Task<EditableTeamViewModel?> CreateAsync(IEnumerable<EditableTeamViewModel>? existingTeams = null)
        {
            var vm = _viewModelLocator.Get<TeamEditionViewModel>();
            vm.New(existingTeams);

            var result = await DialogManager.ShowDialogAsync(vm).ConfigureAwait(false);

            if (result.IsTrue())
            {
                var item = new EditableTeamViewModel(vm.ItemId)
                {
                    ClubName = vm.ClubName,
                    Name = vm.Name,
                    ShortName = vm.ShortName,
                    Category = vm.Category,
                    AwayColor = vm.AwayColor,
                    HomeColor = vm.HomeColor,
                    Logo = vm.Logo,
                    Country = vm.Country,
                    IsMyTeam = vm.IsMyTeam,
                    Stadium = vm.StadiumSelection.SelectedItem ?? (!string.IsNullOrEmpty(vm.StadiumSelection.TextSearch) ? new EditableStadiumViewModel()
                    {
                        Name = vm.StadiumSelection.TextSearch
                    } : null)
                };

                return item;
            }

            return null;
        }

        public async Task<bool?> UpdateAsync(EditableTeamViewModel oldItem, IEnumerable<EditableTeamViewModel>? existingTeams = null)
        {
            var vm = _viewModelLocator.Get<TeamEditionViewModel>();
            vm.Load(oldItem, oldItem.Stadium?.Id, oldItem.IsMyTeam, existingTeams);

            var result = await DialogManager.ShowDialogAsync(vm).ConfigureAwait(false);

            if (result.IsTrue())
            {
                oldItem.ClubName = vm.ClubName;
                oldItem.Name = vm.Name;
                oldItem.ShortName = vm.ShortName;
                oldItem.Category = vm.Category;
                oldItem.AwayColor = vm.AwayColor;
                oldItem.HomeColor = vm.HomeColor;
                oldItem.Logo = vm.Logo;
                oldItem.Country = vm.Country;
                oldItem.IsMyTeam = vm.IsMyTeam;
                oldItem.Stadium = vm.StadiumSelection.SelectedItem ?? (!string.IsNullOrEmpty(vm.StadiumSelection.TextSearch) ? new EditableStadiumViewModel(Guid.NewGuid())
                {
                    Name = vm.StadiumSelection.TextSearch
                } : null);
            }

            return result;
        }

        public async Task EditAsync(TeamViewModel item)
        {
            var vm = _viewModelLocator.Get<TeamEditionViewModel>();
            vm.Load(new EditableTeamViewModel(item.Id)
            {
                ClubName = item.ClubName,
                Name = item.Name,
                ShortName = item.ShortName,
                Category = item.Category,
                AwayColor = item.AwayColor,
                HomeColor = item.HomeColor,
                Logo = item.Logo,
                Country = item.Country,
                IsMyTeam = item.IsMyTeam,
            }, item.Stadium?.Id, item.IsMyTeam);

            var result = await DialogManager.ShowDialogAsync(vm).ConfigureAwait(false);

            if (result.IsTrue())
            {
                _teamService.Save(new TeamDto
                {
                    Id = vm.ItemId,
                    Name = vm.Name,
                    ShortName = vm.ShortName,
                    Category = vm.Category,
                    AwayColor = vm.AwayColor?.ToHex(),
                    HomeColor = vm.HomeColor?.ToHex(),
                    Stadium = vm.StadiumSelection.SelectedItem is not null || !string.IsNullOrEmpty(vm.StadiumSelection.TextSearch)
                                ? new StadiumDto
                                {
                                    Id = vm.StadiumSelection.SelectedItem?.Id ?? Guid.NewGuid(),
                                    Ground = vm.StadiumSelection.SelectedItem?.Ground ?? Ground.Grass,
                                    Address = vm.StadiumSelection.SelectedItem?.Address,
                                    Name = vm.StadiumSelection.SelectedItem?.Name ?? vm.StadiumSelection.TextSearch
                                }
                                : null,
                    Club = !vm.IsMyTeam ? new ClubDto
                    {
                        Name = vm.ClubName,
                        Logo = vm.Logo,
                        Country = vm.Country,
                        AwayColor = vm.AwayColor?.ToHex(),
                        HomeColor = vm.HomeColor?.ToHex(),
                        Stadium = vm.StadiumSelection.SelectedItem is not null || !string.IsNullOrEmpty(vm.StadiumSelection.TextSearch)
                                ? new StadiumDto
                                {
                                    Id = vm.StadiumSelection.SelectedItem?.Id ?? Guid.NewGuid(),
                                    Ground = vm.StadiumSelection.SelectedItem?.Ground ?? Ground.Grass,
                                    Address = vm.StadiumSelection.SelectedItem?.Address,
                                    Name = vm.StadiumSelection.SelectedItem?.Name ?? vm.StadiumSelection.TextSearch
                                }
                                : null
                    } : null
                });
            }
        }

        public async Task ExportAsync(IEnumerable<EditableTeamViewModel> items)
        {
            var vm = _viewModelLocator.Get<TeamsExportViewModel>();
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
                        var players = list.Select(x => new TeamExportDto
                        {
                            Category = x.Category,
                            Club = x.ClubName,
                            Name = x.Name,
                            ShortName = x.ShortName,
                            Logo = x.Logo,
                            Country = x.Country,
                            AwayColor = x.AwayColor?.ToHex(),
                            HomeColor = x.HomeColor?.ToHex(),
                            Stadium = x.Stadium is not null
                                ? new StadiumDto
                                {
                                    Ground = x.Stadium.Ground,
                                    Name = x.Stadium.Name,
                                    Address = x.Stadium.Address
                                }
                                : null
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

        public async Task<IEnumerable<EditableTeamViewModel>> LaunchImportAsync()
        {
            var vm = _viewModelLocator.Get<TeamsImportViewModel>();
            var result = await DialogManager.ShowDialogAsync(vm).ConfigureAwait(false);

            return result.IsTrue()
                ? vm.Items.Where(x => x.Import).Select(x => new EditableTeamViewModel
                {
                    ClubName = x.ClubName,
                    Name = x.Name,
                    ShortName = x.ShortName,
                    Category = x.Category,
                    AwayColor = x.AwayColor,
                    Country = x.Country,
                    HomeColor = x.HomeColor,
                    Logo = x.Logo,
                    Stadium = x.Stadium is not null
                              ? new EditableStadiumViewModel
                              {
                                  Address = x.Stadium.GetAddress(),
                                  Ground = x.Stadium.Ground,
                                  Name = x.Stadium.Name,
                              }
                              : null
                }).ToList()
                : Array.Empty<EditableTeamViewModel>();
        }

        public async Task<EditableTeamViewModel?> ImportFromDatabaseAsync(IEnumerable<string> excludeTeamNames)
        {
            var vm = new TeamsSelectionViewModel(new TeamsDatabaseProvider(_databaseService, _teamService, x => !excludeTeamNames.Contains(x.Name, StringComparer.OrdinalIgnoreCase)), SelectionMode.Single);

            var result = await DialogManager.ShowDialogAsync(vm).ConfigureAwait(false);

            if (result.IsTrue() && vm.SelectedItem is not null)
            {
                var item = new EditableTeamViewModel
                {
                    Name = vm.SelectedItem.Name,
                    ShortName = vm.SelectedItem.ShortName,
                    ClubName = vm.SelectedItem.ClubName,
                    Category = vm.SelectedItem.Category,
                    AwayColor = vm.SelectedItem.AwayColor,
                    HomeColor = vm.SelectedItem.HomeColor,
                    Logo = vm.SelectedItem.Logo,
                    Country = vm.SelectedItem.Country,
                    IsMyTeam = false,
                    Stadium = vm.SelectedItem.Stadium is not null
                              ? new EditableStadiumViewModel
                              {
                                  Address = vm.SelectedItem.Stadium.GetAddress(),
                                  Ground = vm.SelectedItem.Stadium.Ground,
                                  Name = vm.SelectedItem.Stadium.Name,
                              }
                              : null
                };

                return item;
            }

            return null;
        }

        public bool CanImportFromDatabase() => _databaseService.CanConnect();
    }
}
