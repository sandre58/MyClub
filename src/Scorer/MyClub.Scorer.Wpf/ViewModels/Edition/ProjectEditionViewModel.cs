// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.DataAnnotations;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using MyClub.CrossCutting.Localization;
using MyClub.Scorer.Application.Dtos;
using MyClub.Scorer.Application.Services;
using MyClub.Scorer.Wpf.Services.Providers;
using MyNet.Observable.Attributes;
using MyNet.Observable.Collections.Providers;
using MyNet.UI.ViewModels.Edition;
using MyNet.Utilities;
using MyNet.Utilities.Units;

namespace MyClub.Scorer.Wpf.ViewModels.Edition
{
    internal class ProjectEditionViewModel : EditionViewModel
    {
        private readonly ProjectInfoProvider _projectInfoProvider;
        private readonly ParametersService _parametersService;
        private readonly ProjectService _projectService;
        private readonly MatchesProvider _matchesProvider;

        public ProjectEditionViewModel(ProjectInfoProvider projectInfoProvider,
                                       ProjectService projectService,
                                       ParametersService parametersService,
                                       MatchesProvider matchesProvider,
                                       StadiumsProvider stadiumsProvider)
        {
            _projectInfoProvider = projectInfoProvider;
            _parametersService = parametersService;
            _projectService = projectService;
            _matchesProvider = matchesProvider;
            SchedulingParameters = new(new ObservableSourceProvider<IEditableStadiumViewModel>(stadiumsProvider.Items.ToObservableChangeSet().Transform(x => (IEditableStadiumViewModel)x)));
            Disposables.AddRange(
            [
                MatchRules.WhenAnyPropertyChanged().Subscribe(_ => ShowMatchRulesWarning = _matchesProvider.Count > 0 && MatchRules.IsModified()),
                MatchRules.Cards.ToObservableChangeSet().Subscribe(_ => ShowMatchRulesWarning = _matchesProvider.Count > 0 && MatchRules.IsModified())
            ]);
        }

        public EditableSchedulingParametersViewModel SchedulingParameters { get; }

        public EditableMatchFormatViewModel MatchFormat { get; } = new();

        public EditableMatchRulesViewModel MatchRules { get; } = new();

        [IsRequired]
        [Display(Name = nameof(Name), ResourceType = typeof(MyClubResources))]
        public string? Name { get; set; }

        public byte[]? Image { get; set; }

        public bool TreatNoStadiumAsWarning { get; set; }

        [IsRequired]
        [Display(Name = "PeriodForPreviousMatches", ResourceType = typeof(MyClubResources))]
        public int? PeriodForPreviousMatchesValue { get; set; }

        [IsRequired]
        [Display(Name = "PeriodForPreviousMatches", ResourceType = typeof(MyClubResources))]
        public TimeUnit PeriodForPreviousMatchesUnit { get; set; }

        public bool ShowLastMatchFallback { get; set; }

        [IsRequired]
        [Display(Name = "PeriodForNextMatches", ResourceType = typeof(MyClubResources))]
        public int? PeriodForNextMatchesValue { get; set; }

        [IsRequired]
        [Display(Name = "PeriodForNextMatches", ResourceType = typeof(MyClubResources))]
        public TimeUnit PeriodForNextMatchesUnit { get; set; }

        public bool ShowNextMatchFallback { get; set; }

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public bool CanEditMatchFormat { get; private set; }

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public bool CanEditMatchRules { get; private set; }

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public bool ShowMatchRulesWarning { get; private set; }

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public bool ApplyMatchRulesOnExistingMatches { get; set; }

        protected override void RefreshCore()
        {
            var matchFormat = _parametersService.GetMatchFormat();
            var matchRules = _parametersService.GetMatchRules();
            var schedulingParameters = _parametersService.GetSchedulingParameters();
            var preferences = _parametersService.GetPreferences();

            CanEditMatchFormat = _matchesProvider.Count == 0;
            CanEditMatchRules = true;
            MatchFormat.Load(matchFormat);
            MatchRules.Load(matchRules);
            SchedulingParameters.Load(schedulingParameters);
            TreatNoStadiumAsWarning = preferences.TreatNoStadiumAsWarning;
            (PeriodForPreviousMatchesValue, PeriodForPreviousMatchesUnit) = preferences.PeriodForPreviousMatches.Simplify();
            (PeriodForNextMatchesValue, PeriodForNextMatchesUnit) = preferences.PeriodForNextMatches.Simplify();
            ShowLastMatchFallback = preferences.ShowLastMatchFallback;
            ShowNextMatchFallback = preferences.ShowNextMatchFallback;
            Name = _projectInfoProvider.Name;
            Image = _projectInfoProvider.Image;
            ShowMatchRulesWarning = false;
            ApplyMatchRulesOnExistingMatches = false;
        }

        protected override void SaveCore()
        {
            if (CanEditMatchFormat)
                _parametersService.UpdateMatchFormat(MatchFormat.Create());

            if (CanEditMatchRules)
                _parametersService.UpdateMatchRules(MatchRules.Create(), ApplyMatchRulesOnExistingMatches);

            _parametersService.UpdateSchedulingParameters(SchedulingParameters.Create());
            _projectService.Update(new ProjectMetadataDto
            {
                Image = Image,
                Name = Name,
                Preferences = new PreferencesDto
                {
                    PeriodForNextMatches = PeriodForNextMatchesValue.GetValueOrDefault().ToTimeSpan(PeriodForNextMatchesUnit),
                    PeriodForPreviousMatches = PeriodForPreviousMatchesValue.GetValueOrDefault().ToTimeSpan(PeriodForPreviousMatchesUnit),
                    ShowLastMatchFallback = ShowLastMatchFallback,
                    ShowNextMatchFallback = ShowNextMatchFallback,
                    TreatNoStadiumAsWarning = TreatNoStadiumAsWarning
                }
            });
        }
    }
}
