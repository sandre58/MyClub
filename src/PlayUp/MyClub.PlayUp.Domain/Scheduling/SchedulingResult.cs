// -----------------------------------------------------------------------
// <copyright file="SchedulingResult.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Domain.Scheduling;

/// <summary>
/// Outcome of <see cref="ScheduleGenerator"/> — Success, NoSolution, or InvalidRequest.
/// </summary>
public sealed class SchedulingResult
{
    private readonly SchedulingValidationError[] _errors;

    private SchedulingResult(
        bool isInvalidRequest,
        bool isNoSolution,
        Schedule? schedule,
        IReadOnlyList<SchedulingValidationError>? errors)
    {
        IsInvalidRequest = isInvalidRequest;
        IsNoSolution = isNoSolution;
        Schedule = schedule;
        _errors = errors is null ? [] : [.. errors];
    }

    /// <summary>
    /// Gets a value indicating whether the request is structurally invalid.
    /// </summary>
    public bool IsInvalidRequest { get; }

    /// <summary>
    /// Gets a value indicating whether no admissible complete schedule exists.
    /// </summary>
    public bool IsNoSolution { get; }

    /// <summary>
    /// Gets a value indicating whether a complete schedule was found.
    /// </summary>
    public bool IsSuccess => !IsInvalidRequest && !IsNoSolution;

    /// <summary>
    /// Gets the schedule when <see cref="IsSuccess"/>; otherwise <see langword="null"/>.
    /// </summary>
    public Schedule? Schedule { get; }

    /// <summary>
    /// Gets structured errors when <see cref="IsInvalidRequest"/>; otherwise empty.
    /// </summary>
    public IReadOnlyList<SchedulingValidationError> Errors => _errors;

    /// <summary>
    /// Creates a success outcome.
    /// </summary>
    public static SchedulingResult Success(Schedule schedule)
    {
        ArgumentNullException.ThrowIfNull(schedule);
        return new SchedulingResult(false, false, schedule, null);
    }

    /// <summary>
    /// Creates a no-solution outcome.
    /// </summary>
    public static SchedulingResult NoSolution() => new(false, true, null, null);

    /// <summary>
    /// Creates an invalid-request outcome.
    /// </summary>
    public static SchedulingResult InvalidRequest(IReadOnlyList<SchedulingValidationError> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);
        return errors.Count == 0
            ? throw new ArgumentException("InvalidRequest requires at least one error.", nameof(errors))
            : new SchedulingResult(true, false, null, errors);
    }
}
