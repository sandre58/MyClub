// -----------------------------------------------------------------------
// <copyright file="ITeamsContainer.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using MyClub.Shared.Domain.Teams;

namespace MyClub.Scorer.Domain.Primitives;

public interface ITeamsContainer
{
    IReadOnlyCollection<TeamReference> Teams { get; }
}
