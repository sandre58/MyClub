# MyClub.Shared.Infrastructure.Persistence

> Shared persistence infrastructure with advanced error handling, resilience patterns, and Entity Framework Core extensions

![.NET](https://img.shields.io/badge/.NET-10-blue)
![Entity Framework](https://img.shields.io/badge/EF%20Core-10.0-orange)
![Circuit Breaker](https://img.shields.io/badge/Pattern-Circuit%20Breaker-green)
![Clean Architecture](https://img.shields.io/badge/Architecture-Clean-brightgreen)
![LoggerMessage](https://img.shields.io/badge/Logging-High%20Performance-yellow)

## Overview

**MyClub.Shared.Infrastructure.Persistence** provides the foundational persistence infrastructure for the MyClub sports management suite. This project implements enterprise-grade error handling, connection resilience, monitoring patterns, and comprehensive Entity Framework Core extensions that can be shared across all modules.

## 🏗️ Architecture

```
┌─────────────────────────────────────────┐
│              Application                │
├─────────────────────────────────────────┤
│                Domain                   │
├─────────────────────────────────────────┤
│      🗄️ SHARED INFRASTRUCTURE          │ ← This Project
│  ┌─────────────┐ ┌────────────────────┐ │
│  │ Error       │ │  Connection        │ │
│  │ Handling    │ │  Resilience        │ │
│  └─────────────┘ └────────────────────┘ │
│  ┌─────────────┐ ┌────────────────────┐ │
│  │ Retry       │ │  EF Core           │ │
│  │ Policies    │ │  Extensions        │ │
│  └─────────────┘ └────────────────────┘ │
│  ┌─────────────┐ ┌────────────────────┐ │
│  │ Converters  │ │  Conventions       │ │
│  │ & Mapping   │ │  & Patterns        │ │
│  └─────────────┘ └────────────────────┘ │
└─────────────────────────────────────────┘
```

## 🚀 Features

### 🔧 **Error Handling & Resilience**
- **Centralized Error Management**: Consistent error handling across all persistence operations
- **Transient Failure Detection**: Automatic identification of retryable errors
- **Circuit Breaker Pattern**: Automatic failure detection and recovery with state management
- **Retry Policies**: Exponential backoff with configurable limits and jitter support
- **High-Performance Logging**: LoggerMessage delegates for zero-allocation logging

### 🗄️ **Entity Framework Core Extensions**
- **Base Repository Classes**: Generic repository implementations with domain patterns
- **Unit of Work Pattern**: Transaction coordination with domain event dispatching
- **Value Converters**: Strongly-typed ID converters, enum handling, and complex object serialization
- **Database Conventions**: Automatic naming conventions (snake_case, pluralization, FK naming)
- **Join Entity Support**: Explicit many-to-many relationship management

### 📊 **Monitoring & Observability**
- **Performance Metrics**: Operation timing and success rate tracking
- **Connection Health Checks**: Real-time database connectivity monitoring
- **Circuit Breaker Monitoring**: State change tracking and alerting
- **Custom Metrics Integration**: Extensible metrics collection interface

### ⚙️ **Developer Experience**
- **Builder Extensions**: Fluent API extensions for Entity Framework configuration
- **Convention Helpers**: Automatic application of database conventions
- **Testing Support**: Testable interfaces and mock-friendly abstractions

## 🛠️ Quick Start

### Installation
```xml
<PackageReference Include="MyClub.Shared.Infrastructure.Persistence" Version="1.0.0" />
```

### Basic Setup
```csharp
// Program.cs or Startup.cs
public void ConfigureServices(IServiceCollection services)
{
    // Add shared persistence services with configuration
    services.AddSharedPersistenceServices(Configuration);
    
    // Add error handling and resilience
    services.AddPersistenceErrorHandling(options =>
    {
        options.EnableCircuitBreaker = true;
        options.MaxRetryAttempts = 3;
    });
    
    // Add health checks for monitoring
    services.AddPersistenceHealthChecks()
        .AddCheck("database");
}
```

### Configuration
```json
{
  "PersistenceErrorHandling": {
    "EnableRetryOnTransientFailures": true,
    "MaxRetryAttempts": 3,
    "InitialRetryDelayMs": 1000,
    "MaxRetryDelayMs": 30000,
    "EnableCircuitBreaker": true,
    "CircuitBreakerFailureThreshold": 5,
    "CircuitBreakerTimeoutSeconds": 60,
    "EnablePerformanceMonitoring": true,
    "EnableDetailedErrorLogging": true
  }
}
```

## 💡 Usage Examples

### Repository Pattern with Error Handling
```csharp
public class TeamRepository : Repository<Team, TeamId, MyDbContext>, ITeamRepository
{
    private readonly IPersistenceErrorHandler _errorHandler;
    
    public TeamRepository(MyDbContext context, IPersistenceErrorHandler errorHandler) 
        : base(context)
    {
        _errorHandler = errorHandler;
    }
    
    public async Task<Team?> GetByNameAsync(string name)
    {
        return await _errorHandler.ExecuteWithErrorHandlingAsync(
            async () => await DbSet.FirstOrDefaultAsync(t => t.Name == name),
            $"GetTeamByName_{name}");
    }
}
```

### Circuit Breaker Pattern
```csharp
public class OrderService
{
    private readonly IConnectionResilienceService _resilience;
    
    public async Task<Order> ProcessOrderAsync(Order order)
    {
        return await _resilience.ExecuteWithCircuitBreakerAsync(
            async () => await SaveOrderToDatabase(order),
            "ProcessOrder");
    }
}
```

### Entity Framework Conventions
```csharp
public class MyDbContext : DbContext
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Apply shared conventions automatically
        modelBuilder.ApplySharedConventions();
        
        // Or apply specific conventions
        modelBuilder.ApplySnakeCaseNaming();
        modelBuilder.ApplyPluralizedTableNames();
        modelBuilder.ApplyForeignKeyConventions();
    }
}
```

### Value Converters Usage
```csharp
public class TeamConfiguration : IEntityTypeConfiguration<Team>
{
    public void Configure(EntityTypeBuilder<Team> builder)
    {
        // Strongly-typed ID conversion
        builder.Property(t => t.Id)
            .HasConversion<StronglyTypedIdConverter<TeamId>>();
            
        // Enum list conversion
        builder.Property(t => t.Categories)
            .HasConversion<EnumListConverter<TeamCategory>>();
            
        // Complex object JSON conversion
        builder.Property(t => t.Settings)
            .HasConversion<DictionaryConverter<string, object>>();
    }
}
```

### Join Entity Configuration
```csharp
// Define join entity
public class CompetitionTeam : EntityLink<Competition, Team, CompetitionId, TeamId>
{
    public CompetitionTeam(Competition competition, Team team) 
        : base(competition, team, competition.Id, team.Id) { }
}

// Configure many-to-many relationship
public class CompetitionConfiguration : IEntityTypeConfiguration<Competition>
{
    public void Configure(EntityTypeBuilder<Competition> builder)
    {
        builder.HasManyTeams<Competition, CompetitionId, CompetitionTeam>();
    }
}
```

## 🏗️ Project Structure

```
MyClub.Shared.Infrastructure.Persistence/
├── MyClub.Shared.Infrastructure.Persistence.csproj  # Project file with EF Core dependencies
├── ErrorHandling/                                   # Error handling components
│   ├── Connection/                                  # Circuit breaker and resilience
│   │   ├── ConnectionResilienceService.cs          # Circuit breaker implementation
│   │   ├── IConnectionResilienceService.cs         # Resilience service interface
│   │   ├── CircuitBreakerState.cs                  # Circuit breaker states
│   │   ├── CircuitBreakerOpenException.cs          # Custom exception
│   │   └── ConnectionHealthResult.cs               # Health check result
│   ├── PersistenceError/                           # Centralized error handling
│   │   ├── PersistenceErrorHandler.cs              # Main error handler
│   │   ├── IPersistenceErrorHandler.cs             # Error handler interface
│   │   └── PersistenceErrorHandlingOptions.cs     # Configuration options
│   └── RetryPolicy/                                # Retry logic and policies
│       ├── DatabaseRetryPolicy.cs                  # Retry policy implementation
│       └── IDatabaseRetryPolicy.cs                 # Retry policy interface
├── Repositories/                                   # Base repository implementations
│   ├── Repository.cs                               # Generic repository base class
│   └── ReadOnlyRepository.cs                       # Read-only repository base
├── Converters/                                     # EF Core value converters
│   ├── StronglyTypedIdConverter.cs                 # Strongly-typed ID conversion
│   ├── StronglyTypedIdListConverter.cs             # List of strongly-typed IDs
│   ├── EnumListConverter.cs                        # Enum collection conversion
│   ├── EnumClassConverter.cs                       # Enum class conversion
│   ├── DictionaryConverter.cs                      # Dictionary conversion
│   ├── DictionaryJsonConverter.cs                  # JSON dictionary conversion
│   ├── ListConverter.cs                            # Generic list conversion
│   └── ConverterHelper.cs                          # Conversion utilities
├── Conventions/                                    # EF Core model conventions
│   ├── ConventionHelper.cs                         # Convention utilities
│   ├── SnakeCaseColumnConvention.cs                # Snake case column naming
│   ├── PluralizeTableNameConvention.cs             # Table name pluralization
│   └── ForeignKeyNamingConvention.cs               # Consistent FK naming
├── Extensions/                                     # Builder and configuration extensions
│   ├── ServiceCollectionExtensions.cs              # DI registration
│   ├── ModelBuilderExtensions.cs                   # EF model builder extensions
│   └── BuilderExtensions.cs                        # Entity configuration helpers
├── JoinEntities/                                   # Base classes for join entities
│   └── EntityLink.cs                               # Generic join entity base
├── Monitoring/                                     # Metrics and monitoring
│   ├── IPersistenceMetrics.cs                      # Metrics interface
│   └── NullPersistenceMetrics.cs                   # Null object pattern
├── Testing/                                        # Testing support
│   └── ITestableConnectionResilienceService.cs     # Testable resilience interface
└── UnitOfWork.cs                                   # Transaction coordination
```

## 🧪 Testing

### Unit Testing with Mocks
```csharp
[Test]
public async Task Should_Retry_On_Transient_Failure()
{
    // Arrange
    var mockRetryPolicy = new Mock<IDatabaseRetryPolicy>();
    var errorHandler = new PersistenceErrorHandler(options, logger, mockRetryPolicy.Object);
    
    // Act & Assert
    await errorHandler.ExecuteWithErrorHandlingAsync(
        () => ThrowTransientException(),
        "TestOperation");
        
    mockRetryPolicy.Verify(x => x.ShouldRetry(It.IsAny<Exception>(), It.IsAny<int>()), Times.AtLeastOnce);
}
```

### Circuit Breaker Testing
```csharp
[Test]
public async Task Should_Open_Circuit_After_Threshold_Failures()
{
    // Arrange
    var resilience = new ConnectionResilienceService(options, logger);
    
    // Act - Simulate failures
    for (int i = 0; i < options.CircuitBreakerFailureThreshold; i++)
    {
        await Assert.ThrowsAsync<DatabaseException>(() => 
            resilience.ExecuteWithCircuitBreakerAsync(() => ThrowDatabaseError(), "Test"));
    }
    
    // Assert - Circuit should be open
    Assert.False(resilience.IsConnectionHealthy);
    Assert.Equal(CircuitBreakerState.Open, resilience.CircuitBreakerState);
}
```

### Repository Testing
```csharp
[Test]
public async Task Repository_Should_Apply_Conventions()
{
    // Arrange
    using var context = CreateInMemoryContext();
    var repository = new TeamRepository(context);
    
    // Act
    var team = Team.Create("Manchester United", "MAN_UTD");
    repository.Add(team);
    await context.SaveChangesAsync();
    
    // Assert
    var savedTeam = await repository.GetByIdAsync(team.Id);
    Assert.NotNull(savedTeam);
    Assert.Equal(team.Name, savedTeam.Name);
}
```

## 📦 Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| **Microsoft.EntityFrameworkCore** | 10.0.0-preview.6 | Database access framework |
| **Microsoft.EntityFrameworkCore.Relational** | 10.0.0-preview.6 | Relational database provider base |
| **MyNet.Humanizer** | 5.0.0 | String manipulation and formatting |
| **MyClub.Shared.Kernel** | Project Reference | Core domain primitives |

## 🔗 Related Projects

- **MyClub.Shared.Kernel**: Core domain primitives and abstractions
- **MyClub.Shared.Application**: Application layer with CQRS patterns
- **MyClub.Scorer.Infrastructure.Persistence**: Scorer-specific persistence implementations
- **MyClub.*.Infrastructure.Persistence**: Other module-specific persistence implementations

## 🌟 Benefits

### ✅ **Enterprise-Ready**
- Production-tested patterns and practices
- Comprehensive error handling and recovery
- Built-in monitoring and alerting capabilities
- Configurable resilience strategies
- High-performance logging with LoggerMessage delegates

### ✅ **Developer Experience**
- Rich Entity Framework Core extensions
- Automatic database conventions
- Strongly-typed value converters
- Simple registration with sensible defaults
- Extensive configuration options
- Clear documentation and examples

### ✅ **Operational Excellence**
- Zero-allocation logging for performance
- Circuit breaker prevents cascade failures
- Retry policies handle transient issues
- Health check integration for monitoring
- Detailed metrics for performance tracking

### ✅ **Flexibility & Extensibility**
- Modular service registration
- Environment-specific configuration
- Custom metrics provider support
- Extensible error handling strategies
- Pluggable conventions and converters

### ✅ **Type Safety**
- Strongly-typed entity IDs with automatic conversion
- Type-safe repository patterns
- Compile-time validation of configurations
- Generic join entity support

---

This shared infrastructure provides a comprehensive foundation for building resilient, observable, and maintainable persistence layers across the entire MyClub suite, with advanced Entity Framework Core patterns and enterprise-grade reliability features.