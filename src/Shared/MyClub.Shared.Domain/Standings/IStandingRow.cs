// -----------------------------------------------------------------------
// <copyright file="IStandingRow.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using MyClub.Shared.Domain.Enums;
using MyClub.Shared.Domain.Matchs;
using MyClub.Shared.Domain.Standings.Rules;
using MyClub.Shared.Domain.Teams;

namespace MyClub.Shared.Domain.Standings;

public interface IStandingRow
{
    TeamReference Team { get; }

    int Points { get; }

    int PenaltyPoints { get; }

    object? Get(string column);

    T? Get<T>(string column);

    int Get(StandingColumnType column);

    void Compute(IEnumerable<IMatch> matches, StandingRuleSet rules);

    void ApplyMatch(IMatch match, StandingRuleSet rules);
}
