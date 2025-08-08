// -----------------------------------------------------------------------
// <copyright file="ConnectionHealthResult.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace MyClub.Shared.Infrastructure.Persistence.ErrorHandling.Connection;

/// <summary>
/// Represents the result of a database connection health check.
/// Provides detailed information about connection status, performance metrics, and diagnostic data.
/// </summary>
public class ConnectionHealthResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the connection is healthy.
    /// </summary>
    public bool IsHealthy { get; set; }

    /// <summary>
    /// Gets or sets the response time for the health check operation.
    /// </summary>
    public TimeSpan ResponseTime { get; set; }

    /// <summary>
    /// Gets or sets any error message if the health check failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets additional diagnostic information about the connection.
    /// </summary>
    public string? DiagnosticInfo { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the health check was performed.
    /// </summary>
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets a value indicating whether the response time is considered slow (> 1 second).
    /// </summary>
    public bool IsSlowResponse => ResponseTime.TotalMilliseconds > 1000;

    /// <summary>
    /// Gets a value indicating whether the response time is considered very slow (> 5 seconds).
    /// </summary>
    public bool IsVerySlowResponse => ResponseTime.TotalMilliseconds > 5000;

    /// <summary>
    /// Gets a descriptive status based on health and response time.
    /// </summary>
    public string Status => IsHealthy switch
    {
        true when IsVerySlowResponse => "Healthy but very slow",
        true when IsSlowResponse => "Healthy but slow",
        true => "Healthy",
        false => "Unhealthy"
    };

    /// <summary>
    /// Creates a healthy result with the specified response time.
    /// </summary>
    /// <param name="responseTime">The response time for the health check.</param>
    /// <param name="diagnosticInfo">Optional diagnostic information.</param>
    /// <returns>A healthy connection health result.</returns>
    public static ConnectionHealthResult Healthy(TimeSpan responseTime, string? diagnosticInfo = null) =>
        new()
        {
            IsHealthy = true,
            ResponseTime = responseTime,
            DiagnosticInfo = diagnosticInfo,
            CheckedAt = DateTime.UtcNow
        };

    /// <summary>
    /// Creates an unhealthy result with the specified error information.
    /// </summary>
    /// <param name="errorMessage">The error message describing the failure.</param>
    /// <param name="responseTime">The response time before failure occurred.</param>
    /// <param name="diagnosticInfo">Optional diagnostic information.</param>
    /// <returns>An unhealthy connection health result.</returns>
    public static ConnectionHealthResult Unhealthy(string errorMessage, TimeSpan responseTime = default, string? diagnosticInfo = null) =>
        new()
        {
            IsHealthy = false,
            ErrorMessage = errorMessage,
            ResponseTime = responseTime,
            DiagnosticInfo = diagnosticInfo,
            CheckedAt = DateTime.UtcNow
        };

    /// <summary>
    /// Creates an unhealthy result from an exception.
    /// </summary>
    /// <param name="exception">The exception that caused the health check to fail.</param>
    /// <param name="responseTime">The response time before failure occurred.</param>
    /// <returns>An unhealthy connection health result.</returns>
    public static ConnectionHealthResult FromException(Exception exception, TimeSpan responseTime = default) =>
        new()
        {
            IsHealthy = false,
            ErrorMessage = exception.Message,
            ResponseTime = responseTime,
            DiagnosticInfo = $"Exception: {exception.GetType().Name}",
            CheckedAt = DateTime.UtcNow
        };

    /// <summary>
    /// Returns a string representation of the health check result.
    /// </summary>
    /// <returns>A formatted string with health status and key metrics.</returns>
    public override string ToString()
    {
        var status = IsHealthy ? "Healthy" : "Unhealthy";
        var responseMs = Math.Round(ResponseTime.TotalMilliseconds, 2);

        return IsHealthy || string.IsNullOrEmpty(ErrorMessage) ? $"{status} ({responseMs}ms)" : $"{status} ({responseMs}ms) - {ErrorMessage}";
    }
}
