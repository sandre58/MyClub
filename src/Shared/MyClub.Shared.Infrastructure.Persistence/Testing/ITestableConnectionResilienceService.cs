// -----------------------------------------------------------------------
// <copyright file="ITestableConnectionResilienceService.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.Shared.Infrastructure.Persistence.ErrorHandling.Connection;

namespace MyClub.Shared.Infrastructure.Persistence.Testing;

/// <summary>
/// Extended interface for connection resilience service that provides additional methods
/// for testing scenarios. Allows test code to simulate failures and control circuit breaker state.
/// </summary>
/// <remarks>
/// This interface should only be used in test environments to simulate various failure scenarios
/// and verify the behavior of circuit breaker and resilience patterns. It extends the main
/// IConnectionResilienceService with testing-specific capabilities while maintaining
/// the same contract for production usage.
/// </remarks>
public interface ITestableConnectionResilienceService : IConnectionResilienceService
{
    /// <summary>
    /// Simulates a connection failure for testing purposes.
    /// Forces the circuit breaker to record a failure without executing an actual operation.
    /// </summary>
    /// <param name="reason">The simulated failure reason.</param>
    void SimulateFailure(string reason);

    /// <summary>
    /// Simulates multiple consecutive failures to trigger circuit breaker opening.
    /// Useful for testing circuit breaker threshold behavior.
    /// </summary>
    /// <param name="failureCount">The number of failures to simulate.</param>
    /// <param name="reason">The simulated failure reason.</param>
    void SimulateConsecutiveFailures(int failureCount, string reason = "Simulated test failure");

    /// <summary>
    /// Forces the circuit breaker to a specific state for testing.
    /// Bypasses normal state transition logic for controlled testing scenarios.
    /// </summary>
    /// <param name="state">The desired circuit breaker state.</param>
    /// <param name="reason">The reason for the state change.</param>
    void ForceCircuitBreakerState(CircuitBreakerState state, string reason = "Test forced state");

    /// <summary>
    /// Gets the current number of consecutive failures recorded by the circuit breaker.
    /// Useful for verifying failure counting logic in tests.
    /// </summary>
    int ConsecutiveFailureCount { get; }

    /// <summary>
    /// Gets the time when the circuit breaker was last opened.
    /// Useful for testing timeout and recovery behavior.
    /// </summary>
    System.DateTime? CircuitOpenTime { get; }

    /// <summary>
    /// Simulates a successful operation to test recovery scenarios.
    /// Records a success event without executing actual logic.
    /// </summary>
    void SimulateSuccess();

    /// <summary>
    /// Resets all internal state to initial values.
    /// Useful for cleaning up between test cases.
    /// </summary>
    new void ResetConnectionState();
}
