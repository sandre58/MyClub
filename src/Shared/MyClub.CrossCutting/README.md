# MyClub.CrossCutting

> Cross-cutting concerns for the MyClub sports management suite

MyClub.CrossCutting provides the foundational abstractions for cross-cutting concerns in the sports management platform.

## Badges

![.NET](https://img.shields.io/badge/.NET-10-blue)
![Clean Architecture](https://img.shields.io/badge/Architecture-Clean-brightgreen)
![Cross-Cutting](https://img.shields.io/badge/Pattern-CrossCutting-orange)
![Minimal API](https://img.shields.io/badge/Design-Minimal-green)

## Benefits

### ✅ **Consistency**
- Uniform audit trail across all modules
- Standardized user and timestamp handling
- Common interface contracts

### 🔧 **Modularity**
- Zero coupling between modules
- Framework-agnostic abstractions
- Pluggable implementations

### 📋 **Compliance**
- Built-in audit trail support
- User action tracking
- Change history foundation

### 🧪 **Testability**
- Simple interfaces for easy mocking
- Deterministic timestamp testing
- Isolated cross-cutting concerns

## Overview

**MyClub.CrossCutting** provides foundational abstractions and services for cross-cutting concerns that span across all layers and modules of the MyClub sports management suite. This project contains lightweight interfaces and contracts that enable consistent implementation of common functionality throughout the entire application.

## Architecture

This project sits at the foundation of the Clean Architecture, providing abstractions that can be used across all layers:

```text
┌─────────────────────────────────────┐
│              Presentation           │
├─────────────────────────────────────┤
│            Application Layer        │
├─────────────────────────────────────┤
│              Domain Layer           │
├─────────────────────────────────────┤
│           Infrastructure Layer      │
├─────────────────────────────────────┤
│            CROSS-CUTTING            │ ← This Project
│         (Auditing, Security, etc.)  │
└─────────────────────────────────────┘
```

## Features

### 📝 **Auditing Abstractions**
- **IAuditService**: Interface for tracking user actions and timestamps
- **User Context**: Provides current user identification
- **Temporal Context**: Standardized timestamp management
- **Audit Trail**: Foundation for tracking changes across the system

### 🎯 **Design Principles**
- **Zero Dependencies**: No external package references for maximum portability
- **Interface Segregation**: Minimal, focused interfaces
- **Framework Agnostic**: Can be implemented with any technology stack
- **Module Independent**: Usable across all MyClub modules

## Current Services

### Auditing

```csharp
public interface IAuditService
{
    string GetCurrentUser();
    DateTime GetCurrentTimestamp();
}
```

**Usage scenarios:**
- Track entity creation and modification
- Log user actions for compliance
- Provide consistent timestamp source
- Support audit trail requirements

## Usage

### Implementing Audit Service

```csharp
// Infrastructure implementation example
public class AuditService : IAuditService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ISystemClock _systemClock;

    public AuditService(IHttpContextAccessor httpContextAccessor, ISystemClock systemClock)
    {
        _httpContextAccessor = httpContextAccessor;
        _systemClock = systemClock;
    }

    public string GetCurrentUser()
    {
        return _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System";
    }

    public DateTime GetCurrentTimestamp()
    {
        return _systemClock.UtcNow.DateTime;
    }
}
```

### Domain Entity Auditing

```csharp
public abstract class AuditableEntity<TId> : Entity<TId> where TId : EntityId<TId>
{
    public DateTime CreatedAt { get; private set; }
    public string CreatedBy { get; private set; } = string.Empty;
    public DateTime? ModifiedAt { get; private set; }
    public string? ModifiedBy { get; private set; }

    public virtual void MarkedAsCreated(DateTime createdAt, string createdBy)
    {
        CreatedAt = createdAt;
        CreatedBy = createdBy;
    }

    public virtual void MarkedAsModified(DateTime modifiedAt, string modifiedBy)
    {
        ModifiedAt = modifiedAt;
        ModifiedBy = modifiedBy;
    }
}
```

### Dependency Injection

```csharp
// Program.cs or Startup.cs
services.AddScoped<IAuditService, AuditService>();
```

## Project Structure

```text
MyClub.CrossCutting/
├── MyClub.CrossCutting.csproj          # Minimal project file
└── Auditing/                           # Auditing abstractions
    └── IAuditService.cs                # Core audit interface
```

## Integration with MyClub Modules

### Scorer Module
- Audit competition creation and modifications
- Track match result updates
- Log team and stadium changes

### Team'up Module _(planned)_
- Audit player registration and transfers
- Track staff assignments
- Log roster modifications

### Shared Components
- **Domain Entities**: Integrate with `AuditableEntity<T>`
- **Application Layer**: Use in command handlers for change tracking
- **Infrastructure**: Implement concrete audit service

## Benefits

### ✅ **Consistency**
- Uniform audit trail across all modules
- Standardized user and timestamp handling
- Common interface contracts

### 🔧 **Modularity**
- Zero coupling between modules
- Framework-agnostic abstractions
- Pluggable implementations

### 📋 **Compliance**
- Built-in audit trail support
- User action tracking
- Change history foundation

### 🧪 **Testability**
- Simple interfaces for easy mocking
- Deterministic timestamp testing
- Isolated cross-cutting concerns

## Future Enhancements

### Planned Abstractions

```csharp
// Security
public interface ICurrentUser
{
    string? UserId { get; }
    string? Email { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(string role);
}

// Configuration
public interface IAppConfiguration
{
    T GetValue<T>(string key);
    T GetValue<T>(string key, T defaultValue);
}

// Time Abstraction
public interface IDateTimeProvider
{
    DateTime Now { get; }
    DateTime UtcNow { get; }
    DateOnly Today { get; }
}

// Notifications
public interface INotificationService
{
    Task SendAsync(string message, NotificationType type, CancellationToken ct = default);
}
```

### Advanced Auditing

```csharp
public interface IAdvancedAuditService : IAuditService
{
    Task LogActionAsync(string action, object? data = null, CancellationToken ct = default);
    Task LogEntityChangeAsync<T>(string action, T entity, CancellationToken ct = default) where T : class;
}

public record AuditEntry(
    string UserId,
    DateTime Timestamp,
    string Action,
    string? EntityType = null,
    string? EntityId = null,
    string? Data = null);
```

## Design Guidelines

### Interface Design
- Keep interfaces minimal and focused
- Avoid technology-specific dependencies
- Use standard .NET types when possible
- Design for easy testing and mocking

### Implementation Strategy
- Implement in Infrastructure layer
- Use dependency injection for loose coupling
- Support both synchronous and asynchronous operations
- Provide sensible defaults for optional parameters

### Testing Approach
- Create mock implementations for unit tests
- Test interface contracts, not implementations
- Verify audit trail in integration tests
- Use deterministic values for testing

## Dependencies

- **None**: This project has zero external dependencies
- **Framework**: .NET 10 Standard Library only
- **Integration**: Used by all other MyClub projects

## Related Projects

- **MyClub.Shared.Kernel**: Core domain primitives and abstractions
- **MyClub.Shared.Domain**: Shared domain entities (uses auditing)
- **MyClub.Shared.Application**: Application layer services
- **MyClub.*.Infrastructure**: Concrete implementations

---

This project provides the essential foundation for implementing consistent cross-cutting concerns across the entire MyClub suite, ensuring maintainable and auditable software architecture.
