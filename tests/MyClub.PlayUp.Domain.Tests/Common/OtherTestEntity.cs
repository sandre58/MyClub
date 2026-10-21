// -----------------------------------------------------------------------
// <copyright file="OtherTestEntity.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Tests.Common;

internal sealed class OtherTestEntity(CompetitionId id) : Entity<CompetitionId>(id);
