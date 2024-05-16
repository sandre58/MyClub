// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using DynamicData;
using DynamicData.Binding;
using MyClub.Teamup.Domain.ProjectAggregate;
using MyClub.Teamup.Domain.TacticAggregate;
using MyClub.Teamup.Wpf.Services.Providers.Base;
using MyClub.Teamup.Wpf.ViewModels.Entities;

namespace MyClub.Teamup.Wpf.Services.Providers
{
    internal sealed class TacticsProvider : EntitiesProviderBase<Tactic, TacticViewModel>
    {
        public TacticsProvider(ProjectInfoProvider projectInfoProvider, TacticPresentationService tacticPresentationService) : base(projectInfoProvider, x => new(x, tacticPresentationService)) { }

        protected override IObservable<IChangeSet<Tactic, Guid>> ProvideObservable(Project project) => project.Tactics.ToObservableChangeSet(x => x.Id);
    }
}
