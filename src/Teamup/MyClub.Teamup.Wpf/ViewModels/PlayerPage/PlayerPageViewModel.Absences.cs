// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using MyNet.UI.Commands;
using MyNet.UI.ViewModels.Workspace;
using MyClub.Teamup.Domain.Enums;
using MyClub.Teamup.Wpf.ViewModels.Entities;

namespace MyClub.Teamup.Wpf.ViewModels.PlayerPage
{
    internal class PlayerPageAbsencesViewModel : SubItemViewModel<PlayerViewModel>
    {
        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public IEnumerable? SelectedDates { get; set; }

        public ICommand AddInHolidaysCommand { get; }

        public ICommand AddInSelectionCommand { get; }

        public ICommand AddOtherCommand { get; }

        public ICommand EditCommand { get; }

        public ICommand RemoveCommand { get; }

        public PlayerPageAbsencesViewModel()
        {
            AddInHolidaysCommand = CommandsManager.Create(async () => await AddAsync(AbsenceType.InHolidays), CanAdd);
            AddInSelectionCommand = CommandsManager.Create(async () => await AddAsync(AbsenceType.InSelection), CanAdd);
            AddOtherCommand = CommandsManager.Create(async () => await AddAsync(AbsenceType.Other), CanAdd);
            EditCommand = CommandsManager.Create<DateTime>(async x => await EditAsync(x).ConfigureAwait(false), x => Item is not null && Item.Absences.Any(y => y.ContainsDate(x)));
            RemoveCommand = CommandsManager.Create<DateTime>(async x => await RemoveAsync(x).ConfigureAwait(false), x => Item is not null && Item.Absences.Any(y => y.ContainsDate(x)));
        }

        private bool CanAdd() => Item is not null && SelectedDates is not null && SelectedDates.OfType<DateTime>().All(x => !Item.Absences.Any(y => y.ContainsDate(x)));

        private async Task AddAsync(AbsenceType type)
        {
            var dates = SelectedDates?.OfType<DateTime>();

            if (dates is null || Item is null) return;

            await Item.AddAbsenceAsync(type, dates.Min(), dates.Max()).ConfigureAwait(false);
        }

        private async Task EditAsync(DateTime date)
        {
            var absence = Item?.Absences.FirstOrDefault(x => x.ContainsDate(date));

            if (absence is null) return;
            await absence.EditAsync().ConfigureAwait(false);
        }

        private async Task RemoveAsync(DateTime date)
        {
            var absence = Item?.Absences.FirstOrDefault(x => x.ContainsDate(date));

            if (absence is null) return;
            await absence.RemoveAsync().ConfigureAwait(false);
        }
    }
}
