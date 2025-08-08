// -----------------------------------------------------------------------
// <copyright file="Cup.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using JetBrains.Annotations;
using MyClub.Scorer.Domain.CompetitionAggregate.Configurations;
using MyClub.Scorer.Domain.RoundAggregate;
using MyClub.Shared.Kernel.Results;

namespace MyClub.Scorer.Domain.CompetitionAggregate;

/// <summary>
/// Represents a cup competition where teams compete in knockout elimination rounds.
/// A cup is characterized by single-elimination or home-and-away rounds where losing teams are eliminated
/// until only one winner remains.
/// </summary>
public class Cup : Competition
{
    private readonly List<RoundId> _rounds = [];

    [UsedImplicitly(Reason = "Used by EF Core.")]
    private Cup() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="Cup"/> class with the specified parameters.
    /// </summary>
    /// <param name="id">The unique identifier for the cup.</param>
    /// <param name="name">The full name of the cup.</param>
    /// <param name="shortName">The short name or abbreviation of the cup.</param>
    /// <param name="format">The default match format configuration for this cup.</param>
    /// <param name="rules">The default match rules configuration for this cup.</param>
    private Cup(CompetitionId id, string name, string? shortName, MatchFormat format, MatchRules rules)
        : base(id, name, shortName, format, rules) { }

    /// <summary>
    /// Creates a new cup with the specified parameters.
    /// </summary>
    /// <param name="name">The full name of the cup.</param>
    /// <param name="shortName">The short name or abbreviation of the cup.</param>
    /// <param name="format">The default match format configuration for this cup. Uses default if not specified.</param>
    /// <param name="rules">The default match rules configuration for this cup. Uses default if not specified.</param>
    /// <returns>A new <see cref="Cup"/> instance.</returns>
    public static Cup Create(string name, string? shortName = null, MatchFormat? format = null, MatchRules? rules = null)
        => new(CompetitionId.New(), name, shortName, format ?? MatchFormat.Default, rules ?? MatchRules.Default);

    /// <summary>
    /// Gets the read-only collection of round identifiers that belong to this cup.
    /// Rounds represent the elimination phases of the cup competition.
    /// </summary>
    public IReadOnlyCollection<RoundId> Rounds => _rounds.AsReadOnly();

    /// <summary>
    /// Gets the type of competition, which is always <see cref="CompetitionType.Cup"/> for this class.
    /// </summary>
    public override CompetitionType Type => CompetitionType.Cup;

    #region Rounds

    /// <summary>
    /// Adds a round to the cup competition.
    /// </summary>
    /// <param name="roundId">The identifier of the round to add.</param>
    /// <returns>
    /// A <see cref="Result{T}"/> containing the added round ID if successful,
    /// or a failure result if the round already exists in the cup.
    /// </returns>
    public Result<RoundId> AddRound(RoundId roundId)
    {
        if (_rounds.Contains(roundId))
            return Failures.AlreadyExists<RoundId>(roundId.ToString());

        _rounds.Add(roundId);

        return Result.Success(roundId);
    }

    /// <summary>
    /// Removes a round from the cup competition.
    /// </summary>
    /// <param name="roundId">The identifier of the round to remove.</param>
    /// <returns>True if the round was successfully removed; false if the round was not found.</returns>
    public bool RemoveRound(RoundId roundId) => _rounds.Remove(roundId);

    /// <summary>
    /// Clears all rounds from the cup competition.
    /// This operation removes all elimination phases from the cup.
    /// </summary>
    public void Clear() => _rounds.Clear();

    #endregion
}
