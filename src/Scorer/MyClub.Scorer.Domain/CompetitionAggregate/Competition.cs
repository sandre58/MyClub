// -----------------------------------------------------------------------
// <copyright file="Competition.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using MyClub.Scorer.Domain.CompetitionAggregate.Configurations;
using MyClub.Scorer.Domain.CompetitionAggregate.Stadiums;
using MyClub.Scorer.Domain.CompetitionAggregate.Teams;
using MyClub.Scorer.Domain.Primitives;
using MyClub.Shared.Domain.Enums;
using MyClub.Shared.Domain.Stadiums;
using MyClub.Shared.Domain.Teams;
using MyClub.Shared.Domain.ValueObjects;
using MyClub.Shared.Kernel.Primitives;
using MyClub.Shared.Kernel.Results;

namespace MyClub.Scorer.Domain.CompetitionAggregate;

/// <summary>
/// Represents the aggregate root for a sports competition in the Scorer domain.
/// A competition can be of different types (League, Cup, Tournament) and manages teams, stadiums, and match configurations.
/// This is the main aggregate that orchestrates the organization of sporting events.
/// </summary>
public abstract class Competition : AuditableEntity<CompetitionId>, ITeamsContainer
{
    private readonly List<Team> _teams = [];
    private readonly List<Stadium> _stadiums = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="Competition"/> class.
    /// This constructor is used by Entity Framework Core for entity materialization.
    /// </summary>
    [UsedImplicitly(Reason = "Used by EF Core.")]
    protected Competition()
    {
        MatchFormat = null!;
        MatchRules = null!;
        DisplayName = null!;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Competition"/> class with the specified parameters.
    /// </summary>
    /// <param name="id">The unique identifier for the competition.</param>
    /// <param name="name">The full name of the competition.</param>
    /// <param name="shortName">The short name or abbreviation of the competition.</param>
    /// <param name="format">The default match format configuration for this competition.</param>
    /// <param name="rules">The default match rules configuration for this competition.</param>
    protected Competition(CompetitionId id, string name, string? shortName, MatchFormat format, MatchRules rules)
        : base(id)
    {
        DisplayName = new(name, shortName);
        MatchFormat = format;
        MatchRules = rules;
    }

    /// <summary>
    /// Gets the display name of the competition, including both full name and short name.
    /// </summary>
    public DisplayName DisplayName { get; }

    /// <summary>
    /// Gets or sets the logo of the competition as a byte array.
    /// Can be null if no logo is set.
    /// </summary>
    public byte? Logo { get; set; }

    /// <summary>
    /// Gets or sets the default match format configuration for this competition.
    /// This includes timing, extra time, and penalty shootout settings.
    /// </summary>
    public MatchFormat MatchFormat { get; set; }

    /// <summary>
    /// Gets or sets the default match rules configuration for this competition.
    /// This includes allowed cards and other rule-specific settings.
    /// </summary>
    public MatchRules MatchRules { get; set; }

    /// <summary>
    /// Gets the type of competition (League, Cup, or Tournament).
    /// This is implemented by concrete competition classes.
    /// </summary>
    public abstract CompetitionType Type { get; }

    /// <summary>
    /// Gets the read-only collection of teams participating in this competition.
    /// </summary>
    public IReadOnlyCollection<Team> Teams => _teams.AsReadOnly();

    /// <summary>
    /// Gets the read-only collection of stadiums available for this competition.
    /// </summary>
    public IReadOnlyCollection<Stadium> Stadiums => _stadiums.AsReadOnly();

    /// <summary>
    /// Gets the collection of team references for interface compatibility.
    /// Converts concrete teams to team references for use with the ITeamsContainer interface.
    /// </summary>
    IReadOnlyCollection<TeamReference> ITeamsContainer.Teams => Teams.Select(static x => new ConcreteTeamReference(x.Id)).ToList().AsReadOnly();

    #region Teams

    /// <summary>
    /// Adds a new team to the competition by creating it with the specified name.
    /// </summary>
    /// <param name="name">The full name of the team.</param>
    /// <param name="shortName">The short name or abbreviation of the team.</param>
    public virtual void AddTeam(string name, string? shortName = null) => AddTeam(Team.Create(name, shortName));

    /// <summary>
    /// Adds an existing team to the competition.
    /// </summary>
    /// <param name="team">The team to add to the competition.</param>
    /// <returns>
    /// A <see cref="Result{T}"/> containing the added team if successful,
    /// or a failure result if the team already exists in the competition.
    /// </returns>
    public virtual Result<Team> AddTeam(Team team)
    {
        if (_teams.Contains(team))
            return Failures.AlreadyExists<Team>(team.DisplayName);

        _teams.Add(team);

        return Result.Success(team);
    }

    /// <summary>
    /// Removes a team from the competition.
    /// </summary>
    /// <param name="team">The team to remove from the competition.</param>
    /// <returns>True if the team was successfully removed; false if the team was not found.</returns>
    public virtual bool RemoveTeam(Team team) => _teams.Remove(team);

    /// <summary>
    /// Checks if a team with a similar name already exists in the competition.
    /// This method performs a case-insensitive comparison of team names.
    /// </summary>
    /// <param name="name">The name to check for similarity.</param>
    /// <param name="excludeTeamId">Optional team ID to exclude from the check (useful for updates).</param>
    /// <returns>True if a team with a similar name exists; otherwise, false.</returns>
    public bool HasSimilarTeams(string name, TeamId? excludeTeamId = null) => Teams.Any(t => !t.Id.Equals(excludeTeamId ?? TeamId.Empty) && t.DisplayName.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    #endregion

    #region Stadiums

    /// <summary>
    /// Adds a new stadium to the competition by creating it with the specified parameters.
    /// </summary>
    /// <param name="name">The name of the stadium.</param>
    /// <param name="ground">The type of ground surface (default is Grass).</param>
    /// <returns>
    /// A <see cref="Result{T}"/> containing the created stadium if successful,
    /// or a failure result if a stadium with the same name already exists.
    /// </returns>
    public virtual Result<Stadium> AddStadium(string name, Ground ground = Ground.Grass) => AddStadium(Stadium.Create(name, ground));

    /// <summary>
    /// Adds an existing stadium to the competition.
    /// </summary>
    /// <param name="stadium">The stadium to add to the competition.</param>
    /// <returns>
    /// A <see cref="Result{T}"/> containing the added stadium if successful,
    /// or a failure result if the stadium already exists in the competition.
    /// </returns>
    public virtual Result<Stadium> AddStadium(Stadium stadium)
    {
        if (_stadiums.Contains(stadium))
            return Failures.AlreadyExists<Stadium>(stadium.DisplayName);

        _stadiums.Add(stadium);

        return Result.Success(stadium);
    }

    /// <summary>
    /// Removes a stadium from the competition.
    /// </summary>
    /// <param name="stadium">The stadium to remove from the competition.</param>
    /// <returns>True if the stadium was successfully removed; false if the stadium was not found.</returns>
    public virtual bool RemoveStadium(Stadium stadium) => _stadiums.Remove(stadium);

    /// <summary>
    /// Checks if a stadium with the specified ID exists in this competition.
    /// </summary>
    /// <param name="id">The stadium ID to check.</param>
    /// <returns>True if the stadium exists in the competition; otherwise, false.</returns>
    public bool HasStadium(StadiumId id) => Stadiums.Any(s => s.Id == id);

    #endregion
}
