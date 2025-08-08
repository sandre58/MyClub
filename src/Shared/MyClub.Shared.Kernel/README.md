# MyClub.Shared.Kernel

> Core domain primitives and abstractions for the MyClub sports management suite

![.NET](https://img.shields.io/badge/.NET-10-blue)
![DDD](https://img.shields.io/badge/Pattern-DDD-green)
![Clean Architecture](https://img.shields.io/badge/Architecture-Clean-brightgreen)
![Strongly Typed](https://img.shields.io/badge/IDs-StronglyTyped-orange)

## Overview

**MyClub.Shared.Kernel** provides the foundational domain primitives, abstractions, and patterns that form the core building blocks of the MyClub sports management suite. This project implements essential Domain-Driven Design (DDD) patterns and ensures consistency across all modules through shared interfaces and base classes.

## Architecture

This project sits at the very foundation of the Clean Architecture, providing the essential building blocks used by all layers:

```
┌─────────────────────────────────────────┐
│              Presentation               │
├─────────────────────────────────────────┤ 
│            Application Layer            │
├─────────────────────────────────────────┤
│              Domain Layer               │ ← Uses These Primitives
├─────────────────────────────────────────┤
│           Infrastructure Layer          │ ← Implements These Contracts
├─────────────────────────────────────────┤
│         🔧 SHARED KERNEL 🔧            │ ← This Project
│       (Primitives & Abstractions)       │
└─────────────────────────────────────────┘
```
## Core Features

### 🏗️ **Domain Primitives**
- **Entity Base Classes**: Foundation for all domain entities with identity and behavior
- **Strongly-Typed IDs**: Type-safe entity identifiers preventing ID mixing
- **Auditable Entities**: Built-in audit trail support with creation/modification tracking
- **Value Objects**: Immutable objects that describe domain concepts

### 🔄 **Domain Events**
- **Event-Driven Architecture**: Support for domain events and event handling
- **Domain Event Dispatcher**: Interface for publishing domain events
- **Event Collection**: Automatic collection and management of domain events

### 📊 **Result Pattern**
- **Functional Error Handling**: Railway-oriented programming approach
- **Validation Support**: Built-in validation error handling
- **Failure Types**: Common failure patterns and error representations

### 🗃️ **Repository Pattern**
- **Generic Repositories**: Type-safe repository abstractions
- **Unit of Work**: Transaction management interface
- **Read/Write Separation**: Support for CQRS read and write operations

## Technologies

| Package | Version | Purpose |
|---------|---------|---------|
| **JetBrains.Annotations** | 2025.2.0 | Code analysis and annotation support |
| **MyNet.Utilities** | 8.0.0 | General purpose utilities and extensions |

## Core Components

### Entity Framework

```csharp
// Base entity with strongly-typed ID
public abstract class Entity<TId> : IEntity<TId>, IHasDomainEvents
    where TId : EntityId<TId>
{
    public TId Id { get; }
    public IReadOnlyCollection<IDomainEvent> DomainEvents { get; }
    
    protected void AddDomainEvent(IDomainEvent domainEvent);
    public void ClearDomainEvents();
}

// Auditable entity with tracking
public abstract class AuditableEntity<TId> : Entity<TId>, IAuditableEntity<TId>
    where TId : EntityId<TId>
{
    public DateTime? CreatedAt { get; private set; }
    public string? CreatedBy { get; private set; }
    public DateTime? ModifiedAt { get; private set; }
    public string? ModifiedBy { get; private set; }
    
    public virtual void MarkedAsCreated(DateTime? createdAt, string? createdBy = null);
    public virtual void MarkedAsModified(DateTime? modifiedAt, string? modifiedBy = null);
}
```
### Strongly-Typed IDs

```csharp
// Factory methods for creating typed IDs
public static class EntityId
{
    public static TEntityId From<TEntityId>(Guid id) where TEntityId : EntityId<TEntityId>;
    public static TEntityId From<TEntityId>(string value) where TEntityId : EntityId<TEntityId>;
    public static TEntityId New<TEntityId>() where TEntityId : EntityId<TEntityId>;
}

// Base record for typed IDs
public abstract record EntityId<TSelf>(Guid Value) : IEquatable<TSelf>
    where TSelf : EntityId<TSelf>
{
    public static TSelf From(Guid id);
    public static TSelf New();
    public static readonly TSelf Empty;
    
    // Implicit conversions to/from Guid
    public static implicit operator Guid(EntityId<TSelf> entityId);
    public static implicit operator EntityId<TSelf>(Guid guid);
}
```
### Result Pattern

```csharp
// Base result for operations
public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure { get; }
    public string? ErrorCode { get; }
    public string? ErrorMessage { get; }
    public Dictionary<string, string[]>? ValidationErrors { get; }
    
    public static Result Success();
    public static Result<T> Success<T>(T value);
    public static Result Fail(string errorCode, string? message = null);
    public static Result ValidationFailed(Dictionary<string, string[]> errors);
}

// Generic result with value
public class Result<T> : Result
{
    public T Value { get; }
}
```
### Repository Contracts

```csharp
// Read-only repository interface
public interface IReadOnlyRepository<TEntity, in TId>
    where TEntity : Entity<TId>
    where TId : EntityId<TId>
{
    TEntity? GetById(TId id);
    IEnumerable<TEntity> GetAll();
    bool Exists(TId id);
    int Count();
}

// Full repository interface
public interface IRepository<TEntity, in TId> : IReadOnlyRepository<TEntity, TId>
    where TEntity : Entity<TId>
    where TId : EntityId<TId>
{
    void Add(TEntity entity);
    void AddRange(IEnumerable<TEntity> entities);
    void Update(TEntity entity);
    void Update(IEnumerable<TEntity> entities);
    int Delete(TId id);
    int DeleteRange(IEnumerable<TId> ids);
}
```
### Domain Events

```csharp
// Domain event marker interface
public interface IDomainEvent
{
    DateTimeOffset OccurredOn { get; }
}

// Entity with domain events
public interface IHasDomainEvents
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }
    void ClearDomainEvents();
}

// Domain event dispatcher
public interface IDomainEventDispatcher
{
    Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);
    Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
}
```
## Usage Examples

### Creating Domain Entities

```csharp
// Define a strongly-typed ID
public sealed record TeamId(Guid Value) : EntityId<TeamId>(Value);

// Create a domain entity
public class Team : AuditableEntity<TeamId>
{
    private Team() { } // EF Core constructor
    
    private Team(TeamId id, string name) : base(id)
    {
        Name = name;
        AddDomainEvent(new TeamCreatedEvent(Id, Name));
    }
    
    public string Name { get; private set; }
    
    public static Team Create(string name) => new(TeamId.New(), name);
    
    public void ChangeName(string newName)
    {
        if (Name != newName)
        {
            var oldName = Name;
            Name = newName;
            AddDomainEvent(new TeamNameChangedEvent(Id, oldName, newName));
        }
    }
}

// Domain event
public record TeamCreatedEvent(TeamId TeamId, string Name) : DomainEvent;
public record TeamNameChangedEvent(TeamId TeamId, string OldName, string NewName) : DomainEvent;
```
### Using Repository Pattern

```csharp
// Repository interface
public interface ITeamRepository : IRepository<Team, TeamId>
{
    Task<Team?> GetByNameAsync(string name);
    Task<IEnumerable<Team>> GetActiveTeamsAsync();
}

// Usage in application service
public class CreateTeamCommandHandler
{
    private readonly ITeamRepository _teamRepository;
    private readonly IUnitOfWork _unitOfWork;
    
    public async Task<Result<TeamId>> Handle(CreateTeamCommand command)
    {
        // Check if team already exists
        var existingTeam = await _teamRepository.GetByNameAsync(command.Name);
        if (existingTeam != null)
            return Failures.AlreadyExists<TeamId>(command.Name);
        
        // Create new team
        var team = Team.Create(command.Name);
        _teamRepository.Add(team);
        
        // Save changes
        await _unitOfWork.CommitAsync();
        
        return Result.Success(team.Id);
    }
}
```
### Result Pattern Usage

```csharp
public class TeamService
{
    public Result<Team> CreateTeam(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Fail<Team>("Team.InvalidName", "Team name cannot be empty");
            
        var team = Team.Create(name);
        return Result.Success(team);
    }
    
    public async Task<Result> ProcessTeamOperation()
    {
        var createResult = CreateTeam("Manchester United");
        if (createResult.IsFailure)
            return Result.Fail(createResult.ErrorCode, createResult.ErrorMessage);
            
        // Continue with successful result
        var team = createResult.Value;
        // ... more operations
        
        return Result.Success();
    }
}
```
### Working with Strongly-Typed IDs

```csharp
// Creating IDs
var teamId = TeamId.New();                    // New random ID
var specificId = TeamId.From(existingGuid);   // From existing GUID
var parsedId = TeamId.From("guid-string");    // From string

// Type safety - this won't compile!
// PlayerId playerId = teamId;  // Compiler error!

// Implicit conversions
Guid guidValue = teamId;        // Implicit conversion to Guid
TeamId backToTeamId = guidValue; // Implicit conversion from Guid

// Usage in collections
var teamIds = new List<TeamId> { TeamId.New(), TeamId.New() };
var guidValues = teamIds.Select(id => (Guid)id).ToList();
```
## Project Structure

```
MyClub.Shared.Kernel/
├── MyClub.Shared.Kernel.csproj    # Project file with minimal dependencies
├── Events/                        # Domain events abstractions
│   ├── IDomainEvent.cs            # Domain event marker interface
│   ├── IDomainEventDispatcher.cs  # Event dispatcher interface
│   ├── IHasDomainEvents.cs        # Entity event collection interface
│   └── DomainEvent.cs             # Base domain event implementation
├── Primitives/                    # Core domain primitives
│   ├── Entity.cs                  # Base entity class
│   ├── EntityId.cs                # Strongly-typed ID infrastructure
│   ├── AuditableEntity.cs         # Auditable entity base class
│   ├── IEntity.cs                 # Entity interface
│   ├── IAuditableEntity.cs        # Auditable entity interface
│   └── IAggregateRoot.cs          # Aggregate root marker interface
├── Repositories/                  # Repository pattern abstractions
│   ├── IRepository.cs             # Full repository interface
│   └── IReadOnlyRepository.cs     # Read-only repository interface
├── Results/                       # Result pattern implementation
│   ├── Result.cs                  # Result and Result<T> classes
│   └── Failures.cs                # Common failure factory methods
├── Persistence/                   # Persistence abstractions
│   └── IUnitOfWork.cs             # Unit of work interface
└── Interfaces/                    # Additional domain interfaces
    ├── IAuditable.cs              # Auditing marker interface
    ├── IHasPeriod.cs              # Period-based interface
    └── IOrderable.cs              # Ordering interface
```
## Integration with MyClub Modules

### Scorer Module

```csharp
// Competition entities use shared kernel
public class Competition : AuditableEntity<CompetitionId>
public sealed record CompetitionId(Guid Value) : EntityId<CompetitionId>(Value);
public interface ICompetitionRepository : IRepository<Competition, CompetitionId>
```
### Team'up Module _(planned)_

```csharp
// Player entities use shared kernel  
public class Player : Entity<PlayerId>
public sealed record PlayerId(Guid Value) : EntityId<PlayerId>(Value);
public interface IPlayerRepository : IRepository<Player, PlayerId>
```
### Shared Domain

```csharp
// Common entities across modules
public abstract class Person<TId> : AuditableEntity<TId> where TId : EntityId<TId>
public class Stadium : Entity<StadiumId>
```
## Benefits

### ✅ **Type Safety**
- Strongly-typed IDs prevent accidental ID mixing
- Compile-time verification of entity relationships
- Clear domain modeling with explicit types

### ✅ **Consistency**
- Uniform entity lifecycle management
- Standardized audit trail across all entities
- Common error handling patterns

### ✅ **Domain-Driven Design**
- Clear separation of domain concepts
- Domain event support for business workflows
- Aggregate root identification and management

### ✅ **Testability**
- Easy mocking of repository interfaces
- Deterministic entity creation and comparison
- Clear contract definitions

### ✅ **Performance**
- Minimal dependencies for fast compilation
- Efficient equality comparisons
- Optimized for Entity Framework integration

## Design Principles

### Entity Design
- **Identity**: Every entity has a strongly-typed unique identifier
- **Equality**: Entities are equal if their IDs are equal
- **Immutable IDs**: Entity identifiers cannot be changed after creation
- **Domain Events**: Entities can raise events for significant business occurrences

### Repository Design  
- **Generic**: Common operations work for all entities
- **Specific**: Domain-specific queries can be added in derived interfaces
- **Unit of Work**: Changes are committed together in transactions
- **Separation**: Read and write operations can be separated for CQRS

### Result Design
- **Railway-Oriented**: Success and failure paths are explicit
- **Functional**: Avoid exceptions for business logic failures
- **Composable**: Results can be chained and transformed
- **Informative**: Rich error information for debugging and user feedback

## Dependencies

- **JetBrains.Annotations**: Code analysis and documentation annotations
- **MyNet.Utilities**: General purpose utility functions and extensions
- **Framework**: .NET 10 Standard Library

## Related Projects

- **MyClub.Shared.Domain**: Shared domain entities (uses this kernel)
- **MyClub.Shared.Application**: Application layer services (uses repositories)
- **MyClub.*.Domain**: Module-specific domain implementations
- **MyClub.*.Infrastructure**: Concrete repository implementations

---

This project provides the essential foundation for implementing robust, type-safe, and maintainable domain models across the entire MyClub suite using proven Domain-Driven Design patterns and practices.
