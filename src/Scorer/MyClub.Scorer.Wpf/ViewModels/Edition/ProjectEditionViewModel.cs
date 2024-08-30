// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using DynamicData;
using DynamicData.Binding;
using MyClub.CrossCutting.Localization;
using MyClub.Scorer.Application.Dtos;
using MyClub.Scorer.Application.Services;
using MyClub.Scorer.Domain.Enums;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.ProjectAggregate;
using MyClub.Scorer.Wpf.Services;
using MyClub.Scorer.Wpf.Services.Providers;
using MyClub.Scorer.Wpf.ViewModels.BuildAssistant;
using MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces;
using MyNet.Observable;
using MyNet.Observable.Attributes;
using MyNet.Observable.Collections.Providers;
using MyNet.UI.Toasting;
using MyNet.UI.Toasting.Settings;
using MyNet.UI.ViewModels;
using MyNet.UI.ViewModels.Edition;
using MyNet.Utilities;
using MyNet.Wpf.Extensions;

namespace MyClub.Scorer.Wpf.ViewModels.Edition
{
    internal class ProjectEditionViewModel : EditionViewModel
    {
        private readonly ProjectInfoProvider _projectInfoProvider;
        private readonly ProjectService _projectService;

        public ProjectEditionViewModel(ProjectInfoProvider projectInfoProvider,
                                       ProjectService projectService,
                                       PluginsService pluginService,
                                       AddressService addressService)
        {
            _projectInfoProvider = projectInfoProvider;
            _projectService = projectService;

            StadiumsViewModel = new(pluginService, addressService);
            TeamsViewModel = new(pluginService, new ObservableSourceProvider<EditableStadiumViewModel>(StadiumsViewModel.Items));
            RankingRulesViewModel = new(new ObservableSourceProvider<ITeamViewModel>(TeamsViewModel.Items.ToObservableChangeSet().Transform(x => (ITeamViewModel)x)));
            SchedulingParametersViewModel = new(new ObservableSourceProvider<IStadiumViewModel>(StadiumsViewModel.Items.ToObservableChangeSet().Transform(x => (IStadiumViewModel)x)));
            LeagueBuildAssistantParametersViewModel = new(new ObservableSourceProvider<IStadiumViewModel>(StadiumsViewModel.Items.ToObservableChangeSet().Transform(x => (IStadiumViewModel)x)));
            AddSubWorkspaces(
            [
                GeneralViewModel,
                StadiumsViewModel,
                TeamsViewModel,
                MatchFormatViewModel,
                SchedulingParametersViewModel,
                LeagueBuildAssistantParametersViewModel,
                RankingRulesViewModel
            ]);

            NavigationService.Navigating += OnSubWorkspaceNavigating;

            Disposables.AddRange(
            [
                TeamsViewModel.Items.ToObservableChangeSet().Subscribe(_ => CanBuildCompetition = TeamsViewModel.Items.Count > 1),
                GeneralViewModel.WhenPropertyChanged(x => x.Type).Subscribe(_ =>
                {
                    if(Mode != ScreenMode.Creation) return;

                    RankingRulesViewModel.IsEnabled =  GeneralViewModel.Type == CompetitionType.League;
                    LeagueBuildAssistantParametersViewModel.IsEnabled = GeneralViewModel.Type == CompetitionType.League && BuildCompetition && CanBuildCompetition;

                    if(!MatchFormatViewModel.IsModified()) {
                        switch (GeneralViewModel.Type) {
                            case CompetitionType.League:
                                MatchFormatViewModel.Load(MatchFormat.Default);
                                break;
                            default:
                                MatchFormatViewModel.Load(MatchFormat.NoDraw);
                                break;
                        }
                    }
                }),
                this.WhenPropertyChanged(x => x.CanBuildCompetition).Subscribe(_ =>
                {
                    if(Mode == ScreenMode.Creation && !CanBuildCompetition)
                        BuildCompetition = false;
                }),
                this.WhenAnyPropertyChanged(nameof(BuildCompetition), nameof(CanBuildCompetition)).Subscribe(_ =>
                {
                    if(Mode != ScreenMode.Creation) return;

                    LeagueBuildAssistantParametersViewModel.IsEnabled = GeneralViewModel.Type == CompetitionType.League && BuildCompetition && CanBuildCompetition;
                    MatchFormatViewModel.IsEnabled = !BuildCompetition || !CanBuildCompetition;
                    SchedulingParametersViewModel.IsEnabled = !BuildCompetition || !CanBuildCompetition;
                })
            ]);
        }

