// -----------------------------------------------------------------------
// <copyright file="IDomainEvent.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace MyClub.Shared.Kernel.Primitives;

public interface IDomainEvent
{
    DateTime OccurredOn { get; }
}
