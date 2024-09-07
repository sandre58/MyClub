// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyNet.UI.Commands;
using MyNet.Utilities;
using MyNet.UI.ViewModels.List.Filtering.Filters;
using MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces;
using System.Collections.ObjectModel;
using DynamicData.Binding;
using DynamicData;

namespace MyClub.Scorer.Wpf.Filters
{
    internal class MatchParentFilterViewModel : SelectedValueFilterViewModel<IMatchParent, IMatchParent>
    {
        public MatchParentFilterViewModel(string propertyName, IEnumerable<IMatchParent> allowedValues) : base(propertyName, allowedValues)
        {
            PreviousParentCommand = CommandsManager.Create(() => Value = GetPreviousParent(Value), () => GetPreviousParent(Value) is not null);
            NextParentCommand = CommandsManager.Create(() => Value = GetNextParent(Value), () => GetNextParent(Value) is not null);
            NextFixturesCommand = CommandsManager.Create(() => Value = GetNextFixtures(), () => GetNextFixtures() is IMatchParent matchParent && matchParent != Value);
            LatestResultsCommand = CommandsManager.Create(() => Value = GetLatestResults(), () => GetLatestResults() is IMatchParent matchParent && matchParent != Value);

            if (allowedValues is ReadOnlyObservableCollection<IMatchParent> collection)
            {
                collection.ToObservableChangeSet().OnItemAdded(x => Value.IfNull(Reset))
                                                  .OnItemRemoved(x => (Value == x).IfTrue(Reset))
                                                  .Subscribe();
            }
        }

        public ICommand PreviousParentCommand { get; private set; }

        public ICommand NextParentCommand { get; private set; }

        public ICommand NextFixturesCommand { get; private set; }

        public ICommand LatestResultsCommand { get; private set; }

        private IMatchParent? GetPreviousParent(IMatchParent? parent)
            => AvailableValues?.Except([parent]).OrderBy(x => x!.Date).LastOrDefault(x => parent is null || x!.Date.IsBefore(parent.Date.Add(1.Milliseconds())));

        private IMatchParent? GetNextParent(IMatchParent? parent)
            => AvailableValues?.Except([parent]).OrderBy(x => x!.Date).FirstOrDefault(x => parent is null || x!.Date.IsAfter(parent.Date.Subtract(1.Milliseconds())));

        private IMatchParent? GetNextFixtures() => AvailableValues?.OrderBy(x => x.Date).FirstOrDefault(x => x.Date.IsAfter(DateTime.Now));

        private IMatchParent? GetLatestResults() => AvailableValues?.OrderBy(x => x.Date).LastOrDefault(x => x.Date.IsBefore(DateTime.Now));

        public override void Reset() => Value = GetNextFixtures() ?? GetLatestResults();

        protected override FilterViewModel CreateCloneInstance() => new MatchParentFilterViewModel(PropertyName, AvailableValues ?? []);
    }
}
