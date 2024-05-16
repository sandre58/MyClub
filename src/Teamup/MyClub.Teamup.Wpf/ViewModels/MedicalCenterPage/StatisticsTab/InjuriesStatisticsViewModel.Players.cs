// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.ObjectModel;
using DynamicData;
using DynamicData.Binding;
using MyNet.UI.ViewModels.List;
using MyClub.Teamup.Wpf.ViewModels.Entities;

namespace MyClub.Teamup.Wpf.ViewModels.MedicalCenterPage.StatisticsTab
{
    internal class InjuriesStatisticsPlayersViewModel : ListViewModel<PlayerInjuryStatisticsViewModel>
    {
        public InjuriesStatisticsPlayersViewModel(ReadOnlyObservableCollection<PlayerViewModel> players, IObservable<Func<InjuryViewModel, bool>>? predicateChanged = null)
            : base(source: players.ToObservableChangeSet().Transform(x => new PlayerInjuryStatisticsViewModel(x, predicateChanged)).DisposeMany(),
                  parametersProvider: new InjuryPlayerStatisticsListParametersProvider())
        { }
    }
}
