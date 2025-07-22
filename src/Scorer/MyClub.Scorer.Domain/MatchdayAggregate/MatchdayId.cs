// -----------------------------------------------------------------------
// <copyright file="MatchdayId.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using MyClub.Shared.Kernel.Primitives;

namespace MyClub.Scorer.Domain.MatchdayAggregate;

public sealed record MatchdayId(Guid Value) : EntityId<MatchdayId>(Value);
