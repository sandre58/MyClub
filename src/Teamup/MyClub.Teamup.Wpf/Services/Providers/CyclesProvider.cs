// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using DynamicData;
using DynamicData.Binding;
using MyClub.Teamup.Domain.CycleAggregate;
using MyClub.Teamup.Domain.ProjectAggregate;
using MyClub.Teamup.Wpf.Services.Providers.Base;
using MyClub.Teamup.Wpf.ViewModels.Entities;

namespace MyClub.Teamup.Wpf.Services.Providers
{
    internal sealed class CyclesProvider : EntitiesProviderBase<Cycle, CycleViewModel>
    {
        public CyclesProvider(ProjectInfoProvider projectInfoProvider, CyclePresentationService cyclePresentationService) : base(projectInfoProvider, x => new(x, cyclePresentationService)) { }

        protected override IObservable<IChangeSet<Cycle, Guid>> ProvideObservable(Project project) => project.Cycles.ToObservableChangeSet(x => x.Id);
    }
}
