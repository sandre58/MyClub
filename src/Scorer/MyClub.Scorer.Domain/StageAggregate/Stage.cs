// -----------------------------------------------------------------------
// <copyright file="Stage.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using MyClub.Scorer.Domain.CompetitionAggregate.Configurations;
using MyClub.Shared.Domain.Teams;
using MyClub.Shared.Domain.ValueObjects;
using MyClub.Shared.Kernel.Primitives;
using MyClub.Shared.Kernel.Results;
using MyNet.Utilities;

namespace MyClub.Scorer.Domain.StageAggregate;

public abstract class Stage : AuditableEntity<StageId>
{
    private readonly List<TeamReference> _teams = [];

    // <remarks>Used by EF Core</remarks>
    protected Stage()
        : base() => DisplayName = null!;

    protected Stage(StageId id,
                    StageId? ancestorId,
                    string name,
                    string? shortName,
                    MatchFormat? customMatchFormat,
                    MatchRules? customMatchRules,
                    bool isConsolation)
        : base(id)
    {
        IsConsolation = isConsolation;
        AncestorRoundId = ancestorId;
        CustomMatchRules = customMatchRules;
        CustomMatchFormat = customMatchFormat;
        DisplayName = new(name, shortName);
    }

    public DisplayName DisplayName { get; }

    public StageId? AncestorRoundId { get; }

    public bool IsConsolation { get; private set; }

    public MatchFormat? CustomMatchFormat { get; private set; }

    public MatchRules? CustomMatchRules { get; private set; }

    public abstract StageType Type { get; }

    public IReadOnlyCollection<TeamReference> Teams => _teams.AsReadOnly();

    #region Teams

    public Result<TeamReference> AddTeam(TeamReference team)
    {
        if (Teams.Contains(team))
            return Failures.AlreadyExists<TeamReference>(team.ToString());

        _teams.Add(team);

        return Result.Success(team);
    }

    public virtual bool RemoveTeam(TeamReference team) => _teams.Remove(team);

    #endregion

    public override string ToString() => DisplayName;
}
