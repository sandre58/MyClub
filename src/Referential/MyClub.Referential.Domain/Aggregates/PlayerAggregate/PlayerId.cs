// -----------------------------------------------------------------------
// <copyright file="PlayerId.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using MyClub.Shared.Kernel.Primitives;

namespace MyClub.Referential.Domain.Aggregates.PlayerAggregate;

public sealed record PlayerId(Guid Value) : EntityId<PlayerId>(Value);
