// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using DynamicData;
using DynamicData.Binding;
using MyClub.CrossCutting.Localization;
using MyClub.Scorer.Application.Dtos;
using MyClub.Scorer.Application.Services;
using MyClub.Scorer.Domain.Enums;
using MyClub.Scorer.Wpf.Services.Deferrers;
using MyClub.Scorer.Wpf.Services.Providers;
using MyClub.Scorer.Wpf.ViewModels.Edition;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyNet.Observable.Attributes;
using MyNet.UI.Collections;
using MyNet.UI.Commands;
using MyNet.UI.Dialogs;
using MyNet.UI.Selection.Models;
using MyNet.UI.Toasting;
using MyNet.UI.Toasting.Settings;
using MyNet.UI.ViewModels.Edition;
using MyNet.Utilities;
using MyNet.Utilities.Deferring;
using MyNet.Utilities.Helpers;

namespace MyClub.Scorer.Wpf.ViewModels.BuildAssistant
{
    internal enum BuildMethod
    {
        AsSoonAsPossible,

        Automatic,

        Manual,
    }

    internal class LeagueBuildAssistantViewModel : EditionViewModel
    {
        private readonly LeagueService _leagueService;
        private readonly MatchdaysProvider _matchdaysProvider;
        private readonly MatchesProvider _matchesProvider;
        private readonly CompetitionInfoProvider _competitionInfoProvider;
        private readonly ScheduleChangedDeferrer _scheduleChangedDeferrer;
        private readonly ResultsChangedDeferrer _resultsChangedDeferrer;
        private readonly Deferrer _updateNumberOfMatchesDeferrer;
        private readonly Dictionary<BuildMethod, IBuildMethodViewModel> _methodViewModels = new()
        {
            { BuildMethod.AsSoonAsPossible, new BuildAsSoonAsPossibleViewModel() },
            { BuildMethod.Manual, new BuildManualViewModel() },
            { BuildMethod.Automatic, new BuildAutomaticViewModel() },
        };

        public LeagueBuildAssistantViewModel(LeagueService leagueService,
                                             CompetitionInfoProvider competitionInfoProvider,
                                             MatchdaysProvider matchdaysProvider,
                                             MatchesProvider matchesProvider,
                                             ScheduleChangedDeferrer scheduleChangedDeferrer,
                                             ResultsChangedDeferrer resultsChangedDeferrer)
        {
            _leagueService = leagueService;
            _competitionInfoProvider = competitionInfoProvider;
            _matchdaysProvider = matchdaysProvider;
            _matchesProvider = matchesProvider;
            _scheduleChangedDeferrer = scheduleChangedDeferrer;
            _resultsChangedDeferrer = resultsChangedDeferrer;
            _updateNumberOfMatchesDeferrer = new(() =>
            {
                ComputeNumberOfMatches();
                BuildManualViewModel.Update(NumberOfMatchdays, NumberOfMatchesByMatchday, SchedulingParameters.StartTime);
            });

            AddMatchBetweenTeamsCommand = CommandsManager.Create(() => MatchesBetweenTeams.Add(new EditableInvertTeamViewModel(MatchesBetweenTeams.Count + 1)));
            RemoveMatchBetweenTeamsCommand = CommandsManager.Create(() => MatchesBetweenTeams.RemoveAt(MatchesBetweenTeams.Count - 1), () => MatchesBetweenTeams.Count > 1);

            Disposables.AddRange([
                this.WhenPropertyChanged(x => x.Algorythm, false).Subscribe(_ => _updateNumberOfMatchesDeferrer.DeferOrExecute()),
                this.WhenPropertyChanged(x => x.NumberOfMatchesByTeam, false).Subscribe(_ => _updateNumberOfMatchesDeferrer.DeferOrExecute()),
                MatchesBetweenTeams.ToObservableChangeSet().Subscribe(_ => _updateNumberOfMatchesDeferrer.DeferOrExecute()),
                SchedulingParameters.WhenPropertyChanged(x => x.StartTime, false).Subscribe(x => _updateNumberOfMatchesDeferrer.DeferOrExecute()),
                ]);

            Reset();
        }

        [IsRequired]
        [Display(Name = nameof(Algorythm), ResourceType = typeof(MyClubResources))]
        public ChampionshipAlgorithm Algorythm { get; set; }

        [CanSetIsModified(false)]
        [CanBeValidated(false)]
        public bool CanUseSwissSystem { get; private set; }

        [IsRequired]
        [Display(Name = nameof(NumberOfMatchesByTeam), ResourceType = typeof(MyClubResources))]
        public int NumberOfMatchesByTeam { get; set; }

        [CanSetIsModified(false)]
        [CanBeValidated(false)]
        public int NumberOfMatchdays { get; private set; }

        [CanSetIsModified(false)]
        [CanBeValidated(false)]
        public int NumberOfMatchesByMatchday { get; private set; }

        [CanSetIsModified(false)]
        [CanBeValidated(false)]
        public int NumberOfTeams { get; private set; }

        [HasAnyItems]
        [Display(Name = nameof(MatchesBetweenTeams), ResourceType = typeof(MyClubResources))]
        public UiObservableCollection<EditableInvertTeamViewModel> MatchesBetweenTeams { get; } = [];

