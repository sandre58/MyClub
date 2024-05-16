// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using MyNet.UI.Commands;
using MyNet.UI.ViewModels.List;
using MyNet.Wpf.Schedulers;
using MyNet.Utilities;
using MyNet.Observable;
using MyClub.Teamup.Wpf.Services;
using MyClub.Teamup.Wpf.Services.Providers;
using MyClub.Teamup.Wpf.ViewModels.Entities;

namespace MyClub.Teamup.Wpf.ViewModels.HomePage.DashboardContent
{
    internal class OverviewCalendarViewModel : ListViewModel<IAppointment>
    {
        private readonly ReadOnlyObservableCollection<HolidaysViewModel> _holidays;

        public DateTime StartDate { get; private set; }

        public DateTime EndDate { get; private set; }

        public DateTime DisplayDate { get; set; } = DateTime.Today;

        public ICommand AddToDateCommand { get; }

        public ICommand NavigateToCalendarCommand { get; }

        public ReadOnlyObservableCollection<HolidaysViewModel> Holidays => _holidays;

        public OverviewCalendarViewModel(MainItemsProvider mainItemsProvider,
                                         HolidaysProvider holidaysProvider,
                                         TrainingSessionPresentationService trainingSessionPresentationService)
            : base(mainItemsProvider.ConnectEvents())
        {
            NavigateToCalendarCommand = CommandsManager.Create(NavigationCommandsService.NavigateToCalendarPage);
            AddToDateCommand = CommandsManager.CreateNotNull<DateTime>(async x => await trainingSessionPresentationService.AddAsync(x, null));

            Disposables.AddRange(
            [
                holidaysProvider.Items.ToObservableChangeSet(x => x.Id)
                                      .AutoRefreshOnObservable(x => x.WhenAnyPropertyChanged(nameof(HolidaysViewModel.StartDate), nameof(HolidaysViewModel.EndDate)))
                                      .ObserveOn(WpfScheduler.Current)
                                      .Bind(out _holidays)
                                      .Subscribe(_ => RaisePropertyChanged(nameof(Holidays)))
            ]);
        }
    }
}
