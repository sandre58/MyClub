// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Reactive.Linq;
using DynamicData;
using MyNet.UI.Toasting;
using MyNet.UI.ViewModels;
using MyNet.UI.ViewModels.Edition;
using MyNet.Utilities;
using MyNet.Utilities.DateTimes;
using MyNet.Humanizer;
using MyNet.Observable.Attributes;
using MyNet.UI.Resources;
using MyClub.Teamup.Application.Services;
using MyClub.CrossCutting.Localization;
using PropertyChanged;

namespace MyClub.Teamup.Wpf.ViewModels.Edition
{
    internal class TrainingSessionsAddViewModel : EditionViewModel
    {
        private readonly TrainingSessionService _trainingSessionService;
        private int _countCreatedSessions;

        [CanSetIsModified(false)]
        public ReadOnlyCollection<DayOfWeek> AllDays { get; private set; }

        [CanSetIsModified(false)]
        public IReadOnlyCollection<DateTime> Dates { get; private set; } = new List<DateTime>().AsReadOnly();

        [DoNotCheckEquality]
        public IEnumerable? SelectedDays { get; set; }

        [DoNotCheckEquality]
        public IEnumerable? SelectedTeamIds { get; set; }

        [IsRequired]
        [ValidateProperty(nameof(EndTime))]
        [Display(Name = nameof(StartTime), ResourceType = typeof(MyClubResources))]
        public TimeSpan? StartTime { get; set; }

        [IsRequired]
        [ValidateProperty(nameof(StartTime))]
        [Display(Name = nameof(EndTime), ResourceType = typeof(MyClubResources))]
        public TimeSpan? EndTime { get; set; }

        public string? Place { get; set; }

        public string? Theme { get; set; }

        public TrainingSessionsAddViewModel(TrainingSessionService trainingSessionService)
        {
            _trainingSessionService = trainingSessionService;
            Mode = ScreenMode.Creation;

            var firstDayOfWeek = CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek;
            AllDays = Enum.GetValues(typeof(DayOfWeek)).Cast<DayOfWeek>().Rotate((int)firstDayOfWeek).ToList().AsReadOnly();

            ValidationRules.Add<TrainingSessionsAddViewModel, IEnumerable?>(x => x.SelectedDays, MyClubResources.AnySelectedDaysError, x => x is not null && x.Cast<DayOfWeek>().Any());
            ValidationRules.Add<TrainingSessionsAddViewModel, IEnumerable?>(x => x.SelectedTeamIds, MyClubResources.AnySelectedSquadsError, x => x is not null && x.Cast<Guid>().Any());
            ValidationRules.Add<TrainingSessionsAddViewModel, TimeSpan?>(x => x.StartTime, MessageResources.FieldEndTimeMustBeUpperThanStartTimeError, x => x is null || EndTime is null || x < EndTime);
            ValidationRules.Add<TrainingSessionsAddViewModel, TimeSpan?>(x => x.EndTime, MessageResources.FieldEndTimeMustBeUpperThanStartTimeError, x => StartTime is null || x is null || StartTime < x);
        }

        public void SetDates(IEnumerable<DateTime> dates) => Dates = dates.ToList().AsReadOnly();

        protected override void RefreshCore()
        {
            base.RefreshCore();
            var firstDayOfWeek = CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek;
            AllDays = Enum.GetValues(typeof(DayOfWeek)).Cast<DayOfWeek>().Rotate((int)firstDayOfWeek).ToList().AsReadOnly();
        }

        protected override void ResetCore()
        {
            using (ValidatePropertySuspender.Suspend())
            {
                var defaultValues = _trainingSessionService.NewTrainingSession(DateTime.Today);
                base.ResetCore();

                Place = defaultValues.Place;
                Theme = defaultValues.Theme;
                StartTime = defaultValues.StartDate.ToLocalTime().TimeOfDay;
                EndTime = defaultValues.EndDate.ToLocalTime().TimeOfDay;
                SelectedTeamIds = defaultValues.TeamIds?.ToList();
                SelectedDays = null;
            }
        }

        protected override string CreateTitle() => UiResources.Creation;

        #region Validate

        protected override void OnSaveSucceeded() => ToasterManager.ShowSuccess(nameof(MyClubResources.XSessionsAddedSuccess).TranslateAndFormatWithCount(_countCreatedSessions));

        protected override void SaveCore()
        {
            if (SelectedDays is null) return;
            if (SelectedTeamIds is null) return;

            var periods = Dates.Where(x => SelectedDays.Cast<DayOfWeek>().Contains(x.DayOfWeek)).Select(x => new Period(x.AddFluentTimeSpan(StartTime ?? TimeSpan.MinValue).ToUniversalTime(), x.AddFluentTimeSpan(EndTime ?? TimeSpan.MaxValue).ToUniversalTime())).ToList();

            _countCreatedSessions = _trainingSessionService.Add(periods, Place.OrEmpty(), Theme.OrEmpty(), SelectedTeamIds.OfType<Guid>()).Count;
        }

        #endregion
    }
}
