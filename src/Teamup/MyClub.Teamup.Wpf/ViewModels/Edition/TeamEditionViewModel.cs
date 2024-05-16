// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows.Media;
using DynamicData.Binding;
using MyClub.CrossCutting.Localization;
using MyClub.Domain.Enums;
using MyClub.Teamup.Application.Services;
using MyClub.Teamup.Wpf.Services;
using MyClub.Teamup.Wpf.Services.Providers;
using MyNet.Observable.Attributes;
using MyNet.UI.Commands;
using MyNet.UI.ViewModels.Edition;
using MyNet.Utilities;
using MyNet.Utilities.Geography;
using MyNet.Wpf.Extensions;

namespace MyClub.Teamup.Wpf.ViewModels.Edition
{
    internal class TeamEditionViewModel : EditionViewModel
    {
        private readonly TeamService _teamService;
        private EditableTeamViewModel? _originalTeam;
        private Guid? _originalStadiumId;

        public TeamEditionViewModel(TeamService teamService, StadiumsProvider stadiumsProvider, StadiumPresentationService stadiumPresentationService)
        {
            _teamService = teamService;
            StadiumSelection = new EditableStadiumSelectionViewModel(stadiumsProvider, stadiumPresentationService);

            ValidationRules.AddNotNull<TeamEditionViewModel, string>(x => x.Name, MyClubResources.DuplicatedTeamNameError, x => OtherTeams is null || !OtherTeams.Select(x => x.Name).Contains(x));

            SelectedTeamCommand = CommandsManager.Create<EditableTeamViewModel>(CopyTeam, x => x is not null && Mode == MyNet.UI.ViewModels.ScreenMode.Creation);

            Disposables.AddRange(
            [
                StadiumSelection.WhenAnyPropertyChanged(nameof(EditableStadiumSelectionViewModel.SelectedItem), nameof(EditableStadiumSelectionViewModel.TextSearch)).Subscribe(_ => RaisePropertyChanged(nameof(NewStadiumWillBeCreated)))
            ]);
        }

        public Guid? ItemId { get; private set; }

        [CanSetIsModified(false)]
        [CanBeValidated(false)]
        [Display(Name = nameof(MyClubResources.Club), ResourceType = typeof(MyClubResources))]
        [IsRequired]
        public string ClubName { get; set; } = string.Empty;

        [Display(Name = nameof(Name), ResourceType = typeof(MyClubResources))]
        [IsRequired]
        public string Name { get; set; } = string.Empty;

        [Display(Name = nameof(ShortName), ResourceType = typeof(MyClubResources))]
        [IsRequired]
        public string ShortName { get; set; } = string.Empty;

        [Display(Name = nameof(Category), ResourceType = typeof(MyClubResources))]
        [IsRequired]
        public Category? Category { get; set; }

        public byte[]? Logo { get; set; }

        public Color? HomeColor { get; set; }

        public Color? AwayColor { get; set; }

        public Country? Country { get; set; }

        public EditableStadiumSelectionViewModel StadiumSelection { get; }

        [CanSetIsModified(false)]
        [CanBeValidated(false)]
        public bool IsMyTeam { get; private set; }

        [CanSetIsModified(false)]
        [CanBeValidated(false)]
        public bool NewStadiumWillBeCreated => StadiumSelection.SelectedItem == null && !string.IsNullOrEmpty(StadiumSelection.TextSearch);

        [CanSetIsModified(false)]
        [CanBeValidated(false)]
        public ReadOnlyCollection<EditableTeamViewModel>? OtherTeams { get; private set; }

        public ICommand SelectedTeamCommand { get; private set; }

        public void Load(EditableTeamViewModel team, Guid? stadiumId, bool isMyTeam, IEnumerable<EditableTeamViewModel>? existingTeams = null)
        {
            Mode = MyNet.UI.ViewModels.ScreenMode.Edition;

            _originalTeam = team;
            _originalStadiumId = stadiumId;
            OtherTeams = existingTeams?.OrderBy(x => x.Name).ToList().AsReadOnly();
            IsMyTeam = isMyTeam;
        }

        public void New(IEnumerable<EditableTeamViewModel>? existingTeams = null)
        {
            Mode = MyNet.UI.ViewModels.ScreenMode.Creation;

            OtherTeams = existingTeams?.OrderBy(x => x.Name).ToList().AsReadOnly();

            var defaultValue = _teamService.NewTeam();

            _originalTeam = new EditableTeamViewModel(Guid.NewGuid())
            {
                Name = defaultValue.Name.OrEmpty(),
                ClubName = defaultValue.Club.OrThrow().Name.OrEmpty(),
                ShortName = defaultValue.ShortName.OrEmpty(),
                Logo = defaultValue.Club.OrThrow().Logo,
                AwayColor = defaultValue.AwayColor?.ToColor(),
                HomeColor = defaultValue.HomeColor?.ToColor(),
                Country = defaultValue.Club.OrThrow().Country,
                IsMyTeam = false,
                Category = defaultValue.Category,
            };
            _originalStadiumId = defaultValue.Stadium?.Id;
            IsMyTeam = false;
        }

        private void CopyTeam(EditableTeamViewModel? team)
        {
            if (team is null) return;

            Name = team.Name;
            ShortName = team.ShortName;
            ClubName = team.ClubName;
            Category = team.Category;
            Logo = team.Logo;
            AwayColor = team.AwayColor;
            HomeColor = team.HomeColor;
            Country = team.Country;
            IsMyTeam = team.IsMyTeam;
            StadiumSelection.Select(team.Stadium?.Id);
        }

        protected override void RefreshCore()
        {
            StadiumSelection.Reset();

            if (_originalTeam?.Stadium is not null)
                StadiumSelection.Add(_originalTeam.Stadium);

            ItemId = _originalTeam?.Id;
            Name = (_originalTeam?.Name).OrEmpty();
            ShortName = (_originalTeam?.ShortName).OrEmpty();
            ClubName = (_originalTeam?.ClubName).OrEmpty();
            Category = _originalTeam?.Category;
            Logo = _originalTeam?.Logo;
            AwayColor = _originalTeam?.AwayColor;
            HomeColor = _originalTeam?.HomeColor;
            Country = _originalTeam?.Country;
            IsMyTeam = _originalTeam?.IsMyTeam ?? false;
            StadiumSelection.Select(_originalStadiumId);
        }

        protected override void SaveCore() { }
    }
}
