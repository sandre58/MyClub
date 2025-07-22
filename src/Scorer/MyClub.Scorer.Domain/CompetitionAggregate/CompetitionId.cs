// -----------------------------------------------------------------------
// <copyright file="CompetitionId.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using MyClub.Shared.Kernel.Primitives;

namespace MyClub.Scorer.Domain.CompetitionAggregate;

public sealed record CompetitionId(Guid Value) : EntityId<CompetitionId>(Value);
