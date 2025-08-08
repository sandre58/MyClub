# MyClub.Scorer.Infrastructure.Persistence

> Infrastructure persistence layer for football scoring and competition management with advanced error handling and resilience patterns

![.NET](https://img.shields.io/badge/.NET-10-blue)
![Entity Framework](https://img.shields.io/badge/EF%20Core-10.0-purple)
![SQLite](https://img.shields.io/badge/Database-SQLite%2FPostgreSQL%2FMySQL-green)
![Clean Architecture](https://img.shields.io/badge/Architecture-Clean-brightgreen)
![Error Handling](https://img.shields.io/badge/Pattern-Error%20Handling-orange)
![Circuit Breaker](https://img.shields.io/badge/Pattern-Circuit%20Breaker-red)

## 🏆 Overview

**MyClub.Scorer.Infrastructure.Persistence** is the infrastructure layer implementation for data persistence in the MyClub football scoring module. This project provides Entity Framework Core-based implementations of domain repositories and manages the complete data access strategy for football competitions, matches, teams, and related entities.

The infrastructure layer implements the repository contracts defined in the domain layer while providing advanced features like enterprise-grade error handling, connection resilience, custom value converters, entity configurations, and optimized query patterns for complex football domain scenarios.

## 🏗️ Architecture

This project implements the Infrastructure layer in Clean Architecture with advanced Entity Framework Core patterns and enterprise reliability features:

```
┌─────────────────────────────────────────┐
│              Presentation               │
├─────────────────────────────────────────┤
│            Application Layer            │
│            (CQRS Handlers)              │
├─────────────────────────────────────────┤
│               Domain Layer              │
│          (Rich Business Logic)          │
├─────────────────────────────────────────┤
│      🗄️ INFRASTRUCTURE PERSISTENCE     │ ← This Project
│  ┌─────────────┐ ┌────────────────────┐ │
│  │  DbContext  │ │    Repositories    │ │
│  │    (EF)     │ │  (Domain Impl.)    │ │
│  └─────────────┘ └────────────────────┘ │
│  ┌─────────────┐ ┌────────────────────┐ │
│  │ Converters  │ │   Configurations   │ │
│  │ (JSON/EF)   │ │   (Entity Maps)    │ │
│  └─────────────┘ └────────────────────┘ │
│  ┌─────────────┐ ┌────────────────────┐ │
│  │ Error       │ │   Connection       │ │
│  │ Handling    │ │   Resilience       │ │
│  └─────────────┘ └────────────────────┘ │
└─────────────────────────────────────────┘
```

## ⚽ Core Features

### 🗄️ **Advanced Entity Framework Implementation**
- **DbContext Management**: Centralized database context with proper lifecycle management
- **Repository Pattern**: Domain-driven repository implementations with optimized queries
- **Unit of Work**: Transactional consistency with domain event dispatching
- **Migration Support**: Multi-database provider support (SQLite, SQL Server, PostgreSQL)

### 🛡️ **Enterprise Error Handling & Resilience**
- **Circuit Breaker Pattern**: Automatic failure detection and recovery with state management
- **Connection Resilience**: Health monitoring and automatic connection recovery
- **Retry Policies**: Exponential backoff with intelligent transient failure detection
- **High-Performance Logging**: LoggerMessage delegates for zero-allocation logging
- **Error Handler Integration**: Centralized error handling with configurable strategies

### 🔄 **Sophisticated Value Conversion**
- **JSON Converters**: Complex object serialization for polymorphic entities
- **Strongly-Typed IDs**: Safe conversion between domain IDs and database primitives
- **Domain Value Objects**: Persistence mapping for rich domain value objects
- **Team References**: Polymorphic team reference handling for tournament brackets

### ⚙️ **Entity Configuration System**
- **Fluent Configuration**: Type-safe entity mapping with fluent API
- **Join Entity Management**: Complex many-to-many relationships for competitions
- **Convention-Based Mapping**: Automatic configuration based on domain patterns
- **Inheritance Handling**: Table-per-hierarchy for competition and stage types

### 🔗 **Relationship Management**
- **Aggregate Boundaries**: Proper aggregate root handling with related entities
- **Foreign Key Conventions**: Consistent naming and relationship patterns
- **Cascade Behaviors**: Appropriate cascade rules for domain integrity
- **Join Entities**: Explicit join tables for complex many-to-many scenarios

## 🛠️ Technologies

| Dependency | Version | Purpose |
|------------|---------|---------|
| **Entity Framework Core** | 10.0.0-preview.6 | Primary ORM and data access |
| **EF Core Relational** | 10.0.0-preview.6 | Relational database provider base |
| **EF Core Tools** | 10.0.0-preview.6 | Design-time tools and migrations |
| **EF Core Design** | 10.0.0-preview.6 | Design-time services |
| **MyNet.Humanizer** | 5.0.0 | String manipulation and formatting |
| **MyClub.Shared.Infrastructure.Persistence** | Project Reference | Shared persistence patterns and error handling |
| **MyClub.Scorer.Domain** | Project Reference | Domain model and contracts |

## 🏆 Infrastructure Architecture

### DbContext Implementation

```csharp
// Centralized database context for Scorer module
public class ScorerDbContext : DbContext
{
    // Core aggregate DbSets
    public DbSet<Match> Matches => Set<Match>();
    public DbSet<Matchday> Matchdays => Set<Matchday>();
    public DbSet<Round> Rounds => Set<Round>();
    public DbSet<Stage> Stages => Set<Stage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Apply shared naming conventions
        modelBuilder.ApplySharedConventions();
        
        // Apply all entity configurations from assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ScorerDbContext).Assembly);
    }
}
```

### Repository Implementation with Error Handling

```csharp
// Domain-driven repository with EF Core optimizations and error handling
public sealed class MatchRepository : Repository<Match, MatchId, ScorerDbContext>, IMatchRepository
{
    private readonly IPersistenceErrorHandler _errorHandler;
    private readonly IConnectionResilienceService _resilience;

    public MatchRepository(ScorerDbContext context, IPersistenceErrorHandler errorHandler, 
                          IConnectionResilienceService resilience) : base(context)
    {
        _errorHandler = errorHandler;
        _resilience = resilience;
    }

    // Optimized query configuration with includes
    protected override IQueryable<Match> ConfigureQuery(IQueryable<Match> query) =>
        query
            .Include(m => m.Home)
            .Include(m => m.Away)
            .Include(m => m.Stadium)
            .Include(m => m.MatchEvents);

    // Domain-specific query with error handling
    public async Task<IEnumerable<Match>> GetMatchesByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _errorHandler.ExecuteWithErrorHandlingAsync(async () =>
        {
            return await _resilience.ExecuteWithCircuitBreakerAsync(async () =>
            {
                return await DbSet
                    .Where(m => m.OriginDate >= startDate && m.OriginDate <= endDate)
                    .Include(m => m.Home)
                    .Include(m => m.Away)
                    .ToListAsync();
            }, $"GetMatchesByDateRange_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}");
        }, "GetMatchesByDateRange");
    }
}
```

### Advanced Error Handling Integration

```csharp
// Service registration with error handling
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> dbOptions,
        Action<PersistenceErrorHandlingOptions>? errorHandlingOptions = null)
    {
        // Configure error handling options
        var options = new PersistenceErrorHandlingOptions
        {
            EnableRetryOnTransientFailures = true,
            MaxRetryAttempts = 3,
            EnableCircuitBreaker = true,
            CircuitBreakerFailureThreshold = 5,
            EnablePerformanceMonitoring = true
        };
        errorHandlingOptions?.Invoke(options);
        services.AddSingleton(options);

        // Register shared error handling services
        services.AddScoped<IPersistenceErrorHandler, PersistenceErrorHandler>();
        services.AddScoped<IConnectionResilienceService, ConnectionResilienceService>();
        services.AddScoped<IDatabaseRetryPolicy, DatabaseRetryPolicy>();

        // Register domain repositories with error handling
        services.AddScoped<IMatchRepository, MatchRepository>();
        services.AddScoped<ICompetitionRepository, CompetitionRepository>();
        services.AddScoped<IMatchdayRepository, MatchdayRepository>();
        services.AddScoped<IRoundRepository, RoundRepository>();
        services.AddScoped<IStageRepository, StageRepository>();

        // Register EF Core DbContext
        services.AddDbContext<ScorerDbContext>(dbOptions);
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
```

### Advanced Value Converters

```csharp
// JSON-based converter for polymorphic team references
internal sealed class TeamReferenceConverter : ValueConverter<TeamReference, string>
{
    public TeamReferenceConverter() : base(
        // Convert to database
        v => JsonSerializer.Serialize(v, v.GetType(), SerializerOptions),
        // Convert from database
        v => JsonSerializer.Deserialize<TeamReference>(v, DeserializerOptions)!)
    { }
}

// Strongly-typed ID converter
internal sealed class StronglyTypedIdConverter<TId, TValue> : ValueConverter<TId, TValue>
    where TId : EntityId<TId>
{
    public StronglyTypedIdConverter() : base(
        // Convert to database primitive
        id => id.Value,
        // Convert to strongly-typed ID
        value => EntityId.From<TId>(value))
    { }
}

// Complex object converter for round formats
internal sealed class RoundFormatConverter : ValueConverter<RoundFormat, string>
{
    public RoundFormatConverter() : base(
        // Serialize complex format objects
        format => JsonSerializer.Serialize(format, format.GetType(), SerializerOptions),
        // Deserialize with type information
        json => JsonSerializer.Deserialize<RoundFormat>(json, DeserializerOptions)!)
    { }
}
```

### Entity Configuration Examples

```csharp
// Match aggregate configuration
internal sealed class MatchConfiguration : IEntityTypeConfiguration<Match>
{
    public void Configure(EntityTypeBuilder<Match> builder)
    {
        // Configure as aggregate root
        builder.ConfigureEntity<Match, MatchId>();
        
        // Complex property mappings
        builder.Property(m => m.HomeTeamReference)
               .HasConversion<TeamReferenceConverter>();
               
        builder.Property(m => m.AwayTeamReference)
               .HasConversion<TeamReferenceConverter>();
               
        builder.Property(m => m.Format)
               .HasConversion<MatchFormatConverter>();
               
        // Owned entities for match opponents
        builder.OwnsOne(m => m.Home, homeBuilder =>
        {
            homeBuilder.OwnsMany(h => h.Goals);
            homeBuilder.OwnsMany(h => h.Cards);
            homeBuilder.OwnsMany(h => h.Shootout);
        });
        
        builder.OwnsOne(m => m.Away, awayBuilder =>
        {
            awayBuilder.OwnsMany(a => a.Goals);
            awayBuilder.OwnsMany(a => a.Cards);
            awayBuilder.OwnsMany(a => a.Shootout);
        });
        
        // Audit properties
        builder.OwnsAuditableProperties();
    }
}

// Competition inheritance configuration
internal sealed class CompetitionConfiguration : IEntityTypeConfiguration<Competition>
{
    public void Configure(EntityTypeBuilder<Competition> builder)
    {
        // Table-per-hierarchy inheritance
        builder.HasDiscriminator<string>("CompetitionType")
               .HasValue<League>("League")
               .HasValue<Cup>("Cup")
               .HasValue<Tournament>("Tournament");
               
        // Shared properties
        builder.ConfigureEntity<Competition, CompetitionId>();
        builder.Property(c => c.Name).IsRequired().HasMaxLength(200);
        
        // Teams relationship with join entity
        builder.HasMany<Team>()
               .WithMany()
               .UsingEntity<CompetitionTeam>();
               
        // Stadiums relationship
        builder.HasMany<Stadium>()
               .WithMany()
               .UsingEntity<CompetitionStadium>();
    }
}
```

## 🎯 Usage Examples

### Database Configuration with Error Handling

```csharp
// In Program.cs or Startup.cs
public void ConfigureServices(IServiceCollection services)
{
    // Add persistence layer with error handling
    services.AddPersistence(options =>
    {
        // SQLite for development
        options.UseSqlite(connectionString, sqliteOptions =>
        {
            sqliteOptions.MigrationsAssembly("MyClub.Scorer.Infrastructure.Migrations.Sqlite");
        });
    }, errorOptions =>
    {
        // Configure error handling
        errorOptions.EnableRetryOnTransientFailures = true;
        errorOptions.MaxRetryAttempts = 3;
        errorOptions.EnableCircuitBreaker = true;
        errorOptions.CircuitBreakerFailureThreshold = 5;
        errorOptions.EnableDetailedErrorLogging = Environment.IsDevelopment();
    });
    
    // Register domain event dispatcher
    services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
}
```

### Repository Usage with Resilience

```csharp
// Command handler using repository with error handling
public class AddTeamCommandHandler
{
    private readonly ICompetitionRepository _competitionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public async Task<Result<Guid>> Handle(AddTeamCommand command, CancellationToken cancellationToken)
    {
        try
        {
            // Repository automatically handles transient failures and circuit breaker
            var competition = await _competitionRepository.GetByIdAsync(command.CompetitionId);
            if (competition is null)
                return Failures.NotFound<Guid>(command.CompetitionId.ToString());

            // Domain operation
            var team = Team.Create(command.Name, command.ShortName);
            var result = competition.AddTeam(team);
            
            if (result.IsFailure)
                return Result.Fail<Guid>(result);

            // Persist with unit of work (includes error handling and domain events)
            _competitionRepository.Update(competition);
            await _unitOfWork.CommitAsync(cancellationToken);

            return Result.Success(team.Id.Value);
        }
        catch (CircuitBreakerOpenException ex)
        {
            // Handle circuit breaker open scenario
            return Result.Fail<Guid>($"Database unavailable: {ex.Reason}");
        }
    }
}
```

### Health Check Integration

```csharp
// Add health checks for monitoring
public void ConfigureServices(IServiceCollection services)
{
    services.AddHealthChecks()
        .AddDbContextCheck<ScorerDbContext>("scorer_database")
        .AddCheck<ConnectionResilienceHealthCheck>("connection_resilience");
}

// Health check endpoint
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        var response = new
        {
            Status = report.Status.ToString(),
            TotalDuration = report.TotalDuration.TotalMilliseconds,
            Checks = report.Entries.Select(x => new
            {
                Name = x.Key,
                Status = x.Value.Status.ToString(),
                Duration = x.Value.Duration.TotalMilliseconds,
                Data = x.Value.Data,
                Description = x.Value.Description
            })
        };
        
        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
});
```

## 📁 Project Structure

```
MyClub.Scorer.Infrastructure.Persistence/
├── MyClub.Scorer.Infrastructure.Persistence.csproj # Project dependencies and EF packages
├── DbContexts/                                     # Database context implementations
│   └── ScorerDbContext.cs                         # Main EF Core DbContext
├── Repositories/                                   # Domain repository implementations
│   ├── MatchRepository.cs                         # Match aggregate repository
│   ├── CompetitionRepository.cs                   # Competition repository
│   ├── MatchdayRepository.cs                      # Matchday repository
│   ├── RoundRepository.cs                         # Round repository
│   └── StageRepository.cs                         # Stage repository
├── Configuration/                                  # Entity type configurations
│   ├── MatchConfiguration.cs                      # Match entity mapping
│   ├── CompetitionConfiguration.cs                # Competition inheritance mapping
│   ├── LeagueConfiguration.cs                     # League-specific configuration
│   ├── CupConfiguration.cs                        # Cup-specific configuration
│   ├── TournamentConfiguration.cs                 # Tournament-specific configuration
│   ├── TeamConfiguration.cs                       # Team entity mapping
│   ├── StadiumConfiguration.cs                    # Stadium entity mapping
│   ├── MatchdayConfiguration.cs                   # Matchday entity mapping
│   ├── RoundConfiguration.cs                      # Round entity mapping
│   ├── StageConfiguration.cs                      # Stage inheritance mapping
│   ├── GroupStageConfiguration.cs                 # Group stage configuration
│   ├── KnockoutStageConfiguration.cs              # Knockout stage configuration
│   ├── ChampionshipStageConfiguration.cs          # Championship stage configuration
│   ├── GroupConfiguration.cs                      # Group entity mapping
│   ├── FixtureConfiguration.cs                    # Fixture entity mapping
│   ├── RoundStageConfiguration.cs                 # Round stage mapping
│   ├── PlayerConfiguration.cs                     # Player entity mapping
│   └── ManagerConfiguration.cs                    # Manager entity mapping
├── JoinEntities/                                   # Explicit join table entities
│   ├── EntityTeam.cs                              # Team assignment base
│   ├── EntityMatch.cs                             # Match assignment base
│   ├── GroupStageMatchday.cs                      # Group stage to matchday link
│   ├── ChampionshipStageMatchday.cs               # Championship stage to matchday link
│   ├── LeagueMatchday.cs                          # League to matchday link
│   ├── KnockoutStageRound.cs                      # Knockout stage to round link
│   ├── CupRound.cs                                # Cup to round link
│   ├── TournamentStage.cs                         # Tournament to stage link
│   ├── MatchdayMatch.cs                           # Matchday to match link
│   ├── RoundStageMatch.cs                         # Round stage to match link
│   ├── StageTeam.cs                               # Stage to team link
│   ├── GroupTeam.cs                               # Group to team link
│   └── RoundTeam.cs                               # Round to team link
├── Converters/                                     # Value converters for complex types
│   ├── TeamReferenceConverter.cs                  # Polymorphic team reference conversion
│   ├── TeamReferenceJsonConverter.cs              # JSON serialization for team references
│   ├── RoundFormatConverter.cs                    # Round format object conversion
│   ├── RoundFormatJsonConverter.cs                # JSON serialization for round formats
│   ├── StandingComparerConverter.cs               # Standing comparer conversion
│   ├── StandingComparerJsonConverter.cs           # JSON serialization for comparers
│   └── StandingColumnListConverter.cs             # Standing column list conversion
├── Extensions/                                     # Service registration and utilities
│   ├── ServiceCollectionExtensions.cs             # DI container registration with error handling
│   └── BuilderExtensions.cs                       # Configuration helper methods
└── UnitOfWork.cs                                   # Transaction and event management
```

## 🚀 Benefits

### ✅ **Enterprise-Grade Reliability**
- **Circuit Breaker Protection**: Prevents cascade failures with automatic recovery
- **Intelligent Retry Logic**: Exponential backoff for transient failure recovery
- **Connection Health Monitoring**: Real-time database connectivity tracking
- **High-Performance Logging**: LoggerMessage delegates for zero-allocation logging
- **Configurable Error Strategies**: Environment-specific error handling policies

### ✅ **Advanced Entity Framework Patterns**
- **Domain-Driven Repositories**: Rich repository implementations with domain-specific operations
- **Complex Value Conversion**: JSON serialization for polymorphic and complex domain objects
- **Optimized Queries**: Include strategies and query optimization for performance
- **Convention-Based Mapping**: Automatic configuration reducing boilerplate code

### ✅ **Multi-Database Support**
- **Provider Agnostic**: Works with SQLite, SQL Server, PostgreSQL, MySQL
- **Migration Management**: Separate migration assemblies per database provider
- **Connection Flexibility**: Runtime database provider selection
- **Cloud Ready**: Optimized for both on-premises and cloud databases

### ✅ **Football Domain Expertise**
- **Complex Relationships**: Proper handling of tournament brackets and team assignments
- **Polymorphic Entities**: Support for different competition and stage types
- **Performance Optimized**: Queries optimized for football-specific access patterns
- **Referential Integrity**: Proper cascade behaviors for domain consistency

### ✅ **Developer Experience**
- **Error Handling Integration**: Transparent error handling without boilerplate
- **Type Safety**: Strongly-typed configurations and converters
- **Migration Tools**: Full EF Core tooling support
- **Testing Support**: In-memory database provider support for testing
- **Performance Monitoring**: Built-in query performance tracking

## 🧪 Testing Strategy

### Repository Testing with Error Handling

```csharp
[Fact]
public async Task MatchRepository_Should_Handle_Transient_Failures()
{
    // Arrange
    var mockErrorHandler = new Mock<IPersistenceErrorHandler>();
    var mockResilience = new Mock<IConnectionResilienceService>();
    using var context = CreateInMemoryContext();
    var repository = new MatchRepository(context, mockErrorHandler.Object, mockResilience.Object);
    
    // Setup error handler to simulate retry
    mockErrorHandler.Setup(x => x.ExecuteWithErrorHandlingAsync(It.IsAny<Func<Task<Match>>>(), It.IsAny<string>()))
               .Returns<Func<Task<Match>>, string>((func, name) => func());
    
    var match = CreateTestMatch();
    
    // Act
    repository.Add(match);
    await context.SaveChangesAsync();
    
    var loaded = await repository.GetByIdAsync(match.Id);
    
    // Assert
    loaded.Should().NotBeNull();
    mockErrorHandler.Verify(x => x.ExecuteWithErrorHandlingAsync(It.IsAny<Func<Task<Match>>>(), It.IsAny<string>()), Times.Once);
}

[Fact]
public async Task UnitOfWork_Should_Handle_Circuit_Breaker_Open()
{
    // Arrange
    var mockDispatcher = new Mock<IDomainEventDispatcher>();
    using var context = CreateInMemoryContext();
    var unitOfWork = new UnitOfWork(context, mockDispatcher.Object);
    
    // Simulate circuit breaker open
    context.Database.EnsureDeleted(); // Force connection failure
    
    // Act & Assert
    await Assert.ThrowsAsync<CircuitBreakerOpenException>(() => unitOfWork.CommitAsync());
}
```

## 📦 Dependencies

- **Entity Framework Core 10.0.0-preview.6**: Primary ORM and data access framework
- **EF Core Relational**: Relational database provider support
- **MyNet.Humanizer**: String manipulation and formatting utilities
- **MyClub.Shared.Infrastructure.Persistence**: Shared persistence patterns, error handling, and resilience services
- **MyClub.Scorer.Domain**: Domain model and repository contracts

## 🔗 Related Projects

- **MyClub.Scorer.Domain**: Domain model and repository interfaces implemented by this project
- **MyClub.Scorer.Infrastructure.Migrations.Sqlite**: SQLite-specific migration assembly
- **MyClub.Scorer.Infrastructure.Migrations.SqlServer**: SQL Server-specific migration assembly
- **MyClub.Shared.Infrastructure.Persistence**: Shared persistence patterns, error handling, and resilience utilities
- **MyClub.Scorer.Application**: Application layer consuming the repositories
- **MyClub.Scorer.Infrastructure.Persistence.Design**: Design-time tools for EF Core
- **MyClub.Scorer.Infrastructure.Persistence.Tests**: Test project for persistence layer

## 🌟 Enterprise Features

### 🛡️ **Production-Ready Error Handling**
- **Transient Failure Detection**: Automatic identification of retryable database errors
- **Circuit Breaker Pattern**: Prevents cascade failures during database outages
- **Retry Policies**: Configurable exponential backoff with jitter
- **Health Monitoring**: Real-time connection status and performance metrics

### 📊 **Observability & Monitoring**
- **Performance Metrics**: Query execution time tracking
- **Circuit Breaker Metrics**: State change monitoring and alerting
- **Health Check Integration**: ASP.NET Core health check endpoints
- **Structured Logging**: High-performance logging with LoggerMessage delegates

### ⚙️ **Configuration & Flexibility**
- **Environment-Specific Settings**: Different policies for dev/staging/production
- **Runtime Configuration**: Dynamic error handling policy adjustment
- **Custom Error Strategies**: Pluggable error handling implementations
- **Multi-Database Support**: Provider-agnostic with connection string configuration

---

**MyClub.Scorer.Infrastructure.Persistence** provides enterprise-grade data access for football competition management, implementing sophisticated Entity Framework Core patterns with comprehensive error handling and resilience features while maintaining clean separation from domain logic. The infrastructure supports complex football domain scenarios with optimized performance, multi-database flexibility, and production-ready reliability patterns.