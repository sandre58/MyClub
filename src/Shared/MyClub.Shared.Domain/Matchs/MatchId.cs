// -----------------------------------------------------------------------
// <copyright file="MatchId.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using MyClub.Shared.Kernel.Primitives;

namespace MyClub.Shared.Domain.Matchs;

public sealed record MatchId(Guid Value) : EntityId<MatchId>(Value);
