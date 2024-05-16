// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using DynamicData;
using DynamicData.Binding;
using MyClub.Scorer.Wpf.Services.Providers.Base;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyClub.Scorer.Domain.ProjectAggregate;
using MyClub.Scorer.Domain.TeamAggregate;

namespace MyClub.Scorer.Wpf.Services.Providers
{
    internal sealed class TeamsProvider : ProjectEntitiesProvider<Team, TeamViewModel>
    {
        public TeamsProvider(ProjectInfoProvider projectInfoProvider,
                             TeamPresentationService teamPresentationService,
                             PlayerPresentationService playerPresentationService,
                             StadiumsProvider stadiumsProvider)
            : base(projectInfoProvider, x => new(x, teamPresentationService, playerPresentationService, stadiumsProvider)) { }

        protected override IObservable<IChangeSet<Team, Guid>> ProvideProjectObservable(IProject project) => project.Teams.ToObservableChangeSet(x => x.Id);
    }
}
