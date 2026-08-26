// -----------------------------------------------------------------------
// <copyright file="ApplicationFailureException.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.Media.Application;

/// <summary>
/// Exception for Media application orchestration failures.
/// </summary>
public sealed class ApplicationFailureException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationFailureException"/> class.
    /// </summary>
    /// <param name="message">Error message.</param>
    /// <param name="code">Stable machine-readable code.</param>
    public ApplicationFailureException(string message, string code)
        : base(message) => Code = code;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationFailureException"/> class with an inner exception.
    /// </summary>
    /// <param name="message">Error message.</param>
    /// <param name="code">Stable machine-readable code.</param>
    /// <param name="innerException">The cause of the failure.</param>
    public ApplicationFailureException(string message, string code, Exception innerException)
        : base(message, innerException) => Code = code;

    /// <summary>
    /// Gets the stable failure code.
    /// </summary>
    public string Code { get; }
}
