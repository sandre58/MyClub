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
using MyClub.Scorer.Domain.Scheduling;
using MyClub.Scorer.Wpf.Services.Deferrers;
using MyClub.Scorer.Wpf.Services.Providers;
using MyClub.Scorer.Wpf.ViewModels.Edition;
using MyNet.Observable.Attributes;
using MyNet.UI.Collections;
using MyNet.UI.Commands;
using MyNet.UI.Dialogs;
using MyNet.UI.Resources;
using MyNet.UI.Selection.Models;
using MyNet.UI.Toasting;
using MyNet.UI.Toasting.Settings;
using MyNet.UI.ViewModels.Edition;
using MyNet.Utilities;
using MyNet.Utilities.Deferring;
using MyNet.Utilities.Helpers;
using MyNet.Utilities.Units;

namespace MyClub.Scorer.Wpf.ViewModels.BuildAssistant
{
    internal enum DatesSchedulingMethod
    {
        AsSoonAsPossible,

        Automatic,

        Manual,
    }

    internal enum VenuesSchedulingMethod
    {
        None,

        UseHomeVenue,

        AsSoonAsPossible,

        Automatic,
    }

    internal class LeagueBuildAssistantViewModel : EditionViewModel
    {
        private readonly LeagueService _leagueService;
        private readonly MatchdaysProvider _matchdaysProvider;
        private readonly MatchesProvider _matchesProvider;
        private readonly TeamsProvider _teamsProvider;
        private readonly ScheduleChangedDeferrer _scheduleChangedDeferrer;
        private readonly ResultsChangedDeferrer _resultsChangedDeferrer;
        private readonly Deferrer _updateNumberOfMatchesDeferrer;
        private readonly Dictionary<DatesSchedulingMethod, IDatesSchedulingMethodViewModel> _datesSchedulingMethodViewModels = new()
        {
            { DatesSchedulingMethod.AsSoonAsPossible, new DatesSchedulingAsSoonAsPossibleViewModel() },
            { DatesSchedulingMethod.Manual, new DatesSchedulingManualViewModel() },
            { DatesSchedulingMethod.Automatic, new DatesSchedulingAutomaticViewModel() },
        };

