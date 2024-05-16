// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using DynamicData.Aggregation;
using LiveCharts;
using MyNet.Humanizer;
using MyNet.Observable;
using MyClub.Teamup.Wpf.ViewModels.Entities;

namespace MyClub.Teamup.Wpf.ViewModels.TrainingPage.OverviewTab
{
    internal class OverviewCountViewModel : ObservableObject
    {
        public ChartValues<int> Cancelled { get; private set; } = [];

        public ChartValues<int> Performed { get; private set; } = [];

        public ChartValues<int> NotPerformed { get; private set; } = [];

        public double Count { get; private set; }

        public ICollection<TeamsWrapper> TeamValues { get; private set; } = [];

        public OverviewCountViewModel()
            : base()
        { }

        public void Refresh(IEnumerable<TrainingSessionViewModel> sessions)
        {
            Count = sessions.Count();
            Performed = [sessions.Count(x => x.IsPerformed)];
            Cancelled = [sessions.Count(x => x.IsCancelled)];
            NotPerformed = [sessions.Count(x => !x.IsPerformed && !x.IsCancelled)];

            TeamValues = sessions.GroupBy(x => x.Teams, TeamsComparer.Default)
                                      .OrderBy(x => x.Key.Count)
                                      .ThenBy(x => x.Key.Min(y => y.Order))
                                      .Select(x => new TeamsWrapper(x.Count(), x.Key))
                                      .ToList();
        }

        private sealed class TeamsComparer : IEqualityComparer<IReadOnlyCollection<TeamViewModel>>
        {
            public static TeamsComparer Default { get; } = new TeamsComparer();

            public bool Equals(IReadOnlyCollection<TeamViewModel>? x, IReadOnlyCollection<TeamViewModel>? y) => x?.OrderBy(x => x.Order).SequenceEqual(y?.OrderBy(x => x.Order).ToArray() ?? []) ?? false;

            public int GetHashCode([DisallowNull] IReadOnlyCollection<TeamViewModel> obj) => RuntimeHelpers.GetHashCode(this);
        }

        public class TeamsWrapper : Wrapper<int>
        {
            public string Title { get; }

            public TeamsWrapper(int item, IEnumerable<TeamViewModel> teams) : base(item) => Title = teams.OrderBy(x => x.Order).Humanize(", ") ?? string.Empty;
        }
    }
}
