// -----------------------------------------------------------------------
// <copyright file="Team.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using MyClub.Shared.Domain.Extensions;
using MyClub.Shared.Domain.Teams;
using MyClub.Shared.Kernel.Primitives;
using MyClub.Shared.Kernel.Results;
using MyNet.Utilities;

namespace MyClub.Scorer.Domain.CompetitionAggregate.Teams;

public sealed class Team : TeamBase<TeamId>, IAggregateRoot
{
    private readonly List<Player> _players = [];
    private readonly List<Manager> _staff = [];

    // <remarks>Used by EF Core</remarks>
    private Team()
        : base() { }

    private Team(TeamId id, string name, string? shortName = null)
        : base(id, name, shortName ?? name.GetInitials())
    {
    }

    public static Team Create(string name, string? shortName = null) => new(TeamId.New(), name, shortName);

    public IReadOnlyCollection<Player> Players => _players.AsReadOnly();

    public IReadOnlyCollection<Manager> Staff => _staff.AsReadOnly();

    #region Players

    public Result<Player> AddPlayer(string firstName, string lastName) => AddPlayer(Player.Create(firstName, lastName));

    public Result<Player> AddPlayer(Player player)
    {
        if (Players.Contains(player))
            return Failures.AlreadyExists<Player>(player.GetFullName());

        _players.Add(player);

        return Result<Player>.Success(player);
    }

    public bool RemovePlayer(Player player) => _players.Remove(player);

    public int RemovePlayers(IEnumerable<Player> players) => players.Count(RemovePlayer);

    #endregion

    #region Managers

    public Result<Manager> AddManager(string firstName, string lastName) => AddManager(Manager.Create(firstName, lastName));

    public Result<Manager> AddManager(Manager manager)
    {
        if (Staff.Contains(manager))
            return Failures.AlreadyExists<Manager>(manager.GetFullName());

        _staff.Add(manager);

        return Result<Manager>.Success(manager);
    }

    public bool RemoveManager(Manager manager) => _staff.Remove(manager);

    public int RemoveManagers(IEnumerable<Manager> managers) => managers.Count(RemoveManager);

    #endregion
}
