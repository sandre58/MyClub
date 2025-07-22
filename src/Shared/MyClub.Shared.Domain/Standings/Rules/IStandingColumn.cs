// -----------------------------------------------------------------------
// <copyright file="IStandingColumn.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using MyClub.Shared.Domain.Matchs;
using MyClub.Shared.Domain.Teams;

namespace MyClub.Shared.Domain.Standings.Rules;

public interface IStandingColumn<T> : IStandingColumn
    where T : notnull
{
    new T DefaultValue { get; }

    T ComputeIncremental(T previousValue, TeamReference team, IMatch match);

    new T ComputeBatch(TeamReference team, IEnumerable<IMatch> matches);
}

public interface IStandingColumn
{
    string Key { get; }

    object DefaultValue { get; }

    object ComputeIncremental(object previousValue, TeamReference team, IMatch match);

    object ComputeBatch(TeamReference team, IEnumerable<IMatch> matches);
}
