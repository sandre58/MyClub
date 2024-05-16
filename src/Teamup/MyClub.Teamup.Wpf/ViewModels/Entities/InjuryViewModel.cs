// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using DynamicData.Binding;
using MyNet.UI.Commands;
using MyNet.Utilities;
using MyNet.Utilities.DateTimes;
using MyClub.Domain.Enums;
using MyClub.Teamup.Domain.PersonAggregate;
using MyClub.Teamup.Wpf.Services;

namespace MyClub.Teamup.Wpf.ViewModels.Entities
{
    internal class InjuryViewModel : EntityViewModelBase<Injury>
    {
        private readonly PlayerPresentationService _playerPresentationService;

        public InjuryViewModel(Injury item, PlayerViewModel player, PlayerPresentationService playerPresentationService) : base(item)
        {
            _playerPresentationService = playerPresentationService;
            Player = player;

            Disposables.AddRange(
            [
                Player.WhenAnyPropertyChanged().Subscribe(_ => RaisePropertyChanged(nameof(Player))),
                Item.Period.WhenPropertyChanged(x => x.Start).Subscribe(_ => RaisePropertyChanged(nameof(Date))),
                Item.Period.WhenPropertyChanged(x => x.End).Subscribe(_ => RaisePropertyChanged(nameof(EndDate))),
                Item.Period.WhenAnyPropertyChanged(nameof(ObservablePeriodWithOptionalEnd.Start), nameof(ObservablePeriodWithOptionalEnd.End)).Subscribe(_ =>
                {
                    RaisePropertyChanged(nameof(IsCurrent));
                    RaisePropertyChanged(nameof(Duration));
                    RaisePropertyChanged(nameof(NullableDuration));
                    RaisePropertyChanged(nameof(EndDateOrMax));
                })
            ]);

            OpenCommand = CommandsManager.Create(Open);
            EditCommand = CommandsManager.Create(async () => await EditAsync().ConfigureAwait(false));
            RemoveCommand = CommandsManager.Create(async () => await RemoveAsync().ConfigureAwait(false));
        }

        public PlayerViewModel Player { get; }

        public string Condition => Item.Condition;

        public InjurySeverity Severity => Item.Severity;

        public InjuryType Type => Item.Type;

        public InjuryCategory Category => Item.Category;

        public string? Description => Item.Description;

        public DateTime Date => Item.Period.Start.ToLocalTime();

        public DateTime? EndDate => Item.Period.End?.ToLocalTime();

        public DateTime EndDateOrMax => Item.Period.End?.ToLocalTime() ?? DateTime.MaxValue;

        public TimeSpan Duration => Item.Period.Duration;

        public TimeSpan? NullableDuration => Item.Period.NullableDuration;

        public bool IsCurrent => Item.IsCurrent();

        public ICommand OpenCommand { get; }

        public ICommand EditCommand { get; }

        public ICommand RemoveCommand { get; }

        public async Task EditAsync() => await _playerPresentationService.EditInjuryAsync(this).ConfigureAwait(false);

        public async Task RemoveAsync() => await _playerPresentationService.RemoveInjuryAsync(this).ConfigureAwait(false);

        public void Open() => NavigationCommandsService.NavigateToInjuryPage(this);
    }
}
