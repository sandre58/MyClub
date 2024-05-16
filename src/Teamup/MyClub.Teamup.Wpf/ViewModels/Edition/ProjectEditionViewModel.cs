// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using DynamicData.Binding;
using MyClub.CrossCutting.Localization;
using MyClub.Domain.Enums;
using MyClub.Teamup.Application.Dtos;
using MyClub.Teamup.Application.Services;
using MyClub.Teamup.Domain.Factories;
using MyClub.Teamup.Domain.SeasonAggregate;
using MyClub.Teamup.Wpf.Services;
using MyClub.Teamup.Wpf.Services.Providers;
using MyNet.Observable.Attributes;
using MyNet.UI.Resources;
using MyNet.UI.Theming;
using MyNet.UI.ViewModels;
using MyNet.UI.ViewModels.Edition;
using MyNet.Utilities;
using MyNet.Utilities.Geography;
using MyNet.Wpf.Extensions;

namespace MyClub.Teamup.Wpf.ViewModels.Edition
{
    internal class ProjectEditionViewModel : EditionViewModel
    {
        private readonly ProjectService _projectService;
        private readonly ProjectInfoProvider _projectInfoProvider;

        [IsRequired]
        [Display(Name = nameof(Name), ResourceType = typeof(MyClubResources))]
        public string? Name { get; set; }

        public byte[]? Image { get; set; }

        [IsRequired]
        [Display(Name = nameof(Category), ResourceType = typeof(MyClubResources))]
        public Category? Category { get; set; }

        [IsRequired]
        [Display(Name = nameof(StartDate), ResourceType = typeof(MyClubResources))]
        public DateTime? StartDate { get; set; }

        [IsRequired]
        [Display(Name = nameof(EndDate), ResourceType = typeof(MyClubResources))]
        public DateTime? EndDate { get; set; }

        [IsRequired]
        [Display(Name = nameof(Color), ResourceType = typeof(MyClubResources))]
        public Color? Color { get; set; }

        [IsRequired]
        [Display(Name = nameof(Name), ResourceType = typeof(MyClubResources))]
        public string? ClubName { get; set; }

        public byte[]? Logo { get; set; }

        [IsRequired]
        [Display(Name = nameof(Country), ResourceType = typeof(MyClubResources))]
        public Country? Country { get; set; }

        public EditableStadiumSelectionViewModel StadiumSelection { get; }

        [IsRequired]
        [Display(Name = nameof(HomeColor), ResourceType = typeof(MyClubResources))]
        public Color? HomeColor { get; set; }

        [IsRequired]
        [Display(Name = nameof(AwayColor), ResourceType = typeof(MyClubResources))]
        public Color? AwayColor { get; set; }

        [IsRequired]
        [Display(Name = "StartTime", ResourceType = typeof(MyClubResources))]
        public TimeSpan TrainingStartTime { get; set; }

        [IsRequired]
        [Display(Name = "Duration", ResourceType = typeof(MyClubResources))]
        public TimeSpan TrainingDuration { get; set; }

        public Guid? MainTeamId { get; set; }

        [CanSetIsModified(false)]
        [CanBeValidated(false)]
        public bool NewStadiumWillBeCreated => StadiumSelection.SelectedItem == null && !string.IsNullOrEmpty(StadiumSelection.TextSearch);

