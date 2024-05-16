// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using System.Windows.Media;
using MyNet.UI.Commands;
using MyClub.Teamup.Domain.HolidaysAggregate;
using MyClub.Teamup.Wpf.Services;

namespace MyClub.Teamup.Wpf.ViewModels.Entities
{
    internal class HolidaysViewModel : SchedulingPeriodViewModel<Holidays>
    {
        private readonly HolidaysPresentationService _holidaysPresentationService;

        public static readonly Color DefaultColor = Color.FromArgb(153, 127, 92, 193);

        public HolidaysViewModel(Holidays item, HolidaysPresentationService holidaysPresentationService) : base(item)
        {
            _holidaysPresentationService = holidaysPresentationService;

            EditCommand = CommandsManager.Create(async () => await EditAsync().ConfigureAwait(false));
            RemoveCommand = CommandsManager.Create(async () => await RemoveAsync().ConfigureAwait(false));
        }

        public override string Label => Item.Label;

        public override Color Color => DefaultColor;

        public ICommand RemoveCommand { get; }

        public ICommand EditCommand { get; }

        public async Task EditAsync() => await _holidaysPresentationService.EditAsync(this).ConfigureAwait(false);

        public async Task RemoveAsync() => await _holidaysPresentationService.RemoveAsync([this]).ConfigureAwait(false);
    }
}
