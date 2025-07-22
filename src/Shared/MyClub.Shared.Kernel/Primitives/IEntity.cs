// -----------------------------------------------------------------------
// <copyright file="IEntity.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using MyNet.Utilities;

namespace MyClub.Shared.Kernel.Primitives;

public interface IEntity<TId> : IIdentifiable<TId>, IEquatable<Entity<TId>>, IComparable<Entity<TId>>, IComparable
    where TId : EntityId<TId>
{ }
