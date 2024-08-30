// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using DynamicData;
using DynamicData.Binding;
using MyClub.Scorer.Wpf.Services.Providers.Base;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyClub.Scorer.Domain.ProjectAggregate;
using MyClub.Scorer.Domain.TeamAggregate;
using MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces;

namespace MyClub.Scorer.Wpf.Services.Providers
{
    internal sealed class TeamsProvider : ProjectEntitiesProvider<Team, ITeamViewModel>
    {
        public TeamsProvider(ProjectInfoProvider projectInfoProvider,
                             TeamPresentationService teamPresentationService,
                             PersonPresentationService personPresentationService,
                             StadiumsProvider stadiumsProvider)
            : base(projectInfoProvider, x => new TeamViewModel(x, teamPresentationService, personPresentationService, stadiumsProvider)) { }

        protected override IObservable<IChangeSet<Team, Guid>> ProvideProjectObservable(IProject project) => project.Teams.ToObservableChangeSet(x => x.Id);
    }
}
