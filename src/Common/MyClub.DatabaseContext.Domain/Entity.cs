// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using MyNet.Utilities;

namespace MyClub.DatabaseContext.Domain
{

    [DebuggerDisplay("{Id} | {DebuggerDisplayValue}")]
    public abstract class Entity : ISimilar, ISettable
    {
        protected Entity() { }

        protected Entity(Guid id) => Id = id;

        private string? DebuggerDisplayValue => ToString();

        public Guid Id { get; }

        public override bool Equals(object? obj) => obj is Entity other && Equals(Id, other.Id);

        public override int GetHashCode() => Id.GetHashCode();

        public abstract bool IsSimilar(object? obj);

        public abstract void SetFrom(object? from);
    }
}
