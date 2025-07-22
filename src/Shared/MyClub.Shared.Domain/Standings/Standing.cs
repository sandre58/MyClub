// -----------------------------------------------------------------------
// <copyright file="Standing.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using MyClub.Shared.Domain.Enums;
using MyClub.Shared.Domain.Matchs;
using MyClub.Shared.Domain.Standings.Rules;
using MyClub.Shared.Domain.Teams;
using MyNet.Utilities;

namespace MyClub.Shared.Domain.Standings;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1710:Identifiers should have correct suffix", Justification = "It's a special collection.")]
public class Standing(IEnumerable<TeamReference> teams, StandingRuleSet? rules = null, IReadOnlyDictionary<TeamReference, int>? penaltyPoints = null) : IReadOnlyCollection<StandingRow>
{
    private readonly Dictionary<TeamReference, StandingRow> _rows = teams.ToDictionary(x => x, x => new StandingRow(x, penaltyPoints?.GetValueOrDefault(x) ?? 0));
    private readonly List<IMatch> _appliedMatches = [];
    private Dictionary<TeamReference, int>? _rankCache;

    public StandingRuleSet Rules { get; } = rules ?? StandingRuleSet.Default;

    public IEnumerable<StandingRow> SortedRows => _rows.Values.OrderBy(r => r, Rules.Comparer);

    public int Count => _rows.Count;

    public int GetRank(TeamReference team)
    {
        if (_rankCache is null)
            RebuildRankCache();

        return _rankCache?[team] ?? -1;
    }

    public bool Contains(TeamReference team) => _rows.ContainsKey(team);

    public StandingRow GetRow(TeamReference team) => _rows[team];

    public T? GetColumn<T>(TeamReference team, string column) => GetRow(team).Get<T>(column);

    public int GetColumn(TeamReference team, StandingColumnType column) => GetRow(team).Get(column);

    public void ApplyMatch(IMatch match)
    {
        if (!match.HasResult()) return;

        match.GetTeams().ForEach(x => _rows.GetValueOrDefault(x)?.ApplyMatch(match, Rules));

        _appliedMatches.Add(match);
        Rules.Comparer.SetContext(_appliedMatches, Rules);
        SetComparerContext();

        InvalidateRankCache();
    }

    public void ApplyMatches(IEnumerable<IMatch> matches) => matches.ForEach(ApplyMatch);

    public void ComputeAll(IEnumerable<IMatch> matches)
    {
        var newAppliedMatches = matches.Where(m => m.HasResult()).ToList();
        var matchesByTeam = newAppliedMatches.SelectMany(m => m.GetTeams().Select(team => (team, m)))
                                             .GroupBy(x => x.team, x => x.m)
                                             .ToDictionary(g => g.Key, g => g.AsEnumerable());

        _rows.Values.ForEach(x => x.Compute(matchesByTeam.GetValueOrDefault(x.Team, []), Rules));

        _appliedMatches.Set(newAppliedMatches);
        SetComparerContext();

        InvalidateRankCache();
    }

    private void InvalidateRankCache() => _rankCache = null;

    private void RebuildRankCache() => _rankCache = SortedRows
            .Select((row, index) => new { row.Team, Rank = index + 1 })
            .ToDictionary(x => x.Team, x => x.Rank);

    private void SetComparerContext() => Rules.Comparer.SetContext(_appliedMatches, Rules);

    public IEnumerator<StandingRow> GetEnumerator() => SortedRows.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public override string ToString()
    {
        var str = new StringBuilder();

        SortedRows.ForEach((x, index) => str.AppendLine(CultureInfo.CurrentCulture, $"{index + 1} : {x.ToString()}"));

        return str.ToString();
    }
}
