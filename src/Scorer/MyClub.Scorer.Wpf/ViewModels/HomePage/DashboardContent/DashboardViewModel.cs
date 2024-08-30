// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using DynamicData.Binding;
using MyNet.Utilities;
using MyNet.Observable;
using MyClub.Scorer.Wpf.Services.Providers;

namespace MyClub.Scorer.Wpf.ViewModels.HomePage.DashboardContent
{
    internal class DashboardViewModel : ObservableObject
    {
        public DashboardViewModel(ProjectInfoProvider projectInfoProvider, MatchesProvider matchesProvider, TeamsProvider teamsProvider, StadiumsProvider stadiumsProvider)
        {
            CalendarViewModel = new(matchesProvider);

            Disposables.AddRange(
            [
                projectInfoProvider.WhenPropertyChanged(x => x.Name).Subscribe(x => Name = x.Value),
                projectInfoProvider.WhenPropertyChanged(x => x.Image).Subscribe(x => Image = x.Value),
                teamsProvider.Connect().Subscribe(_ => CountTeams = teamsProvider.Count),
                stadiumsProvider.Connect().Subscribe(_ => CountStadiums = stadiumsProvider.Count),
            ]);
        }

        public string? Name { get; private set; }

        public byte[]? Image { get; private set; }

        public int CountTeams { get; private set; }

        public int CountStadiums { get; set; }

        public OverviewCalendarViewModel CalendarViewModel { get; set; }

        protected override void Cleanup()
        {
            base.Cleanup();
            CalendarViewModel.Dispose();
        }
    }
}
