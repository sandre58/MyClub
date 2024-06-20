// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DynamicData;
using DynamicData.Binding;
using MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces;
using MyNet.Observable;
using MyNet.Observable.Attributes;
using MyNet.Observable.Collections.Providers;
using MyNet.UI.Commands;
using MyNet.UI.Selection;
using MyNet.UI.Selection.Models;
using MyNet.UI.Threading;
using MyNet.UI.ViewModels;
using MyNet.UI.ViewModels.List;
using MyNet.Utilities;

namespace MyClub.Scorer.Wpf.ViewModels.BracketPage
{
    internal abstract class MatchParentsViewModel<T> : SelectionListViewModel<T> where T : IMatchParent
    {
        protected MatchParentsViewModel(IMatchdayParent parent, ISourceProvider<T> matchParentsProvider, ListParametersProvider listParametersProvider)
            : base(collection: new MatchParentsCollection<T>(matchParentsProvider), parametersProvider: listParametersProvider)
        {
            Parent = parent;
            Mode = ScreenMode.Read;

            AddToDateCommand = CommandsManager.Create<DateTime>(async x => await AddToDateAsync(x).ConfigureAwait(false));
            AddMatchToSelectedItemCommand = CommandsManager.Create(async () => await SelectedItem!.AddMatchAsync().ConfigureAwait(false), () => SelectedWrappers.Count == 1);
            DuplicateSelectedItemCommand = CommandsManager.Create(async () => await DuplicateAsync(SelectedItem!).ConfigureAwait(false), () => SelectedWrappers.Count == 1);
            PostponeSelectedItemsCommand = CommandsManager.Create(async () => await PostponeAsync(SelectedItems).ConfigureAwait(false), () => SelectedItems.All(x => x.CanBePostponed()));
        }

        public IMatchdayParent Parent { get; }

        public ICommand AddToDateCommand { get; }

        public ICommand AddMatchToSelectedItemCommand { get; }

        public ICommand DuplicateSelectedItemCommand { get; }

        public ICommand PostponeSelectedItemsCommand { get; }

        protected override async Task<T?> CreateNewItemAsync()
        {
            await AddItemAsync().ConfigureAwait(false);

            return default;
        }

        protected override async Task<T?> UpdateItemAsync(T oldItem)
        {
            await EditItemAsync(oldItem).ConfigureAwait(false);

            return default;
        }

        public override async Task RemoveRangeAsync(IEnumerable<T> oldItems) => await RemoveItemsAsync(oldItems).ConfigureAwait(false);

        protected abstract Task AddToDateAsync(DateTime date);

        protected abstract Task AddItemAsync();

        protected abstract Task EditItemAsync(T oldItem);

        protected abstract Task RemoveItemsAsync(IEnumerable<T> oldItems);

        protected abstract Task DuplicateAsync(T item);

        protected abstract Task PostponeAsync(IEnumerable<T> oldItems);
    }

    internal class MatchParentsCollection<T>(ISourceProvider<T> matchParentsSourceProvider)
        : SelectableCollection<T>(matchParentsSourceProvider.Connect(), scheduler: Scheduler.UI, createWrapper: x => new MatchParentWrapper<T>(x)) where T : IMatchParent
    {
    }

    [CanSetIsModified(false)]
    [CanBeValidated(false)]
    internal class MatchParentWrapper<T> : SelectedWrapper<T>, IAppointment where T : IMatchParent
    {
        public MatchParentWrapper(T item) : base(item)
            => Disposables.AddRange(
            [
                Item.WhenPropertyChanged(x => x.Date, false).Subscribe(_ =>
                {
                    RaisePropertyChanged(nameof(StartDate));
                    RaisePropertyChanged(nameof(EndDate));
                }),
            ]);

        public DateTime StartDate => Item.Date.BeginningOfDay();

        public DateTime EndDate => Item.Date.EndOfDay();
    }
}
