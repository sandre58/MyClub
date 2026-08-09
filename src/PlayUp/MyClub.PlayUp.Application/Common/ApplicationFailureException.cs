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
public sealed class ApplicationFailureException(string message, string code) : Exception(message)
{
    /// <summary>
    /// Gets the stable failure code.
    /// </summary>
    public string Code { get; } = code;
}
