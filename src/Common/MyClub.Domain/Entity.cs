// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using System.Diagnostics;

namespace MyClub.Domain
{
    [DebuggerDisplay("{Id} | {DebuggerDisplayValue}")]
    public abstract class Entity : IEntity
    {
        protected Entity(Guid? id = null) => Id = !id.HasValue || id == Guid.Empty ? Guid.NewGuid() : id.Value;

        private string? DebuggerDisplayValue => ToString();

        public Guid Id { get; }

        public event PropertyChangedEventHandler? PropertyChanged
        {
            add => PropertyChangedHandler += value;
            remove => PropertyChangedHandler -= value;
        }

        private event PropertyChangedEventHandler? PropertyChangedHandler;

        protected void RaisePropertyChanged(string? propertyName) => PropertyChangedHandler?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        protected virtual void OnPropertyChanged(string propertyName, object before, object after) => RaisePropertyChanged(propertyName);

        public override bool Equals(object? obj) => obj is Entity other && Equals(Id, other.Id);

        public override int GetHashCode() => Id.GetHashCode();

        #region IComparable

        public virtual int CompareTo(object? obj) => obj is Entity other ? Id.CompareTo(other.Id) : -1;

        public static bool operator ==(Entity left, Entity right) => left?.Equals(right) ?? right is null;

        public static bool operator >(Entity left, Entity right) => left.CompareTo(right) > 0;

        public static bool operator <(Entity left, Entity right) => left.CompareTo(right) < 0;

        public static bool operator >=(Entity left, Entity right) => left.CompareTo(right) >= 0;

        public static bool operator <=(Entity left, Entity right) => left.CompareTo(right) <= 0;

        public static bool operator !=(Entity left, Entity right) => !(left == right);

        #endregion
    }
}
