// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using MyClub.Domain.Enums;
using MyNet.Observable;
using MyNet.Observable.Statistics;
using MyNet.Observable.Threading;
using MyNet.Utilities;
using MyNet.Utilities.Helpers;

namespace MyClub.Teamup.Wpf.ViewModels.Entities
{
    internal class PlayerInjuryStatisticsViewModel : ObservableObject, IIdentifiable<Guid>
    {
        private readonly ReadOnlyObservableCollection<InjuryViewModel> _injuries;

        public PlayerViewModel Player { get; }

        public Guid Id => Player.Id;

        public RangeStatistics<InjuryViewModel> UnaivalableDurationInDays { get; }

        public RangeStatistics<InjuryViewModel> UnaivalableDurationInDaysInLast12Months { get; }

        public CountStatistics<InjuryViewModel> Musculars { get; }

        public CountStatistics<InjuryViewModel> Traumas { get; }

        public CountStatistics<InjuryViewModel> Fractures { get; }

        public CountStatistics<InjuryViewModel> Sicknesses { get; }

        public CountStatistics<InjuryViewModel> Others { get; }

        public CountStatistics<InjuryViewModel> Ligaments { get; }

        public ReadOnlyObservableCollection<InjuryViewModel> Injuries => _injuries;

        public PlayerInjuryStatisticsViewModel(PlayerViewModel player, IObservable<Func<InjuryViewModel, bool>>? predicateChanged = null)
        {
            Player = player;

            var obs = player.Injuries.ToObservableChangeSet(x => x.Id);

            if (predicateChanged is not null)
                obs = obs.Filter(predicateChanged);

            Disposables.Add(obs.ObserveOn(Scheduler.UI).Bind(out _injuries).Subscribe());

            UnaivalableDurationInDaysInLast12Months = new(_injuries, x => true, x => !x.EndDate.HasValue || x.EndDate.Value > DateTime.Now.AddYears(-1)
                                                                                        ? Math.Round(((x.EndDate ?? DateTime.Now) - DateTimeHelper.Max(x.Date, DateTime.Now.AddYears(-1))).TotalDays, 0)
                                                                                        : 0,
                                                                                        x => x.WhenAnyPropertyChanged(nameof(InjuryViewModel.Duration))!);
            UnaivalableDurationInDays = new(_injuries, x => true, x => x.Duration.TotalDays, x => x.WhenAnyPropertyChanged(nameof(InjuryViewModel.Duration))!);
            Musculars = new(_injuries, x => x.Category == InjuryCategory.Muscular, x => x.WhenPropertyChanged(y => y.Category));
            Traumas = new(_injuries, x => x.Category == InjuryCategory.Trauma, x => x.WhenPropertyChanged(y => y.Category));
            Fractures = new(_injuries, x => x.Category == InjuryCategory.Fracture, x => x.WhenPropertyChanged(y => y.Category));
            Sicknesses = new(_injuries, x => x.Category == InjuryCategory.Sickness, x => x.WhenPropertyChanged(y => y.Category));
            Others = new(_injuries, x => x.Category == InjuryCategory.Other, x => x.WhenPropertyChanged(y => y.Category));
            Ligaments = new(_injuries, x => x.Category == InjuryCategory.Ligament, x => x.WhenPropertyChanged(y => y.Category));
        }
    }
}
