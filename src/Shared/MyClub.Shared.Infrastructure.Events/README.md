# MyClub.Shared.Infrastructure.Events

> Domain events infrastructure for the MyClub sports management suite

![.NET](https://img.shields.io/badge/.NET-10-blue)
![MediatR](https://img.shields.io/badge/MediatR-13.0-orange)
![Domain Events](https://img.shields.io/badge/Pattern-DomainEvents-green)
![Clean Architecture](https://img.shields.io/badge/Architecture-Clean-brightgreen)

## Overview

**MyClub.Shared.Infrastructure.Events** provides the concrete infrastructure implementation for domain events publishing and handling within the MyClub sports management suite. This project implements the **Domain Event** pattern using **MediatR**, enabling loose coupling between aggregates and side effects through an event-driven architecture.

## Architecture

This project implements the infrastructure layer for domain events, bridging the gap between domain abstractions and concrete messaging infrastructure:

```text
┌─────────────────────────────────────────┐
│              Presentation               │
├─────────────────────────────────────────┤
│            Application Layer            │ ← Event Handlers (INotificationHandler<T>)
├─────────────────────────────────────────┤
│              Domain Layer               │ ← Domain Events (IDomainEvent)
├─────────────────────────────────────────┤
│         INFRASTRUCTURE EVENTS           │ ← This Project
│      ┌─────────────────────────────┐    │
│      │   DomainEventDispatcher     │    │
│      │      (MediatR Bridge)       │    │
│      └─────────────────────────────┘    │
├─────────────────────────────────────────┤
│             Shared Kernel               │ ← Event Abstractions
└─────────────────────────────────────────┘
```

## Features

### 📨 **Domain Event Dispatching**
- **MediatR Integration**: Leverages MediatR's powerful mediator pattern
- **Async Processing**: Full support for asynchronous event handling
- **Sequential Processing**: Events processed in order to maintain consistency
- **Error Handling**: Comprehensive exception handling and logging

### 🔧 **Infrastructure Concerns**
- **Separation of Concerns**: Focused solely on event dispatching infrastructure
- **Logging Integration**: Detailed logging for event dispatching operations
- **Performance Monitoring**: Debug-level logging for troubleshooting
- **Dependency Injection**: Clean integration with .NET DI container

### 🏗️ **Clean Architecture Compliance**
- **Framework Agnostic Domain**: Domain events remain technology-independent
- **Infrastructure Implementation**: Concrete implementation using modern .NET patterns
- **Testable Design**: Easy to mock and test event dispatching logic

## Technologies

| Package | Version | Purpose |
|---------|---------|---------|
| **MediatR** | 13.0.0 | Mediator pattern implementation for event publishing |
| **Microsoft.Extensions.Logging.Abstractions** | 10.0.0-preview.6 | Logging infrastructure |
| **Microsoft.Extensions.DependencyInjection.Abstractions** | 10.0.0-preview.6 | Dependency injection support |

## Core Components

### DomainEventDispatcher

**Central event dispatcher using MediatR infrastructure**

```csharp
public sealed class DomainEventDispatcher : IDomainEventDispatcher
{
    // Dispatches single domain event
    Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);
    
    // Dispatches multiple domain events sequentially
    Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
}
```

**Key Features:**
- **MediatR Bridge**: Converts domain events to MediatR notifications
- **Sequential Processing**: Maintains event order for consistency
- **Comprehensive Logging**: Debug and error logging for operations
- **Exception Handling**: Proper error propagation and logging

### ServiceCollectionExtensions

**Dependency injection configuration for domain events**

```csharp
public static class ServiceCollectionExtensions
{
    // Registers domain event services
    public static IServiceCollection AddDomainEvents(this IServiceCollection services);
}
```

## Usage Examples

### Service Registration

```csharp
// Program.cs or Startup.cs
using MyClub.Shared.Infrastructure.Events;

var builder = WebApplication.CreateBuilder(args);

// Add domain events infrastructure
builder.Services.AddDomainEvents();

// Add MediatR with handlers from application assemblies
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(
    typeof(ScorerApplicationAssembly).Assembly,
    typeof(TeamUpApplicationAssembly).Assembly
));

var app = builder.Build();
```

### Event Handler Implementation

```csharp
using MediatR;
using MyClub.Shared.Domain.Events;

// Application layer event handler
public sealed class PlayerRegisteredEventHandler : INotificationHandler<PlayerRegisteredEvent>
{
    private readonly IEmailService _emailService;
    private readonly ILogger<PlayerRegisteredEventHandler> _logger;

    public PlayerRegisteredEventHandler(IEmailService emailService, ILogger<PlayerRegisteredEventHandler> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    public async Task Handle(PlayerRegisteredEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing player registration for {PlayerId}", notification.PlayerId);
        
        // Send welcome email
        await _emailService.SendWelcomeEmailAsync(notification.PlayerEmail, cancellationToken);
        
        _logger.LogInformation("Welcome email sent for player {PlayerId}", notification.PlayerId);
    }
}
```

### Domain Entity with Events

```csharp
using MyClub.Shared.Domain.Base;
using MyClub.Shared.Domain.Events;

public class Player : AggregateRoot<PlayerId>
{
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;

    public static Player Register(string firstName, string lastName, string email)
    {
        var player = new Player
        {
            Id = PlayerId.NewId(),
            FirstName = firstName,
            LastName = lastName,
            Email = email
        };

        // Raise domain event
        player.RaiseDomainEvent(new PlayerRegisteredEvent(player.Id, player.Email, player.FirstName, player.LastName));

        return player;
    }
}
```

### Dispatching Events from Repository

```csharp
using MyClub.Shared.Infrastructure.Events;

public class PlayerRepository : IPlayerRepository
{
    private readonly ScorerDbContext _context;
    private readonly IDomainEventDispatcher _eventDispatcher;

    public PlayerRepository(ScorerDbContext context, IDomainEventDispatcher eventDispatcher)
    {
        _context = context;
        _eventDispatcher = eventDispatcher;
    }

    public async Task<Player> AddAsync(Player player, CancellationToken cancellationToken = default)
    {
        _context.Players.Add(player);
        await _context.SaveChangesAsync(cancellationToken);

        // Dispatch all domain events from the aggregate
        await _eventDispatcher.DispatchAsync(player.DomainEvents, cancellationToken);
        player.ClearDomainEvents();

        return player;
    }
}
```

## Event Flow

### 1. Domain Event Creation
```csharp
// In domain entity
player.RaiseDomainEvent(new PlayerRegisteredEvent(playerId, email));
```

### 2. Event Collection
```csharp
// Events collected in aggregate root
public abstract class AggregateRoot<TId> : Entity<TId>
{
    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    
    protected void RaiseDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
}
```

### 3. Event Dispatching
```csharp
// In repository or application service
await _eventDispatcher.DispatchAsync(aggregate.DomainEvents, cancellationToken);
aggregate.ClearDomainEvents();
```

### 4. Event Handling
```csharp
// MediatR automatically routes to registered handlers
public class PlayerRegisteredEventHandler : INotificationHandler<PlayerRegisteredEvent>
{
    public async Task Handle(PlayerRegisteredEvent notification, CancellationToken cancellationToken)
    {
        // Handle the event (send email, update cache, etc.)
    }
}
```

## Integration with MyClub Modules

### Scorer Module Events
- **CompetitionCreatedEvent**: New competition setup
- **MatchCompletedEvent**: Match result finalization
- **TeamRegisteredEvent**: Team registration in competition

### Team'up Module Events _(planned)_
- **PlayerRegisteredEvent**: New player registration
- **PlayerTransferredEvent**: Player transfer between teams
- **ContractSignedEvent**: Player contract signing

### Cross-Module Event Handlers
- **Cache Invalidation**: Clear cached data when entities change
- **Notification System**: Send emails, push notifications
- **Audit Logging**: Track important business events
- **Integration Events**: Sync with external systems

## Project Structure

```text
MyClub.Shared.Infrastructure.Events/
├── MyClub.Shared.Infrastructure.Events.csproj    # Project file with MediatR dependency
├── DomainEventDispatcher.cs                      # Core event dispatcher implementation
├── ServiceCollectionExtensions.cs                # DI registration extensions
└── Properties/
    └── AssemblyInfo.cs                           # Assembly metadata
```

## Benefits

### ✅ **Loose Coupling**
- Domain aggregates don't know about side effects
- Event handlers can be added/removed without changing domain logic
- Clear separation between business logic and infrastructure concerns

### 🚀 **Scalability**
- Async event processing for non-blocking operations
- Easy to add new event handlers for new features
- Sequential processing maintains data consistency

### 🔒 **Consistency**
- Events processed in order within single transaction
- Proper error handling prevents partial state updates
- Transaction boundaries clearly defined

### 👁️ **Observability**
- Comprehensive logging for all event operations
- Easy to trace event flow through the system
- Debug information for troubleshooting

### 🧪 **Testability**
- Easy to mock IDomainEventDispatcher in unit tests
- Event handlers can be tested independently
- Clear separation of concerns simplifies testing

## Best Practices

### Event Design
- **Immutable Events**: Events should be immutable value objects
- **Rich Information**: Include all necessary data in the event
- **Past Tense Naming**: Use past tense for event names (PlayerRegistered, not RegisterPlayer)

### Handler Implementation
- **Idempotent Operations**: Handlers should be safe to retry
- **Single Responsibility**: Each handler should have one clear purpose
- **Error Handling**: Proper exception handling and logging

### Performance Considerations
- **Async Operations**: Use async/await for I/O bound operations
- **Batch Processing**: Group related operations when possible
- **Monitoring**: Track event processing times and failures

## Dependencies

- **MediatR**: Event dispatching and mediator pattern
- **Microsoft.Extensions.Logging**: Structured logging
- **Microsoft.Extensions.DependencyInjection**: Service registration
- **MyClub.Shared.Domain**: Domain event abstractions

## Related Projects

- **MyClub.Shared.Domain**: Contains IDomainEvent and base classes
- **MyClub.Shared.Kernel**: Core abstractions and primitives
- **MyClub.*.Application**: Event handlers for domain events
- **MyClub.*.Infrastructure**: Concrete implementations using this infrastructure

---

This project provides the essential infrastructure foundation for implementing robust domain events across the entire MyClub suite, enabling scalable and maintainable event-driven architecture.
