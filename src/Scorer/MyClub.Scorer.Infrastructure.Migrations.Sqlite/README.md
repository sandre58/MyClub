# MyClub.Scorer.Infrastructure.Migrations.Sqlite

> Entity Framework Core SQLite migrations for football scoring system development and testing

![.NET](https://img.shields.io/badge/.NET-10-blue)
![EF Core](https://img.shields.io/badge/EF%20Core-10.0-green)
![SQLite](https://img.shields.io/badge/Database-SQLite-lightblue)
![Development](https://img.shields.io/badge/Purpose-Development-orange)

## Overview

**MyClub.Scorer.Infrastructure.Migrations.Sqlite** contains Entity Framework Core migrations and database schema management specifically for SQLite. This project maintains the complete database schema evolution history for development and testing environments, providing the SQLite-specific implementation of the design-time DbContext factory for lightweight football scoring system data persistence.

## Architecture

This project serves as the SQLite-specific migration container in the multi-provider database architecture:

```text
┌─────────────────────────────────────────┐
│          EF Core Tools                  │ ← dotnet ef commands
│      (Migration Generation)             │
├─────────────────────────────────────────┤
│        Design Project                   │ ← Configuration & Base Factory
│     (MyClubDbContextFactoryBase)        │
├─────────────────────────────────────────┤
│       SQLITE MIGRATIONS                 │ ← This Project
│     (Provider-Specific Factory          │
│      & Migration History)               │
├─────────────────────────────────────────┤
│       Persistence Layer                 │ ← Core DbContext
│        (ScorerDbContext)                │
├─────────────────────────────────────────┤
│          Domain Layer                   │ ← Domain entities
│        (Football Entities)              │
└─────────────────────────────────────────┘
```

## Features

### 🗃️ **SQLite Migration Management**
- **Migration History**: Complete database schema evolution for SQLite
- **Provider-Specific Factory**: SQLite implementation of design-time DbContext factory
- **Schema Generation**: Automated SQLite DDL generation and execution
- **Version Control**: Database schema versioning and migration tracking

### ⚡ **SQLite Optimizations**
- **File-Based Database**: Self-contained SQLite database files for portability
- **Cross-Platform Support**: SQLite compatibility across Windows, macOS, and Linux
- **Zero Configuration**: No server installation or configuration required
- **Lightweight Performance**: Optimized for development and testing scenarios

### 🧪 **Development & Testing Support**
- **Rapid Development**: Instant database creation and schema updates
- **Testing Integration**: Perfect for unit and integration testing
- **Version Control Friendly**: Database files can be included in source control
- **Migration Assembly**: Proper assembly configuration for SQLite migrations

## Technologies

| Component | Version | Purpose |
|-----------|---------|---------|
| **Microsoft.EntityFrameworkCore.Sqlite** | 10.0.0-preview.6 | SQLite provider for EF Core |
| **Microsoft.EntityFrameworkCore.Tools** | 10.0.0-preview.6 | EF Core command-line tools |
| **Microsoft.EntityFrameworkCore.Design** | 10.0.0-preview.6 | Design-time services for EF Core |
| **.NET** | 10.0 | Target framework for migration assembly |

## Project Structure

```text
MyClub.Scorer.Infrastructure.Migrations.Sqlite/
├── MyClub.Scorer.Infrastructure.Migrations.Sqlite.csproj    # Project file with SQLite packages
├── MyClubDbContextFactory.cs                               # SQLite-specific factory implementation
└── Migrations/                                             # Generated migration files directory
    ├── 20250802053340_InitSchema.cs                        # Initial schema migration
    ├── 20250802053340_InitSchema.Designer.cs              # Migration metadata
    └── MyClubDbContextModelSnapshot.cs                    # Current model snapshot
```

## Core Components

### MyClubDbContextFactory

The SQLite-specific implementation of the design-time DbContext factory that extends the abstract base factory:

**Key Features:**
- **Provider Identification**: Specifies "Sqlite" as the provider name for configuration lookup
- **SQLite Configuration**: Configures EF Core to use SQLite with migration assembly
- **Assembly Management**: Ensures migrations are stored in the SQLite-specific assembly
- **Connection String**: Uses SQLite connection string from configuration

**Implementation Details:**
```csharp
public class MyClubDbContextFactory : MyClubDbContextFactoryBase
{
    protected override string ProviderName => "Sqlite";

    protected override void ConfigureOptions(DbContextOptionsBuilder<ScorerDbContext> optionsBuilder, string connectionString)
        => optionsBuilder.UseSqlite(connectionString, static x => x.MigrationsAssembly(Assembly.GetExecutingAssembly()));
}
```

### Migration Files

The project contains EF Core migration files that define the database schema evolution for SQLite:

**Migration Structure:**
- **Migration Classes**: Define schema changes with Up() and Down() methods
- **Designer Files**: Contain metadata and model snapshots for each migration
- **Model Snapshot**: Represents the current complete database model state

**Initial Schema Migration:**
- **Tables**: Competition, Team, Match, Stadium, Player, and related entities
- **Relationships**: Foreign keys and navigation properties for football data
- **Indexes**: Optimized indexes for common query patterns
- **Constraints**: Data integrity constraints adapted for SQLite capabilities

## Database Schema

### Core Football Entities

The SQLite schema includes comprehensive football data structures optimized for development:

**Competition Management:**
- **Competitions**: League, Cup, and Tournament entities with inheritance
- **Teams**: Team information with players, staff, and stadium relationships
- **Stadiums**: Venue data with geographic information and capacity details

**Match System:**
- **Matches**: Match entities with opponent data and event tracking
- **Match Events**: Goals, cards, and penalty shootout events
- **Match Days**: Fixture organization and scheduling information

**Tournament Structure:**
- **Stages**: Group, knockout, and championship stage entities
- **Rounds**: Tournament round management with fixture pairings
- **Groups**: Group-based competition organization

### SQLite Specific Features

**Data Types:**
- **TEXT**: For string data including UUIDs and JSON
- **INTEGER**: For numeric data and foreign keys
- **REAL**: For floating-point geographic coordinates
- **BLOB**: For binary data like images and files

**Constraints:**
- **Primary Keys**: TEXT columns with UUID values
- **Foreign Keys**: Referential integrity with cascade behaviors
- **Check Constraints**: Business rule enforcement at database level
- **Unique Constraints**: Data uniqueness enforcement

**SQLite Advantages:**
- **File-Based**: Single file database for easy deployment
- **Cross-Platform**: Works identically across all operating systems
- **Zero Configuration**: No server installation required
- **ACID Compliance**: Full transaction support despite being lightweight

## Usage Scenarios

### Migration Generation

Generate new migrations for schema changes:

```bash
# Add new migration
dotnet ef migrations add AddPlayerStatistics \
  --project src/Scorer/MyClub.Scorer.Infrastructure.Migrations.Sqlite \
  --startup-project src/Scorer/MyClub.Scorer.Infrastructure.Persistence.Design

# List existing migrations
dotnet ef migrations list \
  --project src/Scorer/MyClub.Scorer.Infrastructure.Migrations.Sqlite \
  --startup-project src/Scorer/MyClub.Scorer.Infrastructure.Persistence.Design
```

### Database Updates

Apply migrations to SQLite databases:

```bash
# Update to latest migration
dotnet ef database update \
  --project src/Scorer/MyClub.Scorer.Infrastructure.Migrations.Sqlite \
  --startup-project src/Scorer/MyClub.Scorer.Infrastructure.Persistence.Design

# Update to specific migration
dotnet ef database update 20250802053340_InitSchema \
  --project src/Scorer/MyClub.Scorer.Infrastructure.Migrations.Sqlite \
  --startup-project src/Scorer/MyClub.Scorer.Infrastructure.Persistence.Design
```

### Schema Management

Generate SQL scripts and manage database schema:

```bash
# Generate SQL script for all migrations
dotnet ef migrations script \
  --project src/Scorer/MyClub.Scorer.Infrastructure.Migrations.Sqlite \
  --startup-project src/Scorer/MyClub.Scorer.Infrastructure.Persistence.Design

# Generate script for specific migration range
dotnet ef migrations script 20250802053340_InitSchema \
  --project src/Scorer/MyClub.Scorer.Infrastructure.Migrations.Sqlite \
  --startup-project src/Scorer/MyClub.Scorer.Infrastructure.Persistence.Design

# Drop database (development only)
dotnet ef database drop --force \
  --project src/Scorer/MyClub.Scorer.Infrastructure.Migrations.Sqlite \
  --startup-project src/Scorer/MyClub.Scorer.Infrastructure.Persistence.Design
```

## Connection String Configuration

### Development Configuration

**Local SQLite File for Development:**
```json
{
  "ConnectionStrings": {
    "Sqlite": "Data Source=Data\\MyClub_Dev.db"
  }
}
```

**Features:**
- Local SQLite file for development
- No server installation required
- Fast database creation and updates
- Easy to reset and recreate

### Testing Configuration

**In-Memory SQLite for Tests:**
```json
{
  "ConnectionStrings": {
    "Sqlite": "Data Source=:memory:"
  }
}
```

**Features:**
- In-memory database for testing
- Ultra-fast test execution
- Automatic cleanup after tests
- No file system dependencies

**Temporary File for Integration Tests:**
```json
{
  "ConnectionStrings": {
    "Sqlite": "Data Source=TestDb_{Guid}.db"
  }
}
```

**Features:**
- Temporary file-based database for tests
- Persistent during test execution
- Automatic cleanup with unique names
- Suitable for integration testing

## SQLite Optimizations

### Index Strategy

**Primary Indexes:**
- Indexes on TEXT primary key columns (UUIDs)
- Foreign key indexes for relationship queries
- Composite indexes for common query patterns

**Query Optimization:**
```sql
-- Optimized for match queries by date and teams
CREATE INDEX IX_Matches_Date_Teams ON Matches (OriginDate, HomeTeamId, AwayTeamId);

-- Optimized for competition team lookups
CREATE INDEX IX_Teams_Competition ON Teams (CompetitionId);

-- Optimized for match event queries
CREATE INDEX IX_MatchEvents_Match_Type ON MatchEvents (MatchId, EventType);
```

### Performance Considerations

**SQLite Tuning:**
- WAL mode for better concurrency
- Optimized cache size for memory usage
- Pragma settings for development performance

**Development Benefits:**
- Instant database creation
- Fast schema updates during development
- No network latency for database operations
- Perfect for rapid prototyping

## Migration Management

### Development Workflow

**Schema Changes:**
1. Modify domain entities or configurations
2. Generate migration using EF Core tools
3. Review generated SQL for SQLite compatibility
4. Test migration in development environment
5. Verify schema changes work correctly

**Best Practices:**
- Test migrations with sample data
- Ensure SQLite compatibility for all features
- Maintain consistency with SQL Server schema
- Use version control for migration files

### Development Benefits

**Rapid Development:**
- Instant database setup for new developers
- Fast iteration cycles during development
- No database server configuration required
- Easy to experiment with schema changes

**Testing Integration:**
- Perfect for unit testing with in-memory databases
- Fast integration test execution
- Isolated test environments
- Reproducible test data scenarios

## Integration Points

### Design Project Integration

**Shared Configuration:**
- Uses configuration from MyClub.Scorer.Infrastructure.Persistence.Design
- Inherits from MyClubDbContextFactoryBase for consistency
- Leverages environment-aware configuration loading

**Factory Pattern:**
- Implements provider-specific DbContext factory
- Configures SQLite-specific options
- Sets migration assembly for proper migration storage

### Application Integration

**Development Usage:**
- Application uses SQLite provider for development
- Connection string loaded from development configuration
- DbContext instances created with SQLite options

**Testing Integration:**
- Test projects can use SQLite for fast test execution
- In-memory databases for isolated unit tests
- File-based databases for integration testing

## Benefits

### 🚀 **Development Productivity**
- Zero configuration database setup
- Instant database creation and schema updates
- Cross-platform development support
- No server maintenance or administration

### 🧪 **Testing Excellence**
- Perfect for unit testing scenarios
- In-memory databases for test isolation
- Fast test execution and cleanup
- Reproducible test environments

### 📦 **Deployment Simplicity**
- Single file database deployment
- No server installation requirements
- Version control friendly database files
- Easy backup and restore operations

### ⚡ **Performance for Development**
- Fast database operations for development
- No network overhead for database access
- Efficient storage for moderate data volumes
- Quick schema migration execution

### ??? **Reliability & Portability**
- ACID-compliant transactions
- Cross-platform compatibility
- Self-contained database files
- No external dependencies

## Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| **Microsoft.EntityFrameworkCore.Sqlite** | 10.0.0-preview.6 | SQLite provider for EF Core |
| **Microsoft.EntityFrameworkCore.Tools** | 10.0.0-preview.6 | EF Core command-line tools |
| **Microsoft.EntityFrameworkCore.Design** | 10.0.0-preview.6 | Design-time services |

## Related Projects

- **MyClub.Scorer.Infrastructure.Persistence.Design**: Shared design-time factory and configuration
- **MyClub.Scorer.Infrastructure.Persistence**: Core persistence layer with ScorerDbContext
- **MyClub.Scorer.Infrastructure.Migrations.SqlServer**: SQL Server-specific migrations for production
- **MyClub.Scorer.Domain**: Domain entities that define the database schema

## Development Scenarios

### New Developer Setup

**Quick Start:**
1. Clone repository with SQLite migration project
2. Run `dotnet ef database update` for instant database creation
3. Start developing immediately without database setup
4. SQLite file created automatically in Data directory

### Rapid Prototyping

**Schema Experimentation:**
- Quickly test new entity relationships
- Fast iteration on domain model changes
- Easy rollback with migration commands
- No impact on shared development databases

### Offline Development

**Disconnected Development:**
- Work offline without database server access
- Full functionality without network connectivity
- Local database file travels with source code
- Perfect for remote development scenarios

## Testing Integration

### Unit Testing

**In-Memory Database:**
```csharp
var options = new DbContextOptionsBuilder<ScorerDbContext>()
    .UseSqlite("Data Source=:memory:")
    .Options;

using var context = new ScorerDbContext(options);
context.Database.OpenConnection(); // Keep in-memory database alive
context.Database.EnsureCreated();
```

### Integration Testing

**File-Based Testing:**
```csharp
var testDbPath = $"TestDb_{Guid.NewGuid()}.db";
var connectionString = $"Data Source={testDbPath}";

var options = new DbContextOptionsBuilder<ScorerDbContext>()
    .UseSqlite(connectionString)
    .Options;

// Use database for tests, automatically cleaned up
```

## Deployment Considerations

### Environment Requirements

**Development:**
- No additional software installation required
- SQLite included with .NET runtime
- Works on Windows, macOS, and Linux
- Compatible with all development environments

**File Management:**
- SQLite database files in Data directory
- Can be included in version control for seed data
- Easy backup and restore operations
- Portable across different machines

### Performance Characteristics

**Suitable For:**
- Development and testing environments
- Small to medium datasets (< 1GB)
- Single-user development scenarios
- Rapid prototyping and experimentation

**Considerations:**
- Limited concurrent write operations
- No built-in user management or security
- File locking on some network file systems
- Consider SQL Server for production deployment

---

This project provides the essential SQLite-specific migration infrastructure for the MyClub football scoring system, enabling rapid development, comprehensive testing, and cross-platform compatibility with zero-configuration database management for development and testing scenarios.