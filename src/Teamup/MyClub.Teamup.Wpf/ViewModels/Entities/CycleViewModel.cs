// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Media;
using MyNet.UI.Commands;
using MyNet.Wpf.Extensions;
using MyClub.Teamup.Domain.CycleAggregate;
using MyClub.Teamup.Wpf.Services;

namespace MyClub.Teamup.Wpf.ViewModels.Entities
{
    internal class CycleViewModel : SchedulingPeriodViewModel<Cycle>
    {
        private readonly CyclePresentationService _cyclePresentationService;

        public CycleViewModel(Cycle item, CyclePresentationService cyclePresentationService) : base(item)
        {
            _cyclePresentationService = cyclePresentationService;

            EditCommand = CommandsManager.Create(async () => await EditAsync().ConfigureAwait(false));
            RemoveCommand = CommandsManager.Create(async () => await RemoveAsync().ConfigureAwait(false));
        }

        public override string Label => Item.Label;

        public override Color Color => Item.Color.ToColor() ?? default;

        public ICollection<string> TechnicalGoals => Item.TechnicalGoals;

        public ICollection<string> TacticalGoals => Item.TacticalGoals;

        public ICollection<string> PhysicalGoals => Item.PhysicalGoals;

        public ICollection<string> MentalGoals => Item.MentalGoals;

        public ICommand RemoveCommand { get; }

        public ICommand EditCommand { get; }

        public async Task EditAsync() => await _cyclePresentationService.EditAsync(this).ConfigureAwait(false);

        public async Task RemoveAsync() => await _cyclePresentationService.RemoveAsync([this]).ConfigureAwait(false);
    }
}
