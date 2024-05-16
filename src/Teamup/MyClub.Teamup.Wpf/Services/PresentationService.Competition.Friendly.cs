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
    internal class FriendlyPresentationService(CompetitionService service, IViewModelLocator viewModelLocator) : PresentationServiceBase<FriendlyViewModel, FriendlyEditionViewModel, CompetitionService>(service, viewModelLocator)
    {
        public async Task<Guid?> DuplicateAsync(FriendlyViewModel item)
            => await AddAsync(x =>
            {
                var itemToDuplicated = Service.GetById(item.Id);

                if (itemToDuplicated is not null)
                {
                    x.Name = itemToDuplicated.Competition.Name.Increment(Service.GetAll().Select(x => x.Competition.Name), format: " (#)");
                    x.ShortName = itemToDuplicated.Competition.ShortName;
                    x.StartDate = itemToDuplicated.Period.Start;
                    x.EndDate = itemToDuplicated.Period.End;
                    x.MatchTime = itemToDuplicated.Rules.MatchTime;
                    x.MatchFormat.Load(itemToDuplicated.Rules.MatchFormat);
                }
            }).ConfigureAwait(false);

        public int Remove(IEnumerable<FriendlyViewModel> oldItems) => !oldItems.Any() ? 0 : Service.Remove(oldItems.Select(x => x.Id).ToList());
    }
}
