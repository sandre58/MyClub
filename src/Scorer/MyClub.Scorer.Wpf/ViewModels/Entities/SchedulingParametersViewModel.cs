// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyClub.Scorer.Domain.ProjectAggregate;
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
                UseTeamVenues = x is not null ? x.UseTeamVenues : SchedulingParameters.Default.UseTeamVenues;
                RestTime = x is not null ? x.RestTime : SchedulingParameters.Default.RestTime;
            }));

        public DateTime StartDate { get; private set; }

        public DateTime EndDate { get; private set; }

        public TimeSpan StartTime { get; private set; }

        public TimeSpan RotationTime { get; private set; }

        public TimeSpan RestTime { get; private set; }

        public bool UseTeamVenues { get; private set; }
    }
}