        [IsRequired]
        [Display(Name = nameof(Name), ResourceType = typeof(MyClubResources))]
        public string? Name { get; set; }

        public byte[]? Image { get; set; }

        public ProjectEditionGeneralViewModel GeneralViewModel { get; } = new();

        public ProjectEditionTeamsViewModel TeamsViewModel { get; }

        public ProjectEditionStadiumsViewModel StadiumsViewModel { get; }

        public EditableMatchFormatViewModel MatchFormatViewModel { get; } = new();

        public EditableRankingRulesViewModel RankingRulesViewModel { get; }

        public EditableSchedulingParametersViewModel SchedulingParametersViewModel { get; }

        public LeagueBuildAssistantParametersViewModel LeagueBuildAssistantParametersViewModel { get; }

        [CanSetIsModified(false)]
        [CanBeValidated(false)]
        public bool BuildCompetition { get; set; }

        [CanSetIsModified(false)]
        [CanBeValidated(false)]
        public bool CanBuildCompetition { get; private set; }

        [CanSetIsModified(false)]
        [CanBeValidated(false)]
        public bool StretchSize { get; private set; } = false;

        private void OnSubWorkspaceNavigating(object? sender, MyNet.UI.Navigation.NavigatingEventArgs e)
        {
            if (AllWorkspaces.IndexOf(e.NewPage) > AllWorkspaces.IndexOf(e.OldPage) && e.OldPage is IEditableObject editableObject && !editableObject.ValidateProperties())
            {
                editableObject.GetErrors().ForEach(x => ToasterManager.ShowError(x, ToastClosingStrategy.AutoClose));
                e.Cancel = true;
                return;
            }

            StretchSize = e.NewPage == LeagueBuildAssistantParametersViewModel;

            if (e.NewPage == RankingRulesViewModel)
                RankingRulesViewModel.ShowShootouts = BuildCompetition && CanBuildCompetition ? LeagueBuildAssistantParametersViewModel.MatchFormat.ShootoutsIsEnabled : MatchFormatViewModel.ShootoutsIsEnabled;

            if (e.NewPage == LeagueBuildAssistantParametersViewModel)
                LeagueBuildAssistantParametersViewModel.Refresh(TeamsViewModel.Items.Count);
        }

        public void New()
        {
            Mode = ScreenMode.Creation;

            StadiumsViewModel.IsEnabled = true;
            TeamsViewModel.IsEnabled = true;
            MatchFormatViewModel.IsEnabled = true;
            SchedulingParametersViewModel.IsEnabled = true;
            LeagueBuildAssistantParametersViewModel.IsEnabled = true;
            RankingRulesViewModel.IsEnabled = true;
            GeneralViewModel.CanEditType = true;
            Reset();
        }

        public void Edit()
        {
            GoToTab(0);
            Mode = ScreenMode.Edition;
            Reset();
            Name = _projectInfoProvider.Name;
            Image = _projectInfoProvider.Image;
            GeneralViewModel.Type = _projectInfoProvider.Type;
            GeneralViewModel.CanEditType = false;
            GeneralViewModel.TreatNoStadiumAsWarning = _projectInfoProvider.TreatNoStadiumAsWarning;

            StadiumsViewModel.IsEnabled = false;
            TeamsViewModel.IsEnabled = false;
            MatchFormatViewModel.IsEnabled = false;
            SchedulingParametersViewModel.IsEnabled = false;
            LeagueBuildAssistantParametersViewModel.IsEnabled = false;
            RankingRulesViewModel.IsEnabled = false;
        }

        protected override void ResetCore()
        {
            base.ResetCore();
            Name = MyClubResources.League;
            Image = null;
            BuildCompetition = false;
            CanBuildCompetition = false;
            LeagueBuildAssistantParametersViewModel.IsEnabled = false;
            GoToTab(0);
        }

        public override void GoToPreviousTab()
        {
            if (SelectedWorkspace is null || Mode != ScreenMode.Creation) return;

            var activeSubWorkaces = AllWorkspaces.Where(x => x.IsEnabled).ToList();
            var currentIndex = activeSubWorkaces.IndexOf(SelectedWorkspace);
            var previousSubworkspace = activeSubWorkaces.GetByIndex(currentIndex - 1);

            if (previousSubworkspace is not null)
                GoToTab(previousSubworkspace);
        }

