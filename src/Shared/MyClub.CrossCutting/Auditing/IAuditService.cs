// -----------------------------------------------------------------------
// <copyright file="IAuditService.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace MyClub.CrossCutting.Auditing;

/// <summary>
/// Provides auditing services for tracking user actions and timestamps across the MyClub application.
/// This service is a cross-cutting concern that enables consistent audit trail implementation.
/// </summary>
public interface IAuditService
{
    /// <summary>
    /// Gets the identifier of the currently authenticated user.
    /// Returns "System" or similar when no user is authenticated.
    /// </summary>
    /// <returns>The current user identifier (username, email, or user ID).</returns>
    string GetCurrentUser();

    /// <summary>
    /// Gets the current timestamp for audit purposes.
    /// Typically, returns UTC time for consistency across time zones.
    /// </summary>
    /// <returns>The current date and time.</returns>
    DateTime GetCurrentTimestamp();
}
