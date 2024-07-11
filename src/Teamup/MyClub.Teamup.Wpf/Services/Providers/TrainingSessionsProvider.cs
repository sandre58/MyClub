// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using DynamicData;
using DynamicData.Binding;
using MyNet.Utilities;
using MyClub.Teamup.Application.Deferrers;
using MyClub.Teamup.Domain.ProjectAggregate;
using MyClub.Teamup.Domain.TrainingAggregate;
using MyClub.Teamup.Wpf.Services.Providers.Base;
using MyClub.Teamup.Wpf.ViewModels.Entities;

namespace MyClub.Teamup.Wpf.Services.Providers
{
    internal sealed class TrainingSessionsProvider : EntitiesProviderBase<TrainingSession, TrainingSessionViewModel>
    {
        private readonly PlayersProvider _playersProvider;

        public TrainingSessionsProvider(ProjectInfoProvider projectInfoProvider,
                                 PlayersProvider playersProvider,
                                 TrainingSessionPresentationService trainingSessionPresentationService,
                                 HolidaysProvider holidaysProvider,
                                 CyclesProvider cyclesProvider,
                                 TrainingStatisticsRefreshDeferrer trainingStatisticsRefreshDeferrer)
            : base(projectInfoProvider, x => new(x, trainingSessionPresentationService, playersProvider, holidaysProvider, cyclesProvider))
        {
            _playersProvider = playersProvider;
            trainingStatisticsRefreshDeferrer.Subscribe(this, () => _playersProvider.Items.ForEach(x => x.TrainingStatistics.Refresh(Items)));
        }

        protected override IObservable<IChangeSet<TrainingSession, Guid>> ProvideObservable(Project project) => project.TrainingSessions.ToObservableChangeSet(x => x.Id);
    }
}
