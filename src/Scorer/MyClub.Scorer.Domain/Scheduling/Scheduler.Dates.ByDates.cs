﻿// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyNet.Utilities;

namespace MyClub.Scorer.Domain.Scheduling
{
    public class ByDatesScheduler<T> : IDateScheduler<T> where T : ISchedulable
    {
        private List<DateTime> _dates = [];

        public ByDatesScheduler<T> SetDates(IEnumerable<DateOnly> dates, TimeOnly time)
        {
            _dates = dates.Select(x => x.At(time)).ToList();
            return this;
        }

        public ByDatesScheduler<T> SetDates(IEnumerable<DateTime> dates)
        {
            _dates = dates.ToList();
            return this;
        }

        public void Schedule(IEnumerable<T> items) => items.ForEach((item, index) => item.Schedule(_dates.GetByIndex(index)));

        public DateTime GetFromDate() => _dates.LastOrDefault();

        public void Reset(DateTime fromDate, IEnumerable<T>? scheduledItems = null) => _dates.Clear();
    }
}
