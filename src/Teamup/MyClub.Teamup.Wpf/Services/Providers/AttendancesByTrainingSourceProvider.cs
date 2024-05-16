// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Reactive.Subjects;
using DynamicData.Binding;
using MyClub.Teamup.Wpf.Services.Providers.Base;
using MyClub.Teamup.Wpf.ViewModels.Entities;

namespace MyClub.Teamup.Wpf.Services.Providers
{
    internal class AttendancesByTrainingSourceProvider : ItemChangedSourceProvider<TrainingAttendanceViewModel, TrainingSessionViewModel>
    {
        public AttendancesByTrainingSourceProvider(Subject<TrainingSessionViewModel?> trainingSessionChanged)
            : base(trainingSessionChanged, x => x.Attendances.ToObservableChangeSet(x => x.Id))
        { }
    }
}
