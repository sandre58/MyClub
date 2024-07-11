// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MyClub.Scorer.Application.Dtos;
using MyClub.Scorer.Application.Services;
using MyClub.Scorer.Plugins.Contracts;
using MyClub.Scorer.Wpf.Services.Deferrers;
using MyClub.Scorer.Wpf.ViewModels.Edition;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyClub.Scorer.Wpf.ViewModels.Export;
using MyClub.Scorer.Wpf.ViewModels.Import;
using MyClub.Scorer.Wpf.ViewModels.MessageBox;
using MyNet.Humanizer;
using MyNet.UI.Dialogs;
using MyNet.UI.Dialogs.Settings;
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
using MyNet.Wpf.Extensions;

namespace MyClub.Scorer.Wpf.Services
{
    internal class TeamPresentationService(TeamService service,
                                           PlayerService playerService,
                                           PluginsService pluginsService,
                                           TeamsChangedDeferrer teamsChangedDeferrer,
                                           IViewModelLocator viewModelLocator) : PresentationServiceBase<TeamViewModel, TeamEditionViewModel, TeamService>(service, viewModelLocator)
    {
        private readonly PlayerService _playerService = playerService;
        private readonly PluginsService _pluginsService = pluginsService;
        private readonly TeamsChangedDeferrer _teamsChangedDeferrer = teamsChangedDeferrer;

        public async Task OpenAsync(TeamViewModel item) => await EditAsync(item).ConfigureAwait(false);

        public async Task<PlayerViewModel?> AddPlayerAsync(TeamViewModel item)
        {
            var vm = ViewModelLocator.Get<PlayerEditionViewModel>();
            vm.New(item);

            var result = await DialogManager.ShowDialogAsync(vm).ConfigureAwait(false);

            return result.HasValue && result.Value ? item.Players.FirstOrDefault(x => x.Id == vm.ItemId) : null;
        }

        public override async Task RemoveAsync(IEnumerable<TeamViewModel> oldItems)
        {
            var idsList = oldItems.Select(x => x.Id).ToList();

            if (idsList.Count == 0) return;

            var messageBox = new RemoveTeamsMessageBoxViewModel(nameof(MessageResources.XItemsRemovingQuestion).TranslateWithCountAndOptionalFormat(idsList.Count)!, UiResources.Removing, MessageSeverity.Question, MessageBoxResultOption.YesNo, MessageBoxResult.Yes);
            var cancel = await DialogManager.ShowMessageBoxAsync(messageBox).ConfigureAwait(false) != MessageBoxResult.Yes;

            if (!cancel)
            {
                await AppBusyManager.WaitAsync(() =>
                {
                    using (_teamsChangedDeferrer.Defer())
                        Service.Remove(idsList, messageBox.RemoveStadium);
                });
            }
        }

        public async Task RemovePlayerAsync(PlayerViewModel player)
        {
            if (await DialogManager.ShowQuestionAsync(MessageResources.XItemsRemovingQuestion.TranslateWithCountAndOptionalFormat(1).OrEmpty(), UiResources.Removing).ConfigureAwait(false) == MessageBoxResult.Yes)
            {
                await AppBusyManager.WaitAsync(() => _playerService.Remove(player.Id)).ConfigureAwait(false);
            }
        }

        public async Task RemovePlayersAsync(IEnumerable<PlayerViewModel> oldItems)
        {
            var idsList = oldItems.Select(x => x.Id).ToList();
            if (idsList.Count == 0) return;

            var cancel = await DialogManager.ShowQuestionAsync(nameof(MessageResources.XItemsRemovingQuestion).TranslateWithCountAndOptionalFormat(idsList.Count)!, UiResources.Removing).ConfigureAwait(false) != MessageBoxResult.Yes;

            if (!cancel)
            {
                await AppBusyManager.WaitAsync(() => _playerService.Remove(idsList));
            }
        }

        public async Task ExportAsync(IEnumerable<TeamViewModel> items)
        {
            var vm = ViewModelLocator.Get<TeamsExportViewModel>();
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
                        var players = list.Select(x => new TeamDto
                        {
                            Name = x.Name,
                            ShortName = x.ShortName,
                            AwayColor = x.AwayColor?.ToHex(),
                            HomeColor = x.HomeColor?.ToHex(),
                            Country = x.Country,
                            Logo = x.Logo,
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

        public async Task LauchImportAsync()
        {
            var vm = ViewModelLocator.Get<TeamsImportBySourcesDialogViewModel>();
            var result = await DialogManager.ShowDialogAsync(vm).ConfigureAwait(false);

            if (result.IsFalse()) return;

            var itemsToImport = vm.List.ImportItems.Select(x => new
            {
                x.Mode,
                Item = new TeamDto
                {
                    Name = x.Name,
                    ShortName = x.ShortName,
                    AwayColor = x.AwayColor?.ToHex(),
                    Country = x.Country,
                    HomeColor = x.HomeColor?.ToHex(),
                    Logo = x.Logo,
                    Stadium = x.Stadium is not null
                                  ? new StadiumDto
                                  {
                                      Address = x.Stadium.GetAddress(),
                                      Ground = x.Stadium.Ground,
                                      Name = x.Stadium.Name,
                                  }
                                  : null
                }
            }).ToList();

            await AppBusyManager.WaitAsync(() =>
            {
                using (_teamsChangedDeferrer.Defer())
                    Service.Import(itemsToImport.Where(x => x.Mode == ImportMode.Add).Select(x => x.Item).ToList(), itemsToImport.Where(x => x.Mode == ImportMode.Update).Select(x => x.Item).ToList());
            });
        }

        public bool HasImportSources() => _pluginsService.HasPlugin<IImportTeamsSourcePlugin>();
    }
}
