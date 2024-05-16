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
    internal class DateFilterViewModel : SelectedValueFilterViewModel<DateTime?, DateTime>
    {
        public DateFilterViewModel(string propertyName, IEnumerable<DateTime> allowedValues) : base(propertyName, allowedValues)
        {
            PreviousDateCommand = CommandsManager.Create(() => Value = GetPreviousDate(Value?.Date), () => GetPreviousDate(Value?.Date) != default);
            NextDateCommand = CommandsManager.Create(() => Value = GetNextDate(Value?.Date), () => GetNextDate(Value?.Date) != default);
            NextFixturesCommand = CommandsManager.Create(() => Value = GetNextFixtures(), () => GetNextFixtures() != default && GetNextFixtures() != Value);
            LatestResultsCommand = CommandsManager.Create(() => Value = GetLatestResults(), () => GetLatestResults() != default && GetLatestResults() != Value);
        }

        public ICommand PreviousDateCommand { get; private set; }

        public ICommand NextDateCommand { get; private set; }

        public ICommand NextFixturesCommand { get; private set; }

        public ICommand LatestResultsCommand { get; private set; }

        private DateTime GetPreviousDate(DateTime? date)
            => AvailableValues!.OrderBy(x => x).LastOrDefault(x => !date.HasValue || x.IsBefore(date.Value));

        private DateTime GetNextDate(DateTime? date)
            => AvailableValues!.OrderBy(x => x).FirstOrDefault(x => !date.HasValue || x.IsAfter(date.Value));

        private DateTime GetNextFixtures() => AvailableValues!.OrderBy(x => x).FirstOrDefault(x => x.IsAfter(DateTime.Now));

        private DateTime GetLatestResults() => AvailableValues!.OrderBy(x => x).LastOrDefault(x => x.IsBefore(DateTime.Now));

        public override void Reset()
        {
            var previousDate = GetPreviousDate(DateTime.Today);

            if (previousDate != default)
                Value = previousDate;
            else
            {
                var nextDate = GetNextDate(DateTime.Today);

                if (nextDate != default)
                    Value = nextDate;
            }
        }

        protected override FilterViewModel CreateCloneInstance() => new DateFilterViewModel(PropertyName, AvailableValues ?? []);
    }
}
