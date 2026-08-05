// -----------------------------------------------------------------------
// <copyright file="DomainException.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Domain.Common;

/// <summary>
/// Base exception for domain invariant violations and business rule failures.
/// </summary>
public class DomainException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DomainException"/> class with a message.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    public DomainException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainException"/> class with a message and a stable business code.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="code">A machine-readable code identifying the business failure.</param>
    public DomainException(string message, string code)
        : base(message) => Code = code;

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainException"/> class with a message and an inner exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public DomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainException"/> class with a message, a business code, and an inner exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="code">A machine-readable code identifying the business failure.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public DomainException(string message, string code, Exception innerException)
        : base(message, innerException) => Code = code;

    /// <summary>
    /// Gets an optional stable machine-readable code for the business failure.
    /// </summary>
    public string? Code { get; }
}
