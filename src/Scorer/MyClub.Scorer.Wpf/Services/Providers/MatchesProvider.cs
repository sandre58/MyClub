// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using DynamicData;
using MyClub.Scorer.Domain.ProjectAggregate;
using MyClub.Scorer.Wpf.Services.Providers.Base;
using MyClub.Scorer.Wpf.ViewModels.Entities;

namespace MyClub.Scorer.Wpf.Services.Providers
{
    internal class MatchesProvider : EntitiesProviderBase<MatchViewModel>
    {
        private readonly CompetitionInfoProvider _competitionInfoProvider;

        public MatchesProvider(ProjectInfoProvider projectInfoProvider, CompetitionInfoProvider competitionInfoProvider) : base(projectInfoProvider) => _competitionInfoProvider = competitionInfoProvider;

        protected override IObservable<IChangeSet<MatchViewModel>> ProvideObservable(IProject project) => _competitionInfoProvider.GetCompetition().ProvideMatches();
    }
}
