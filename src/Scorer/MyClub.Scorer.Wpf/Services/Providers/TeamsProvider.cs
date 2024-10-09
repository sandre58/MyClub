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
using System.Collections.Generic;
using MyNet.Utilities;
using MyClub.Scorer.Domain.MatchAggregate;

namespace MyClub.Scorer.Wpf.Services.Providers
{
    internal sealed class TeamsProvider : ProjectEntitiesProvider<Team, TeamViewModel>
    {
        private readonly Dictionary<Guid, IVirtualTeamViewModel> _virtualTeams = [];

        public TeamsProvider(ProjectInfoProvider projectInfoProvider,
                             TeamPresentationService teamPresentationService,
                             PersonPresentationService personPresentationService,
                             StadiumsProvider stadiumsProvider)
            : base(projectInfoProvider, x => new TeamViewModel(x, teamPresentationService, personPresentationService, stadiumsProvider)) { }

        protected override IObservable<IChangeSet<Team>> ProvideProjectObservable(IProject project) => project.Teams.ToObservableChangeSet();

        protected override void Clear()
        {
            base.Clear();
            _virtualTeams.Clear();
        }

        public IVirtualTeamViewModel GetVirtualTeam(IVirtualTeam team)
            => team switch
            {
                Team t => GetOrThrow(t.Id),
                _ => _virtualTeams[team.Id],
            };

        public void RegisterVirtualTeam(IVirtualTeamViewModel team) => _virtualTeams.Add(team.Id, team);

        public void RemoveVirtualTeam(IVirtualTeamViewModel team) => _virtualTeams.Remove(team.Id);
    }
}
