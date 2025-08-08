// -----------------------------------------------------------------------
// <copyright file="Stage.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using MyClub.Scorer.Domain.CompetitionAggregate.Configurations;
using MyClub.Shared.Domain.Teams;
using MyClub.Shared.Domain.ValueObjects;
using MyClub.Shared.Kernel.Primitives;
using MyClub.Shared.Kernel.Results;

namespace MyClub.Scorer.Domain.StageAggregate;

/// <summary>
/// Abstract base class representing a stage within a tournament competition.
/// A stage defines a distinct phase of competition with its own format, rules, and participating teams.
/// Stages can be organized hierarchically and support various competition formats.
/// </summary>
public abstract class Stage : AuditableEntity<StageId>
{
    private readonly List<TeamReference> _teams = [];

    [UsedImplicitly(Reason = "Used by EF Core.")]
    protected Stage()
    {
        MatchFormat = null!;
        Rules = null!;
        DisplayName = null!;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Stage"/> class with the specified parameters.
    /// </summary>
    /// <param name="id">The unique identifier for the stage.</param>
    /// <param name="ancestorId">The identifier of the ancestor stage in the tournament hierarchy. Can be null for root stages.</param>
    /// <param name="name">The full name of the stage (e.g., "Group Stage", "Quarter-finals").</param>
    /// <param name="shortName">The short name or abbreviation of the stage (e.g., "GS", "QF").</param>
    /// <param name="matchFormat">The match format configuration for this stage.</param>
    /// <param name="rules">The match rules configuration for this stage.</param>
    /// <param name="isConsolation">Whether this stage is a consolation tournament for eliminated teams.</param>
    protected Stage(StageId id,
                    StageId? ancestorId,
                    string name,
                    string? shortName,
                    MatchFormat matchFormat,
                    MatchRules rules,
                    bool isConsolation)
        : base(id)
    {
        IsConsolation = isConsolation;
        AncestorStageId = ancestorId;
        Rules = rules;
        MatchFormat = matchFormat;
        DisplayName = new(name, shortName);
    }

    /// <summary>
    /// Gets the display name of the stage, including both full name and short name.
    /// This is used for presenting the stage in various UI contexts.
    /// </summary>
    public DisplayName DisplayName { get; }

    /// <summary>
    /// Gets the identifier of the ancestor stage in the tournament hierarchy.
    /// This creates a parent-child relationship between stages, useful for progression rules.
    /// </summary>
    /// <remarks>
    /// The ancestor relationship allows for complex tournament structures where teams
    /// progress from one stage to another based on their performance. For example,
    /// group winners might advance to a knockout stage, or eliminated teams might
    /// enter a consolation tournament.
    /// </remarks>
    public StageId? AncestorStageId { get; }

    /// <summary>
    /// Gets a value indicating whether this stage is a consolation tournament.
    /// Consolation stages are typically for teams eliminated from the main competition.
    /// </summary>
    public bool IsConsolation { get; private set; }

    /// <summary>
    /// Gets or sets the match format configuration for this stage.
    /// This defines timing, extra time, and penalty shootout rules specific to this stage.
    /// </summary>
    public MatchFormat MatchFormat { get; set; }

    /// <summary>
    /// Gets or sets the match rules configuration for this stage.
    /// This includes allowed cards and other rule-specific settings for this stage.
    /// </summary>
    public MatchRules Rules { get; set; }

    /// <summary>
    /// Gets the type of stage (Group, Knockout, Championship).
    /// This is implemented by concrete stage classes to define their behavior.
    /// </summary>
    public abstract StageType Type { get; }

    /// <summary>
    /// Gets the read-only collection of team references participating in this stage.
    /// Team references can be either concrete teams or virtual placeholders resolved from other results.
    /// </summary>
    public IReadOnlyCollection<TeamReference> Teams => _teams.AsReadOnly();

    #region Teams

    /// <summary>
    /// Adds a team reference to this stage.
    /// </summary>
    /// <param name="team">The team reference to add. Can be concrete or virtual.</param>
    /// <returns>
    /// A <see cref="Result{T}"/> containing the added team reference if successful,
    /// or a failure result if the team reference already exists in this stage.
    /// </returns>
    /// <remarks>
    /// Team references support complex tournament progression where teams may not be known
    /// until earlier stages are completed. Examples include "Winner of Group A" or
    /// "Runner-up of Semi-final 1".
    /// </remarks>
    public Result<TeamReference> AddTeam(TeamReference team)
    {
        if (Teams.Contains(team))
            return Failures.AlreadyExists<TeamReference>(team.ToString());

        _teams.Add(team);

        return Result.Success(team);
    }

    /// <summary>
    /// Removes a team reference from this stage.
    /// </summary>
    /// <param name="team">The team reference to remove from this stage.</param>
    /// <returns>True if the team reference was successfully removed; false if the team reference was not found.</returns>
    public virtual bool RemoveTeam(TeamReference team) => _teams.Remove(team);

    #endregion

    /// <summary>
    /// Returns a string representation of the stage using its display name.
    /// This provides a user-friendly representation of the stage for logging and UI purposes.
    /// </summary>
    /// <returns>The display name of the stage.</returns>
    public override string ToString() => DisplayName;
}
