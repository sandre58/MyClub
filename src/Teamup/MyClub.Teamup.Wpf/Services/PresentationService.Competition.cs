// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MyClub.CrossCutting.Localization;
using MyClub.Teamup.Application.Dtos;
using MyClub.Teamup.Application.Services;
using MyClub.Teamup.Plugins.Contracts;
using MyClub.Teamup.Wpf.ViewModels.Entities;
using MyClub.Teamup.Wpf.ViewModels.Export;
using MyClub.Teamup.Wpf.ViewModels.Import;
using MyNet.Humanizer;
using MyNet.UI.Dialogs;
using MyNet.UI.Extensions;
using MyNet.UI.Locators;
using MyNet.UI.Messages;
using MyNet.UI.Resources;
using MyNet.UI.Services;
using MyNet.UI.Toasting;
using MyNet.Utilities;
using MyNet.Utilities.Exceptions;
using MyNet.Utilities.Helpers;
using MyNet.Utilities.Messaging;

namespace MyClub.Teamup.Wpf.Services
{
    internal class CompetitionPresentationService(CompetitionService competitionService,
                                                  PluginsService pluginsService,
                                                  IViewModelLocator viewModelLocator)
    {
        private readonly IViewModelLocator _viewModelLocator = viewModelLocator;
        private readonly CompetitionService _competitionService = competitionService;
        private readonly PluginsService _pluginsService = pluginsService;

        public async Task ExportAsync(IEnumerable<CompetitionViewModel> items)
        {
            var vm = _viewModelLocator.Get<CompetitionsExportViewModel>();
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
                        try
                        {
                            var competitions = list.Select(x => new CompetitionExportDto
                            {
                                MatchTime = x.Rules.MatchTime,
                                Name = x.Name,
                                ShortName = x.ShortName,
                                Category = x.Category,
                                Type = x.Type.ToString(),
                                ExtraTime = x.Rules.MatchFormat.ExtraTime,
                                RegulationTime = x.Rules.MatchFormat.RegulationTime,
                                HasExtraTime = x.Rules.MatchFormat.ExtraTimeIsEnabled,
                                HasShootouts = x.Rules.MatchFormat.ShootoutIsEnabled,
                                NumberOfShootouts = x.Rules.MatchFormat.NumberOfPenaltyShootouts,
                                ByGamesDrawn = (x as LeagueViewModel)?.RankingRules.PointsByGamesDrawn,
                                ByGamesWon = (x as LeagueViewModel)?.RankingRules.PointsByGamesWon,
                                ByGamesLost = (x as LeagueViewModel)?.RankingRules.PointsByGamesLost,
                                Labels = (x as LeagueViewModel)?.RankingRules.Labels,
                                RankingSortingColumns = (x as LeagueViewModel)?.RankingRules.SortingColumns
                            }).ToList();
                            await ExportService.ExportAsCsvOrExcelAsync(competitions, vm.Columns.Where(x => x.IsSelected).Select(x => x.Item.ColumnMapping).ToList(), filepath, vm.ShowHeaderColumnTraduction).ConfigureAwait(false);

                            Messenger.Default.Send(new FileExportedMessage(filepath, ProcessHelper.OpenInExcel));
                        }
                        catch (Exception e)
                        {
                            ToasterManager.ShowError(e.Message, MyNet.UI.Toasting.Settings.ToastClosingStrategy.CloseButton, onClick: async x =>
                            {
                                ToasterManager.Hide(x);
                                await ExportAsync(items).ConfigureAwait(false);
                            });
                        }
                    });
                }
                catch (DirectoryNotFoundException)
                {
                    throw new TranslatableException(MessageResources.FileXNotFoundError.FormatWith(filepath));
                }
            }
        }

        public async Task LaunchImportAsync()
        {
            var vm = _viewModelLocator.Get<CompetitionsImportBySourcesDialogViewModel>();

            if (vm.Sources.Count == 0) return;

            var result = await DialogManager.ShowDialogAsync(vm).ConfigureAwait(false);

            if (result.IsTrue())
            {
                await AppBusyManager.WaitAsync(() =>
                {
                    var defaultValues = _competitionService.NewCup();
                    var competitions = vm.List.ImportItems.Select(x =>
                    {
                        CompetitionDto competition = x.Type switch
                        {
                            CompetitionType.League => new LeagueDto(),
                            CompetitionType.Cup => new CupDto(),
                            _ => new FriendlyDto(),
                        };
                        competition.Rules = x.CompetitionRules;
                        competition.EndDate = x.EndDate ?? defaultValues.EndDate;
                        competition.Logo = x.Logo;
                        competition.Name = x.Name;
                        competition.Category = x.Category;
                        competition.ShortName = x.ShortName;
                        competition.StartDate = x.StartDate ?? defaultValues.StartDate;

                        return competition;
                    }).ToList();
                    _competitionService.Import(competitions);

                    ToasterManager.ShowSuccess(nameof(MyClubResources.XPlayersHasBeenImportedSuccess).TranslateAndFormatWithCount(competitions.Count));
                }).ConfigureAwait(false);
            }
        }

        public bool HasImportSources() => _pluginsService.HasPlugin<IImportCompetitionsSourcePlugin>();
    }
}