        public LeagueBuildAssistantViewModel(LeagueService leagueService,
                                             CompetitionInfoProvider competitionInfoProvider,
                                             MatchdaysProvider matchdaysProvider,
                                             MatchesProvider matchesProvider,
                                             StadiumsProvider stadiumsProvider,
                                             TeamsProvider teamsProvider,
                                             ScheduleChangedDeferrer scheduleChangedDeferrer,
                                             ResultsChangedDeferrer resultsChangedDeferrer)
        {
            VenueRules = new(stadiumsProvider.Items);

            _leagueService = leagueService;
            _matchdaysProvider = matchdaysProvider;
            _matchesProvider = matchesProvider;
            _teamsProvider = teamsProvider;
            _scheduleChangedDeferrer = scheduleChangedDeferrer;
            _resultsChangedDeferrer = resultsChangedDeferrer;
            _updateNumberOfMatchesDeferrer = new(() =>
            {
                ComputeNumberOfMatches();
                ManualDatesSchedulingViewModel.Update(NumberOfMatchdays, NumberOfMatchesByMatchday, StartTime);
            });

            AddMatchBetweenTeamsCommand = CommandsManager.Create(() => MatchesBetweenTeams.Add(new EditableInvertTeamViewModel(MatchesBetweenTeams.Count + 1)));
            RemoveMatchBetweenTeamsCommand = CommandsManager.Create(() => MatchesBetweenTeams.RemoveAt(MatchesBetweenTeams.Count - 1), () => MatchesBetweenTeams.Count > 1);

            Disposables.AddRange([
                this.WhenPropertyChanged(x => x.Algorythm, false).Subscribe(_ => _updateNumberOfMatchesDeferrer.DeferOrExecute()),
                this.WhenPropertyChanged(x => x.NumberOfMatchesByTeam, false).Subscribe(_ => _updateNumberOfMatchesDeferrer.DeferOrExecute()),
                this.WhenPropertyChanged(x => x.StartTime, false).Subscribe(x =>
                {
                    _updateNumberOfMatchesDeferrer.DeferOrExecute();
                    AsSoonAsPossibleDatesSchedulingViewModel.StartTime = StartTime;
                }),
                this.WhenPropertyChanged(x => x.StartDate, false).Subscribe(x =>
                {
                    AutomaticDatesSchedulingViewModel.StartDate = StartDate;
                    AsSoonAsPossibleDatesSchedulingViewModel.StartDate = StartDate;
                }),
                this.WhenPropertyChanged(x => x.DatesSchedulingMethod, false).Subscribe(x =>
                {
                    if(DatesSchedulingMethod != DatesSchedulingMethod.AsSoonAsPossible && VenuesSchedulingMethod == VenuesSchedulingMethod.AsSoonAsPossible)
                        VenuesSchedulingMethod = VenuesSchedulingMethod.UseHomeVenue;
                }),
                MatchesBetweenTeams.ToObservableChangeSet().Subscribe(_ => _updateNumberOfMatchesDeferrer.DeferOrExecute()),
                ]);

            ValidationRules.Add<LeagueBuildAssistantViewModel, DateTime?>(x => x.EndDate, MessageResources.FieldEndDateMustBeUpperOrEqualsThanStartDateError, x => !x.HasValue || !StartDate.HasValue || x.Value > StartDate);

            Reset();

            competitionInfoProvider.WhenCompetitionChanged(_ => Reset());
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
        [ValidateProperty(nameof(EndDate))]
        [Display(Name = nameof(StartDate), ResourceType = typeof(MyClubResources))]
        public DateTime? StartDate { get; set; }

        [IsRequired]
        [Display(Name = nameof(EndDate), ResourceType = typeof(MyClubResources))]
        public DateTime? EndDate { get; set; }

        [CanSetIsModified(false)]
        [CanBeValidated(false)]
        public bool AutomaticStartDate { get; set; }

        [CanSetIsModified(false)]
        [CanBeValidated(false)]
        public bool AutomaticEndDate { get; set; }

        [IsRequired]
        [Display(Name = nameof(StartTime), ResourceType = typeof(MyClubResources))]
        public TimeSpan? StartTime { get; set; }

        [IsRequired]
        [Display(Name = "RotationTime", ResourceType = typeof(MyClubResources))]
        public int? RotationTimeValue { get; set; }

        [IsRequired]
        [Display(Name = "RotationTime", ResourceType = typeof(MyClubResources))]
        public TimeUnit RotationTimeUnit { get; set; }

        [IsRequired]
        [Display(Name = "RestTime", ResourceType = typeof(MyClubResources))]
        public int? RestTimeValue { get; set; }

        [IsRequired]
        [Display(Name = "RestTime", ResourceType = typeof(MyClubResources))]
        public TimeUnit RestTimeUnit { get; set; }

        [IsRequired]
        [Display(Name = "Name", ResourceType = typeof(MyClubResources))]
        public string? NamePattern { get; set; }

        [IsRequired]
        [Display(Name = "ShortName", ResourceType = typeof(MyClubResources))]
        public string? ShortNamePattern { get; set; }

        [IsRequired]
        public DatesSchedulingMethod DatesSchedulingMethod { get; set; }

        [IsRequired]
        public VenuesSchedulingMethod VenuesSchedulingMethod { get; set; }

        public bool ScheduleVenuesBeforeDates { get; set; }

        public ICommand AddMatchBetweenTeamsCommand { get; }

        public ICommand RemoveMatchBetweenTeamsCommand { get; }

        [CanBeValidated(false)]
        public DatesSchedulingAutomaticViewModel AutomaticDatesSchedulingViewModel => (DatesSchedulingAutomaticViewModel)_datesSchedulingMethodViewModels[DatesSchedulingMethod.Automatic];

        [CanBeValidated(false)]
        public DatesSchedulingManualViewModel ManualDatesSchedulingViewModel => (DatesSchedulingManualViewModel)_datesSchedulingMethodViewModels[DatesSchedulingMethod.Manual];

        [CanBeValidated(false)]
        public DatesSchedulingAsSoonAsPossibleViewModel AsSoonAsPossibleDatesSchedulingViewModel => (DatesSchedulingAsSoonAsPossibleViewModel)_datesSchedulingMethodViewModels[DatesSchedulingMethod.AsSoonAsPossible];

        [CanBeValidated(false)]
        public EditableVenueSchedulingRulesViewModel VenueRules { get; }

        public EditableMatchFormatViewModel MatchFormat { get; } = new();

        private void ComputeNumberOfMatches()
        {
            NumberOfMatchesByMatchday = _leagueService.GetNumberOfMatchesByMatchday(ToBuildParameters());
            NumberOfMatchdays = _leagueService.GetNumberOfMatchays(ToBuildParameters());
        }

        private void EnsureIsValid()
        {
            using (_updateNumberOfMatchesDeferrer.Defer())
            {
                var numberOfTeams = _teamsProvider.Count;
                NumberOfTeams = numberOfTeams;
                CanUseSwissSystem = numberOfTeams % 2 == 0;
                if (!CanUseSwissSystem && Algorythm == ChampionshipAlgorithm.SwissSystem)
                    Algorythm = ChampionshipAlgorithm.RoundRobin;
            }
        }

        protected override void RefreshCore() => EnsureIsValid();

        protected override void ResetCore()
        {
            using (_updateNumberOfMatchesDeferrer.Defer())
            {
                AutomaticStartDate = true;
                AutomaticEndDate = true;
                MatchFormat.Load(_leagueService.GetMatchFormat());
                LoadSchedulingParameters(_leagueService.GetSchedulingParameters());
                MatchesBetweenTeams.Set(EnumerableHelper.Range(1, 2, 1).Select(x => new EditableInvertTeamViewModel(x)));
                NumberOfMatchesByTeam = 8;
                NamePattern = MyClubResources.MatchdayNamePattern;
                ShortNamePattern = MyClubResources.MatchdayShortNamePattern;

                EnsureIsValid();
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
                parameters.BuildDatesParameters = _datesSchedulingMethodViewModels[DatesSchedulingMethod].ProvideBuildDatesParameters(NumberOfMatchdays, NumberOfMatchesByMatchday, StartTime.GetValueOrDefault());
                var schedulingParameters = _leagueService.Build(parameters);

                if (AutomaticStartDate)
                    StartDate = schedulingParameters.StartDate;

                if (AutomaticEndDate)
                    EndDate = schedulingParameters.EndDate;
            }
        }

        protected override Task<bool> CanCancelAsync() => Task.FromResult(true);

        protected override Task<MessageBoxResult> SavingRequestAsync() => Task.FromResult(MessageBoxResult.No);

        public override bool ValidateProperties()
        {
            if (!base.ValidateProperties()) return false;

            if (!_datesSchedulingMethodViewModels[DatesSchedulingMethod].ValidateProperties())
            {
                _datesSchedulingMethodViewModels[DatesSchedulingMethod].GetErrors().ToList().ForEach(x => ToasterManager.ShowError(x, ToastClosingStrategy.AutoClose));
                return false;
            }

            if (VenuesSchedulingMethod == VenuesSchedulingMethod.Automatic && !VenueRules.ValidateProperties())
            {
                VenueRules.GetErrors().ToList().ForEach(x => ToasterManager.ShowError(x, ToastClosingStrategy.AutoClose));
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
            NamePattern = NamePattern,
            ShortNamePattern = ShortNamePattern,
            ScheduleVenues = VenuesSchedulingMethod is not VenuesSchedulingMethod.None,
            AsSoonAsPossibleVenues = VenuesSchedulingMethod is VenuesSchedulingMethod.AsSoonAsPossible,
            ScheduleVenuesBeforeDates = DatesSchedulingMethod is DatesSchedulingMethod.AsSoonAsPossible && ScheduleVenuesBeforeDates,
            StartDate = AutomaticStartDate ? null : StartDate.GetValueOrDefault().ToUniversalTime(),
            EndDate = AutomaticEndDate ? null : EndDate.GetValueOrDefault().ToUniversalTime(),
            StartTime = StartTime.GetValueOrDefault(),
            RotationTime = RotationTimeValue.GetValueOrDefault().ToTimeSpan(RotationTimeUnit),
            RestTime = RestTimeValue.GetValueOrDefault().ToTimeSpan(RestTimeUnit),
            UseHomeVenue = VenuesSchedulingMethod == VenuesSchedulingMethod.UseHomeVenue,
            AsSoonAsPossible = DatesSchedulingMethod == DatesSchedulingMethod.AsSoonAsPossible,
            Interval = AutomaticDatesSchedulingViewModel.IntervalValue.GetValueOrDefault().ToTimeSpan(AutomaticDatesSchedulingViewModel.IntervalUnit),
            AsSoonAsPossibleRules = DatesSchedulingMethod == DatesSchedulingMethod.AsSoonAsPossible ? AsSoonAsPossibleDatesSchedulingViewModel.Rules.Rules.Select(x => x.ProvideRule()).ToList() : [],
            DateRules = DatesSchedulingMethod == DatesSchedulingMethod.Automatic ? AutomaticDatesSchedulingViewModel.DateRules.Rules.Select(x => x.ProvideRule()).ToList() : [],
            TimeRules = DatesSchedulingMethod == DatesSchedulingMethod.Automatic ? AutomaticDatesSchedulingViewModel.TimeRules.Rules.Select(x => x.ProvideRule()).ToList() : [],
            VenueRules = VenuesSchedulingMethod == VenuesSchedulingMethod.Automatic ? VenueRules.Rules.Select(x => x.ProvideRule()).ToList() : []
        };

        private void LoadSchedulingParameters(SchedulingParameters schedulingParameters)
        {
            StartDate = schedulingParameters.StartDate.ToLocalTime();
            EndDate = schedulingParameters.EndDate.ToLocalTime();
            StartTime = schedulingParameters.StartTime;
            (RotationTimeValue, RotationTimeUnit) = schedulingParameters.RotationTime.Simplify();
            (RestTimeValue, RestTimeUnit) = schedulingParameters.RestTime.Simplify();
            VenuesSchedulingMethod = schedulingParameters.UseHomeVenue ? VenuesSchedulingMethod.UseHomeVenue : schedulingParameters.VenueRules.Count > 0 ? VenuesSchedulingMethod.Automatic : schedulingParameters.AsSoonAsPossible ? VenuesSchedulingMethod.AsSoonAsPossible : VenuesSchedulingMethod.None;
            DatesSchedulingMethod = schedulingParameters.AsSoonAsPossible ? DatesSchedulingMethod.AsSoonAsPossible : schedulingParameters.DateRules.Count > 0 ? DatesSchedulingMethod.Automatic : DatesSchedulingMethod.Manual;
            ScheduleVenuesBeforeDates = schedulingParameters.UseHomeVenue;

            switch (VenuesSchedulingMethod)
            {
                case VenuesSchedulingMethod.None:
                case VenuesSchedulingMethod.UseHomeVenue:
                case VenuesSchedulingMethod.AsSoonAsPossible:
                    VenueRules.Rules.Clear();
                    break;
                case VenuesSchedulingMethod.Automatic:
                    VenueRules.Load(schedulingParameters.VenueRules);
                    break;
                default:
                    break;
            }

            var startDate = schedulingParameters.StartDate.ToLocalDateTime(schedulingParameters.StartTime);
            switch (DatesSchedulingMethod)
            {
                case DatesSchedulingMethod.AsSoonAsPossible:
                    AsSoonAsPossibleDatesSchedulingViewModel.Load(schedulingParameters);
                    AutomaticDatesSchedulingViewModel.Reset(startDate);
                    ManualDatesSchedulingViewModel.Reset(startDate);
                    break;
                case DatesSchedulingMethod.Automatic:
                    AsSoonAsPossibleDatesSchedulingViewModel.Reset(startDate);
                    AutomaticDatesSchedulingViewModel.Load(schedulingParameters);
                    ManualDatesSchedulingViewModel.Reset(startDate);
                    break;
                case DatesSchedulingMethod.Manual:
                    AsSoonAsPossibleDatesSchedulingViewModel.Reset(startDate);
                    AutomaticDatesSchedulingViewModel.Reset(startDate);
                    ManualDatesSchedulingViewModel.Load(schedulingParameters);
                    break;
                default:
                    break;
            }
        }
    }

    internal class EditableInvertTeamViewModel : SelectedWrapper<int>
    {
        public EditableInvertTeamViewModel(int index) : base(index, index % 2 == 0) => IsSelectable = index > 1;
    }
}
