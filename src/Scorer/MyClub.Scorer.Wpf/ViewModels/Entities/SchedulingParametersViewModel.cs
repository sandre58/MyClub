// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyClub.Scorer.Domain.Scheduling;
using MyNet.Observable;

namespace MyClub.Scorer.Wpf.ViewModels.Entities
{
    public class SchedulingParametersViewModel : ObservableObject
    {
        public SchedulingParametersViewModel(IObservable<SchedulingParameters?> observable)
        => Disposables.Add(observable.Subscribe(x =>
            {
                StartDate = x is not null ? x.StartDate : SchedulingParameters.Default.StartDate;
                EndDate = x is not null ? x.EndDate : SchedulingParameters.Default.EndDate;
                StartTime = x is not null ? x.StartTime : SchedulingParameters.Default.StartTime;
                RotationTime = x is not null ? x.RotationTime : SchedulingParameters.Default.RotationTime;
                UseHomeVenue = x is not null ? x.UseHomeVenue : SchedulingParameters.Default.UseHomeVenue;
                RestTime = x is not null ? x.RestTime : SchedulingParameters.Default.RestTime;
                AsSoonAsPossible = x is not null ? x.AsSoonAsPossible : SchedulingParameters.Default.AsSoonAsPossible;
            }));

        public DateOnly StartDate { get; private set; }

        public DateOnly EndDate { get; private set; }

        public TimeOnly StartTime { get; private set; }

        public TimeSpan RotationTime { get; private set; }

        public TimeSpan RestTime { get; private set; }

        public bool UseHomeVenue { get; private set; }

        public bool AsSoonAsPossible { get; private set; }
    }
}
