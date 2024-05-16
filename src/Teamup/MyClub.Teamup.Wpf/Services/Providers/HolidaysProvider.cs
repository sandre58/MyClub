// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using DynamicData;
using DynamicData.Binding;
using MyClub.Teamup.Domain.HolidaysAggregate;
using MyClub.Teamup.Domain.ProjectAggregate;
using MyClub.Teamup.Wpf.Services.Providers.Base;
using MyClub.Teamup.Wpf.ViewModels.Entities;

namespace MyClub.Teamup.Wpf.Services.Providers
{
    internal sealed class HolidaysProvider : EntitiesProviderBase<Holidays, HolidaysViewModel>
    {
        public HolidaysProvider(ProjectInfoProvider projectInfoProvider, HolidaysPresentationService holidaysPresentationService) : base(projectInfoProvider, x => new(x, holidaysPresentationService)) { }

        protected override IObservable<IChangeSet<Holidays, Guid>> ProvideObservable(Project project) => project.Holidays.ToObservableChangeSet(x => x.Id);
    }
}
