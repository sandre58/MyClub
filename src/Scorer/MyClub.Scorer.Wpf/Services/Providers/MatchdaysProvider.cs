// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using DynamicData;
using DynamicData.Binding;
using MyClub.Scorer.Wpf.Services.Providers.Base;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyClub.Scorer.Domain.ProjectAggregate;

namespace MyClub.Scorer.Wpf.Services.Providers
{
    internal class MatchdaysProvider : EntitiesProviderBase<MatchdayViewModel>
    {
        private readonly CompetitionInfoProvider _competitionInfoProvider;

        public MatchdaysProvider(ProjectInfoProvider projectInfoProvider, CompetitionInfoProvider competitionInfoProvider) : base(projectInfoProvider) => _competitionInfoProvider = competitionInfoProvider;

        protected override IObservable<IChangeSet<MatchdayViewModel, Guid>> ProvideObservable(IProject project) => _competitionInfoProvider.GetCompetition<LeagueViewModel>().Matchdays.ToObservableChangeSet(x => x.Id);
    }
}