        public override void GoToNextTab()
        {
            if (SelectedWorkspace is null || Mode != ScreenMode.Creation) return;

            var activeSubWorkaces = AllWorkspaces.Where(x => x.IsEnabled).ToList();
            var currentIndex = activeSubWorkaces.IndexOf(SelectedWorkspace);
            var nextSubworkspace = activeSubWorkaces.GetByIndex(currentIndex + 1);

            if (nextSubworkspace is not null)
                GoToTab(nextSubworkspace);
        }

        protected override bool CanGoToNextTab()
        {
            if (SelectedWorkspace is null || Mode != ScreenMode.Creation) return false;

            var activeSubWorkaces = AllWorkspaces.Where(x => x.IsEnabled).ToList();
            var currentIndex = activeSubWorkaces.IndexOf(SelectedWorkspace);

            return activeSubWorkaces.GetByIndex(currentIndex + 1) is not null;
        }

        protected override bool CanGoToPreviousTab()
        {
            if (SelectedWorkspace is null || Mode != ScreenMode.Creation) return false;

            var activeSubWorkaces = AllWorkspaces.Where(x => x.IsEnabled).ToList();
            var currentIndex = activeSubWorkaces.IndexOf(SelectedWorkspace);

            return activeSubWorkaces.GetByIndex(currentIndex - 1) is not null;
        }

        protected override bool CanSave() => SelectedWorkspace == AllWorkspaces.Last(x => x.IsEnabled);


        protected override void SaveCore()
        {
            if (Mode != ScreenMode.Edition) return;

            _projectService.Update(new ProjectMetadataDto
            {
                Image = Image,
                Name = Name,
                TreatNoStadiumAsWarning = GeneralViewModel.TreatNoStadiumAsWarning,
            });
        }

        protected override void Cleanup()
        {
            base.Cleanup();
            NavigationService.Navigating -= OnSubWorkspaceNavigating;
        }

        internal ProjectMetadataDto ToMetadata() => GeneralViewModel.Type switch
        {
            CompetitionType.League => new LeagueMetadataDto
            {
                Image = Image,
                Name = Name,
                TreatNoStadiumAsWarning = GeneralViewModel.TreatNoStadiumAsWarning,
                Stadiums = ToStadiumDtos(),
                Teams = ToTeamDtos(),
                BuildParameters = ToLeagueBuildParameters()
            },
            CompetitionType.Cup => new CupMetadataDto
            {
                Image = Image,
                Name = Name,
                TreatNoStadiumAsWarning = GeneralViewModel.TreatNoStadiumAsWarning,
                Stadiums = ToStadiumDtos(),
                Teams = ToTeamDtos()
            },
            CompetitionType.Tournament => new TournamentMetadataDto
            {
                Image = Image,
                Name = Name,
                TreatNoStadiumAsWarning = GeneralViewModel.TreatNoStadiumAsWarning,
                Stadiums = ToStadiumDtos(),
                Teams = ToTeamDtos()
            },
            _ => throw new InvalidOperationException("No competition type provided")
        };

        private List<TeamDto> ToTeamDtos() => TeamsViewModel.Items.Select(x => new TeamDto
        {
            Name = x.Name,
            Id = x.Id,
            AwayColor = x.AwayColor?.ToHex(),
            Country = x.Country,
            HomeColor = x.HomeColor?.ToHex(),
            Logo = x.Logo,
            ShortName = x.ShortName,
            Stadium = x.Stadium is not null ? new StadiumDto { Id = x.Stadium.Id } : null
        }).ToList();

        private List<StadiumDto> ToStadiumDtos() => StadiumsViewModel.Items.Select(x => new StadiumDto
        {
            Id = x.Id,
            Address = x.Address,
            Ground = x.Ground,
            Name = x.Name,
        }).ToList();

        private BuildParametersDto ToLeagueBuildParameters()
            => BuildCompetition && CanBuildCompetition
                ? LeagueBuildAssistantParametersViewModel.ToBuildParameters()
                : new()
                {
                    MatchFormat = MatchFormatViewModel.Create(),
                    SchedulingParameters = SchedulingParametersViewModel.Create(),
                    AutomaticEndDate = false,
                    AutomaticStartDate = false,
                };
    }
}
