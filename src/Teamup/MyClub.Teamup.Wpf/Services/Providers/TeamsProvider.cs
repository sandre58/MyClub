// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using MyNet.Utilities;
using MyNet.DynamicData.Extensions;
using MyNet.Utilities.Messaging;
using MyClub.Teamup.Domain.Extensions;
using MyClub.Teamup.Domain.ProjectAggregate;
using MyClub.Teamup.Domain.TeamAggregate;
using MyClub.Teamup.Wpf.Messages;
using MyClub.Teamup.Wpf.Services.Providers.Base;
using MyClub.Teamup.Wpf.ViewModels.Entities;

namespace MyClub.Teamup.Wpf.Services.Providers
{
    internal sealed class TeamsProvider : EntitiesProviderBase<Team, TeamViewModel>
    {
        public TeamsProvider(ProjectInfoProvider projectInfoProvider, StadiumsProvider stadiumsProvider, TeamPresentationService teamPresentationService)
            : base(projectInfoProvider, x => new(x, stadiumsProvider, teamPresentationService, Equals(projectInfoProvider.GetCurrentProject()?.Club, x.Club)))
        {
            projectInfoProvider.WhenProjectChanged(x => SetMainTeams((x?.GetMainTeams() ?? []).Select(x => x.Id)));
            Messenger.Default.Register<MainTeamChangedMessage>(this, OnMainTeamChanged);
        }

        protected override IObservable<IChangeSet<Team, Guid>> ProvideObservable(Project project)
            => project.Club.Teams.ToObservableChangeSet(x => x.Id).Merge(project.Competitions.ToObservableChangeSet(x => x.Id).MergeManyEx(x => x.Teams.ToObservableChangeSet(x => x.Id).Filter(x => x.Club != project.Club), y => y.Id));

        internal IObservable<IChangeSet<TeamViewModel>> ConnectMyTeams() => Connect().Filter(x => x.IsMyTeam);

        private void OnMainTeamChanged(MainTeamChangedMessage message) => SetMainTeams(message.MainTeams.Select(x => x.Id));

        private void SetMainTeams(IEnumerable<Guid> mainTeamIds) => Items.Where(x => x.IsMyTeam).ForEach(x => x.SetMainTeam(mainTeamIds.Contains(x.Id)));
    }
}
