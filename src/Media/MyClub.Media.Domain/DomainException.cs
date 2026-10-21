// -----------------------------------------------------------------------
// <copyright file="DomainException.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.Media.Domain;

/// <summary>
/// Base exception for Media domain invariant violations.
/// </summary>
public class DomainException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DomainException"/> class with a message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public DomainException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainException"/> class with a message and code.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="code">A machine-readable failure code.</param>
    public DomainException(string message, string code)
        : base(message) => Code = code;

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainException"/> class with a message and inner exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public DomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainException"/> class with a message, code, and inner exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="code">A machine-readable failure code.</param>
    /// <param name="innerException">The inner exception.</param>
    public DomainException(string message, string code, Exception innerException)
        : base(message, innerException) => Code = code;

    /// <summary>
    /// Gets an optional stable machine-readable code for the failure.
    /// </summary>
    public string? Code { get; }
}
