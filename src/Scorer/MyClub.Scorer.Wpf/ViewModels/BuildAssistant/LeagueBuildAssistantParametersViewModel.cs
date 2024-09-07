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
using MyClub.Scorer.Domain.Extensions;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.Scheduling;
using MyClub.Scorer.Wpf.ViewModels.Edition;
using MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces;
using MyNet.Observable.Attributes;
using MyNet.Observable.Collections.Providers;
using MyNet.UI.Collections;
using MyNet.UI.Commands;
using MyNet.UI.Resources;
using MyNet.UI.Selection.Models;
using MyNet.UI.Toasting;
using MyNet.UI.Toasting.Settings;
using MyNet.UI.ViewModels.Workspace;
using MyNet.Utilities;
using MyNet.Utilities.Deferring;
using MyNet.Utilities.Helpers;
using MyNet.Utilities.Localization;
using MyNet.Utilities.Units;

namespace MyClub.Scorer.Wpf.ViewModels.BuildAssistant
{
    internal enum VenuesSchedulingMethod
    {
        None,

        UseHomeVenue,

        AsSoonAsPossible,

        Automatic,
    }

    internal class LeagueBuildAssistantParametersViewModel : NavigableWorkspaceViewModel
    {
        private readonly Deferrer _updateNumberOfMatchesDeferrer;
        private readonly Dictionary<DatesSchedulingMethod, IDatesSchedulingMethodViewModel> _datesSchedulingMethodViewModels = new()
        {
            { DatesSchedulingMethod.AsSoonAsPossible, new DatesSchedulingAsSoonAsPossibleViewModel() },
            { DatesSchedulingMethod.Manual, new DatesSchedulingManualViewModel() },
            { DatesSchedulingMethod.Automatic, new DatesSchedulingAutomaticViewModel() },
        };

