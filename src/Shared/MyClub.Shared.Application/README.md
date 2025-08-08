# MyClub.Shared.Application

> Shared application layer for the MyClub sports management suite

![.NET](https://img.shields.io/badge/.NET-10-blue)
![CQRS](https://img.shields.io/badge/Pattern-CQRS-green)
![MediatR](https://img.shields.io/badge/MediatR-13.0-orange)
![Clean Architecture](https://img.shields.io/badge/Architecture-Clean-brightgreen)

## Overview

**MyClub.Shared.Application** provides the foundational application layer components for the MyClub modular sports management suite. It implements the **CQRS pattern** using **MediatR** and provides reusable abstractions for commands, queries, handlers, and cross-cutting concerns.

## Architecture

This project follows **Clean Architecture** principles and implements the **Command Query Responsibility Segregation (CQRS)** pattern:

```
┌─────────────────────────────────────────┐
│              Presentation               │
├─────────────────────────────────────────┤
│            APPLICATION LAYER            │ ← This Project
│  ┌─────────────┐ ┌────────────────────┐ │
│  │  Commands   │ │      Handlers      │ │
│  │   Queries   │ │    (CRUD + Read)   │ │
│  └─────────────┘ └────────────────────┘ │
│  ┌─────────────┐ ┌────────────────────┐ │
│  │  Behaviors  │ │     Validators     │ │
│  │ (Pipeline)  │ │   (FluentVal.)     │ │
│  └─────────────┘ └────────────────────┘ │
├─────────────────────────────────────────┤
│               DOMAIN LAYER              │
├─────────────────────────────────────────┤
│           INFRASTRUCTURE LAYER          │
└─────────────────────────────────────────┘
```
## Features

### 🏗️ **Complete CQRS Foundation**
- **Abstract Commands**: `CreateCommand`, `UpdateCommand`, `DeleteCommand`
- **Abstract Queries**: `GetByIdQuery<T>`, `GetAllQuery<T>`
- **Generic Handlers**: Template method implementations for CRUD + Read operations
- **Result Pattern**: Functional error handling with `Result<T>`

### 🔄 **Advanced MediatR Pipeline**
- **ValidationBehavior**: Automatic request validation using FluentValidation
- **LoggingBehavior**: Comprehensive request/response logging with performance metrics
- **PerformanceBehavior**: Performance monitoring with configurable thresholds
- **CachingBehavior**: Intelligent caching for queries implementing `ICacheableRequest`
- **Extensible Pipeline**: Ready for additional custom behaviors

### 🎯 **Template Method Pattern**
- **Reusable Handlers**: Generic implementations for common operations
- **Customization Points**: Virtual methods for specific business logic
- **Consistent Structure**: Standardized command/query processing flow

### 📊 **Performance & Monitoring**
- **Execution Time Tracking**: Built-in performance monitoring
- **Memory Caching**: Intelligent caching with automatic expiration
- **Structured Logging**: JSON-serialized request/response logging
- **Threshold-based Alerts**: Configurable performance warnings

## Technologies

| Package | Version | Purpose |
|---------|---------|---------|
| **MediatR** | 13.0.0 | CQRS pattern implementation |
| **FluentValidation** | 12.0.0 | Request validation |
| **AutoMapper** | 15.0.1 | Object-to-object mapping |
| **Microsoft.Extensions.Caching.Memory** | 9.0.0 | In-memory caching |
| **Microsoft.Extensions.Logging.Abstractions** | 9.0.0 | Structured logging |

## Usage

### Creating Commands & Queries

```csharp
// Commands
public record CreateTeamCommand(string Name, string? ShortName) : CreateCommand;
public record UpdateTeamCommand(Guid Id, string Name) : UpdateCommand(Id);
public record DeleteTeamCommand(Guid Id) : DeleteCommand(Id);

// Queries
public record GetTeamByIdQuery(Guid Id) : GetByIdQuery<TeamDto>(Id);
public record GetAllTeamsQuery : GetAllQuery<TeamDto>;

// Cacheable Query
public record GetTeamStatisticsQuery(Guid TeamId) : GetByIdQuery<TeamStatsDto>(TeamId), ICacheableRequest
{
    public string CacheKey => $"team-stats-{TeamId}";
    public TimeSpan? CacheExpiration => TimeSpan.FromMinutes(15);
}
```
### Creating Handlers

```csharp
// Command Handler
public class CreateTeamCommandHandler : CreateCommandHandler<ITeamRepository, Team, TeamId, CreateTeamCommand>
{
    public CreateTeamCommandHandler(
        ITeamRepository repository, 
        IUnitOfWork unitOfWork, 
        IMapper mapper) 
        : base(repository, unitOfWork, mapper) { }

    // Override for custom logic if needed
    protected override Result<TeamId> Add(CreateTeamCommand command)
    {
        // Custom validation or business logic
        return base.Add(command);
    }
}

// Query Handler
public class GetTeamByIdQueryHandler : GetByIdQueryHandler<ITeamRepository, Team, TeamId, TeamDto, GetTeamByIdQuery>
{
    public GetTeamByIdQueryHandler(
        ITeamRepository repository, 
        IMapper mapper) 
        : base(repository, mapper) { }
}
```
### Validation

```csharp
public class CreateTeamCommandValidator : AbstractValidator<CreateTeamCommand>
{
    public CreateTeamCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);
            
        RuleFor(x => x.ShortName)
            .MaximumLength(10)
            .When(x => !string.IsNullOrEmpty(x.ShortName));
    }
}
```
### Registration (DI Container)

```csharp
services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

// Register all behaviors in order (order matters!)
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CachingBehavior<,>));
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));

// Register validation and mapping
services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
services.AddAutoMapper(Assembly.GetExecutingAssembly());

// Register caching
services.AddMemoryCache();
```
## Project Structure

```
MyClub.Shared.Application/
├── Behaviors/                    # MediatR pipeline behaviors
│   ├── ValidationBehavior.cs     # Request validation
│   ├── LoggingBehavior.cs        # Request/response logging
│   ├── PerformanceBehavior.cs    # Performance monitoring
│   └── CachingBehavior.cs        # Query result caching
├── Commands/                     # Abstract command definitions
│   ├── CreateCommand.cs          # Create entity commands
│   ├── UpdateCommand.cs          # Update entity commands
│   └── DeleteCommand.cs          # Delete entity commands
├── Queries/                      # Abstract query definitions
│   ├── GetByIdQuery.cs           # Single entity retrieval
│   ├── GetAllQuery.cs            # All entities retrieval
├── Handlers/                     # Abstract handler implementations
│   ├── CreateCommandHandler.cs   # Create operation handler
│   ├── UpdateCommandHandler.cs   # Update operation handler
│   ├── DeleteCommandHandler.cs   # Delete operation handler
│   ├── GetByIdQueryHandler.cs    # Single entity query handler
│   └── GetAllQueryHandler.cs     # Collection query handler
└── Extensions/                   # DI configuration extensions
    └── ApplicationServiceCollectionExtensions.cs
```
## Benefits

### ✅ **Complete CQRS Implementation**
- Full separation of commands and queries
- Consistent patterns for all CRUD operations
- Intelligent caching for read operations

### ✅ **Production-Ready Monitoring**
- Comprehensive logging with request/response details
- Performance monitoring with configurable thresholds
- Structured logging for easy analysis
- Memory usage optimization

### ✅ **High Performance**
- Intelligent caching reduces database load
- Configurable cache expiration policies
- Performance threshold monitoring
- Minimal overhead pipeline design

### ✅ **Developer Experience**
- Strongly-typed queries and commands
- Generic handlers reduce boilerplate code
- Template methods for easy customization
- Comprehensive documentation and examples

### ✅ **Enterprise Features**
- Robust error handling with Result pattern
- Automatic validation pipeline
- Extensible behavior system
- Clean separation of concerns

## Pipeline Execution Order

The MediatR pipeline executes behaviors in registration order:

```
1. LoggingBehavior      → Logs incoming requests
2. ValidationBehavior   → Validates request data
3. CachingBehavior      → Checks cache for queries
4. PerformanceBehavior  → Monitors execution time
5. Handler Execution    → Actual business logic
6. Response Processing  → Caching, logging, performance metrics
```
## Performance Monitoring

### Default Thresholds
- **Warning**: 500ms execution time
- **Error**: 2000ms execution time
- **Cache**: 5 minutes default expiration

### Custom Configuration

```csharp
services.AddTransient<IPipelineBehavior<TRequest, TResponse>>(provider => 
    new PerformanceBehavior<TRequest, TResponse>(
        provider.GetRequiredService<ILogger<PerformanceBehavior<TRequest, TResponse>>>(),
        warningThresholdMs: 300,  // Custom warning threshold
        errorThresholdMs: 1000    // Custom error threshold
    ));
```
## Caching Strategy

### Cacheable Queries

Implement `ICacheableRequest` for automatic caching:

```csharp
public record GetExpensiveDataQuery(Guid Id) : GetByIdQuery<ExpensiveDataDto>(Id), ICacheableRequest
{
    public string CacheKey => $"expensive-data-{Id}";
    public TimeSpan? CacheExpiration => TimeSpan.FromHours(1); // Long-lived cache
}
```
### Cache Invalidation
Cache entries automatically expire based on the configured expiration time. For manual invalidation, inject `IMemoryCache` and call `Remove(cacheKey)`.

## Extension Points

### Custom Behaviors

```csharp
public class AuthorizationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        // Authorization logic
        if (!IsAuthorized(request))
            throw new UnauthorizedAccessException();
            
        return await next(cancellationToken);
    }
}
```
### Handler Customization

```csharp
protected override async Task<Result<TeamDto>> GetByIdAsync(GetTeamQuery query, CancellationToken cancellationToken)
{
    // Custom business rules or additional data loading
    var baseResult = await base.GetByIdAsync(query, cancellationToken);
    
    if (baseResult.IsSuccess)
    {
        // Enrich the result with additional data
        var enrichedData = await EnrichTeamData(baseResult.Value);
        return Result.Success(enrichedData);
    }
    
    return baseResult;
}
```
## Quick Start with Extensions

For easy setup, use the provided extension methods:

```csharp
// Program.cs or Startup.cs
services.AddSharedApplication(Assembly.GetExecutingAssembly());

// Or with custom configuration
services.AddMediatRWithBehaviors(
    includeLogging: true,
    includeValidation: true,
    includeCaching: true,
    includePerformance: true,
    Assembly.GetExecutingAssembly());
```
## Dependencies

- **MyClub.Shared.Domain**: Domain entities and value objects
- **MyClub.Shared.Kernel**: Core primitives and abstractions

## Related Projects

- **MyClub.Scorer.Application**: Scorer-specific application logic
- **MyClub.TeamUp.Application**: Team management application logic
- **MyClub.Shared.Infrastructure**: Infrastructure implementations

---

This project provides a complete, production-ready foundation for building scalable, maintainable, and high-performance application services across the MyClub suite. The comprehensive CQRS implementation, advanced pipeline behaviors, and enterprise-grade monitoring make it suitable for both development and production environments.