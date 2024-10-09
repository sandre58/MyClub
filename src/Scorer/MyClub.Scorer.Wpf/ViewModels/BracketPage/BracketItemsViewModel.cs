// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DynamicData;
using DynamicData.Binding;
using MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces;
using MyNet.Observable;
using MyNet.Observable.Attributes;
using MyNet.Observable.Collections.Providers;
using MyNet.UI.Selection;
using MyNet.UI.Selection.Models;
using MyNet.UI.Threading;
using MyNet.UI.ViewModels;
using MyNet.UI.ViewModels.List;
using MyNet.Utilities;

namespace MyClub.Scorer.Wpf.ViewModels.BracketPage
{
    internal abstract class BracketItemsViewModel<T> : SelectionListViewModel<T> where T : IStageViewModel
    {
        protected BracketItemsViewModel(ISourceProvider<T> matchStagesProvider, ListParametersProvider listParametersProvider)
            : base(collection: new BracketItemsCollection<T>(matchStagesProvider), parametersProvider: listParametersProvider) => Mode = ScreenMode.Read;

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

        protected abstract Task AddItemAsync();

        protected abstract Task EditItemAsync(T oldItem);

        protected abstract Task RemoveItemsAsync(IEnumerable<T> oldItems);
    }

    internal class BracketItemsCollection<T>(ISourceProvider<T> matchStagesSourceProvider)
        : SelectableCollection<T>(matchStagesSourceProvider.Connect(), scheduler: Scheduler.UI, createWrapper: x => new BracketItemWrapper<T>(x)) where T : IStageViewModel
    {
    }

    [CanSetIsModified(false)]
    [CanBeValidated(false)]
    internal class BracketItemWrapper<T> : SelectedWrapper<T>, IAppointment where T : IStageViewModel
    {
        public BracketItemWrapper(T item) : base(item)
            => Disposables.AddRange(
            [
                item.WhenPropertyChanged(x => x.StartDate, false).Subscribe(_ =>
                {
                    RaisePropertyChanged(nameof(StartDate));
                    RaisePropertyChanged(nameof(EndDate));
                }),
            ]);

        public DateTime StartDate => Item.StartDate;

        public DateTime EndDate => Item.StartDate.EndOfDay();
    }
}
