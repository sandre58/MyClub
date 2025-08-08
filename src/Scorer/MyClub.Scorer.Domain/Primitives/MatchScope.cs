// -----------------------------------------------------------------------
// <copyright file="MatchScope.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using MyClub.Shared.Domain.Matches;
using MyClub.Shared.Kernel.Primitives;
using MyClub.Shared.Kernel.Results;

namespace MyClub.Scorer.Domain.Primitives;

/// <summary>
/// Abstract base class for entities that represent a scope or container for organizing matches.
/// This class provides common functionality for scheduling, postponing, and managing collections of matches
/// within a defined time frame or organizational context.
/// </summary>
/// <typeparam name="TId">The strongly-typed identifier for the match scope entity.</typeparam>
[SuppressMessage("Design", "CA1036:Override methods on comparable types", Justification = "Domain entity comparison is handled through domain-specific methods")]
public abstract class MatchScope<TId> : AuditableEntity<TId>, IComparable<MatchScope<TId>>
    where TId : EntityId<TId>
{
    private readonly List<MatchId> _matches = [];
    private DateTime? _postponedDate;

    [UsedImplicitly(Reason = "Used by EF Core.")]
    protected MatchScope() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="MatchScope{TId}"/> class with the specified parameters.
    /// </summary>
    /// <param name="id">The unique identifier for the match scope.</param>
    /// <param name="date">The original scheduled date for this match scope.</param>
    protected MatchScope(TId id, DateTime date)
        : base(id) => OriginDate = date;

    /// <summary>
    /// Gets the original scheduled date for this match scope.
    /// This represents the initial planned date before any postponements.
    /// </summary>
    public DateTime OriginDate { get; private set; }

    /// <summary>
    /// Gets the effective date for this match scope.
    /// Returns the postponed date if the scope has been rescheduled, otherwise returns the original date.
    /// </summary>
    public DateTime Date => _postponedDate ?? OriginDate;

    /// <summary>
    /// Gets a value indicating whether this match scope has been postponed from its original date.
    /// </summary>
    public bool IsPostponed { get; private set; }

    /// <summary>
    /// Gets the read-only collection of match identifiers that belong to this scope.
    /// </summary>
    public IReadOnlyCollection<MatchId> Matches => _matches.AsReadOnly();

    /// <summary>
    /// Postpones this match scope to a new date or leaves it without a specific date.
    /// </summary>
    /// <param name="date">The new date for the postponed scope. If null, the scope is postponed without a specific new date.</param>
    /// <remarks>
    /// When a match scope is postponed, all matches within the scope are typically affected.
    /// The postponement can be temporary (without a new date) or rescheduled to a specific new date.
    /// </remarks>
    public void Postpone(DateTime? date = null)
    {
        IsPostponed = true;
        _postponedDate = date;
    }

    /// <summary>
    /// Schedules or reschedules this match scope to a specific date.
    /// This clears any previous postponement status and sets a new original date.
    /// </summary>
    /// <param name="date">The new scheduled date for this match scope.</param>
    public void Schedule(DateTime date)
    {
        IsPostponed = false;
        _postponedDate = null;
        OriginDate = date;
    }

    /// <summary>
    /// Adds a match to this scope.
    /// </summary>
    /// <param name="matchId">The identifier of the match to add to this scope.</param>
    /// <returns>
    /// A <see cref="Result{T}"/> containing the added match ID if successful,
    /// or a failure result if the match already exists in this scope.
    /// </returns>
    public virtual Result<MatchId> AddMatch(MatchId matchId)
    {
        if (_matches.Contains(matchId))
            return Failures.AlreadyExists<MatchId>(matchId.ToString());

        _matches.Add(matchId);

        return Result.Success(matchId);
    }

    /// <summary>
    /// Removes a match from this scope.
    /// </summary>
    /// <param name="matchId">The identifier of the match to remove from this scope.</param>
    /// <returns>True if the match was successfully removed; false if the match was not found in this scope.</returns>
    public bool RemoveMatch(MatchId matchId) => _matches.Remove(matchId);

    /// <summary>
    /// Compares this match scope with another match scope for ordering purposes.
    /// Comparison is based on the original scheduled date.
    /// </summary>
    /// <param name="other">The other match scope to compare with.</param>
    /// <returns>
    /// A value that indicates the relative order of the objects being compared.
    /// Less than zero: This scope is earlier than the other scope.
    /// Zero: This scope is at the same time as the other scope.
    /// Greater than zero: This scope is later than the other scope.
    /// </returns>
    public int CompareTo(MatchScope<TId>? other) => other is not null ? OriginDate.CompareTo(other.OriginDate) : 1;
}
