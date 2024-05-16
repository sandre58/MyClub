// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DynamicData;
using DynamicData.Binding;
using MyNet.UI.Commands;
using MyNet.UI.ViewModels.List;
using MyNet.Observable.Collections.Providers;
using MyClub.Domain.Enums;
using MyClub.Teamup.Wpf.Services;
using MyClub.Teamup.Wpf.ViewModels.Entities;
using MyClub.Teamup.Wpf.ViewModels.Entities.Interfaces;

namespace MyClub.Teamup.Wpf.ViewModels.CompetitionPage
{
    internal class MatchesViewModel : SelectionListViewModel<MatchViewModel>
    {
        private readonly MatchPresentationService _matchPresentationService;

        public MatchesViewModel(IMatchParent parent, MatchPresentationService matchPresentationService, IListParametersProvider? parametersProvider = null)
            : this(parent.AllMatches.ToObservableChangeSet(), matchPresentationService, parametersProvider) => Parent = parent;

        public MatchesViewModel(SourceProvider<MatchViewModel> provider, MatchPresentationService matchPresentationService, IListParametersProvider? parametersProvider = null)
            : this(provider.Connect(), matchPresentationService, parametersProvider) { }

        public MatchesViewModel(IObservable<IChangeSet<MatchViewModel>> observable, MatchPresentationService matchPresentationService, IListParametersProvider? parametersProvider = null)
            : base(source: observable, parametersProvider: parametersProvider)
        {
            _matchPresentationService = matchPresentationService;

            AddMultipleCommand = CommandsManager.Create(async () => await AddMultipleAsync().ConfigureAwait(false), () => CanAdd);
            StartCommand = CommandsManager.Create(async () => await StartAsync().ConfigureAwait(false), () => SelectedItems.Any() && SelectedItems.All(x => x.State is MatchState.None or MatchState.Suspended));
            SuspendCommand = CommandsManager.Create(async () => await SuspendAsync().ConfigureAwait(false), () => SelectedItems.Any() && SelectedItems.All(x => x.State is MatchState.InProgress));
            PostponeCommand = CommandsManager.Create(async () => await PostponeAsync().ConfigureAwait(false), () => SelectedItems.Any() && SelectedItems.All(x => x.State is MatchState.None or MatchState.InProgress or MatchState.Suspended));
            CancelCommand = CommandsManager.Create(async () => await CancelAsync().ConfigureAwait(false), () => SelectedItems.Any() && SelectedItems.All(x => x.State is MatchState.None && x.Parent.CanCancelMatch()));
            ResetMatchesCommand = CommandsManager.Create(async () => await ResetMatchesAsync().ConfigureAwait(false));
        }

        public override bool CanAdd => Parent is not null;

        public IMatchParent? Parent { get; protected set; }

        public ICommand AddMultipleCommand { get; private set; }

        public ICommand ResetMatchesCommand { get; }

        public ICommand StartCommand { get; }

        public ICommand PostponeCommand { get; }

        public ICommand CancelCommand { get; }

        public ICommand SuspendCommand { get; }

        protected override void OpenCore(MatchViewModel item, int? selectedTab = null) => item.Open();

        public override async Task AddAsync()
        {
            if (Parent is not null)
                await _matchPresentationService.AddAsync(Parent).ConfigureAwait(false);
        }

        public virtual async Task AddMultipleAsync()
        {
            if (Parent is not null)
                await _matchPresentationService.AddMultipleAsync(Parent).ConfigureAwait(false);
        }

        public override async Task EditRangeAsync(IEnumerable<MatchViewModel> oldItems)
        {
            if (oldItems.Count() == 1)
                await EditAsync(oldItems.First()).ConfigureAwait(false);
            else
                await base.EditRangeAsync(oldItems).ConfigureAwait(false);
        }

        protected override async Task<MatchViewModel?> UpdateItemAsync(MatchViewModel oldItem)
        {
            await _matchPresentationService.EditAsync(oldItem).ConfigureAwait(false);
            return null;
        }

        protected override async Task<IEnumerable<MatchViewModel>> UpdateRangeAsync(IEnumerable<MatchViewModel> oldItems)
        {
            await _matchPresentationService.EditAsync(oldItems).ConfigureAwait(false);
            return [];
        }

        public override async Task RemoveRangeAsync(IEnumerable<MatchViewModel> oldItems) => await _matchPresentationService.RemoveAsync(oldItems).ConfigureAwait(false);

        public async Task StartAsync() => await _matchPresentationService.StartAsync(SelectedItems).ConfigureAwait(false);

        public async Task ResetMatchesAsync() => await _matchPresentationService.ResetAsync(SelectedItems).ConfigureAwait(false);

        public async Task CancelAsync() => await _matchPresentationService.CancelAsync(SelectedItems).ConfigureAwait(false);

        public async Task PostponeAsync() => await _matchPresentationService.PostponeAsync(SelectedItems).ConfigureAwait(false);

        public async Task SuspendAsync() => await _matchPresentationService.SuspendAsync(SelectedItems).ConfigureAwait(false);

    }
}
