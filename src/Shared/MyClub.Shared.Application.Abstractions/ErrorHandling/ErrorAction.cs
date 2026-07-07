// -----------------------------------------------------------------------
// <copyright file="ErrorAction.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.Shared.Application.Abstractions.ErrorHandling;

/// <summary>
/// Defines the possible actions that can be taken in response to a database error.
/// </summary>
public enum ErrorAction
{
    /// <summary>
    /// Retry the operation after a delay.
    /// </summary>
    Retry,

    /// <summary>
    /// Fail the operation immediately.
    /// </summary>
    Fail,

    /// <summary>
    /// Continue execution despite the error.
    /// </summary>
    Continue,

    /// <summary>
    /// Escalate the error to a higher level for manual intervention.
    /// </summary>
    Escalate
}
