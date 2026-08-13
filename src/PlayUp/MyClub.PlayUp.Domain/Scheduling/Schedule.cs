// -----------------------------------------------------------------------
// <copyright file="Schedule.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Scheduling;

/// <summary>
/// Complete set of match assignments (Existing input or Success output). Not an aggregate root.
/// </summary>
public sealed class Schedule
{
    private readonly ScheduleAssignment[] _assignments;

    /// <summary>
    /// Initializes a new instance of the <see cref="Schedule"/> class.
    /// </summary>
    /// <param name="assignments">Assignments (unique MatchIds).</param>
    public Schedule(IReadOnlyList<ScheduleAssignment> assignments)
    {
        ArgumentNullException.ThrowIfNull(assignments);

        if (assignments.Select(a => a.MatchId).Distinct().Count() != assignments.Count)
        {
            throw new DomainException(
                "Schedule assignments must have unique match identities.",
                SchedulingErrorCodes.AssignmentInvalid);
        }

        _assignments = [..assignments];
    }

    /// <summary>
    /// Gets an empty schedule.
    /// </summary>
    public static Schedule Empty { get; } = new([]);

    /// <summary>
    /// Gets the assignments.
    /// </summary>
    public IReadOnlyList<ScheduleAssignment> Assignments => _assignments;

    /// <summary>
    /// Tries to get the assignment for a match.
    /// </summary>
    public bool TryGet(MatchId matchId, out ScheduleAssignment assignment)
    {
        foreach (var item in _assignments)
        {
            if (!item.MatchId.Equals(matchId)) continue;
            assignment = item;
            return true;
        }

        assignment = null!;
        return false;
    }
}
