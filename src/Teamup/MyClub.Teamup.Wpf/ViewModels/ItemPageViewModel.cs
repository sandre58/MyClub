// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using MyClub.Teamup.Wpf.Services;
using MyClub.Teamup.Wpf.Services.Providers;
using MyNet.Observable;
using MyNet.Observable.Attributes;
using MyNet.Observable.Collections;
using MyNet.Observable.Collections.Filters;
using MyNet.UI.Commands;
using MyNet.UI.Navigation.Models;
using MyNet.UI.ViewModels;
using MyNet.UI.ViewModels.List.Filtering;
using MyNet.Utilities;
using PropertyChanged;

namespace MyClub.Teamup.Wpf.ViewModels
{
    [CanBeValidated(false)]
    [CanSetIsModified(false)]
    internal abstract class ItemPageViewModel<T> : PageViewModel, IItemViewModel<T>
        where T : IIdentifiable<Guid>
    {
        private readonly Type _parentPageType;
        private readonly ExtendedCollection<T> _otherItems;

        protected ItemPageViewModel(ProjectInfoProvider projectInfoProvider, ExtendedCollection<T> otherItems, Type parentPageType) : base(projectInfoProvider)
        {
            _parentPageType = parentPageType;
            _otherItems = otherItems;

            NavigateToItemCommand = CommandsManager.CreateNotNull<T>(NavigateToItem);
            NavigateToPreviousItemCommand = CommandsManager.Create(() => NavigateToItem(PreviousItem!), () => PreviousItem is not null);
            NavigateToNextItemCommand = CommandsManager.Create(() => NavigateToItem(NextItem!), () => NextItem is not null);

            Disposables.Add(_otherItems.Connect().Subscribe(_ => UpdateItems()));
        }

        protected ExtendedCollection<T> OtherItemsCollection => _otherItems;

        public ReadOnlyObservableCollection<T> OtherItems => _otherItems.Items;

        public T? PreviousItem { get; private set; }

        public T? NextItem { get; private set; }

        protected CompositeDisposable? ItemSubscriptions { get; private set; }

        public Subject<T?> ItemChanged { get; } = new();

        [DoNotCheckEquality]
        public T? Item { get; protected set; }

        public ICommand NavigateToItemCommand { get; }

        public ICommand NavigateToPreviousItemCommand { get; }

        public ICommand NavigateToNextItemCommand { get; }

        public override Type? GetParentPageType() => _parentPageType;

        protected virtual void OnItemChanged()
        {
            ItemSubscriptions?.Dispose();
            ItemSubscriptions = [];

            FilterOtherItems();
            UpdateItems();
            ItemChanged.OnNext(Item);
        }

        protected void FilterOtherItems()
        {
            using (_otherItems.DeferFilter())
                _otherItems.Filters.Set(ProvideOtherItemsFilters(Item).Select(x => new CompositeFilter(x)));
        }

        protected virtual IEnumerable<IFilterViewModel> ProvideOtherItemsFilters(T? item) => [];

        public override void LoadParameters(INavigationParameters? parameters)
        {
            if (parameters is null) return;

            if (parameters.Get<T>(NavigationCommandsService.ItemParameterKey) is T item)
                Item = item is not ObservableObject observableObject || !observableObject.IsDisposed ? item : default;

            base.LoadParameters(parameters);
        }

        protected void UpdateItems()
        {
            if (Item is null) return;

            var indexOfItems = _otherItems.Items.IndexOf(Item);

            PreviousItem = _otherItems.Items.GetByIndex(indexOfItems - 1);
            NextItem = _otherItems.Items.GetByIndex(indexOfItems + 1);
        }

        protected abstract void NavigateToItem(T item);

        public override bool Equals(object? obj) => obj is ItemPageViewModel<T> itemViewModel && Equals(Item, itemViewModel.Item);

        public override int GetHashCode() => Item?.GetHashCode() ?? RuntimeHelpers.GetHashCode(this);
    }
}
