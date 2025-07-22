// -----------------------------------------------------------------------
// <copyright file="ManagerId.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using MyClub.Shared.Kernel.Primitives;

namespace MyClub.Scorer.Domain.CompetitionAggregate.Teams;

public sealed record ManagerId(Guid Value) : EntityId<ManagerId>(Value);
