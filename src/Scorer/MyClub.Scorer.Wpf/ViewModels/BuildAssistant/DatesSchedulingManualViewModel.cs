// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using DynamicData;
using DynamicData.Binding;
using MyClub.CrossCutting.Localization;
using MyClub.Scorer.Application.Dtos;
using MyClub.Scorer.Domain.Scheduling;
using MyNet.Observable;
using MyNet.Observable.Attributes;
using MyNet.UI.Collections;
using MyNet.UI.Commands;
using MyNet.UI.Toasting.Settings;
using MyNet.UI.Toasting;
using MyNet.UI.ViewModels.Display;
using MyNet.Utilities;
using MyNet.Utilities.Helpers;
using MyClub.Scorer.Domain.Extensions;

namespace MyClub.Scorer.Wpf.ViewModels.BuildAssistant
{
    internal enum TimeSelectionMethod
    {
        UseDefaultTime,

        UseUniqueTimeByMatchday,

        UseUniqueTimeByMatches,

        UseUniqueTimeByMatch,
    }

    internal class DatesSchedulingManualViewModel : EditableObject, IDatesSchedulingMethodViewModel
    {
        private int _countMatchdays;
        private int _countMatches;
        private TimeOnly? _defaultTime;
        private readonly UiObservableCollection<EditableDateOfMatchdayWrapper> _dates = [];
        private readonly ReadOnlyObservableCollection<EditableDateOfMatchdayWrapper> _sortedDates;

        public DatesSchedulingManualViewModel()
        {
            AddToDateCommand = CommandsManager.CreateNotNull<DateTime>(x => AddToDate(x.ToDate()), x => Dates.Count < _countMatchdays);
            RemoveFromDateCommand = CommandsManager.CreateNotNull<DateOnly>(x => Remove(Dates.LastOrDefault(y => y.Date == x)), x => Dates.Any(y => y.Date == x));
            RemoveCommand = CommandsManager.CreateNotNull<EditableDateOfMatchdayWrapper>(Remove);

            Disposables.AddRange(
                [
                    _dates.ToObservableChangeSet().Sort(SortExpressionComparer<EditableDateOfMatchdayWrapper>.Ascending(x => x.Date)).Bind(out _sortedDates).Subscribe(_ =>
                    {
                        CountMissingDates = _countMatchdays - _dates.Count;
                        UpdateIndexes(Dates);
                    }),
                    MatchTimes.ToObservableChangeSet().Subscribe(_ => UpdateIndexes(MatchTimes))
                ]);

            ValidationRules.Add<DatesSchedulingManualViewModel, ReadOnlyObservableCollection<EditableDateOfMatchdayWrapper>?>(x => x.Dates, MyClubResources.MissingDatesError, x => CountMissingDates == 0);
        }

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public DisplayModeMonth DateSelection { get; } = new();

        public TimeSelectionMethod TimeSelectionMethod { get; set; }

        public ReadOnlyObservableCollection<EditableDateOfMatchdayWrapper> Dates => _sortedDates;

        [CanBeValidated(false)]
        public UiObservableCollection<EditableTimeWrapper> MatchTimes { get; } = [];

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public int CountMissingDates { get; private set; }

        public ICommand AddToDateCommand { get; }

        public ICommand RemoveFromDateCommand { get; }

        public ICommand RemoveCommand { get; }

        private static void UpdateIndexes(IEnumerable<EditableTimeWrapper> items) => items.ForEach(x => x.Index = items.IndexOf(x) + 1);

        public void Update(int countMatchdays, int countMatches, TimeOnly? defaultTime)
        {
            _defaultTime = defaultTime;
            _countMatchdays = countMatchdays;
            _countMatches = countMatches;
            if (Dates.Count > countMatchdays)
                _dates.Set(Dates.Take(countMatchdays).ToList());
            CountMissingDates = _countMatchdays - _dates.Count;

            if (countMatches > MatchTimes.Count)
            {
                MatchTimes.AddRange(EnumerableHelper.Range(MatchTimes.Count + 1, countMatches, 1).Select(x => new EditableTimeWrapper(_defaultTime)).ToList());

                Dates.ForEach(x => x.Times.AddRange(EnumerableHelper.Range(x.Times.Count + 1, countMatches, 1).Select(y => new EditableDateTimeWrapper(x.Date, x.Time) { Index = y }).ToList()));
            }
            else if (countMatches < MatchTimes.Count)
            {
                MatchTimes.Set(MatchTimes.Take(countMatches).ToList());

                Dates.ForEach(x => x.Times.Set(x.Times.Take(countMatches).ToList()));
            }
        }

        public override bool ValidateProperties()
        {
            if (!base.ValidateProperties()) return false;

            if (TimeSelectionMethod == TimeSelectionMethod.UseUniqueTimeByMatch && !MatchTimes.All(x => x.ValidateProperties()))
            {
                MatchTimes.SelectMany(x => x.GetErrors()).ToList().ForEach(x => ToasterManager.ShowError(x, ToastClosingStrategy.AutoClose));
                return false;
            }

            return true;
        }

