// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using DynamicData;
using DynamicData.Binding;
using MyClub.Scorer.Wpf.Services.Providers.Base;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyClub.Scorer.Domain.ProjectAggregate;
using MyClub.Scorer.Domain.StadiumAggregate;

namespace MyClub.Scorer.Wpf.Services.Providers
{
    internal sealed class StadiumsProvider : ProjectEntitiesProvider<Stadium, StadiumViewModel>
    {
        public StadiumsProvider(ProjectInfoProvider projectInfoProvider, StadiumPresentationService stadiumPresentationService) : base(projectInfoProvider, x => new(x, stadiumPresentationService)) { }

        protected override IObservable<IChangeSet<Stadium, Guid>> ProvideProjectObservable(IProject project) => project.Stadiums.ToObservableChangeSet(x => x.Id);
    }
}
