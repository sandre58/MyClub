// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MyClub.Domain.Enums;
using MyClub.Teamup.Application.Dtos;
using MyClub.Teamup.Application.Services;
using MyClub.Teamup.Plugins.Contracts;
using MyClub.Teamup.Wpf.ViewModels.Edition;
using MyClub.Teamup.Wpf.ViewModels.Entities;
using MyClub.Teamup.Wpf.ViewModels.Export;
using MyClub.Teamup.Wpf.ViewModels.Import;
using MyNet.UI.Dialogs;
using MyNet.UI.Extensions;
using MyNet.UI.Locators;
using MyNet.UI.Messages;
using MyNet.UI.Resources;
using MyNet.UI.Services;
using MyNet.Utilities;
using MyNet.Utilities.Exceptions;
using MyNet.Utilities.Helpers;
using MyNet.Utilities.Messaging;
using MyNet.Wpf.Extensions;

namespace MyClub.Teamup.Wpf.Services
{
    internal class TeamPresentationService(TeamService teamService,
                                           PluginsService pluginsService,
                                           IViewModelLocator viewModelLocator)
    {
        private readonly IViewModelLocator _viewModelLocator = viewModelLocator;
        private readonly TeamService _teamService = teamService;
        private readonly PluginsService _pluginsService = pluginsService;

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
                        await ExportService.ExportAsCsvOrExcelAsync(players, vm.Columns.Where(x => x.IsSelected).Select(x => x.Item.ColumnMapping).ToList(), filepath, vm.ShowHeaderColumnTraduction).ConfigureAwait(false);

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
            var vm = _viewModelLocator.Get<TeamsImportBySourcesDialogViewModel>();

            if (vm.Sources.Count == 0) return [];

            var result = await DialogManager.ShowDialogAsync(vm).ConfigureAwait(false);

            return result.IsTrue()
                ? vm.List.ImportItems.Select(x => new EditableTeamViewModel
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
                : [];
        }

        public async Task<EditableTeamViewModel?> ImportAsync(IEnumerable<string> excludeTeamNames)
        {
            var plugin = _pluginsService.GetPlugin<IImportTeamsPlugin>();

            if (plugin is null) return null;

            var vm = new TeamsImportDialogViewModel(plugin, _teamService, x => !excludeTeamNames.Contains(x.Name, StringComparer.OrdinalIgnoreCase));

            var result = await DialogManager.ShowDialogAsync(vm).ConfigureAwait(false);

            var selectedItem = vm.List.SelectedItem;
            if (result.IsTrue() && selectedItem is not null)
            {
                var item = new EditableTeamViewModel
                {
                    Name = selectedItem.Name,
                    ShortName = selectedItem.ShortName,
                    ClubName = selectedItem.ClubName,
                    Category = selectedItem.Category,
                    AwayColor = selectedItem.AwayColor,
                    HomeColor = selectedItem.HomeColor,
                    Logo = selectedItem.Logo,
                    Country = selectedItem.Country,
                    IsMyTeam = false,
                    Stadium = selectedItem.Stadium is not null
                              ? new EditableStadiumViewModel
                              {
                                  Address = selectedItem.Stadium.GetAddress(),
                                  Ground = selectedItem.Stadium.Ground,
                                  Name = selectedItem.Stadium.Name,
                              }
                              : null
                };

                return item;
            }

            return null;
        }

        public bool CanImport() => _pluginsService.GetPlugin<IImportStadiumsPlugin>()?.IsEnabled() ?? false;

        public bool HasImportSources() => _pluginsService.HasPlugin<IImportTeamsSourcePlugin>();
    }
}
