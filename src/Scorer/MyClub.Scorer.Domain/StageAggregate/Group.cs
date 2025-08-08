// -----------------------------------------------------------------------
// <copyright file="Group.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using MyClub.Scorer.Domain.CompetitionAggregate.Configurations;
using MyClub.Scorer.Domain.MatchdayAggregate;
using MyClub.Scorer.Domain.Primitives;
using MyClub.Shared.Domain.Standings.Rules;
using MyClub.Shared.Domain.Teams;
using MyClub.Shared.Domain.ValueObjects;
using MyClub.Shared.Kernel.Primitives;
using MyNet.Utilities;

namespace MyClub.Scorer.Domain.StageAggregate;

/// <summary>
/// Represents a group within a group stage competition where teams compete in a round-robin format.
/// A group is a subset of teams within a larger group stage that play against each other
/// to determine standings and qualification for subsequent tournament phases.
/// </summary>
public class Group : AuditableEntity<GroupId>, IChampionship, ISimilar<Group>
{
    private readonly List<TeamReference> _teams = [];
    private readonly GroupStage _groupStage;

    [UsedImplicitly(Reason = "Used by EF Core.")]
    private Group()
    {
        DisplayName = null!;
        _groupStage = null!;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Group"/> class with the specified parameters.
    /// </summary>
    /// <param name="id">The unique identifier for the group.</param>
    /// <param name="groupStage">The parent group stage that contains this group.</param>
    /// <param name="teams">The teams assigned to compete within this group.</param>
    /// <param name="name">The name of the group (e.g., "Group A", "Group B").</param>
    /// <param name="shortName">The short name or abbreviation of the group (e.g., "A", "B").</param>
    private Group(GroupId id, GroupStage groupStage, IEnumerable<TeamReference> teams, string name, string? shortName)
        : base(id)
    {
        _groupStage = groupStage;
        DisplayName = new(name, shortName);
        _teams = [.. teams];
    }

    /// <summary>
    /// Creates a new group within the specified group stage.
    /// </summary>
    /// <param name="groupStage">The parent group stage that will contain this group.</param>
    /// <param name="teams">The teams to be assigned to this group.</param>
    /// <param name="name">The name of the group (e.g., "Group A", "Group B").</param>
    /// <param name="shortName">The short name of the group (e.g., "A", "B"). Will be auto-generated if not provided.</param>
    /// <returns>A new <see cref="Group"/> instance.</returns>
    /// <remarks>
    /// Group naming typically follows alphabetical conventions (A, B, C, D...) or numerical patterns (1, 2, 3, 4...)
    /// depending on the tournament tradition. The number of teams per group varies by competition but is usually
    /// between 3-6 teams to ensure a balanced round-robin format.
    /// </remarks>
    public static Group Create(GroupStage groupStage, IEnumerable<TeamReference> teams, string name, string? shortName = null)
        => new(GroupId.New(), groupStage, teams, name, shortName);

    /// <summary>
    /// Gets or sets the display name of the group, including both full name and short name.
    /// </summary>
    public DisplayName DisplayName { get; set; }

    /// <summary>
    /// Gets the read-only collection of teams competing within this group.
    /// Teams can be concrete references or virtual references resolved from other results.
    /// </summary>
    public IReadOnlyCollection<TeamReference> Teams => _teams.AsReadOnly();

    /// <summary>
    /// Gets the standing rules configuration inherited from the parent group stage.
    /// These rules determine how teams are ranked within the group standings.
    /// </summary>
    public StandingRuleSet StandingRules => _groupStage.StandingRules;

    /// <summary>
    /// Gets the standing labels configuration inherited from the parent group stage.
    /// These labels define what each position in the group standings means (e.g., "Qualified", "Eliminated").
    /// </summary>
    public StandingLabels Labels => _groupStage.Labels;

    /// <summary>
    /// Gets the read-only collection of matchday identifiers associated with this group.
    /// Matchdays organize the scheduling of group matches across multiple game weeks.
    /// </summary>
    public IReadOnlyCollection<MatchdayId> Matchdays => _groupStage.Matchdays;

    /// <summary>
    /// Gets the penalty points assigned to teams within this group.
    /// Penalty points affect final standings and can result from disciplinary actions or rule violations.
    /// </summary>
    /// <remarks>
    /// Penalty points are filtered to include only teams that belong to this specific group,
    /// even though they are managed at the group stage level for administrative efficiency.
    /// </remarks>
    public IReadOnlyDictionary<TeamId, int> PenaltyPoints => _groupStage.PenaltyPoints.Where(x => _teams.Contains(x.Key)).ToDictionary().AsReadOnly();

    /// <summary>
    /// Determines whether this group is similar to another group based on display name comparison.
    /// </summary>
    /// <param name="obj">The other group to compare with.</param>
    /// <returns>True if the groups have similar names; otherwise, false.</returns>
    public bool IsSimilar(Group? obj) => DisplayName.IsSimilar(obj?.DisplayName);

    /// <summary>
    /// Returns a string representation of the group using its display name.
    /// </summary>
    /// <returns>The display name of the group.</returns>
    public override string ToString() => DisplayName;
}
