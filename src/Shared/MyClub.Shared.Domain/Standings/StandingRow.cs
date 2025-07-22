// -----------------------------------------------------------------------
// <copyright file="StandingRow.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Text;
using MyClub.Shared.Domain.Enums;
using MyClub.Shared.Domain.Matchs;
using MyClub.Shared.Domain.Standings.Rules;
using MyClub.Shared.Domain.Teams;
using MyNet.Utilities;

namespace MyClub.Shared.Domain.Standings;

public record StandingRow(TeamReference Team, int PenaltyPoints = 0) : IStandingRow
{
    private readonly Dictionary<string, object> _columns = [];

    public int Points { get; private set; } = PenaltyPoints;

    public object? Get(string column) => _columns.GetOrDefault(column);

    public T? Get<T>(string column) => (T?)Get(column);

    public int Get(StandingColumnType column) => Get<int?>(column.ToString()) ?? 0;

    public void Compute(IEnumerable<IMatch> matches, StandingRuleSet rules)
    {
        Points = rules.ComputePoints(Team, matches);
        foreach (var column in rules.Columns)
        {
            var value = column.ComputeBatch(Team, matches);
            _columns[column.Key] = value!;
        }

        Points -= PenaltyPoints;
    }

    public void ApplyMatch(IMatch match, StandingRuleSet rules)
    {
        Points += rules.GetPoints(match.GetResultOf(Team));
        foreach (var column in rules.Columns)
        {
            var previousValue = _columns.TryGetValue(column.Key, out var val) ? val! : column.DefaultValue;
            _columns[column.Key] = column.ComputeIncremental(previousValue, Team, match)!;
        }
    }

    public override string ToString()
    {
        var str = new StringBuilder($"{Team} | {Points} PTS | ");

        _ = str.Append(string.Join(" | ", _columns.Select(x => $"{Get(x.Key)} ({x.Key})")));

        return str.ToString();
    }
}
