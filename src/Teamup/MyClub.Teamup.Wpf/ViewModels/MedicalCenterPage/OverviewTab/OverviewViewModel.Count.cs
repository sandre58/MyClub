// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using LiveCharts;
using LiveCharts.Configurations;
using MyNet.Wpf.LiveCharts;
using MyNet.Utilities;
using MyNet.Utilities.Helpers;
using MyNet.Humanizer;
using MyNet.Observable;
using MyNet.Observable.Attributes;
using MyNet.Observable.Translatables;
using MyNet.Utilities.Units;
using MyClub.Domain.Enums;
using MyClub.Teamup.Wpf.ViewModels.Entities;

namespace MyClub.Teamup.Wpf.ViewModels.MedicalCenterPage.OverviewTab
{
    internal class OverviewCountViewModel : ObservableObject
    {
        private IList<Func<string>> _months = [];

        public ChartValues<int> Severe { get; private set; } = [];

        public ChartValues<int> Serious { get; private set; } = [];

        public ChartValues<int> Moderate { get; private set; } = [];

        public ChartValues<int> Minor { get; private set; } = [];

        public ChartValues<int> Musculars { get; private set; } = [];

        public ChartValues<int> Traumas { get; private set; } = [];

        public ChartValues<int> Others { get; private set; } = [];

        public ChartValues<int> Ligaments { get; private set; } = [];

        public ChartValues<int> Fractures { get; private set; } = [];

        public ChartValues<int> Sicknesses { get; private set; } = [];

        public double Count { get; private set; }

        public double MaxCountInjuriesByMonth { get; private set; }

        [UpdateOnCultureChanged]
        public IList<string> MonthLabels => GetMonthLabels();

        public ChartSerie<int> InjuriesByMonths { get; private set; }

        public IDictionary<InjuryType, int> InjuriesByTypes { get; private set; }

        public OverviewCountViewModel()
            : base()
        {
            InjuriesByMonths = new ChartSerie<int>(Mappers.Xy<int>().X((value, index) => index).Y(value => value));
            InjuriesByTypes = Enum.GetValues<InjuryType>().ToDictionary(x => x, x => 0);
        }

        public void Refresh(IEnumerable<InjuryViewModel> injuries)
        {
            Count = injuries.Count();
            Severe = [injuries.Count(x => x.Severity == InjurySeverity.Severe)];
            Serious = [injuries.Count(x => x.Severity == InjurySeverity.Serious)];
            Moderate = [injuries.Count(x => x.Severity == InjurySeverity.Moderate)];
            Minor = [injuries.Count(x => x.Severity == InjurySeverity.Minor)];

            Traumas = [injuries.Count(x => x.Category == InjuryCategory.Trauma)];
            Musculars = [injuries.Count(x => x.Category == InjuryCategory.Muscular)];
            Ligaments = [injuries.Count(x => x.Category == InjuryCategory.Ligament)];
            Others = [injuries.Count(x => x.Category == InjuryCategory.Other)];
            Fractures = [injuries.Count(x => x.Category == InjuryCategory.Fracture)];
            Sicknesses = [injuries.Count(x => x.Category == InjuryCategory.Sickness)];

            var months = DateTimeHelper.Range(DateTime.Today.AddMonths(-12), DateTime.Today, 1, TimeUnit.Month).ToList();
            var injuriesByMonth = months.ToDictionary(x => x, x => injuries.Count(y => y.Date.SameMonth(x)));

            InjuriesByMonths.Update(injuriesByMonth.Values);
            InjuriesByTypes = Enum.GetValues<InjuryType>().ToDictionary(x => x, x => injuries.Count(y => y.Type == x && y.Date.IsAfter(DateTime.Now.AddMonths(-12))));
            _months = injuriesByMonth.Keys.Select(x => new DisplayWrapper<DateTime>(x, new TranslatableObject<string>(() => ToMonthFormat(x))))
                                        .Select(x => new Func<string>(() => x.DisplayName.Value.OrEmpty()))
                                        .ToList();
            MaxCountInjuriesByMonth = Math.Max(injuriesByMonth.Values.Max(), 5);
        }

        private static string ToMonthFormat(DateTime x)
            => x.ToString("MMM", CultureInfo.CurrentCulture)[..2].ToTitle() + "."
            + (x.Month == 1 || x.Month == DateTime.Today.Month ? $"\n{x.ToString("yy", CultureInfo.CurrentCulture)}" : "");

        private List<string> GetMonthLabels() => _months.Select(x => x.Invoke()).ToList();
    }
}
