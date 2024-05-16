// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyNet.UI.Locators;
using MyNet.Utilities;
using MyClub.Teamup.Application.Services;
using MyClub.Teamup.Wpf.ViewModels.Edition;
using MyClub.Teamup.Wpf.ViewModels.Entities;

namespace MyClub.Teamup.Wpf.Services
{
    internal class HolidaysPresentationService(HolidaysService service, IViewModelLocator viewModelLocator) : PresentationServiceBase<HolidaysViewModel, HolidaysEditionViewModel, HolidaysService>(service, viewModelLocator)
    {
        public async Task<Guid?> AddAsync(DateTime? startDate, DateTime? endDate)
            => await AddAsync(x =>
            {
                if (startDate.HasValue)
                    x.StartDate = startDate.Value.BeginningOfDay();
                if (endDate.HasValue)
                    x.EndDate = endDate.Value.EndOfDay();
            }).ConfigureAwait(false);

        public int Remove(IEnumerable<HolidaysViewModel> oldItems) => !oldItems.Any() ? 0 : Service.Remove(oldItems.Select(x => x.Id).ToList());
    }
}
