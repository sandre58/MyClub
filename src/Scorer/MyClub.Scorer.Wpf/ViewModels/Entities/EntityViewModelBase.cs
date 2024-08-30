// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Reactive.Linq;
using MyNet.Utilities;
using MyNet.Observable;
using MyClub.Domain;

namespace MyClub.Scorer.Wpf.ViewModels.Entities
{
    [DebuggerDisplay("{Id} | {DebuggerDisplayValue}")]
    internal class EntityViewModelBase<T> : LocalizableObject, IIdentifiable<Guid>, IComparable
        where T : IEntity
    {
        protected T Item { get; }

        private string? DebuggerDisplayValue => ToString();

        protected EntityViewModelBase(T item)
        {
            Item = item;

            Disposables.Add(Observable.FromEventPattern<PropertyChangedEventHandler, PropertyChangedEventArgs>(x => Item.PropertyChanged += x, x => Item.PropertyChanged -= x).Subscribe(x => RaisePropertyChanged(x.EventArgs.PropertyName)));
        }

        public Guid Id => Item.Id;

        public override string? ToString() => Item.ToString();

        public override bool Equals(object? obj) => obj is EntityViewModelBase<T> vm && Item.Equals(vm.Item);

        public override int GetHashCode() => Item.GetHashCode();

        #region IComparable

        public virtual int CompareTo(object? obj) => obj is EntityViewModelBase<T> other ? Item.CompareTo(other.Item) : -1;

        public static bool operator ==(EntityViewModelBase<T> left, EntityViewModelBase<T> right) => left?.Equals(right) ?? right is null;

        public static bool operator >(EntityViewModelBase<T> left, EntityViewModelBase<T> right) => left.CompareTo(right) > 0;

        public static bool operator <(EntityViewModelBase<T> left, EntityViewModelBase<T> right) => left.CompareTo(right) < 0;

        public static bool operator >=(EntityViewModelBase<T> left, EntityViewModelBase<T> right) => left.CompareTo(right) >= 0;

        public static bool operator <=(EntityViewModelBase<T> left, EntityViewModelBase<T> right) => left.CompareTo(right) <= 0;

        public static bool operator !=(EntityViewModelBase<T> left, EntityViewModelBase<T> right) => !(left == right);

        #endregion
    }
}
