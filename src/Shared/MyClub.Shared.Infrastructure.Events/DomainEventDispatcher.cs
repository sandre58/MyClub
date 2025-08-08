// -----------------------------------------------------------------------
// <copyright file="DomainEventDispatcher.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using MyClub.Shared.Kernel.Events;

namespace MyClub.Shared.Infrastructure.Events;

/// <summary>
/// Implementation of domain event dispatcher using MediatR.
/// Coordinates the publication of domain events to all registered handlers.
/// Focused solely on event dispatching without persistence concerns.
/// </summary>
public sealed partial class DomainEventDispatcher(IMediator mediator, ILogger<DomainEventDispatcher> logger) : IDomainEventDispatcher
{
    private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    private readonly ILogger<DomainEventDispatcher> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    #region LoggerMessage Definitions

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Debug,
        Message = "Dispatching domain event: {EventType}")]
    private static partial void LogDispatchingEvent(ILogger logger, string eventType);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Debug,
        Message = "Successfully dispatched domain event: {EventType}")]
    private static partial void LogEventDispatched(ILogger logger, string eventType);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Error,
        Message = "Failed to dispatch domain event: {EventType}")]
    private static partial void LogEventDispatchFailed(ILogger logger, Exception exception, string eventType);

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Debug,
        Message = "Dispatching {EventCount} domain events")]
    private static partial void LogDispatchingMultipleEvents(ILogger logger, int eventCount);

    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Debug,
        Message = "Successfully dispatched all {EventCount} domain events")]
    private static partial void LogMultipleEventsDispatched(ILogger logger, int eventCount);

    [LoggerMessage(
        EventId = 6,
        Level = LogLevel.Error,
        Message = "Failed to dispatch domain events")]
    private static partial void LogMultipleEventsDispatchFailed(ILogger logger, Exception exception);

    #endregion

    /// <summary>
    /// Dispatches a single domain event to all registered handlers.
    /// </summary>
    /// <param name="domainEvent">The domain event to dispatch.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the asynchronous dispatch operation.</returns>
    public async Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        var eventType = domainEvent.GetType().Name;

        LogDispatchingEvent(_logger, eventType);

        try
        {
            await _mediator.Publish(domainEvent, cancellationToken).ConfigureAwait(false);

            LogEventDispatched(_logger, eventType);
        }
        catch (Exception ex)
        {
            LogEventDispatchFailed(_logger, ex, eventType);
            throw;
        }
    }

    /// <summary>
    /// Dispatches multiple domain events to their respective handlers.
    /// Events are processed sequentially to maintain order.
    /// </summary>
    /// <param name="domainEvents">The collection of domain events to dispatch.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the asynchronous dispatch operation.</returns>
    public async Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvents);

        var events = domainEvents.ToList();
        if (events.Count == 0) return;

        LogDispatchingMultipleEvents(_logger, events.Count);

        try
        {
            // Process events sequentially to maintain order
            foreach (var domainEvent in events)
            {
                await DispatchAsync(domainEvent, cancellationToken).ConfigureAwait(false);
            }

            LogMultipleEventsDispatched(_logger, events.Count);
        }
        catch (Exception ex)
        {
            LogMultipleEventsDispatchFailed(_logger, ex);
            throw;
        }
    }
}
