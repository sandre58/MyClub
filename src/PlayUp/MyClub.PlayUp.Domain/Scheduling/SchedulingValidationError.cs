// -----------------------------------------------------------------------
// <copyright file="SchedulingValidationError.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Domain.Scheduling;

/// <summary>
/// One structured InvalidRequest error (code contractuel; message non contractuel).
/// </summary>
public sealed record SchedulingValidationError
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SchedulingValidationError"/> class.
    /// </summary>
    public SchedulingValidationError(string code, string message, IReadOnlyList<string>? subjects = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        Code = code;
        Message = message;
        Subjects = subjects is null ? [] : [.. subjects];
    }

    /// <summary>
    /// Gets the stable error code.
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// Gets a descriptive message (not contractual).
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets optional subject identifiers (match/resource ids as strings).
    /// </summary>
    public IReadOnlyList<string> Subjects { get; }
}
