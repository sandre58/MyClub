// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyNet.UI.Dialogs;
using MyNet.UI.Locators;
using MyNet.UI.Extensions;
using MyNet.UI.Services;
using MyNet.Utilities;
using MyClub.Teamup.Application.Dtos;
using MyClub.Teamup.Application.Services;
using MyClub.Teamup.Wpf.ViewModels.Edition;
using MyClub.Teamup.Wpf.ViewModels.Entities;
using MyClub.Teamup.Wpf.ViewModels.Entities.Interfaces;

namespace MyClub.Teamup.Wpf.Services
{
    internal class MatchdayPresentationService(MatchdayService service, IViewModelLocator viewModelLocator)
        : PresentationServiceBase<MatchdayViewModel, MatchdayEditionViewModel, MatchdayService>(service, viewModelLocator)
    {
        public override async Task EditAsync(MatchdayViewModel matchday)
        {
            var vm = ViewModelLocator.Get<MatchdayEditionViewModel>();
            vm.Load(matchday.Parent, matchday.Id);

            _ = await DialogManager.ShowDialogAsync(vm).ConfigureAwait(false);
        }

        public async Task<IEnumerable<Guid>?> DuplicateAsync(MatchdayViewModel matchday) => await AddMultipleAsync(matchday.Parent, new[] { matchday }, []).ConfigureAwait(false);

        public async Task<IMatchdayViewModel?> AddAsync(IMatchdayParent parent, DateTime? date = null)
        {
            var vm = ViewModelLocator.Get<MatchdayEditionViewModel>();
            vm.New(parent, () => date.IfNotNull(x => vm.Date = x));

            var result = await DialogManager.ShowDialogAsync(vm).ConfigureAwait(false);

            return result.HasValue && result.Value ? parent.GetAllMatchdays().FirstOrDefault(x => x.Id == vm.ItemId) : null;
        }

        public async Task<IEnumerable<Guid>?> AddMultipleAsync(IMatchdayParent parent) => await AddMultipleAsync(parent, []).ConfigureAwait(false);

        public async Task<IEnumerable<Guid>?> AddMultipleAsync(IMatchdayParent parent, IEnumerable<DateTime> dates) => await AddMultipleAsync(parent, Array.Empty<MatchdayViewModel>(), dates).ConfigureAwait(false);

        public async Task<IEnumerable<Guid>?> AddMultipleAsync(IMatchdayParent parent, IEnumerable<IMatchdayViewModel> duplicatedMatchdays, IEnumerable<DateTime> dates)
        {
            var vm = new MatchdaysAddViewModel(parent);
            vm.Load(duplicatedMatchdays, dates);

            var result = await DialogManager.ShowDialogAsync(vm).ConfigureAwait(false);

            return result.IsTrue()
                ? Service.Save(vm.Matchdays.Select(x => new MatchdayDto
                {
                    ParentId = parent.Id,
                    DuplicatedMatchdayId = x.DuplicatedMatchday?.Id,
                    InvertTeams = x.InvertTeams,
                    Name = x.Name,
                    Date = (x.Date ?? DateTime.Today).ToUtc(x.Time),
                    ShortName = x.ShortName
                }).ToList(), false).Select(x => x.Id)
                : null;
        }

        public async Task PostponeAsync(MatchdayViewModel item, DateTime? postponedDate = null) => await PostponeAsync([item], postponedDate).ConfigureAwait(false);

        public async Task PostponeAsync(IEnumerable<MatchdayViewModel> items, DateTime? postponedDate = null)
        {
            var idsList = items.Select(x => x.Id).ToList();

            if (idsList.Count == 0) return;

            await AppBusyManager.WaitAsync(() => Service.Postpone(items.Select(x => x.Id), postponedDate)).ConfigureAwait(false);
        }
    }
}
