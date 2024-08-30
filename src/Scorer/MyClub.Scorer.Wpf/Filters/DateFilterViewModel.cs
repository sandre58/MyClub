// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyNet.UI.Commands;
using MyNet.UI.ViewModels.List.Filtering.Filters;
using MyNet.Utilities;

namespace MyClub.Scorer.Wpf.Filters
{
    internal class DateFilterViewModel : SelectedValueFilterViewModel<DateOnly?, DateOnly>
    {
        public DateFilterViewModel(string propertyName, IEnumerable<DateOnly> allowedValues) : base(propertyName, allowedValues)
        {
            PreviousDateCommand = CommandsManager.Create(() => Value = GetPreviousDate(Value), () => GetPreviousDate(Value) != default);
            NextDateCommand = CommandsManager.Create(() => Value = GetNextDate(Value), () => GetNextDate(Value) != default);
            NextFixturesCommand = CommandsManager.Create(() => Value = GetNextFixtures(), () => GetNextFixtures() != default && GetNextFixtures() != Value);
            LatestResultsCommand = CommandsManager.Create(() => Value = GetLatestResults(), () => GetLatestResults() != default && GetLatestResults() != Value);
        }

        public ICommand PreviousDateCommand { get; private set; }

        public ICommand NextDateCommand { get; private set; }

        public ICommand NextFixturesCommand { get; private set; }

        public ICommand LatestResultsCommand { get; private set; }

        private DateOnly GetPreviousDate(DateOnly? date)
            => AvailableValues!.OrderBy(x => x).LastOrDefault(x => !date.HasValue || x.IsBefore(date.Value));

        private DateOnly GetNextDate(DateOnly? date)
            => AvailableValues!.OrderBy(x => x).FirstOrDefault(x => !date.HasValue || x.IsAfter(date.Value));

        private DateOnly GetNextFixtures() => AvailableValues!.OrderBy(x => x).FirstOrDefault(x => x.IsInFuture());

        private DateOnly GetLatestResults() => AvailableValues!.OrderBy(x => x).LastOrDefault(x => x.IsInPast());

        public override void Reset()
        {
            var nextDate = GetNextDate(DateTime.UtcNow.ToDate());

            if (nextDate != default)
                Value = nextDate;
            else
            {
                var previousDate = GetPreviousDate(DateTime.UtcNow.ToDate());

                if (previousDate != default)
                    Value = previousDate;
            }
        }

        protected override FilterViewModel CreateCloneInstance() => new DateFilterViewModel(PropertyName, AvailableValues ?? []);
    }
}
