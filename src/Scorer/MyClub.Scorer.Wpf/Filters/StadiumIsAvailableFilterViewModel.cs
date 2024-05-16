// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyNet.UI.ViewModels.List.Filtering.Filters;
using MyNet.Utilities;
using MyNet.Utilities.DateTimes;
using MyNet.Utilities.Helpers;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyClub.Scorer.Application.Services;

namespace MyClub.Scorer.Wpf.Filters
{
    internal class StadiumIsAvailableFilterViewModel : FilterViewModel
    {
        private readonly AvailibilityCheckingService _availibilityCheckingService;
        private readonly Period? _defaultPeriod;

        public StadiumIsAvailableFilterViewModel(AvailibilityCheckingService availibilityCheckingService, Period? defaultPeriod = null)
            : base(string.Empty)
        {
            _availibilityCheckingService = availibilityCheckingService;
            _defaultPeriod = defaultPeriod;

            Reset();
        }

        public DateTime? StartDate { get; set; }

        public TimeSpan? StartTime { get; set; }

        public DateTime? EndDate { get; set; }

        public TimeSpan? EndTime { get; set; }

        public override bool IsEmpty() => !StartDate.HasValue || !EndDate.HasValue;

        public override void Reset()
        {
            StartDate = _defaultPeriod?.Start.Date;
            StartTime = _defaultPeriod?.Start.TimeOfDay;
            EndDate = _defaultPeriod?.End.Date;
            EndTime = _defaultPeriod?.End.TimeOfDay;
        }

        public override bool Equals(object? obj) => obj is StadiumIsAvailableFilterViewModel && base.Equals(obj);

        public override int GetHashCode() => base.GetHashCode();

        public override void SetFrom(object? from)
        {
            if (from is StadiumIsAvailableFilterViewModel other)
            {
                StartDate = other.StartDate;
                StartTime = other.StartTime;
                EndDate = other.EndDate;
                EndTime = other.EndTime;
            }
        }

        protected override FilterViewModel CreateCloneInstance()
            => new StadiumIsAvailableFilterViewModel(_availibilityCheckingService) { StartDate = StartDate, EndDate = EndDate, StartTime = StartTime, EndTime = EndTime };

        protected override bool IsMatchProperty(object toCompare)
        {
            if (toCompare is not StadiumViewModel stadium) return false;
            if (!StartDate.HasValue || !EndDate.HasValue) return true;

            var startDate = StartDate.Value.AddFluentTimeSpan(StartTime ?? TimeSpan.Zero);
            var endDate = EndDate.Value.AddFluentTimeSpan(EndTime ?? TimeSpan.Zero);
            var period = new Period(DateTimeHelper.Min(startDate, endDate), DateTimeHelper.Max(startDate, endDate));
            return _availibilityCheckingService.StadiumIsAvailable(stadium.Id, period);
        }
    }
}
