// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyNet.UI.Commands;
using MyNet.Utilities;
using MyNet.UI.ViewModels.List.Filtering.Filters;
using MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces;

namespace MyClub.Scorer.Wpf.Filters
{
    internal class MatchParentFilterViewModel : SelectedValueFilterViewModel<IMatchParent, IMatchParent>
    {
        public MatchParentFilterViewModel(string propertyName, IEnumerable<IMatchParent> allowedValues) : base(propertyName, allowedValues)
        {
            PreviousParentCommand = CommandsManager.Create(() => Value = GetPreviousParent(Value?.Date), () => GetPreviousParent(Value?.Date) is not null);
            NextParentCommand = CommandsManager.Create(() => Value = GetNextParent(Value?.Date), () => GetNextParent(Value?.Date) is not null);
            NextFixturesCommand = CommandsManager.Create(() => Value = GetNextFixtures(), () => GetNextFixtures() is IMatchParent matchParent && matchParent != Value);
            LatestResultsCommand = CommandsManager.Create(() => Value = GetLatestResults(), () => GetLatestResults() is IMatchParent matchParent && matchParent != Value);
        }

        public ICommand PreviousParentCommand { get; private set; }

        public ICommand NextParentCommand { get; private set; }

        public ICommand NextFixturesCommand { get; private set; }

        public ICommand LatestResultsCommand { get; private set; }

        private IMatchParent? GetPreviousParent(DateTime? date)
            => AvailableValues?.OrderBy(x => x.Date).LastOrDefault(x => !date.HasValue || x.Date.IsBefore(date.Value));

        private IMatchParent? GetNextParent(DateTime? date)
            => AvailableValues?.OrderBy(x => x.Date).FirstOrDefault(x => !date.HasValue || x.Date.IsAfter(date.Value));

        private IMatchParent? GetNextFixtures() => AvailableValues?.OrderBy(x => x.Date).FirstOrDefault(x => x.Date.IsAfter(DateTime.Now));

        private IMatchParent? GetLatestResults() => AvailableValues?.OrderBy(x => x.Date).LastOrDefault(x => x.Date.IsBefore(DateTime.Now));

        public override void Reset() => Value = GetPreviousParent(DateTime.Today) ?? GetNextParent(DateTime.Today);

        protected override FilterViewModel CreateCloneInstance() => new MatchParentFilterViewModel(PropertyName, AvailableValues ?? []);
    }
}
