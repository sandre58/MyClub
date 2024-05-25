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
using MyClub.Teamup.Domain.Enums;
using MyClub.Teamup.Domain.Factories.Extensions;
using MyClub.Teamup.Plugins.Contracts;
using MyClub.Teamup.Wpf.ViewModels.Edition;
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
    internal class PlayerPresentationService(PlayerService service,
                                             InjuryService injuryService,
                                             AbsenceService playerAbsenceService,
                                             PluginsService pluginsService,
                                             IViewModelLocator viewModelLocator)
        : PresentationServiceBase<PlayerViewModel, PlayerEditionViewModel, PlayerService>(service, viewModelLocator)
    {
        private readonly InjuryService _injuryService = injuryService;
        private readonly AbsenceService _playerAbsenceService = playerAbsenceService;
        private readonly PluginsService _pluginsService = pluginsService;

        public async Task<Guid?> AddAsync(Guid? teamId)
            => await AddAsync(x =>
            {
                if (teamId.HasValue)
                    x.TeamId = teamId;
            }).ConfigureAwait(false);

        public async Task EditMultipleAsync(IEnumerable<PlayerViewModel> players)
        {
            var vm = ViewModelLocator.Get<PlayersEditionViewModel>();

            vm.Load(players);

            _ = await DialogManager.ShowDialogAsync(vm).ConfigureAwait(false);
        }

        public async Task MoveAsync(PlayerViewModel item, TeamViewModel? team) => await MoveAsync([item], team).ConfigureAwait(false);

        public async Task MoveAsync(IEnumerable<PlayerViewModel> items, TeamViewModel? team)
        {
            var idsList = items.Select(x => x.Id).ToList();

            if (idsList.Count == 0) return;

            await AppBusyManager.WaitAsync(() =>
            {
                Service.Move(idsList, team?.Id);

                if (team is not null)
                    ToasterManager.ShowSuccess($"{nameof(MyClubResources.XPlayersHasBeenMovedInSuccess).TranslateWithCountAndOptionalFormat(idsList.Count)} {team.Name}");
                else
                    ToasterManager.ShowSuccess(nameof(MyClubResources.XPlayersHasBeenMovedOutSuccess).TranslateWithCountAndOptionalFormat(idsList.Count));

            }).ConfigureAwait(false);
        }

        public async Task<InjuryViewModel?> AddInjuryAsync(PlayerViewModel item)
        {
            var vm = ViewModelLocator.Get<InjuryEditionViewModel>();
            vm.Load(item, null);

            var result = await DialogManager.ShowDialogAsync(vm).ConfigureAwait(false);

            return result.HasValue && result.Value ? item.Injuries.FirstOrDefault(x => x.Id == vm.ItemId) : null;
        }

        public async Task EditInjuryAsync(InjuryViewModel injury)
        {
            var vm = ViewModelLocator.Get<InjuryEditionViewModel>();
            vm.Load(injury.Player, injury.Id);

            _ = await DialogManager.ShowDialogAsync(vm).ConfigureAwait(false);
        }

        public async Task RemoveInjuryAsync(InjuryViewModel injury)
        {
            if (await DialogManager.ShowQuestionAsync(MessageResources.XItemsRemovingQuestion.TranslateWithCountAndOptionalFormat(1).OrEmpty(), UiResources.Removing).ConfigureAwait(false) == MessageBoxResult.Yes)
            {
                await AppBusyManager.WaitAsync(() => _injuryService.Remove(injury.Id)).ConfigureAwait(false);
            }
        }

        public async Task RemoveInjuriesAsync(IEnumerable<InjuryViewModel> oldItems)
        {
            var idsList = oldItems.Select(x => x.Id).ToList();
            if (idsList.Count == 0) return;

            var cancel = await DialogManager.ShowQuestionAsync(nameof(MessageResources.XItemsRemovingQuestion).TranslateWithCountAndOptionalFormat(idsList.Count)!, UiResources.Removing).ConfigureAwait(false) != MessageBoxResult.Yes;

            if (!cancel)
            {
                await AppBusyManager.WaitAsync(() => _injuryService.Remove(idsList));
            }
        }

        public async Task AddAbsenceAsync(PlayerViewModel item, AbsenceType type = AbsenceType.Other, DateTime? startDate = null, DateTime? endDate = null)
            => await AddAbsenceAsync([item], type, startDate, endDate).ConfigureAwait(false);

        public async Task AddAbsenceAsync(IEnumerable<PlayerViewModel> items, AbsenceType type = AbsenceType.Other, DateTime? startDate = null, DateTime? endDate = null)
        {
            var idsList = items.Select(x => x.Id).ToList();

            if (idsList.Count == 0) return;

            if (type != AbsenceType.Other && startDate.HasValue && endDate.HasValue)
            {
                await AppBusyManager.WaitAsync(() => items.ForEach(x => _playerAbsenceService.Save(new AbsenceDto
                {
                    PlayerId = x.PlayerId,
                    Type = type,
                    Label = type.GetDefaultLabel(),
                    StartDate = startDate.Value.BeginningOfDay(),
                    EndDate = endDate.Value.EndOfDay()
                }))).ConfigureAwait(false);
            }
            else
            {
                var vm = ViewModelLocator.Get<AbsenceEditionViewModel>();
                vm.Load(items, null, type, startDate, endDate);

                _ = await DialogManager.ShowDialogAsync(vm).ConfigureAwait(false);
            }
        }

        public async Task EditAbsenceAsync(AbsenceViewModel absence)
        {
            var vm = ViewModelLocator.Get<AbsenceEditionViewModel>();
            vm.Load([absence.Player], absence.Id, absence.Type, absence.StartDate, absence.EndDate);

            _ = await DialogManager.ShowDialogAsync(vm).ConfigureAwait(false);
        }

        public async Task RemoveAbsenceAsync(AbsenceViewModel absence)
        {
            if (await DialogManager.ShowQuestionAsync(MessageResources.XItemsRemovingQuestion.TranslateWithCountAndOptionalFormat(1).OrEmpty(), UiResources.Removing).ConfigureAwait(false) == MessageBoxResult.Yes)
            {
                await AppBusyManager.WaitAsync(() => _playerAbsenceService.Remove(absence.Id)).ConfigureAwait(false);
            }
        }

        public async Task ExportAsync(IEnumerable<PlayerViewModel> items)
        {
            var vm = ViewModelLocator.Get<PlayersExportViewModel>();
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
                        var players = list.Select(x => new SquadPlayerExportDto
                        {
                            Age = x.Age,
                            Team = x.Team?.Name,
                            Category = x.Category,
                            Position = x.NaturalPosition,
                            Street = x.Address?.Street,
                            PostalCode = x.Address?.PostalCode,
                            City = x.Address?.City,
                            Email = x.Email?.Value,
                            Phone = x.Phone?.Value,
                            Laterality = x.Laterality,
                            Height = x.Height,
                            Weight = x.Weight,
                            ShoesSize = x.ShoesSize,
                            LicenseState = x.LicenseState,
                            IsMutation = x.IsMutation,
                            Number = x.Number,
                            Positions = x.Positions.Select(xPosition => new RatedPositionDto
                            {
                                Position = xPosition.Position,
                                Rating = xPosition.Rating,
                                IsNatural = xPosition.IsNatural
                            }).ToList(),
                            LastName = x.LastName,
                            FirstName = x.FirstName,
                            Birthdate = x.Birthdate,
                            FromDate = x.FromDate,
                            PlaceOfBirth = x.PlaceOfBirth,
                            Country = x.Country,
                            Photo = x.Photo,
                            Gender = x.Gender,
                            LicenseNumber = x.LicenseNumber,
                            Description = x.Description,
                            Size = x.Size,
                            Emails = x.Emails.Select(xEmail => new EmailDto
                            {
                                Label = xEmail.Label,
                                Default = xEmail.Default,
                                Value = xEmail.Value
                            }).ToList(),
                            Phones = x.Phones.Select(xPhone => new PhoneDto
                            {
                                Label = xPhone.Label,
                                Default = xPhone.Default,
                                Value = xPhone.Value
                            }).ToList()
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

        public async Task LaunchImportAsync()
        {
            var vm = ViewModelLocator.Get<PlayersImportBySourcesDialogViewModel>();

            if (vm.Sources.Count == 0) return;

            var result = await DialogManager.ShowDialogAsync(vm).ConfigureAwait(false);

            if (result.IsTrue())
            {
                await AppBusyManager.WaitAsync(() =>
                {
                    var players = vm.List.ImportItems.Select(x => new SquadPlayerDto
                    {
                        FirstName = x.FirstName,
                        LastName = x.LastName,
                        TeamId = x.Team?.Id,
                        Category = x.Category,
                        Photo = x.Photo,
                        Gender = x.Gender,
                        Number = x.Number.Value,
                        FromDate = x.FromDate,
                        LicenseState = x.LicenseState,
                        LicenseNumber = x.LicenseNumber,
                        IsMutation = x.IsMutation,
                        Description = x.Description,
                        Address = x.GetAddress(),
                        Birthdate = x.Birthdate,
                        Country = x.Country,
                        Height = x.Height.Value,
                        Laterality = x.Laterality,
                        PlaceOfBirth = x.PlaceOfBirth,
                        ShoesSize = x.ShoesSize.Value,
                        Size = x.Size,
                        Weight = x.Weight.Value,
                        Phones = x.Phones.Where(x => !string.IsNullOrEmpty(x.Value)).Select(x => new PhoneDto
                        {
                            Value = x.Value,
                            Default = x.Default,
                            Label = x.Label
                        }).ToList(),
                        Emails = x.Emails.Where(x => !string.IsNullOrEmpty(x.Value)).Select(x => new EmailDto
                        {
                            Value = x.Value,
                            Default = x.Default,
                            Label = x.Label
                        }).ToList(),
                        Positions = x.Positions.Select(x => new RatedPositionDto
                        {
                            Id = x.Id,
                            IsNatural = x.IsNatural,
                            Position = x.Position,
                            Rating = x.Rating
                        }).ToList(),
                        Injuries = x.ImportInjuries ? x.Injuries.Select(x => new InjuryDto
                        {
                            Category = x.Category,
                            Condition = x.Condition,
                            Date = x.Period.Start,
                            Description = x.Description,
                            EndDate = x.Period.End,
                            Severity = x.Severity,
                            Type = x.Type
                        }).ToList() : null
                    }).ToList();
                    Service.Import(players);

                    ToasterManager.ShowSuccess(nameof(MyClubResources.XPlayersHasBeenImportedSuccess).TranslateWithCountAndOptionalFormat(players.Count));
                }).ConfigureAwait(false);
            }
        }

        public bool HasImportSources() => _pluginsService.HasPlugin<IImportPlayersSourcePlugin>();
    }
}
