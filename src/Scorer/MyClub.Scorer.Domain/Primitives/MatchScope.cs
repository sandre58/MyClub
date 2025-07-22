// -----------------------------------------------------------------------
// <copyright file="MatchScope.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using MyClub.Shared.Domain.Matchs;
using MyClub.Shared.Kernel.Primitives;
using MyClub.Shared.Kernel.Results;

namespace MyClub.Scorer.Domain.Primitives;

public abstract class MatchScope<TId> : AuditableEntity<TId>
    where TId : EntityId<TId>
{
    private readonly List<MatchId> _matches = [];
    private DateTime? _postponedDate;

    // <remarks>Used by EF Core</remarks>
    protected MatchScope()
        : base() { }

    protected MatchScope(TId id, DateTime date)
        : base(id) => OriginDate = date;

    public DateTime OriginDate { get; private set; }

    public DateTime Date => _postponedDate ?? OriginDate;

    public bool IsPostponed { get; private set; }

    public IReadOnlyCollection<MatchId> Matches => _matches.AsReadOnly();

    public void Postpone(DateTime? date = null)
    {
        IsPostponed = true;
        _postponedDate = date;
    }

    public void Schedule(DateTime date)
    {
        IsPostponed = false;
        _postponedDate = null;
        OriginDate = date;
    }

    public virtual Result<MatchId> AddMatch(MatchId matchId)
    {
        if (_matches.Contains(matchId))
            return Failures.AlreadyExists<MatchId>(matchId.ToString());

        _matches.Add(matchId);

        return Result.Success(matchId);
    }

    public bool RemoveMatch(MatchId matchId) => _matches.Remove(matchId);

    public override int CompareTo(Entity<TId>? other) => other is MatchScope<TId> entity ? OriginDate.CompareTo(entity.OriginDate) : 1;
}
