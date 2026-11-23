// -----------------------------------------------------------------------
// <copyright file="ApplicationFailureException.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Application;

/// <summary>
/// Exception for application orchestration failures.
/// </summary>
public sealed class ApplicationFailureException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationFailureException"/> class.
    /// </summary>
    /// <param name="message">Error message.</param>
    /// <param name="code">Stable machine-readable code.</param>
    /// <param name="reasons">Optional machine reason codes (e.g. completion blockers).</param>
    public ApplicationFailureException(string message, string code, IReadOnlyList<string>? reasons = null)
        : base(message)
    {
        Code = code;
        Reasons = reasons ?? [];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationFailureException"/> class.
    /// </summary>
    /// <param name="message">Error message.</param>
    /// <param name="code">Stable machine-readable code.</param>
    /// <param name="innerException">The PostgreSQL failure mapped to <paramref name="code"/>.</param>
    public ApplicationFailureException(string message, string code, Exception innerException)
        : base(message, innerException)
    {
        Code = code;
        Reasons = [];
    }

    /// <summary>
    /// Gets the stable failure code.
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// Gets optional machine reason codes explaining the failure.
    /// </summary>
    public IReadOnlyList<string> Reasons { get; }
}
