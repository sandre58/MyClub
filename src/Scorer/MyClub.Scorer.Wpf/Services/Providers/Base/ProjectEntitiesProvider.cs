// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using DynamicData;
using MyClub.Domain;
using MyClub.Scorer.Domain.ProjectAggregate;
using MyNet.Utilities;

namespace MyClub.Scorer.Wpf.Services.Providers.Base
{
    internal abstract class ProjectEntitiesProvider<T, TViewModel> : EntitiesProviderBase<TViewModel>
        where T : IEntity
        where TViewModel : IIdentifiable<Guid>, INotifyPropertyChanged
    {
        private readonly Func<T, TViewModel> _createViewModel;

        protected ProjectEntitiesProvider(ProjectInfoProvider projectInfoProvider, Func<T, TViewModel> createViewModel) : base(projectInfoProvider) => _createViewModel = createViewModel;

        protected override IObservable<IChangeSet<TViewModel>> ProvideObservable(IProject project) => ProvideProjectObservable(project).Transform(_createViewModel);

        protected abstract IObservable<IChangeSet<T>> ProvideProjectObservable(IProject project);
    }
}
