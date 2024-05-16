// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using DynamicData;
using DynamicData.Binding;
using MyClub.Teamup.Domain.SquadAggregate;
using MyClub.Teamup.Domain.ProjectAggregate;
using MyClub.Teamup.Wpf.Services.Providers.Base;
using MyClub.Teamup.Wpf.ViewModels.Entities;

namespace MyClub.Teamup.Wpf.Services.Providers
{
    internal sealed class PlayersProvider : EntitiesProviderBase<SquadPlayer, PlayerViewModel>
    {
        public PlayersProvider(ProjectInfoProvider projectInfoProvider, PlayerPresentationService playerPresentationService)
            : base(projectInfoProvider, x => new(x, playerPresentationService)) { }

        protected override IObservable<IChangeSet<SquadPlayer, Guid>> ProvideObservable(Project project) => project.Players.ToObservableChangeSet(x => x.Id);

        public PlayerViewModel GetByPlayerIdOrThrow(Guid id) => Items.FirstOrDefault(x => x.PlayerId == id) ?? throw new ArgumentNullException(nameof(id));
    }
}
