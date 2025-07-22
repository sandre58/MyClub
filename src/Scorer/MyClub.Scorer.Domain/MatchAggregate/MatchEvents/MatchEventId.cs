// -----------------------------------------------------------------------
// <copyright file="MatchEventId.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using MyClub.Shared.Kernel.Primitives;

namespace MyClub.Scorer.Domain.MatchAggregate.MatchEvents;

public sealed record MatchEventId(Guid Value) : EntityId<MatchEventId>(Value);
