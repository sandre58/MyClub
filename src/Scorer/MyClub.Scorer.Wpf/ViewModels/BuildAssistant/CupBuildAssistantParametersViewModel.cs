// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using DynamicData.Binding;
using MyClub.CrossCutting.Localization;
using MyClub.Scorer.Application.Dtos;
using MyClub.Scorer.Application.Services;
using MyClub.Scorer.Domain.Enums;
using MyClub.Scorer.Domain.Extensions;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.Scheduling;
using MyClub.Scorer.Wpf.ViewModels.Edition;
using MyNet.Observable.Attributes;
using MyNet.Observable.Collections.Providers;
using MyNet.UI.Resources;
using MyNet.UI.Toasting;
using MyNet.UI.Toasting.Settings;
using MyNet.UI.ViewModels.Workspace;
using MyNet.Utilities;
using MyNet.Utilities.Deferring;
using MyNet.Utilities.Localization;
using MyNet.Utilities.Units;

namespace MyClub.Scorer.Wpf.ViewModels.BuildAssistant
{
    internal class CupBuildAssistantParametersViewModel : NavigableWorkspaceViewModel
    {
        private readonly Deferrer _updateNumberOfRoundsDeferrer;
        private readonly Dictionary<DatesSchedulingMethod, IDatesSchedulingMethodViewModel> _datesSchedulingMethodViewModels = new()
        {
            { DatesSchedulingMethod.AsSoonAsPossible, new DatesSchedulingAsSoonAsPossibleViewModel() },
            { DatesSchedulingMethod.Manual, new DatesSchedulingManualViewModel() },
            { DatesSchedulingMethod.Automatic, new DatesSchedulingAutomaticViewModel() },
        };

        public CupBuildAssistantParametersViewModel(ISourceProvider<IEditableStadiumViewModel> stadiumsProvider)
        {
            VenueRules = new(stadiumsProvider.Source);

            _updateNumberOfRoundsDeferrer = new(ComputeNumberOfRounds);

            Disposables.AddRange([
                this.WhenPropertyChanged(x => x.Algorythm, false).Subscribe(_ => _updateNumberOfRoundsDeferrer.DeferOrExecute()),
                this.WhenPropertyChanged(x => x.StartTime, false).Subscribe(x =>
                {
                    _updateNumberOfRoundsDeferrer.DeferOrExecute();
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
                ]);

            ValidationRules.Add<CupBuildAssistantParametersViewModel, DateOnly?>(x => x.EndDate, MessageResources.FieldEndDateMustBeUpperOrEqualsThanStartDateError, x => !x.HasValue || !StartDate.HasValue || x.Value > StartDate);

            Reset();
        }

        [IsRequired]
        [Display(Name = nameof(Algorythm), ResourceType = typeof(MyClubResources))]
        public KnockoutAlgorithm Algorythm { get; set; }

        [CanSetIsModified(false)]
        [CanBeValidated(false)]
        public int NumberOfRounds { get; private set; }

        [CanSetIsModified(false)]
        [CanBeValidated(false)]
        public int NumberOfTeams { get; private set; }

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

        [CanSetIsModified(false)]
        [CanBeValidated(false)]
        public bool CanSelectAlgorithm { get; private set; }

        public EditableMatchFormatViewModel MatchFormat { get; } = new();

        [IsRequired]
        public DatesSchedulingMethod DatesSchedulingMethod { get; set; }

        [IsRequired]
        public VenuesSchedulingMethod VenuesSchedulingMethod { get; set; }

        [CanBeValidated(false)]
        public DatesSchedulingAutomaticViewModel AutomaticDatesSchedulingViewModel => (DatesSchedulingAutomaticViewModel)_datesSchedulingMethodViewModels[DatesSchedulingMethod.Automatic];

        [CanBeValidated(false)]
        public DatesSchedulingManualViewModel ManualDatesSchedulingViewModel => (DatesSchedulingManualViewModel)_datesSchedulingMethodViewModels[DatesSchedulingMethod.Manual];

        [CanBeValidated(false)]
        public DatesSchedulingAsSoonAsPossibleViewModel AsSoonAsPossibleDatesSchedulingViewModel => (DatesSchedulingAsSoonAsPossibleViewModel)_datesSchedulingMethodViewModels[DatesSchedulingMethod.AsSoonAsPossible];

        public bool ScheduleVenuesBeforeDates { get; set; }

        [CanBeValidated(false)]
        public EditableVenueSchedulingRulesViewModel VenueRules { get; }

        public void Load(MatchFormat matchFormat, SchedulingParameters schedulingParameters, int numberOfTeams)
        {
            MatchFormat.Load(matchFormat);
            LoadSchedulingParameters(schedulingParameters);
            Refresh(numberOfTeams);

        }
        internal void Refresh(int numberOfTeams)
        {
            using (_updateNumberOfRoundsDeferrer.Defer())
            {
                NumberOfTeams = numberOfTeams;
                CanSelectAlgorithm = (numberOfTeams & (numberOfTeams - 1)) != 0;
                if (!CanSelectAlgorithm && Algorythm == KnockoutAlgorithm.PreliminaryRound)
                    Algorythm = KnockoutAlgorithm.ByeTeam;
            }
        }

        protected override void ResetCore()
        {
            using (_updateNumberOfRoundsDeferrer.Defer())
            {
                AutomaticStartDate = true;
                AutomaticEndDate = true;
                NamePattern = MyClubResources.MatchdayNamePattern;
                ShortNamePattern = MyClubResources.MatchdayShortNamePattern;
                MatchFormat.Reset();
                LoadSchedulingParameters(SchedulingParameters.Default);
            }
        }

        private void ComputeNumberOfRounds() => NumberOfRounds = CupService.GetNumberOfRounds(ToAlgorithmParameters());

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

        public RoundsAlgorithmDto ToAlgorithmParameters() => Algorythm switch
        {
            KnockoutAlgorithm.PreliminaryRound => new PreliminaryRoundDto { NumberOfTeams = NumberOfTeams },
            KnockoutAlgorithm.ByeTeam => new ByeTeamDto { NumberOfTeams = NumberOfTeams },
            _ => throw new InvalidOperationException("Algorithm is invalid")
        };

        public BuildParametersDto ToBuildParameters() => new()
        {
            MatchFormat = MatchFormat.Create(),
            SchedulingParameters = ToSchedulingParameters(),
            BracketParameters = new BuildRoundsParametersDto
            {
                NamePattern = NamePattern,
                ShortNamePattern = ShortNamePattern,
                ScheduleVenuesBeforeDates = DatesSchedulingMethod is DatesSchedulingMethod.AsSoonAsPossible && ScheduleVenuesBeforeDates,
                ScheduleVenues = VenuesSchedulingMethod is not VenuesSchedulingMethod.None,
                AsSoonAsPossibleVenues = VenuesSchedulingMethod is VenuesSchedulingMethod.AsSoonAsPossible,
                UseHomeVenue = VenuesSchedulingMethod == VenuesSchedulingMethod.UseHomeVenue,
                VenueRules = VenuesSchedulingMethod == VenuesSchedulingMethod.Automatic ? VenueRules.Rules.Select(x => x.ProvideRule()).ToList() : [],
                BuildDatesParameters = _datesSchedulingMethodViewModels[DatesSchedulingMethod].ProvideBuildDatesParameters(NumberOfRounds, 0, StartTime.GetValueOrDefault()),
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
    }
}
