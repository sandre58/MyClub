// -----------------------------------------------------------------------
// <copyright file="FixtureId.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using MyClub.Shared.Kernel.Primitives;

namespace MyClub.Scorer.Domain.RoundAggregate;

public sealed record FixtureId(Guid Value) : EntityId<FixtureId>(Value);