        public LeagueBuildAssistantParametersViewModel(ISourceProvider<IStadiumViewModel> stadiumsProvider)
        {
            VenueRules = new(stadiumsProvider.Source);

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
                    AsSoonAsPossibleDatesSchedulingViewModel.Start.Time = StartTime;
                }),
                this.WhenPropertyChanged(x => x.StartDate, false).Subscribe(x =>
                {
                    AutomaticDatesSchedulingViewModel.StartDate = StartDate;
                    AsSoonAsPossibleDatesSchedulingViewModel.Start.Date = StartDate;
                }),
                this.WhenPropertyChanged(x => x.DatesSchedulingMethod, false).Subscribe(x =>
                {
                    if(DatesSchedulingMethod != DatesSchedulingMethod.AsSoonAsPossible && VenuesSchedulingMethod == VenuesSchedulingMethod.AsSoonAsPossible)
                        VenuesSchedulingMethod = VenuesSchedulingMethod.UseHomeVenue;
                }),
                MatchesBetweenTeams.ToObservableChangeSet().Subscribe(_ => _updateNumberOfMatchesDeferrer.DeferOrExecute()),
                ]);

            ValidationRules.Add<LeagueBuildAssistantParametersViewModel, DateOnly?>(x => x.EndDate, MessageResources.FieldEndDateMustBeUpperOrEqualsThanStartDateError, x => !x.HasValue || !StartDate.HasValue || x.Value > StartDate);

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
        [ValidateProperty(nameof(EndDate))]
        [Display(Name = nameof(StartDate), ResourceType = typeof(MyClubResources))]
        public DateOnly? StartDate { get; set; }

        [IsRequired]
        [Display(Name = nameof(EndDate), ResourceType = typeof(MyClubResources))]
        public DateOnly? EndDate { get; set; }

        [CanSetIsModified(false)]
        [CanBeValidated(false)]
        public bool AutomaticStartDate { get; set; }

        [CanSetIsModified(false)]
        [CanBeValidated(false)]
        public bool AutomaticEndDate { get; set; }

        [IsRequired]
        [Display(Name = nameof(StartTime), ResourceType = typeof(MyClubResources))]
        public TimeOnly? StartTime { get; set; }

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

        public void Load(MatchFormat matchFormat, SchedulingParameters schedulingParameters, int numberOfTeams)
        {
            MatchFormat.Load(matchFormat);
            LoadSchedulingParameters(schedulingParameters);
            Refresh(numberOfTeams);
        }

        public void Refresh(int numberOfTeams)
        {
            using (_updateNumberOfMatchesDeferrer.Defer())
            {
                NumberOfTeams = numberOfTeams;
                CanUseSwissSystem = numberOfTeams % 2 == 0;
                if (!CanUseSwissSystem && Algorythm == ChampionshipAlgorithm.SwissSystem)
                    Algorythm = ChampionshipAlgorithm.RoundRobin;
            }
        }

        protected override void ResetCore()
        {
            using (_updateNumberOfMatchesDeferrer.Defer())
            {
                AutomaticStartDate = true;
                AutomaticEndDate = true;
                MatchesBetweenTeams.Set(EnumerableHelper.Range(1, 2, 1).Select(x => new EditableInvertTeamViewModel(x)));
                NumberOfMatchesByTeam = 8;
                NamePattern = MyClubResources.MatchdayNamePattern;
                ShortNamePattern = MyClubResources.MatchdayShortNamePattern;
                MatchFormat.Reset();
                LoadSchedulingParameters(SchedulingParameters.Default);
            }
        }

        private void ComputeNumberOfMatches()
        {
            NumberOfMatchesByMatchday = LeagueService.GetNumberOfMatchesByMatchday(ToAlgorithmParameters());
            NumberOfMatchdays = LeagueService.GetNumberOfMatchays(ToAlgorithmParameters());
        }

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

        public BuildAlgorithmParametersDto ToAlgorithmParameters() => Algorythm switch
        {
            ChampionshipAlgorithm.RoundRobin => new RoundRobinParametersDto { NumberOfTeams = NumberOfTeams, MatchesBetweenTeams = MatchesBetweenTeams.Select(x => x.IsSelected).ToArray() },
            ChampionshipAlgorithm.SwissSystem => new SwissSystemParametersDto { NumberOfTeams = NumberOfTeams, NumberOfMatchesByTeam = NumberOfMatchesByTeam },
            _ => throw new InvalidOperationException("Algorithm is invalid")
        };

        public BuildParametersDto ToBuildParameters() => new()
        {
            MatchFormat = MatchFormat.Create(),
            SchedulingParameters = ToSchedulingParameters(),
            BracketParameters = new BuildMatchdaysParametersDto
            {
                NamePattern = NamePattern,
                ShortNamePattern = ShortNamePattern,
                ScheduleVenuesBeforeDates = DatesSchedulingMethod is DatesSchedulingMethod.AsSoonAsPossible && ScheduleVenuesBeforeDates,
                ScheduleVenues = VenuesSchedulingMethod is not VenuesSchedulingMethod.None,
                AsSoonAsPossibleVenues = VenuesSchedulingMethod is VenuesSchedulingMethod.AsSoonAsPossible,
                UseHomeVenue = VenuesSchedulingMethod == VenuesSchedulingMethod.UseHomeVenue,
                VenueRules = VenuesSchedulingMethod == VenuesSchedulingMethod.Automatic ? VenueRules.Rules.Select(x => x.ProvideRule()).ToList() : [],
                BuildDatesParameters = _datesSchedulingMethodViewModels[DatesSchedulingMethod].ProvideBuildDatesParameters(NumberOfMatchdays, NumberOfMatchesByMatchday, StartTime.GetValueOrDefault()),
                AlgorithmParameters = ToAlgorithmParameters()
            }
        };

        public SchedulingParameters ToSchedulingParameters()
            => new(StartDate.GetValueOrDefault().ToDateTime(StartTime.GetValueOrDefault()).ToUniversalTime().ToDate(),
                   EndDate.GetValueOrDefault().ToDateTime(StartTime.GetValueOrDefault()).ToUniversalTime().ToDate(),
                   StartDate.GetValueOrDefault().ToDateTime(StartTime.GetValueOrDefault()).ToUniversalTime().ToTime(),
                   RotationTimeValue.GetValueOrDefault().ToTimeSpan(RotationTimeUnit),
                   RestTimeValue.GetValueOrDefault().ToTimeSpan(RestTimeUnit),
                   VenuesSchedulingMethod == VenuesSchedulingMethod.UseHomeVenue,
                   DatesSchedulingMethod == DatesSchedulingMethod.AsSoonAsPossible,
                   AutomaticDatesSchedulingViewModel.IntervalValue.GetValueOrDefault().ToTimeSpan(AutomaticDatesSchedulingViewModel.IntervalUnit),
                   true,
                   DatesSchedulingMethod == DatesSchedulingMethod.AsSoonAsPossible ? AsSoonAsPossibleDatesSchedulingViewModel.Rules.Rules.Select(x => x.ProvideRule()).SelectMany(x => x.ConvertToTimeZone(GlobalizationService.Current.TimeZone, TimeZoneInfo.Utc)).ToList() : [],
                   DatesSchedulingMethod == DatesSchedulingMethod.Automatic ? AutomaticDatesSchedulingViewModel.DateRules.Rules.Select(x => x.ProvideRule()).SelectMany(x => x.ConvertToTimeZone(GlobalizationService.Current.TimeZone, TimeZoneInfo.Utc)).ToList() : [],
                   DatesSchedulingMethod == DatesSchedulingMethod.Automatic ? AutomaticDatesSchedulingViewModel.TimeRules.Rules.Select(x => x.ProvideRule()).SelectMany(x => x.ConvertToTimeZone(GlobalizationService.Current.TimeZone, TimeZoneInfo.Utc)).ToList() : [],
                   VenuesSchedulingMethod == VenuesSchedulingMethod.Automatic ? VenueRules.Rules.Select(x => x.ProvideRule()).ToList() : []);

        private void LoadSchedulingParameters(SchedulingParameters schedulingParameters)
        {
            StartDate = schedulingParameters.GetCurrentStartDate();
            EndDate = schedulingParameters.GetCurrentEndDate();
            StartTime = schedulingParameters.GetCurrentStartTime();
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

            var startDate = schedulingParameters.Start();
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

        internal class EditableInvertTeamViewModel : SelectedWrapper<int>
        {
            public EditableInvertTeamViewModel(int index) : base(index, index % 2 == 0) => IsSelectable = index > 1;
        }
    }
}
