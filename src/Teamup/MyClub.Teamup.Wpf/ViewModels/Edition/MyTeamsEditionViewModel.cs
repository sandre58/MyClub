// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using DynamicData;
using DynamicData.Binding;
using MyNet.UI.Commands;
using MyNet.UI.ViewModels;
using MyNet.UI.ViewModels.Edition;
using MyNet.Wpf.Extensions;
using MyNet.Utilities;
using MyNet.Utilities.Deferring;
using MyNet.Observable.Attributes;
using MyClub.Teamup.Application.Dtos;
using MyClub.Teamup.Application.Services;
using MyClub.Teamup.Domain.StadiumAggregate;
using MyClub.CrossCutting.Localization;
using MyClub.Teamup.Wpf.DragAndDrop;
using MyClub.Teamup.Wpf.Services;
using MyClub.Teamup.Wpf.Services.Providers;

namespace MyClub.Teamup.Wpf.ViewModels.Edition
{

    internal class MyTeamsEditionViewModel : EditionViewModel
    {
        private readonly TeamService _teamService;
        private readonly ProjectInfoProvider _projectInfoProvider;
        private readonly Deferrer _validateOrderDeferrer;

        [HasUniqueItems]
        [HasAnyItems]
        [Display(Name = nameof(Teams), ResourceType = typeof(MyClubResources))]
        public ObservableCollection<EditableTeamViewModel> Teams { get; } = [];

        [CanSetIsModified(false)]
        [CanBeValidated(false)]
        public bool HasInvalidOrder { get; set; }

        public OrderDropHandler<EditableTeamViewModel> DropHandler { get; } = new();

        public EditableTeamViewModel? SelectedItem { get; set; }

        public EditableStadiumSelectionViewModel StadiumSelection { get; }

        public ICommand AddCommand { get; set; }

        public ICommand RemoveCommand { get; set; }

        public ICommand RemoveSelectedItemCommand { get; set; }

        public ICommand ConsolidateOrderCommand { get; set; }

        public MyTeamsEditionViewModel(TeamService teamService, ProjectInfoProvider projectInfoProvider, StadiumsProvider stadiumsProvider, StadiumPresentationService stadiumPresentationService)
        {
            _teamService = teamService;
            _projectInfoProvider = projectInfoProvider;
            Mode = ScreenMode.Edition;
            _validateOrderDeferrer = new Deferrer(ValidateOrder);
            StadiumSelection = new EditableStadiumSelectionViewModel(stadiumsProvider, stadiumPresentationService);

            AddCommand = CommandsManager.Create(AddTeam);
            RemoveCommand = CommandsManager.CreateNotNull<EditableTeamViewModel>(x => Teams.Remove(x), x => x is not null);
            RemoveSelectedItemCommand = CommandsManager.Create(() => Teams.Remove(SelectedItem!), () => SelectedItem is not null);
            ConsolidateOrderCommand = CommandsManager.Create(ConsolidateOrder, () => HasInvalidOrder);

            Disposables.AddRange(
            [
                Teams.ToObservableChangeSet()
                     .SubscribeMany(x => x.WhenPropertyChanged(x => x.Order).Subscribe(_ => _validateOrderDeferrer.DeferOrExecute()))
                     .Subscribe(_ => _validateOrderDeferrer.DeferOrExecute()),
                StadiumSelection.WhenPropertyChanged(x => x.SelectedItem).Subscribe(x => SelectedItem.IfNotNull(y => y.Stadium = x.Value))
            ]);
        }

        private void AddTeam()
        {
            var defaultValues = _teamService.NewMyTeam(Teams.Select(x => (x.Category.OrThrow(), x.Name, x.Order)));

            var teamEdition = new EditableTeamViewModel()
            {
                Name = defaultValues.Name.OrEmpty(),
                ShortName = defaultValues.ShortName.OrEmpty(),
                ClubName = _projectInfoProvider.GetCurrentProject().OrThrow().Club.Name,
                Category = defaultValues.Category,
                AwayColor = defaultValues.AwayColor?.ToColor(),
                HomeColor = defaultValues.HomeColor?.ToColor(),
                Order = defaultValues.Order,
                Stadium = defaultValues.Stadium is StadiumDto stadium
                            ? new EditableStadiumViewModel(stadium.Id)
                            {
                                Name = stadium.Name,
                                Address = stadium.Address,
                                Ground = stadium.Ground,
                            }
                            : null
            };

            Teams.Add(teamEdition);
        }

        protected override void RefreshCore()
        {
            StadiumSelection.Reset();
            using (_validateOrderDeferrer.Defer())
                Teams.Set(_teamService.GetMyTeams().OrderBy(x => x.Order).Select(x =>
                {
                    var teamEdition = new EditableTeamViewModel(x.Id)
                    {
                        Category = x.Category,
                        ShortName = x.ShortName,
                        Name = x.Name,
                        ClubName = x.Club.Name,
                        AwayColor = x.AwayColor.OverrideValue?.ToColor(),
                        HomeColor = x.HomeColor.OverrideValue?.ToColor(),
                        Order = x.Order,
                        Stadium = x.Stadium.OverrideValue is Stadium stadium
                            ? new EditableStadiumViewModel(stadium.Id)
                            {
                                Name = stadium.Name,
                                Address = stadium.Address,
                                Ground = stadium.Ground,
                            }
                            : null
                    };

                    return teamEdition;
                }));
        }

        protected override void SaveCore()
        {
            if (IsModified())
                _teamService.SaveMyTeams(Teams.Select(x => new TeamDto()
                {
                    Id = x.Id,
                    Name = x.Name,
                    ShortName = x.ShortName,
                    AwayColor = x.AwayColor?.ToHex(),
                    HomeColor = x.HomeColor?.ToHex(),
                    Category = x.Category,
                    Order = x.Order,
                    Stadium = x.Stadium is EditableStadiumViewModel stadium
                            ? new StadiumDto
                            {
                                Id = stadium.Id,
                                Name = stadium.Name,
                                Address = stadium.Address,
                                Ground = stadium.Ground,
                            }
                            : null
                }).ToList(), true);
        }

        private void ValidateOrder() => HasInvalidOrder = Teams.Select(x => x.Order).Distinct().Count() != Teams.Count;

        private void ConsolidateOrder()
        {
            using (_validateOrderDeferrer.Defer())
                Teams.Select((x, index) => (Index: index, Value: x)).ForEach(x => x.Value.Order = x.Index + 1);
        }
    }

}