        [IsRequired]
        [Display(Name = nameof(NamePattern), ResourceType = typeof(MyClubResources))]
        public string? NamePattern { get; set; }

        [IsRequired]
        [Display(Name = nameof(ShortNamePattern), ResourceType = typeof(MyClubResources))]
        public string? ShortNamePattern { get; set; }

        [IsRequired]
        [Display(Name = nameof(BuildMethod), ResourceType = typeof(MyClubResources))]
        public BuildMethod BuildMethod { get; set; }

        public ICommand AddMatchBetweenTeamsCommand { get; }

        public ICommand RemoveMatchBetweenTeamsCommand { get; }

        [CanBeValidated(false)]
        public BuildAutomaticViewModel BuildAutomaticViewModel => (BuildAutomaticViewModel)_methodViewModels[BuildMethod.Automatic];

        [CanBeValidated(false)]
        public BuildManualViewModel BuildManualViewModel => (BuildManualViewModel)_methodViewModels[BuildMethod.Manual];

        [CanBeValidated(false)]
        public BuildAsSoonAsPossibleViewModel BuildAsSoonAsPossibleViewModel => (BuildAsSoonAsPossibleViewModel)_methodViewModels[BuildMethod.AsSoonAsPossible];

        public EditableMatchFormatViewModel MatchFormat { get; } = new();

        public EditableSchedulingParametersViewModel SchedulingParameters { get; } = new();

        private void ComputeNumberOfMatches()
        {
            NumberOfMatchesByMatchday = _leagueService.GetNumberOfMatchesByMatchday(ToBuildParameters());
            NumberOfMatchdays = _leagueService.GetNumberOfMatchays(ToBuildParameters());
        }

        protected override void RefreshCore()
        {
            using (_updateNumberOfMatchesDeferrer.Defer())
            {
                var league = _competitionInfoProvider.GetCompetition<LeagueViewModel>();

                NumberOfTeams = league.GetAvailableTeams().Count();
                MatchFormat.Load(league.MatchFormat);
                SchedulingParameters.Load(league.SchedulingParameters);
                CanUseSwissSystem = NumberOfTeams % 2 == 0;
                _methodViewModels.ForEach(x => x.Value.Refresh(league.SchedulingParameters.StartDate.ToLocalDateTime(league.SchedulingParameters.StartTime)));
                if (!CanUseSwissSystem && Algorythm == ChampionshipAlgorithm.SwissSystem)
                    Algorythm = ChampionshipAlgorithm.RoundRobin;
            }
        }

        protected override void ResetCore()
        {
            using (_updateNumberOfMatchesDeferrer.Defer())
            {
                var league = _competitionInfoProvider.GetCompetition<LeagueViewModel>();

                MatchesBetweenTeams.Set(EnumerableHelper.Range(1, 2, 1).Select(x => new EditableInvertTeamViewModel(x)));
                NumberOfMatchesByTeam = 8;
                _methodViewModels.ForEach(x => x.Value.Reset(league.SchedulingParameters.StartDate.ToLocalDateTime(league.SchedulingParameters.StartTime)));
                NamePattern = MyClubResources.MatchdayNamePattern;
                ShortNamePattern = MyClubResources.MatchdayShortNamePattern;
                RefreshCore();
            }
        }

        protected override void SaveCore()
        {
            using (_scheduleChangedDeferrer.Defer())
            using (_resultsChangedDeferrer.Defer())
            using (_matchesProvider.DeferReload())
            using (_matchdaysProvider.DeferReload())
            {
                var parameters = ToBuildParameters();
                parameters.BuildDatesParameters = _methodViewModels[BuildMethod].ProvideBuildDatesParameters(NumberOfMatchdays, NumberOfMatchesByMatchday, SchedulingParameters.StartTime.GetValueOrDefault());
                _leagueService.Build(parameters);
            }
        }

        protected override Task<bool> CanCancelAsync() => Task.FromResult(true);

        protected override Task<MessageBoxResult> SavingRequestAsync() => Task.FromResult(MessageBoxResult.No);

        public override bool ValidateProperties()
        {
            if (!base.ValidateProperties()) return false;

            if (!_methodViewModels[BuildMethod].ValidateProperties())
            {
                _methodViewModels[BuildMethod].GetErrors().ToList().ForEach(x => ToasterManager.ShowError(x, ToastClosingStrategy.AutoClose));
                return false;
            }

            return true;
        }

        private BuildParametersDto ToBuildParameters() => new()
        {
            Algorithm = Algorythm,
            MatchesBetweenTeams = MatchesBetweenTeams.Select(x => x.IsSelected).ToArray(),
            NumberOfMatchesByTeam = NumberOfMatchesByTeam,
            MatchFormat = MatchFormat.Create(),
            SchedulingParameters = SchedulingParameters.Create(),
            NamePattern = NamePattern,
            ShortNamePattern = ShortNamePattern,
        };
    }

    internal class EditableInvertTeamViewModel : SelectedWrapper<int>
    {
        public EditableInvertTeamViewModel(int index) : base(index, index % 2 == 0) => IsSelectable = index > 1;
    }
}
