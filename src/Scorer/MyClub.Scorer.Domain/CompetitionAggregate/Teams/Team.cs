// -----------------------------------------------------------------------
// <copyright file="Team.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using MyClub.Shared.Domain.Extensions;
using MyClub.Shared.Domain.Teams;
using MyClub.Shared.Kernel.Primitives;
using MyClub.Shared.Kernel.Results;
using MyNet.Utilities;

namespace MyClub.Scorer.Domain.CompetitionAggregate.Teams;

/// <summary>
/// Represents a football team entity that participates in competitions.
/// A team serves as an aggregate root that manages its roster of players and staff members,
/// and can participate in various competitions and matches.
/// </summary>
public sealed class Team : TeamBase<TeamId>, IAggregateRoot
{
    private readonly List<Player> _players = [];
    private readonly List<Manager> _staff = [];

    [UsedImplicitly(Reason = "Used by EF Core.")]
    private Team() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="Team"/> class with the specified parameters.
    /// </summary>
    /// <param name="id">The unique identifier for the team.</param>
    /// <param name="name">The full name of the team.</param>
    /// <param name="shortName">The short name or abbreviation of the team. If not provided, will be auto-generated from initials.</param>
    private Team(TeamId id, string name, string? shortName = null)
        : base(id, name, shortName ?? name.GetInitials())
    {
    }

    /// <summary>
    /// Creates a new team with the specified name.
    /// </summary>
    /// <param name="name">The full name of the team.</param>
    /// <param name="shortName">The short name or abbreviation of the team. If not provided, will be auto-generated from initials.</param>
    /// <returns>A new <see cref="Team"/> instance.</returns>
    /// <remarks>
    /// The team name should be unique within a competition context. The short name is useful
    /// for display in limited space contexts like standings tables or match scoreboards.
    /// </remarks>
    public static Team Create(string name, string? shortName = null) => new(TeamId.New(), name, shortName);

    /// <summary>
    /// Gets the read-only collection of players belonging to this team.
    /// </summary>
    public IReadOnlyCollection<Player> Players => _players.AsReadOnly();

    /// <summary>
    /// Gets the read-only collection of staff members (managers, coaches) belonging to this team.
    /// </summary>
    public IReadOnlyCollection<Manager> Staff => _staff.AsReadOnly();

    #region Players

    /// <summary>
    /// Creates and adds a new player to the team roster.
    /// </summary>
    /// <param name="firstName">The first name of the player.</param>
    /// <param name="lastName">The last name of the player.</param>
    /// <returns>
    /// A <see cref="Result{T}"/> containing the created player if successful,
    /// or a failure result if a player with the same name already exists on the team.
    /// </returns>
    public Result<Player> AddPlayer(string firstName, string lastName) => AddPlayer(Player.Create(firstName, lastName));

    /// <summary>
    /// Adds an existing player to the team roster.
    /// </summary>
    /// <param name="player">The player to add to the team.</param>
    /// <returns>
    /// A <see cref="Result{T}"/> containing the added player if successful,
    /// or a failure result if the player already exists on the team.
    /// </returns>
    /// <remarks>
    /// This method ensures that each player can only be registered once per team,
    /// preventing duplicate player registrations.
    /// </remarks>
    public Result<Player> AddPlayer(Player player)
    {
        if (Players.Contains(player))
            return Failures.AlreadyExists<Player>(player.GetFullName());

        _players.Add(player);

        return Result.Success(player);
    }

    /// <summary>
    /// Removes a player from the team roster.
    /// </summary>
    /// <param name="player">The player to remove from the team.</param>
    /// <returns>True if the player was successfully removed; false if the player was not found on the team.</returns>
    public bool RemovePlayer(Player player) => _players.Remove(player);

    /// <summary>
    /// Removes multiple players from the team roster in a single operation.
    /// </summary>
    /// <param name="players">The collection of players to remove from the team.</param>
    /// <returns>The number of players successfully removed from the team.</returns>
    public int RemovePlayers(IEnumerable<Player> players) => players.Count(RemovePlayer);

    #endregion

    #region Managers

    /// <summary>
    /// Creates and adds a new manager to the team staff.
    /// </summary>
    /// <param name="firstName">The first name of the manager.</param>
    /// <param name="lastName">The last name of the manager.</param>
    /// <returns>
    /// A <see cref="Result{T}"/> containing the created manager if successful,
    /// or a failure result if a manager with the same name already exists on the team.
    /// </returns>
    public Result<Manager> AddManager(string firstName, string lastName) => AddManager(Manager.Create(firstName, lastName));

    /// <summary>
    /// Adds an existing manager to the team staff.
    /// </summary>
    /// <param name="manager">The manager to add to the team staff.</param>
    /// <returns>
    /// A <see cref="Result{T}"/> containing the added manager if successful,
    /// or a failure result if the manager already exists on the team staff.
    /// </returns>
    /// <remarks>
    /// This method ensures that each manager can only be registered once per team,
    /// preventing duplicate staff registrations.
    /// </remarks>
    public Result<Manager> AddManager(Manager manager)
    {
        if (Staff.Contains(manager))
            return Failures.AlreadyExists<Manager>(manager.GetFullName());

        _staff.Add(manager);

        return Result.Success(manager);
    }

    /// <summary>
    /// Removes a manager from the team staff.
    /// </summary>
    /// <param name="manager">The manager to remove from the team staff.</param>
    /// <returns>True if the manager was successfully removed; false if the manager was not found on the team staff.</returns>
    public bool RemoveManager(Manager manager) => _staff.Remove(manager);

    /// <summary>
    /// Removes multiple managers from the team staff in a single operation.
    /// </summary>
    /// <param name="managers">The collection of managers to remove from the team staff.</param>
    /// <returns>The number of managers successfully removed from the team staff.</returns>
    public int RemoveManagers(IEnumerable<Manager> managers) => managers.Count(RemoveManager);

    #endregion
}