        public void Reset(DateTime startDate)
        {
            DateSelection.DisplayDate = startDate.Date;
            MatchTimes.ForEach(x => x.Time = startDate.ToTime());
            TimeSelectionMethod = TimeSelectionMethod.UseDefaultTime;
            _dates.Clear();
        }

        public void Load(SchedulingParameters schedulingParameters)
        {
            DateSelection.DisplayDate = schedulingParameters.GetCurrentStartDate().BeginningOfDay();
            Reset(schedulingParameters.Start().ToCurrentTime());
        }

        public BuildDatesParametersDto ProvideBuildDatesParameters(int countMatchdays, int countMatchesByMatchday, TimeOnly defaultTime) => new BuildManualDatesParametersDto
        {
            Dates = ProvideDates(countMatchdays, countMatchesByMatchday, defaultTime)
        };
        private List<(DateTime, IEnumerable<DateTime>)> ProvideDates(int countMatchdays, int countMatchesByMatchday, TimeOnly defaultTime)
            => Dates.Take(countMatchdays).Select(x => (x.To(x => TimeSelectionMethod switch
            {
                TimeSelectionMethod.UseUniqueTimeByMatchday => x.Date.At(x.Time.GetValueOrDefault()).ToUniversalTime(),
                TimeSelectionMethod.UseUniqueTimeByMatches => x.Date.At(MatchTimes.OrderBy(x => x.Time).FirstOrDefault()?.Time ?? defaultTime).ToUniversalTime(),
                TimeSelectionMethod.UseUniqueTimeByMatch => x.Date.At(x.Times.OrderBy(x => x.Time).FirstOrDefault()?.Time ?? defaultTime).ToUniversalTime(),
                _ => x.Date.At(defaultTime).ToUniversalTime(),
            }), EnumerableHelper.Range(0, countMatchesByMatchday - 1, 1).Select(y => TimeSelectionMethod switch
                {
                    TimeSelectionMethod.UseUniqueTimeByMatchday => x.Date.At(x.Time.GetValueOrDefault()).ToUniversalTime(),
                    TimeSelectionMethod.UseUniqueTimeByMatches => x.Date.At(MatchTimes.GetByIndex(y)?.Time ?? defaultTime).ToUniversalTime(),
                    TimeSelectionMethod.UseUniqueTimeByMatch => (x.Times.GetByIndex(y) is EditableDateTimeWrapper wrapper && wrapper.UpdateDate ? wrapper.Date : x.Date).At(x.Times.GetByIndex(y)?.Time ?? defaultTime).ToUniversalTime(),
                    _ => x.Date.At(defaultTime).ToUniversalTime(),
                }))).ToList();

        private void AddToDate(DateOnly date, TimeOnly? time = null) => _dates.Add(new EditableDateOfMatchdayWrapper(date, time ?? _defaultTime, _countMatches));

        private void Remove(EditableDateOfMatchdayWrapper? date)
        {
            if (date is not null)
                _dates.Remove(date);
        }
    }

    internal class EditableTimeWrapper : EditableObject
    {
        public EditableTimeWrapper(TimeOnly? time) => Time = time;

        [IsRequired]
        [Display(Name = nameof(Time), ResourceType = typeof(MyClubResources))]
        public TimeOnly? Time { get; set; }

        public int Index { get; internal set; }

        public override bool Equals(object? obj) => ReferenceEquals(this, obj);

        public override int GetHashCode() => Time.GetHashCode();
    }

    internal class EditableDateTimeWrapper : EditableTimeWrapper
    {
        public EditableDateTimeWrapper(DateOnly date, TimeOnly? time) : base(time)
        {
            Date = date;
            Time = time;
        }

        [IsRequired]
        [Display(Name = nameof(Date), ResourceType = typeof(MyClubResources))]
        public DateOnly Date { get; set; }

        public bool UpdateDate { get; set; }
    }

    internal class EditableDateOfMatchdayWrapper : EditableTimeWrapper, IAppointment
    {
        public EditableDateOfMatchdayWrapper(DateOnly date, TimeOnly? time, int countMatches) : base(time)
        {
            Date = date;
            Times = new(EnumerableHelper.Range(1, countMatches, 1).Select(x => new EditableDateTimeWrapper(date, time)
            {
                Index = x
            }));
        }

        [IsRequired]
        [Display(Name = nameof(Date), ResourceType = typeof(MyClubResources))]
        public DateOnly Date { get; }

        public DateTime StartDate => Date.BeginningOfDay();

        public DateTime EndDate => Date.EndOfDay();

        public ObservableCollection<EditableDateTimeWrapper> Times { get; }
    }
}
