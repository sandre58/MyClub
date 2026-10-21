// -----------------------------------------------------------------------
// <copyright file="SchedulingErrorCodes.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Domain.Scheduling;

/// <summary>
/// Stable machine-readable codes for Scheduling validation and value-object failures.
/// </summary>
public static class SchedulingErrorCodes
{
    /// <summary>
    /// Gets the code when a scheduling request is structurally invalid.
    /// </summary>
    public const string InvalidRequest = "Scheduling.InvalidRequest";

    /// <summary>
    /// Gets the code when a duration value is invalid.
    /// </summary>
    public const string DurationInvalid = "Scheduling.DurationInvalid";

    /// <summary>
    /// Gets the code when a time granularity value is invalid.
    /// </summary>
    public const string GranularityInvalid = "Scheduling.GranularityInvalid";

    /// <summary>
    /// Gets the code when a horizon is invalid.
    /// </summary>
    public const string HorizonInvalid = "Scheduling.HorizonInvalid";

    /// <summary>
    /// Gets the code when a time window is invalid.
    /// </summary>
    public const string TimeWindowInvalid = "Scheduling.TimeWindowInvalid";

    /// <summary>
    /// Gets the code when a schedule assignment is invalid.
    /// </summary>
    public const string AssignmentInvalid = "Scheduling.AssignmentInvalid";

    /// <summary>
    /// Gets the code when a constraint payload is invalid.
    /// </summary>
    public const string ConstraintInvalid = "Scheduling.ConstraintInvalid";

    /// <summary>
    /// Gets the code when a match scheduling context is invalid.
    /// </summary>
    public const string MatchContextInvalid = "Scheduling.MatchContextInvalid";

    /// <summary>
    /// Gets the code when a resource scheduling context is invalid.
    /// </summary>
    public const string ResourceContextInvalid = "Scheduling.ResourceContextInvalid";
}
