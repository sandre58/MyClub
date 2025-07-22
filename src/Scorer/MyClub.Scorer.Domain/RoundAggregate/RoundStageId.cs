// -----------------------------------------------------------------------
// <copyright file="RoundStageId.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using MyClub.Shared.Kernel.Primitives;

namespace MyClub.Scorer.Domain.RoundAggregate;

public sealed record RoundStageId(Guid Value) : EntityId<RoundStageId>(Value);
