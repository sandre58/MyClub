// -----------------------------------------------------------------------
// <copyright file="PenaltyShootoutId.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using MyClub.Shared.Kernel.Primitives;

namespace MyClub.Scorer.Domain.MatchAggregate.MatchEvents;

public sealed record PenaltyShootoutId(Guid Value) : EntityId<PenaltyShootoutId>(Value);
