// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using DynamicData;
using DynamicData.Binding;
using MyClub.Teamup.Domain.SendedMailAggregate;
using MyClub.Teamup.Domain.ProjectAggregate;
using MyClub.Teamup.Wpf.Services.Providers.Base;
using MyClub.Teamup.Wpf.ViewModels.Entities;

namespace MyClub.Teamup.Wpf.Services.Providers
{
    internal sealed class SendedMailsProvider : EntitiesProviderBase<SendedMail, SendedMailViewModel>
    {
        public SendedMailsProvider(ProjectInfoProvider projectInfoProvider) : base(projectInfoProvider, x => new(x)) { }

        protected override IObservable<IChangeSet<SendedMail, Guid>> ProvideObservable(Project project) => project.SendedMails.ToObservableChangeSet(x => x.Id);
    }
}
