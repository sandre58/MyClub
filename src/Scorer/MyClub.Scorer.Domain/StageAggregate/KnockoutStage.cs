// -----------------------------------------------------------------------
// <copyright file="KnockoutStage.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using JetBrains.Annotations;
using MyClub.Scorer.Domain.CompetitionAggregate.Configurations;
using MyClub.Scorer.Domain.Primitives;
using MyClub.Scorer.Domain.RoundAggregate;
using MyClub.Shared.Kernel.Results;

namespace MyClub.Scorer.Domain.StageAggregate;

/// <summary>
/// Represents a knockout stage in a tournament where teams compete in elimination-based rounds.
/// This stage type implements single or double elimination formats where losing teams
/// are progressively eliminated until only one winner remains.
/// </summary>
public class KnockoutStage : Stage, IKnockout
{
    private readonly List<RoundId> _rounds = [];

    [UsedImplicitly(Reason = "Used by EF Core.")]
    private KnockoutStage() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="KnockoutStage"/> class with the specified parameters.
    /// </summary>
    /// <param name="id">The unique identifier for the knockout stage.</param>
    /// <param name="ancestorStageId">The identifier of the ancestor stage from which teams qualify.</param>
    /// <param name="name">The name of the knockout stage.</param>
    /// <param name="shortName">The short name of the knockout stage.</param>
    /// <param name="matchFormat">The match format configuration for this stage.</param>
    /// <param name="rules">The match rules configuration for this stage.</param>
    /// <param name="isConsolation">Whether this is a consolation knockout tournament.</param>
    private KnockoutStage(StageId id,
                          StageId ancestorStageId,
                          string name,
                          string? shortName,
                          MatchFormat matchFormat,
                          MatchRules rules,
                          bool isConsolation)
        : base(id, ancestorStageId, name, shortName, matchFormat, rules, isConsolation) { }

    /// <summary>
    /// Creates a new knockout stage with the specified configuration.
    /// </summary>
    /// <param name="ancestorStageId">The identifier of the ancestor stage from which teams qualify.</param>
    /// <param name="name">The name of the knockout stage.</param>
    /// <param name="shortName">The short name of the knockout stage. Will be auto-generated if not provided.</param>
    /// <param name="format">The match format. Uses no-draw format if not specified (appropriate for knockouts).</param>
    /// <param name="rules">The match rules. Uses default if not specified.</param>
    /// <param name="isConsolation">Whether this is a consolation knockout. Defaults to false.</param>
    /// <returns>A new <see cref="KnockoutStage"/> instance.</returns>
    /// <remarks>
    /// Knockout stages typically use no-draw match formats to ensure decisive results in each match.
    /// This means extra time and penalty shootouts are used to determine winners when regulation time ends in a draw.
    /// </remarks>
    public static KnockoutStage Create(StageId ancestorStageId, string name, string? shortName = null, MatchFormat? format = null, MatchRules? rules = null, bool isConsolation = false)
        => new(StageId.New(), ancestorStageId, name, shortName, format ?? MatchFormat.NoDraw, rules ?? MatchRules.Default, isConsolation);

    /// <summary>
    /// Gets the read-only collection of round identifiers that comprise this knockout stage.
    /// Rounds represent the elimination phases (e.g., Round of 16, Quarter-finals, Semi-finals, Final).
    /// </summary>
    public IReadOnlyCollection<RoundId> Rounds => _rounds.AsReadOnly();

    /// <summary>
    /// Gets the stage type, which is always <see cref="StageType.Knockout"/> for knockout stages.
    /// </summary>
    public override StageType Type => StageType.Knockout;

    #region Rounds

    /// <summary>
    /// Adds a round to this knockout stage.
    /// </summary>
    /// <param name="roundId">The identifier of the round to add to the knockout stage.</param>
    /// <returns>
    /// A <see cref="Result{T}"/> containing the added round ID if successful.
    /// </returns>
    /// <remarks>
    /// Rounds should be added in the order they will be played, typically from earliest to latest
    /// (e.g., Round of 16, then Quarter-finals, then Semi-finals, then Final).
    /// </remarks>
    public Result<RoundId> AddRound(RoundId roundId)
    {
        _rounds.Add(roundId);

        return Result.Success(roundId);
    }

    /// <summary>
    /// Removes a round from this knockout stage.
    /// </summary>
    /// <param name="roundId">The identifier of the round to remove.</param>
    /// <returns>True if the round was successfully removed; false if the round was not found.</returns>
    public bool RemoveRound(RoundId roundId) => _rounds.Remove(roundId);

    /// <summary>
    /// Removes all rounds from this knockout stage.
    /// This effectively resets the knockout stage structure.
    /// </summary>
    public void Clear() => _rounds.Clear();

    #endregion
}
