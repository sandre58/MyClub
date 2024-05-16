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
using MyNet.UI.Toasting;
using MyNet.Utilities;
using MyNet.Humanizer;
using MyNet.UI.Resources;
using MyClub.Teamup.Application.Services;
using MyClub.Teamup.Domain.Enums;
using MyClub.CrossCutting.Localization;
using MyClub.Teamup.Wpf.ViewModels.Edition;
using MyClub.Teamup.Wpf.ViewModels.Entities;

namespace MyClub.Teamup.Wpf.Services
{
    internal class TrainingSessionPresentationService(TrainingSessionService service, IViewModelLocator viewModelLocator) : PresentationServiceBase<TrainingSessionViewModel, TrainingSessionEditionViewModel, TrainingSessionService>(service, viewModelLocator)
    {
        public async Task<Guid?> AddAsync(DateTime? date = null, Guid? teamId = null)
            => await AddAsync(x =>
            {
                if (date.HasValue)
                    x.Date = date.Value.Date;

                if (teamId.HasValue)
                    x.SelectedTeamIds = new[] { teamId.Value };
            }).ConfigureAwait(false);

        public async Task AddMultipleAsync(IEnumerable<DateTime> dates)
        {
            var vm = ViewModelLocator.Get<TrainingSessionsAddViewModel>();

            vm.Reset();
            vm.SetDates(dates);

            _ = await DialogManager.ShowDialogAsync(vm).ConfigureAwait(false);
        }

        public async Task EditMultipleAsync(IEnumerable<TrainingSessionViewModel> sessions)
        {
            var vm = ViewModelLocator.Get<TrainingSessionsEditionViewModel>();

            vm.Load(sessions);

            _ = await DialogManager.ShowDialogAsync(vm).ConfigureAwait(false);
        }

        public async Task CancelAsync(IEnumerable<TrainingSessionViewModel> items)
        {
            var idsList = items.Where(x => !x.IsCancelled).Select(x => x.Id).ToList();
            if (idsList.Count == 0) return;

            await AppBusyManager.WaitAsync(() =>
            {
                Service.Cancel(idsList);

                ToasterManager.ShowSuccess(nameof(MyClubResources.XTrainingsHasBeenCancelledSuccess).TranslateWithCountAndOptionalFormat(idsList.Count));
            }).ConfigureAwait(false);
        }

        public async Task<Guid?> DuplicateAsync(TrainingSessionViewModel item)
            => await AddAsync(x =>
            {
                var itemToDuplicated = Service.GetById(item.Id);

                if (itemToDuplicated is not null)
                {
                    x.Date = DateTime.Today;
                    x.StartTime = itemToDuplicated.Start.ToLocalTime().TimeOfDay;
                    x.EndTime = itemToDuplicated.End.ToLocalTime().TimeOfDay;
                    x.IsCancelled = false;
                    x.Place = itemToDuplicated.Place;
                    x.Theme = itemToDuplicated.Theme;
                    x.SelectedTeamIds = itemToDuplicated.TeamIds.ToList();
                }
            }).ConfigureAwait(false);

        public async Task EditAttendancesAsync(TrainingSessionViewModel item)
        {
            var vm = ViewModelLocator.Get<TrainingAttendancesEditionViewModel>();
            vm.Load(item.Id);

            _ = await DialogManager.ShowDialogAsync(vm).ConfigureAwait(false);
        }

        public async Task InitializeAttendancesAsync(IEnumerable<TrainingSessionViewModel> items)
        {
            var idsList = items.Select(x => x.Id).ToList();
            if (idsList.Count == 0) return;

            await AppBusyManager.WaitAsync(() => Service.InitializeAttendances(idsList)).ConfigureAwait(false);
        }

        public async Task SetAttendancesAsync(IEnumerable<TrainingAttendanceViewModel> items, Attendance attendance)
        {
            var count = items.Count();
            if (count == 0) return;

            await AppBusyManager.WaitAsync(() =>
            {
                var attendancesBySession = items.GroupBy(x => x.Session.Id, x => x.Player.PlayerId);
                attendancesBySession.ForEach(x => Service.SetAttendances(x.Key, [.. x], attendance));
            }).ConfigureAwait(false);
        }

        public async Task RemoveAttendancesAsync(IEnumerable<TrainingAttendanceViewModel> oldItems)
        {
            var count = oldItems.Count();
            if (count == 0) return;

            var cancel = await DialogManager.ShowQuestionAsync(nameof(MessageResources.XItemsRemovingQuestion).TranslateWithCountAndOptionalFormat(count)!, UiResources.Removing).ConfigureAwait(false) != MessageBoxResult.Yes;

            if (!cancel)
            {
                await AppBusyManager.WaitAsync(() =>
                {
                    var attendancesBySession = oldItems.GroupBy(x => x.Session.Id, x => x.Player.PlayerId);
                    attendancesBySession.ForEach(x => Service.RemoveAttendances(x.Key, [.. x]));
                });
            }
        }
    }
}
