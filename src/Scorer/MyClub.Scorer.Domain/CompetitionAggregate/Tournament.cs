// -----------------------------------------------------------------------
// <copyright file="Tournament.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using MyClub.Scorer.Domain.CompetitionAggregate.Configurations;
using MyClub.Scorer.Domain.RoundAggregate;
using MyClub.Shared.Kernel.Results;

namespace MyClub.Scorer.Domain.CompetitionAggregate;

public class Tournament : Competition
{
    private readonly List<RoundId> _rounds = [];

    // <remarks>Used by EF Core</remarks>
    private Tournament()
        : base() { }

    private Tournament(CompetitionId id, string name, string? shortName, MatchFormat format, MatchRules rules)
        : base(id, name, shortName, format, rules) { }

    public static Tournament Create(string name, string? shortName = null, MatchFormat? format = null, MatchRules? rules = null)
        => new(CompetitionId.New(), name, shortName, format ?? MatchFormat.Default, rules ?? MatchRules.Default);

    public IReadOnlyCollection<RoundId> Rounds => _rounds.AsReadOnly();

    public override CompetitionType Type => CompetitionType.Cup;

    #region Rounds

    public Result<RoundId> AddRound(RoundId roundId)
    {
        _rounds.Add(roundId);

        return Result.Success(roundId);
    }

    public bool RemoveRound(RoundId roundId) => _rounds.Remove(roundId);

    public void Clear() => _rounds.Clear();

    #endregion
}
