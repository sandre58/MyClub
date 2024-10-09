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
using MyClub.Scorer.Wpf.Services;
using MyClub.Scorer.Wpf.ViewModels.BuildAssistant;
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
    internal class ProjectCreationViewModel : EditionViewModel
    {
        public ProjectCreationViewModel(PluginsService pluginService, AddressService addressService)
        {
            Mode = ScreenMode.Creation;
            Stadiums = new(pluginService, addressService);
            Teams = new(pluginService, new ObservableSourceProvider<EditableStadiumViewModel>(Stadiums.Items));
            RankingRules = new(new ObservableSourceProvider<IEditableTeamViewModel>(Teams.Items.ToObservableChangeSet().Transform(x => (IEditableTeamViewModel)x)));
            SchedulingParameters = new(new ObservableSourceProvider<IEditableStadiumViewModel>(Stadiums.Items.ToObservableChangeSet().Transform(x => (IEditableStadiumViewModel)x)));
            LeagueBuildAssistantParameters = new(new ObservableSourceProvider<IEditableStadiumViewModel>(Stadiums.Items.ToObservableChangeSet().Transform(x => (IEditableStadiumViewModel)x)));
            AddSubWorkspaces(
            [
                General,
                Preferences,
                MatchRules,
                Stadiums,
                Teams,
                MatchFormat,
                SchedulingParameters,
                LeagueBuildAssistantParameters,
                RankingRules
            ]);

            NavigationService.Navigating += OnSubWorkspaceNavigating;

            Disposables.AddRange(
            [
                Teams.Items.ToObservableChangeSet().Subscribe(_ => CanBuildCompetition = Teams.Items.Count > 1),
                General.WhenPropertyChanged(x => x.Type).Subscribe(_ =>
                {
                    RankingRules.IsEnabled =  General.Type == CompetitionType.League;
                    LeagueBuildAssistantParameters.IsEnabled = General.Type == CompetitionType.League && BuildCompetition && CanBuildCompetition;

                    if(!MatchFormat.IsModified()) {
                        switch (General.Type) {
                            case CompetitionType.League:
                                MatchFormat.Load(Domain.MatchAggregate.MatchFormat.Default);
                                break;
                            default:
                                MatchFormat.Load(Domain.MatchAggregate.MatchFormat.NoDraw);
                                break;
                        }
                    }
                }),
                this.WhenPropertyChanged(x => x.CanBuildCompetition).Subscribe(_ =>
                {
                    if(!CanBuildCompetition)
                        BuildCompetition = false;
                }),
                this.WhenAnyPropertyChanged(nameof(BuildCompetition), nameof(CanBuildCompetition)).Subscribe(_ =>
                {
                    LeagueBuildAssistantParameters.IsEnabled = General.Type == CompetitionType.League && BuildCompetition && CanBuildCompetition;
                    MatchFormat.IsEnabled = !BuildCompetition || !CanBuildCompetition;
                    SchedulingParameters.IsEnabled = !BuildCompetition || !CanBuildCompetition;
                })
            ]);

            Reset();
        }

        [IsRequired]
        [Display(Name = nameof(Name), ResourceType = typeof(MyClubResources))]
        public string? Name { get; set; }

        public byte[]? Image { get; set; }

        public ProjectCreationGeneralViewModel General { get; } = new();

        public ProjectCreationTeamsViewModel Teams { get; }

        public ProjectCreationStadiumsViewModel Stadiums { get; }

        public EditableMatchFormatViewModel MatchFormat { get; } = new();

        public EditableMatchRulesViewModel MatchRules { get; } = new();

        public EditableRankingRulesViewModel RankingRules { get; }

        public EditableSchedulingParametersViewModel SchedulingParameters { get; }

        public LeagueBuildAssistantParametersViewModel LeagueBuildAssistantParameters { get; }

        public ProjectCreationPreferencesViewModel Preferences { get; } = new();

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

            StretchSize = e.NewPage == LeagueBuildAssistantParameters;

            if (e.NewPage == RankingRules)
                RankingRules.ShowShootouts = BuildCompetition && CanBuildCompetition ? LeagueBuildAssistantParameters.MatchFormat.ShootoutsIsEnabled : MatchFormat.ShootoutsIsEnabled;

            if (e.NewPage == LeagueBuildAssistantParameters)
                LeagueBuildAssistantParameters.Refresh(Teams.Items.Count);
        }

        protected override void ResetCore()
        {
            base.ResetCore();
            Name = MyClubResources.League;
            Image = null;
            BuildCompetition = false;
            CanBuildCompetition = false;
            LeagueBuildAssistantParameters.IsEnabled = false;
            GoToTab(0);
        }

        public override void GoToPreviousTab()
        {
            if (SelectedWorkspace is null) return;

            var activeSubWorkaces = AllWorkspaces.Where(x => x.IsEnabled).ToList();
            var currentIndex = activeSubWorkaces.IndexOf(SelectedWorkspace);
            var previousSubworkspace = activeSubWorkaces.GetByIndex(currentIndex - 1);

            if (previousSubworkspace is not null)
                GoToTab(previousSubworkspace);
        }

        public override void GoToNextTab()
        {
            if (SelectedWorkspace is null) return;

            var activeSubWorkaces = AllWorkspaces.Where(x => x.IsEnabled).ToList();
            var currentIndex = activeSubWorkaces.IndexOf(SelectedWorkspace);
            var nextSubworkspace = activeSubWorkaces.GetByIndex(currentIndex + 1);

            if (nextSubworkspace is not null)
                GoToTab(nextSubworkspace);
        }

        protected override bool CanGoToNextTab()
        {
            if (SelectedWorkspace is null) return false;

            var activeSubWorkaces = AllWorkspaces.Where(x => x.IsEnabled).ToList();
            var currentIndex = activeSubWorkaces.IndexOf(SelectedWorkspace);

            return activeSubWorkaces.GetByIndex(currentIndex + 1) is not null;
        }

        protected override bool CanGoToPreviousTab()
        {
            if (SelectedWorkspace is null) return false;

            var activeSubWorkaces = AllWorkspaces.Where(x => x.IsEnabled).ToList();
            var currentIndex = activeSubWorkaces.IndexOf(SelectedWorkspace);

            return activeSubWorkaces.GetByIndex(currentIndex - 1) is not null;
        }

        protected override bool CanSave() => SelectedWorkspace == AllWorkspaces.Last(x => x.IsEnabled);


        protected override void SaveCore()
        {
        }

        protected override void Cleanup()
        {
            base.Cleanup();
            NavigationService.Navigating -= OnSubWorkspaceNavigating;
        }

        internal ProjectMetadataDto ToMetadata() => General.Type switch
        {
            CompetitionType.League => new LeagueMetadataDto
            {
                Image = Image,
                Name = Name,
                Preferences = ToPreferencesDto(),
                Stadiums = ToStadiumDtos(),
                Teams = ToTeamDtos(),
                MatchRules = MatchRules.Create(),
                BuildParameters = ToLeagueBuildParameters()
            },
            CompetitionType.Cup => new CupMetadataDto
            {
                Image = Image,
                Name = Name,
                Preferences = ToPreferencesDto(),
                Stadiums = ToStadiumDtos(),
                MatchRules = MatchRules.Create(),
                Teams = ToTeamDtos()
            },
            CompetitionType.Tournament => new TournamentMetadataDto
            {
                Image = Image,
                Name = Name,
                Preferences = ToPreferencesDto(),
                Stadiums = ToStadiumDtos(),
                Teams = ToTeamDtos(),
                MatchRules = MatchRules.Create(),
            },
            _ => throw new InvalidOperationException("No competition type provided")
        };

        private PreferencesDto ToPreferencesDto() => new()
        {
            TreatNoStadiumAsWarning = Preferences.TreatNoStadiumAsWarning,
            PeriodForNextMatches = Preferences.PeriodForNextMatchesValue.GetValueOrDefault().ToTimeSpan(General.PeriodForNextMatchesUnit),
            PeriodForPreviousMatches = Preferences.PeriodForPreviousMatchesValue.GetValueOrDefault().ToTimeSpan(General.PeriodForPreviousMatchesUnit),
            ShowNextMatchFallback = Preferences.ShowNextMatchFallback,
            ShowLastMatchFallback = Preferences.ShowLastMatchFallback,
        };

        private List<TeamDto> ToTeamDtos() => Teams.Items.Select(x => new TeamDto
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

        private List<StadiumDto> ToStadiumDtos() => Stadiums.Items.Select(x => new StadiumDto
        {
            Id = x.Id,
            Address = x.Address,
            Ground = x.Ground,
            Name = x.Name,
        }).ToList();

        private BuildParametersDto ToLeagueBuildParameters()
            => BuildCompetition && CanBuildCompetition
                ? LeagueBuildAssistantParameters.ToBuildParameters()
                : new()
                {
                    MatchFormat = MatchFormat.Create(),
                    SchedulingParameters = SchedulingParameters.Create(),
                    AutomaticEndDate = false,
                    AutomaticStartDate = false,
                };
    }
}
