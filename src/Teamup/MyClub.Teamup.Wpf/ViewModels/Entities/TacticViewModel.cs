// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DynamicData;
using DynamicData.Binding;
using MyNet.UI.Commands;
using MyNet.Utilities;
using MyNet.Observable.Threading;
using MyClub.Teamup.Domain.TacticAggregate;
using MyClub.Teamup.Wpf.Services;

namespace MyClub.Teamup.Wpf.ViewModels.Entities
{
    internal class TacticViewModel : EntityViewModelBase<Tactic>
    {
        private readonly TacticPresentationService _tacticPresentationService;
        private readonly ObservableCollectionExtended<TacticPositionViewModel> _positions = [];
        private readonly ObservableCollectionExtended<string> _instructions = [];

        public TacticViewModel(Tactic item, TacticPresentationService tacticPresentationService) : base(item)
        {
            _tacticPresentationService = tacticPresentationService;
            Positions = new(_positions);
            Instructions = new(_instructions);

            Disposables.AddRange(
            [
                Item.Positions.ToObservableChangeSet(x => x.Id).Transform(x => new TacticPositionViewModel(x)).ObserveOn(Scheduler.UI).Bind(_positions).DisposeMany().Subscribe(),
                Item.Instructions.ToObservableChangeSet().ObserveOn(Scheduler.UI).Bind(_instructions).Subscribe()
            ]);

            OpenCommand = CommandsManager.Create(Open);
            EditCommand = CommandsManager.Create(async () => await EditAsync().ConfigureAwait(false));
            DuplicateCommand = CommandsManager.Create(async () => await DuplicateAsync().ConfigureAwait(false));
            RemoveCommand = CommandsManager.Create(async () => await RemoveAsync().ConfigureAwait(false));
        }

        public string? Description => Item.Description;

        public string Code => Item.Code;

        public string Label => Item.Label;

        public int Order => Item.Order;

        public ReadOnlyObservableCollection<TacticPositionViewModel> Positions { get; }

        public ReadOnlyObservableCollection<string> Instructions { get; }

        public ICommand OpenCommand { get; }

        public ICommand EditCommand { get; }

        public ICommand DuplicateCommand { get; }

        public ICommand RemoveCommand { get; }

        public async Task EditAsync() => await _tacticPresentationService.EditAsync(this).ConfigureAwait(false);

        public async Task DuplicateAsync() => await _tacticPresentationService.DuplicateAsync(this).ConfigureAwait(false);

        public async Task RemoveAsync() => await _tacticPresentationService.RemoveAsync([this]).ConfigureAwait(false);

        public void Open() => NavigationCommandsService.NavigateToTacticPage(this);
    }
}
