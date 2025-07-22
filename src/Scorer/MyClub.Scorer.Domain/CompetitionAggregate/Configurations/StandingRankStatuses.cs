// -----------------------------------------------------------------------
// <copyright file="StandingRankStatuses.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MyNet.Utilities.Sequences;

namespace MyClub.Scorer.Domain.CompetitionAggregate.Configurations;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1710:Identifiers should have correct suffix", Justification = "It's a special collection.")]
public class StandingRankStatuses : IReadOnlyCollection<StandingRankStatus>
{
    private readonly List<StandingRankStatus> _statuses = [];

    public int Count => _statuses.Count;

    public StandingRankStatuses Add(int rank, string? color, string name, string shortName, string? description = null, int? order = null)
        => Add(new Interval<int>(rank, rank), color, name, shortName, description, order);

    public StandingRankStatuses Add(Interval<int> ranks, string? color, string name, string shortName, string? description = null, int? order = null)
    {
        _statuses.Add(new StandingRankStatus(ranks, color, name, shortName, description, order));
        return this;
    }

    public StandingRankStatus? GetStatus(int rank) => _statuses.FirstOrDefault(s => s.Contains(rank));

    public IEnumerator<StandingRankStatus> GetEnumerator() => _statuses.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
