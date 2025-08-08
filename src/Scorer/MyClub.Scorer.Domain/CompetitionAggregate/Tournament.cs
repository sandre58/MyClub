// -----------------------------------------------------------------------
// <copyright file="Tournament.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using JetBrains.Annotations;
using MyClub.Scorer.Domain.CompetitionAggregate.Configurations;
using MyClub.Scorer.Domain.StageAggregate;
using MyClub.Shared.Kernel.Results;

namespace MyClub.Scorer.Domain.CompetitionAggregate;

/// <summary>
/// Represents a tournament competition that combines multiple phases and formats.
/// A tournament is characterized by complex structures that can include group stages,
/// knockout phases, and various progression rules between different stages.
/// </summary>
public class Tournament : Competition
{
    private readonly List<StageId> _stages = [];

    [UsedImplicitly(Reason = "Used by EF Core.")]
    private Tournament() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="Tournament"/> class with the specified parameters.
    /// </summary>
    /// <param name="id">The unique identifier for the tournament.</param>
    /// <param name="name">The full name of the tournament.</param>
    /// <param name="shortName">The short name or abbreviation of the tournament.</param>
    /// <param name="format">The default match format configuration for this tournament.</param>
    /// <param name="rules">The default match rules configuration for this tournament.</param>
    private Tournament(CompetitionId id, string name, string? shortName, MatchFormat format, MatchRules rules)
        : base(id, name, shortName, format, rules) { }

    /// <summary>
    /// Creates a new tournament with the specified parameters.
    /// </summary>
    /// <param name="name">The full name of the tournament.</param>
    /// <param name="shortName">The short name or abbreviation of the tournament.</param>
    /// <param name="format">The default match format configuration for this tournament. Uses default if not specified.</param>
    /// <param name="rules">The default match rules configuration for this tournament. Uses default if not specified.</param>
    /// <returns>A new <see cref="Tournament"/> instance.</returns>
    public static Tournament Create(string name, string? shortName = null, MatchFormat? format = null, MatchRules? rules = null)
        => new(CompetitionId.New(), name, shortName, format ?? MatchFormat.Default, rules ?? MatchRules.Default);

    /// <summary>
    /// Gets the read-only collection of stage identifiers that belong to this tournament.
    /// Stages represent the different phases of the tournament (e.g., group stage, quarter-finals, semi-finals, final).
    /// </summary>
    public IReadOnlyCollection<StageId> Stages => _stages.AsReadOnly();

    /// <summary>
    /// Gets the type of competition, which is always <see cref="CompetitionType.Tournament"/> for this class.
    /// </summary>
    public override CompetitionType Type => CompetitionType.Tournament;

    #region Stages

    /// <summary>
    /// Adds a stage to the tournament competition.
    /// </summary>
    /// <param name="stageId">The identifier of the stage to add.</param>
    /// <returns>
    /// A <see cref="Result{T}"/> containing the added stage ID if successful,
    /// or a failure result if the stage already exists in the tournament.
    /// </returns>
    public Result<StageId> AddStage(StageId stageId)
    {
        if (_stages.Contains(stageId))
            return Failures.AlreadyExists<StageId>(stageId.ToString());

        _stages.Add(stageId);

        return Result.Success(stageId);
    }

    /// <summary>
    /// Removes a stage from the tournament competition.
    /// </summary>
    /// <param name="stageId">The identifier of the stage to remove.</param>
    /// <returns>True if the stage was successfully removed; false if the stage was not found.</returns>
    public bool RemoveStage(StageId stageId) => _stages.Remove(stageId);

    /// <summary>
    /// Clears all stages from the tournament competition.
    /// This operation removes all phases from the tournament.
    /// </summary>
    public void Clear() => _stages.Clear();

    #endregion
}
