// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyNet.UI.Commands;
using MyNet.UI.ViewModels.List.Filtering.Filters;
using MyNet.Utilities;
using MyClub.Teamup.Wpf.ViewModels.Entities;

namespace MyClub.Teamup.Wpf.Filters
{
    internal class MatchdayFilterViewModel : SelectedValueFilterViewModel<MatchdayViewModel, MatchdayViewModel>
    {
        public MatchdayFilterViewModel(string propertyName, IEnumerable<MatchdayViewModel>? allowedValues = null) : base(propertyName, allowedValues ?? [])
        {
            PreviousMatchdayCommand = CommandsManager.Create(() => Value = GetPreviousMatchday(Value?.OriginDate), () => GetPreviousMatchday(Value?.OriginDate) is not null);
            NextMatchdayCommand = CommandsManager.Create(() => Value = GetNextMatchday(Value?.OriginDate), () => GetNextMatchday(Value?.OriginDate) is not null);
            NextFixturesCommand = CommandsManager.Create(() => Value = GetNextFixtures(), () => GetNextFixtures() is not null);
            LatestResultsCommand = CommandsManager.Create(() => Value = GetLatestResults(), () => GetLatestResults() is not null);
        }

        public ICommand PreviousMatchdayCommand { get; private set; }

        public ICommand NextMatchdayCommand { get; private set; }

        public ICommand NextFixturesCommand { get; private set; }

        public ICommand LatestResultsCommand { get; private set; }

        private MatchdayViewModel? GetPreviousMatchday(DateTime? date)
            => AvailableValues?.OrderBy(x => x.OriginDate).LastOrDefault(x => !date.HasValue || x.OriginDate.IsBefore(date.Value));

        private MatchdayViewModel? GetNextMatchday(DateTime? date)
            => AvailableValues?.OrderBy(x => x.OriginDate).FirstOrDefault(x => !date.HasValue || x.OriginDate.IsAfter(date.Value));

        private MatchdayViewModel? GetNextFixtures() => AvailableValues?.OrderBy(x => x.Date).FirstOrDefault(x => x.OriginDate.IsAfter(DateTime.Now));

        private MatchdayViewModel? GetLatestResults() => AvailableValues?.OrderBy(x => x.Date).LastOrDefault(x => x.OriginDate.IsBefore(DateTime.Now));

        public void Initialize(IEnumerable<MatchdayViewModel> matchdays)
        {
            AvailableValues = matchdays;
            Value = GetPreviousMatchday(DateTime.Today) ?? GetNextMatchday(DateTime.Today);
        }
    }
}
