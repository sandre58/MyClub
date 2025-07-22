// -----------------------------------------------------------------------
// <copyright file="IKnockout.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using MyClub.Scorer.Domain.RoundAggregate;

namespace MyClub.Scorer.Domain.Primitives;

public interface IKnockout : ITeamsContainer
{
    IReadOnlyCollection<RoundId> Rounds { get; }
}
