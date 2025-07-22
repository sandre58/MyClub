// -----------------------------------------------------------------------
// <copyright file="StageId.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using MyClub.Shared.Kernel.Primitives;

namespace MyClub.Scorer.Domain.StageAggregate;

public sealed record StageId(Guid Value) : EntityId<StageId>(Value);
