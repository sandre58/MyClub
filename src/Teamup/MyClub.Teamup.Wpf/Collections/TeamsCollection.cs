// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using DynamicData;
using MyClub.Teamup.Wpf.Services.Providers;
using MyClub.Teamup.Wpf.ViewModels.Entities;
using MyNet.Observable.Collections;
using MyNet.Observable.Threading;

namespace MyClub.Teamup.Wpf.Collections
{
    internal class TeamsCollection : ExtendedCollection<TeamViewModel>
    {
        private static TeamsCollection _teams = null!;
        private static TeamsCollection _myTeams = null!;

        public static TeamsCollection All => _teams ?? throw new InvalidOperationException("TeamsCollection has not been initialized");

        public static TeamsCollection MyTeams => _myTeams ?? throw new InvalidOperationException("TeamsCollection has not been initialized");

        private TeamsCollection(IObservable<IChangeSet<TeamViewModel>> observable) : base(observable, Scheduler.UI)
            => SortingProperties.AscendingRange([nameof(TeamViewModel.Category), nameof(TeamViewModel.ShortName), nameof(TeamViewModel.Order)]);

        public static void Initialize(TeamsProvider teamsProvider)
        {
            _teams = new(teamsProvider.Connect());
            _myTeams = new(teamsProvider.ConnectMyTeams());
        }
    }
}