        public ProjectEditionViewModel(ProjectInfoProvider projectInfoProvider, ProjectService projectService, StadiumsProvider stadiumsProvider, StadiumPresentationService stadiumPresentationService)
        {
            _projectService = projectService;
            _projectInfoProvider = projectInfoProvider;
            Mode = ScreenMode.Edition;
            StadiumSelection = new EditableStadiumSelectionViewModel(stadiumsProvider, stadiumPresentationService);

            ValidationRules.Add<ProjectEditionViewModel, DateTime?>(x => x.StartDate, MessageResources.FieldStartDateMustBeLowerOrEqualsThanEndDateError, x => !x.HasValue || !EndDate.HasValue || x.Value.Date <= EndDate.Value.Date);
            ValidationRules.Add<ProjectEditionViewModel, DateTime?>(x => x.EndDate, MessageResources.FieldEndDateMustBeUpperOrEqualsThanStartDateError, x => !StartDate.HasValue || !x.HasValue || StartDate.Value.Date <= x.Value.Date);

            Disposables.AddRange(
            [
                StadiumSelection.WhenAnyPropertyChanged(nameof(EditableStadiumSelectionViewModel.SelectedItem), nameof(EditableStadiumSelectionViewModel.TextSearch)).Subscribe(_ => RaisePropertyChanged(nameof(NewStadiumWillBeCreated)))
            ]);
        }

        protected override string CreateTitle() => Mode == ScreenMode.Edition ? MyClubResources.EditProject : MyClubResources.NewProject;

        protected override void RefreshCore()
        {
            StadiumSelection.Reset();

            if (Mode == ScreenMode.Edition)
            {
                var project = _projectInfoProvider.GetCurrentProject().OrThrow();

                Name = project.Name;
                Image = project.Image;
                Color = project.Color.ToColor();
                Category = project.Category;
                StartDate = project.Season.Period.Start.Date;
                EndDate = project.Season.Period.End.Date;
                Country = project.Club.Country;
                MainTeamId = project.MainTeam?.Id;
                HomeColor = project.Club.HomeColor.ToColor() ?? default;
                AwayColor = project.Club.AwayColor.ToColor() ?? default;
                StadiumSelection.Select(project.Club.Stadium?.Id);
                ClubName = project.Club.Name;
                Logo = project.Club.Logo;

                TrainingStartTime = project.Preferences.TrainingStartTime;
                TrainingDuration = project.Preferences.TrainingDuration;
            }
            else
            {
                var season = Season.Current;
                Category = Category.Adult;
                Name = ProjectFactory.GetDefaultName(season, Category);
                Image = null;
                Color = ThemeManager.CurrentTheme?.PrimaryColor.ToColor() ?? default;
                HomeColor = ThemeManager.CurrentTheme?.PrimaryColor.ToColor() ?? default;
                AwayColor = ThemeManager.CurrentTheme?.AccentColor.ToColor() ?? default;
                StartDate = season.Period.Start.Date;
                EndDate = season.Period.End.Date;
                Country = Country.France;
                ClubName = MyClubResources.DefaultClubName;
                Logo = null;
                StadiumSelection.SelectedItem = null;

                TrainingStartTime = new TimeSpan(17, 30, 0);
                TrainingDuration = new TimeSpan(1, 30, 0);
            }
        }

        protected override void SaveCore()
        {
            if (Mode != ScreenMode.Edition) return;

            _projectService.Update(new ProjectMetadataDto
            {
                Name = Name,
                Image = Image,
                Category = Category,
                Color = Color?.ToHex() ?? string.Empty,
                MainTeamId = MainTeamId,
                Club = new ClubDto
                {
                    Name = ClubName,
                    HomeColor = HomeColor?.ToHex() ?? string.Empty,
                    AwayColor = AwayColor?.ToHex() ?? string.Empty,
                    Logo = Logo,
                    Country = Country,
                    Stadium = StadiumSelection.SelectedItem is not null ? new StadiumDto
                    {
                        Id = StadiumSelection.SelectedItem.Id,
                        Name = StadiumSelection.SelectedItem.Name,
                        Ground = StadiumSelection.SelectedItem.Ground,
                        Address = StadiumSelection.SelectedItem?.Address,
                    } : null,
                },
                Season = new SeasonDto
                {
                    StartDate = StartDate ?? DateTime.Today.BeginningOfYear(),
                    EndDate = EndDate ?? DateTime.Today.EndOfYear(),
                },
                TrainingStartTime = TrainingStartTime,
                TrainingDuration = TrainingDuration
            });
        }
    }
}
