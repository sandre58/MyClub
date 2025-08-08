// -----------------------------------------------------------------------
// <copyright file="Round.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using MyClub.Scorer.Domain.CompetitionAggregate.Configurations;
using MyClub.Scorer.Domain.RoundAggregate.Format;
using MyClub.Shared.Domain.Teams;
using MyClub.Shared.Domain.ValueObjects;
using MyClub.Shared.Kernel.Primitives;
using MyClub.Shared.Kernel.Results;

namespace MyClub.Scorer.Domain.RoundAggregate;

/// <summary>
/// Represents a round entity in a knockout-style competition where teams compete in elimination matches.
/// A round groups together fixtures between teams and manages the progression through different stages
/// of an elimination tournament.
/// </summary>
public sealed class Round : AuditableEntity<RoundId>
{
    private readonly List<TeamReference> _teams = [];
    private readonly List<RoundStage> _stages = [];
    private readonly List<Fixture> _fixtures = [];

    [UsedImplicitly(Reason = "Used by EF Core.")]
    private Round()
    {
        Format = null!;
        DisplayName = null!;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Round"/> class with the specified parameters.
    /// </summary>
    /// <param name="id">The unique identifier for the round.</param>
    /// <param name="ancestorId">The identifier of the ancestor round in the tournament hierarchy. Can be null for initial rounds.</param>
    /// <param name="name">The full name of the round (e.g., "Quarter-finals", "Round of 16").</param>
    /// <param name="shortName">The short name or abbreviation of the round (e.g., "QF", "R16").</param>
    /// <param name="format">The format configuration for this round (single, home-and-away, etc.).</param>
    /// <param name="rules">The match rules configuration for this round.</param>
    /// <param name="isConsolation">Whether this round is part of a consolation tournament.</param>
    private Round(RoundId id,
        RoundId? ancestorId,
        string name,
        string? shortName,
        RoundFormat format,
        MatchRules rules,
        bool isConsolation)
        : base(id)
    {
        IsConsolation = isConsolation;
        AncestorRoundId = ancestorId;
        Format = format;
        Rules = rules;
        DisplayName = new(name, shortName);
    }

    /// <summary>
    /// Creates a new round with the specified parameters.
    /// </summary>
    /// <param name="ancestorId">The identifier of the ancestor round. Can be null for initial rounds.</param>
    /// <param name="format">The format configuration for this round.</param>
    /// <param name="name">The full name of the round.</param>
    /// <param name="shortName">The short name of the round. Will be auto-generated if not provided.</param>
    /// <param name="rules">The match rules for this round. Uses default if not specified.</param>
    /// <param name="isConsolation">Whether this is a consolation round. Defaults to false.</param>
    /// <returns>A new <see cref="Round"/> instance.</returns>
    public static Round Create(RoundId? ancestorId,
        RoundFormat format,
        string name,
        string? shortName = null,
        MatchRules? rules = null,
        bool isConsolation = false)
        => new(RoundId.New(), ancestorId, name, shortName, format, rules ?? MatchRules.Default, isConsolation);

    /// <summary>
    /// Gets the display name of the round, including both full name and short name.
    /// </summary>
    public DisplayName DisplayName { get; }

    /// <summary>
    /// Gets the identifier of the ancestor round in the tournament hierarchy.
    /// This creates a parent-child relationship for tournament progression.
    /// </summary>
    public RoundId? AncestorRoundId { get; }

    /// <summary>
    /// Gets a value indicating whether this round is part of a consolation tournament.
    /// Consolation rounds are for teams eliminated from the main competition.
    /// </summary>
    public bool IsConsolation { get; private set; }

    /// <summary>
    /// Gets the format configuration for this round, defining how matches are organized.
    /// This includes whether it's single elimination, home-and-away, best-of series, etc.
    /// </summary>
    public RoundFormat Format { get; private set; }

    /// <summary>
    /// Gets or sets the match rules configuration for this round.
    /// Can be null to inherit rules from the parent competition.
    /// </summary>
    public MatchRules? Rules { get; set; }

    /// <summary>
    /// Gets the read-only collection of team references participating in this round.
    /// Team references can be concrete teams or virtual placeholders.
    /// </summary>
    public IReadOnlyCollection<TeamReference> Teams => _teams.AsReadOnly();

    /// <summary>
    /// Gets the read-only collection of fixtures (matchups) in this round.
    /// Each fixture represents a pairing between two teams.
    /// </summary>
    public IReadOnlyCollection<Fixture> Fixtures => _fixtures.AsReadOnly();

    /// <summary>
    /// Gets the read-only collection of stages within this round.
    /// Stages represent different phases like first leg, second leg, or individual matches in a series.
    /// </summary>
    public IReadOnlyCollection<RoundStage> Stages => _stages.AsReadOnly();

    /// <summary>
    /// Updates the match format configuration for this round.
    /// This method preserves the round format type while updating timing and rules.
    /// </summary>
    /// <param name="matchFormat">The new match format configuration to apply.</param>
    public void UpdateMatchFormat(MatchFormat matchFormat)
        => Format = Format with
        {
            RegulationTime = matchFormat.RegulationTime,
            ExtraTime = matchFormat.ExtraTime,
            NumberOfPenaltyShootouts = matchFormat.NumberOfPenaltyShootouts
        };

    #region Fixtures

    /// <summary>
    /// Creates and adds a new fixture between two teams to this round.
    /// </summary>
    /// <param name="team1">The first team in the fixture.</param>
    /// <param name="team2">The second team in the fixture.</param>
    /// <returns>
    /// A <see cref="Result{T}"/> containing the created fixture if successful,
    /// or a failure result if either team is already assigned to another fixture.
    /// </returns>
    public Result<Fixture> AddFixture(TeamReference team1, TeamReference team2) => AddFixture(Fixture.Create(team1, team2));

    /// <summary>
    /// Adds an existing fixture to this round.
    /// </summary>
    /// <param name="fixture">The fixture to add to this round.</param>
    /// <returns>
    /// A <see cref="Result{T}"/> containing the added fixture if successful,
    /// or a failure result if either team in the fixture is already assigned to another fixture in this round.
    /// </returns>
    /// <remarks>
    /// Each team can only participate in one fixture per round to ensure proper tournament bracket structure.
    /// </remarks>
    public Result<Fixture> AddFixture(Fixture fixture)
    {
        var teams = Fixtures.SelectMany(static x => new List<TeamReference>
        {
            x.Team1, x.Team2
        }).ToList();
        if (teams.Contains(fixture.Team1))
            return Failures.TeamIsAlreadyAssignedToFeature<Fixture>(fixture.Team1.ToString());
        if (teams.Contains(fixture.Team2))
            return Failures.TeamIsAlreadyAssignedToFeature<Fixture>(fixture.Team2.ToString());

        _fixtures.Add(fixture);

        return Result.Success(fixture);
    }

    /// <summary>
    /// Removes a fixture from this round.
    /// </summary>
    /// <param name="item">The fixture to remove.</param>
    /// <returns>True if the fixture was successfully removed; false if the fixture was not found.</returns>
    public bool RemoveFixture(Fixture item) => _fixtures.Remove(item);

    #endregion

    #region Teams

    /// <summary>
    /// Adds a team reference to this round.
    /// </summary>
    /// <param name="team">The team reference to add. Can be concrete or virtual.</param>
    /// <returns>
    /// A <see cref="Result{T}"/> containing the added team reference if successful,
    /// or a failure result if the team reference already exists in this round.
    /// </returns>
    public Result<TeamReference> AddTeam(TeamReference team)
    {
        if (Teams.Contains(team))
            return Failures.AlreadyExists<TeamReference>(team.ToString());

        _teams.Add(team);

        return Result.Success(team);
    }

    /// <summary>
    /// Removes a team reference from this round.
    /// This operation also removes any fixtures in which the team participates.
    /// </summary>
    /// <param name="team">The team reference to remove.</param>
    /// <returns>True if the team reference was successfully removed; false if the team reference was not found.</returns>
    /// <remarks>
    /// When a team is removed, all fixtures involving that team are automatically removed to maintain consistency.
    /// </remarks>
    public bool RemoveTeam(TeamReference team)
    {
        Fixtures.Where(x => x.Participate(team)).ToList().ForEach(y => RemoveFixture(y));
        return _teams.Remove(team);
    }

    #endregion
}
