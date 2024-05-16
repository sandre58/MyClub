// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using DynamicData.Binding;
using MyNet.UI.Commands;
using MyNet.Utilities;
using MyNet.Utilities.DateTimes;
using MyNet.Observable;
using MyClub.Domain.Enums;
using MyClub.Teamup.Domain.PersonAggregate;
using MyClub.Teamup.Wpf.Services;
using MyClub.Teamup.Domain.Enums;

namespace MyClub.Teamup.Wpf.ViewModels.Entities
{
    internal class AbsenceViewModel : EntityViewModelBase<Absence>, IAppointment
    {
        private readonly PlayerPresentationService _playerPresentationService;

        public AbsenceViewModel(Absence item, PlayerViewModel player, PlayerPresentationService playerPresentationService) : base(item)
        {
            _playerPresentationService = playerPresentationService;
            Player = player;

            Disposables.AddRange(
            [
                Item.Period.WhenPropertyChanged(x => x.Start).Subscribe(_ => RaisePropertyChanged(nameof(StartDate))),
                Item.Period.WhenPropertyChanged(x => x.Start).Subscribe(_ => RaisePropertyChanged(nameof(EndDate))),
                Item.Period.WhenAnyPropertyChanged(nameof(ObservablePeriodWithOptionalEnd.Start), nameof(ObservablePeriodWithOptionalEnd.End)).Subscribe(_ => RaisePropertyChanged(nameof(IsCurrent)))
            ]);

            EditCommand = CommandsManager.Create(async () => await EditAsync().ConfigureAwait(false));
            RemoveCommand = CommandsManager.Create(async () => await RemoveAsync().ConfigureAwait(false));
        }

        public PlayerViewModel Player { get; }

        public AbsenceType Type => Item.Type;

        public string Label => Item.Label;

        public DateTime StartDate => Item.Period.Start.Date;

        public DateTime EndDate => Item.Period.End.Date;

        public bool IsCurrent => Item.Period.IsCurrent();

        public ICommand EditCommand { get; }

        public ICommand RemoveCommand { get; }

        internal bool ContainsDate(DateTime x) => Item.Period.Contains(x);

        public async Task EditAsync() => await _playerPresentationService.EditAbsenceAsync(this).ConfigureAwait(false);

        public async Task RemoveAsync() => await _playerPresentationService.RemoveAbsenceAsync(this).ConfigureAwait(false);
    }
}
