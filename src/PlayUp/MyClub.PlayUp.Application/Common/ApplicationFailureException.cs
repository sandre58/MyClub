// -----------------------------------------------------------------------
// <copyright file="ApplicationFailureException.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Application;

/// <summary>
/// Exception for application orchestration failures.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ApplicationFailureException"/> class.
/// </remarks>
/// <param name="message">Error message.</param>
/// <param name="code">Stable machine-readable code.</param>
/// <param name="reasons">Optional machine reason codes (e.g. completion blockers).</param>
public sealed class ApplicationFailureException(
    string message,
    string code,
    IReadOnlyList<string>? reasons = null) : Exception(message)
{
    /// <summary>
    /// Gets the stable failure code.
    /// </summary>
    public string Code { get; } = code;

    /// <summary>
    /// Gets optional machine reason codes explaining the failure.
    /// </summary>
    public IReadOnlyList<string> Reasons { get; } = reasons ?? [];
}
