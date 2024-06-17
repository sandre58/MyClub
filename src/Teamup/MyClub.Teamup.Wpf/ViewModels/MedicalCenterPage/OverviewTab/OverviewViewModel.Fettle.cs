// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MyClub.Domain.Enums;
using MyClub.Teamup.Wpf.ViewModels.Entities;
using MyNet.Observable;
using MyNet.UI.Collections;
using MyNet.Utilities;

namespace MyClub.Teamup.Wpf.ViewModels.MedicalCenterPage.OverviewTab
{
    internal class OverviewFettleViewModel : ObservableObject
    {
        public int NotInjured { get; private set; }

        public int Severe { get; private set; }

        public int Moderate { get; private set; }

        public int Serious { get; private set; }

        public int Minor { get; private set; }

        private readonly UiObservableCollection<PlayerInjuryStatisticsViewModel> _mostUnaivalableOnLast12Months = [];
        private readonly UiObservableCollection<PlayerInjuryStatisticsViewModel> _leastUnaivalableOnLast12Months = [];

        public ReadOnlyObservableCollection<PlayerInjuryStatisticsViewModel> MostUnaivalableOnLast12Months { get; }
        public ReadOnlyObservableCollection<PlayerInjuryStatisticsViewModel> LeastUnaivalableOnLast12Months { get; }

        public int CountItems { get; }

        public OverviewFettleViewModel(int countItems)
            : base()
        {
            CountItems = countItems;
            MostUnaivalableOnLast12Months = new(_mostUnaivalableOnLast12Months);
            LeastUnaivalableOnLast12Months = new(_leastUnaivalableOnLast12Months);
        }

        public void Refresh(IEnumerable<PlayerInjuryStatisticsViewModel> players)
        {
            NotInjured = players.Count(x => !x.Player.IsInjured);
            Severe = players.Count(x => x.Player.Injury is not null && x.Player.Injury.Severity == InjurySeverity.Severe);
            Serious = players.Count(x => x.Player.Injury is not null && x.Player.Injury.Severity == InjurySeverity.Serious);
            Minor = players.Count(x => x.Player.Injury is not null && x.Player.Injury.Severity == InjurySeverity.Minor);
            Moderate = players.Count(x => x.Player.Injury is not null && x.Player.Injury.Severity == InjurySeverity.Moderate);

            _mostUnaivalableOnLast12Months.Set(players.OrderByDescending(x => x.UnaivalableDurationInDaysInLast12Months.Sum).Take(CountItems));
            _leastUnaivalableOnLast12Months.Set(players.OrderBy(x => x.UnaivalableDurationInDaysInLast12Months.Sum).Take(CountItems));
        }
    }
}
