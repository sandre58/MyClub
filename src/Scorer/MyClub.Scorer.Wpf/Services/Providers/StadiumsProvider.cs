// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using DynamicData;
using DynamicData.Binding;
using MyClub.Scorer.Domain.ProjectAggregate;
using MyClub.Scorer.Domain.StadiumAggregate;
using MyClub.Scorer.Wpf.Services.Providers.Base;
using MyClub.Scorer.Wpf.ViewModels.Entities;

namespace MyClub.Scorer.Wpf.Services.Providers
{
    internal sealed class StadiumsProvider : ProjectEntitiesProvider<Stadium, StadiumViewModel>
    {
        public StadiumsProvider(ProjectInfoProvider projectInfoProvider, StadiumPresentationService stadiumPresentationService) : base(projectInfoProvider, x => new StadiumViewModel(x, stadiumPresentationService)) { }

        protected override IObservable<IChangeSet<Stadium>> ProvideProjectObservable(IProject project) => project.Stadiums.ToObservableChangeSet();
    }
}
