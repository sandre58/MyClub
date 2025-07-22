// -----------------------------------------------------------------------
// <copyright file="StadiumId.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using MyClub.Shared.Kernel.Primitives;

namespace MyClub.Shared.Domain.Stadiums;

public sealed record StadiumId(Guid Value) : EntityId<StadiumId>(Value);
