// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using DynamicData;
using MyNet.Utilities.Messaging;
using MyClub.Teamup.Application.Messages;
using MyClub.Teamup.Domain.ProjectAggregate;
using MyClub.Teamup.Domain.StadiumAggregate;
using MyClub.Teamup.Wpf.Services.Providers.Base;
using MyClub.Teamup.Wpf.ViewModels.Entities;

namespace MyClub.Teamup.Wpf.Services.Providers
{
    internal sealed class StadiumsProvider : EntitiesProviderBase<Stadium, StadiumViewModel>
    {
        private readonly IStadiumRepository _stadiumRepository;
        public StadiumsProvider(ProjectInfoProvider projectInfoProvider, IStadiumRepository stadiumRepository, StadiumPresentationService stadiumPresentationService) : base(projectInfoProvider, x => new(x, stadiumPresentationService))
        {
            _stadiumRepository = stadiumRepository;
            Messenger.Default.Register<StadiumsChangedMessage>(this, _ => Reload(CurrentProject));
        }

        protected override IObservable<IChangeSet<Stadium, Guid>> ProvideObservable(Project project)
            => _stadiumRepository.GetAll().AsObservableChangeSet(x => x.Id);
    }
}
