// -----------------------------------------------------------------------
// <copyright file="KnockoutStage.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using MyClub.Scorer.Domain.CompetitionAggregate.Configurations;
using MyClub.Scorer.Domain.Primitives;
using MyClub.Scorer.Domain.RoundAggregate;
using MyClub.Shared.Kernel.Results;

namespace MyClub.Scorer.Domain.StageAggregate;

public class KnockoutStage : Stage, IKnockout
{
    private readonly List<RoundId> _rounds = [];

    // <remarks>Used by EF Core</remarks>
    private KnockoutStage() { }

    private KnockoutStage(StageId id,
                          StageId ancestorStageId,
                          string name,
                          string? shortName,
                          MatchFormat? matchFormat,
                          MatchRules? rules,
                          bool isConsolation)
        : base(id, ancestorStageId, name, shortName, matchFormat, rules, isConsolation) { }

    public static KnockoutStage Create(StageId ancestorStageId, string name, string? shortName = null, MatchFormat? format = null, MatchRules? rules = null, bool isConsolation = false)
        => new(StageId.New(), ancestorStageId, name, shortName, format, rules, isConsolation);

    public IReadOnlyCollection<RoundId> Rounds => _rounds.AsReadOnly();

    public override StageType Type => StageType.Knockout;

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
