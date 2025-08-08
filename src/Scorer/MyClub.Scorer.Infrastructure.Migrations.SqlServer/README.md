# MyClub.Scorer.Infrastructure.Migrations.SqlServer

> Entity Framework Core SQL Server migrations for football scoring system database schema

![.NET](https://img.shields.io/badge/.NET-10-blue)
![EF Core](https://img.shields.io/badge/EF%20Core-10.0-green)
![SQL Server](https://img.shields.io/badge/Database-SQL%20Server-red)
![Migrations](https://img.shields.io/badge/Feature-Migrations-purple)

## Overview

**MyClub.Scorer.Infrastructure.Migrations.SqlServer** contains Entity Framework Core migrations and database schema management specifically for Microsoft SQL Server. This project maintains the complete database schema evolution history and provides the SQL Server-specific implementation of the design-time DbContext factory for football scoring system data persistence.

## Architecture

This project serves as the SQL Server-specific migration container in the multi-provider database architecture:

```text
┌─────────────────────────────────────────┐
│          EF Core Tools                  │ ← dotnet ef commands
│      (Migration Generation)             │
├─────────────────────────────────────────┤
│        Design Project                   │ ← Configuration & Base Factory
│     (MyClubDbContextFactoryBase)        │
├─────────────────────────────────────────┤
│      SQL SERVER MIGRATIONS              │ ← This Project
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

### 🗄️ **SQL Server Migration Management**
- **Migration History**: Complete database schema evolution for SQL Server
- **Provider-Specific Factory**: SQL Server implementation of design-time DbContext factory
- **Schema Generation**: Automated SQL Server DDL generation and execution
- **Version Control**: Database schema versioning and migration tracking

### ⚡ **SQL Server Optimizations**
- **Identity Columns**: SQL Server identity column configuration for primary keys
- **Index Strategies**: SQL Server-specific index optimization for football data queries
- **Data Types**: Optimal SQL Server data type mapping for domain entities
- **Constraints**: Foreign key and check constraint management

### 🏢 **Development & Production Support**
- **LocalDB Integration**: Development support with SQL Server LocalDB
- **Production Ready**: Enterprise SQL Server deployment configuration
- **Connection Pooling**: Optimized connection management for high-load scenarios
- **Migration Assembly**: Proper assembly configuration for SQL Server migrations

## Technologies

| Component | Version | Purpose |
|-----------|---------|---------|
| **Microsoft.EntityFrameworkCore.SqlServer** | 10.0.0-preview.6 | SQL Server provider for EF Core |
| **Microsoft.EntityFrameworkCore.Tools** | 10.0.0-preview.6 | EF Core command-line tools |
| **Microsoft.EntityFrameworkCore.Design** | 10.0.0-preview.6 | Design-time services for EF Core |
| **.NET** | 10.0 | Target framework for migration assembly |

## Project Structure

```text
MyClub.Scorer.Infrastructure.Migrations.SqlServer/
├── MyClub.Scorer.Infrastructure.Migrations.SqlServer.csproj  # Project file with SQL Server packages
├── MyClubDbContextFactory.cs                                # SQL Server-specific factory implementation
└── Migrations/                                              # Generated migration files directory
    ├── 20250802044619_InitSchema.cs                         # Initial schema migration
    ├── 20250802044619_InitSchema.Designer.cs               # Migration metadata
    └── MyClubDbContextModelSnapshot.cs                     # Current model snapshot
```

## Core Components

### MyClubDbContextFactory

The SQL Server-specific implementation of the design-time DbContext factory that extends the abstract base factory:

**Key Features:**
- **Provider Identification**: Specifies "SQLServer" as the provider name for configuration lookup
- **SQL Server Configuration**: Configures EF Core to use SQL Server with migration assembly
- **Assembly Management**: Ensures migrations are stored in the SQL Server-specific assembly
- **Connection String**: Uses SQL Server connection string from configuration

**Implementation Details:**
```csharp
public class MyClubDbContextFactory : MyClubDbContextFactoryBase
{
    protected override string ProviderName => "SQLServer";

    protected override void ConfigureOptions(DbContextOptionsBuilder<ScorerDbContext> optionsBuilder, string connectionString)
        => optionsBuilder.UseSqlServer(connectionString, static x => x.MigrationsAssembly(Assembly.GetExecutingAssembly()));
}
```

### Migration Files

The project contains EF Core migration files that define the database schema evolution:

**Migration Structure:**
- **Migration Classes**: Define schema changes with Up() and Down() methods
- **Designer Files**: Contain metadata and model snapshots for each migration
- **Model Snapshot**: Represents the current complete database model state

**Initial Schema Migration:**
- **Tables**: Competition, Team, Match, Stadium, Player, and related entities
- **Relationships**: Foreign keys and navigation properties for football data
- **Indexes**: Optimized indexes for common query patterns
- **Constraints**: Data integrity constraints for business rules

## Database Schema

### Core Football Entities

The SQL Server schema includes comprehensive football data structures:

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

### SQL Server Specific Features

**Data Types:**
- **UNIQUEIDENTIFIER**: For strongly-typed entity IDs
- **NVARCHAR**: Unicode string support for international data
- **DATETIME2**: High-precision date/time storage
- **TINYINT**: Efficient storage for small numeric values

**Constraints:**
- **Primary Keys**: UNIQUEIDENTIFIER columns with clustered indexes
- **Foreign Keys**: Referential integrity with cascade behaviors
- **Check Constraints**: Business rule enforcement at database level
- **Unique Constraints**: Data uniqueness enforcement

## Usage Scenarios

### Migration Generation

Generate new migrations for schema changes:

```bash
# Add new migration
dotnet ef migrations add AddPlayerStatistics \
  --project src/Scorer/MyClub.Scorer.Infrastructure.Migrations.SqlServer \
  --startup-project src/Scorer/MyClub.Scorer.Infrastructure.Persistence.Design

# List existing migrations
dotnet ef migrations list \
  --project src/Scorer/MyClub.Scorer.Infrastructure.Migrations.SqlServer \
  --startup-project src/Scorer/MyClub.Scorer.Infrastructure.Persistence.Design
```

### Database Updates

Apply migrations to SQL Server databases:

```bash
# Update to latest migration
dotnet ef database update \
  --project src/Scorer/MyClub.Scorer.Infrastructure.Migrations.SqlServer \
  --startup-project src/Scorer/MyClub.Scorer.Infrastructure.Persistence.Design

# Update to specific migration
dotnet ef database update 20250802044619_InitSchema \
  --project src/Scorer/MyClub.Scorer.Infrastructure.Migrations.SqlServer \
  --startup-project src/Scorer/MyClub.Scorer.Infrastructure.Persistence.Design
```

### Schema Management

Generate SQL scripts and manage database schema:

```bash
# Generate SQL script for all migrations
dotnet ef migrations script \
  --project src/Scorer/MyClub.Scorer.Infrastructure.Migrations.SqlServer \
  --startup-project src/Scorer/MyClub.Scorer.Infrastructure.Persistence.Design

# Generate script for specific migration range
dotnet ef migrations script 20250802044619_InitSchema \
  --project src/Scorer/MyClub.Scorer.Infrastructure.Migrations.SqlServer \
  --startup-project src/Scorer/MyClub.Scorer.Infrastructure.Persistence.Design

# Drop database (development only)
dotnet ef database drop --force \
  --project src/Scorer/MyClub.Scorer.Infrastructure.Migrations.SqlServer \
  --startup-project src/Scorer/MyClub.Scorer.Infrastructure.Persistence.Design
```

## Connection String Configuration

### Development Configuration

**LocalDB for Development:**
```json
{
  "ConnectionStrings": {
    "SQLServer": "Server=(localdb)\\MSSQLLocalDB;Database=MyClub_Dev;Trusted_Connection=True;"
  }
}
```

**Features:**
- Local SQL Server instance for development
- No separate SQL Server installation required
- Integrated Windows authentication
- Automatic database creation

### Production Configuration

**Enterprise SQL Server:**
```json
{
  "ConnectionStrings": {
    "SQLServer": "Server=myclub-sql.database.windows.net;Database=MyClub_Prod;User Id=myclub_user;Password=SecurePassword123!;Encrypt=True;TrustServerCertificate=False;"
  }
}
```

**Features:**
- Azure SQL Database or on-premises SQL Server
- SQL authentication for production deployment
- SSL encryption for secure connections
- Connection pooling for performance

## SQL Server Optimizations

### Index Strategy

**Primary Indexes:**
- Clustered indexes on UNIQUEIDENTIFIER primary keys
- Non-clustered indexes on foreign key columns
- Composite indexes for common query patterns

**Query Optimization:**
```sql
-- Optimized for match queries by date and teams
CREATE INDEX IX_Matches_Date_Teams ON Matches (OriginDate, HomeTeamId, AwayTeamId);

-- Optimized for competition team lookups
CREATE INDEX IX_Teams_Competition ON Teams (CompetitionId) INCLUDE (Name);

-- Optimized for match event queries
CREATE INDEX IX_MatchEvents_Match_Type ON MatchEvents (MatchId, EventType);
```

### Performance Considerations

**Connection Pooling:**
- Configured through connection string parameters
- Optimized pool size for concurrent users
- Connection timeout settings for reliability

**Query Performance:**
- Indexes designed for football-specific query patterns
- Optimized for reporting and analytics workloads
- Efficient joins for complex tournament queries

## Migration Management

### Development Workflow

**Schema Changes:**
1. Modify domain entities or configurations
2. Generate migration using EF Core tools
3. Review generated SQL for correctness
4. Test migration in development environment
5. Apply to staging and production databases

**Best Practices:**
- Always review generated migrations before applying
- Test migrations with production-like data volumes
- Maintain backward compatibility during deployments
- Use transaction-wrapped migrations for safety

### Production Deployment

**Deployment Strategy:**
- Generate SQL scripts for production deployment
- Review scripts with database administrators
- Plan maintenance windows for schema changes
- Monitor migration execution and rollback if needed

**Safety Measures:**
- Database backups before migration execution
- Rollback scripts for emergency recovery
- Monitoring for migration execution time
- Validation of data integrity after migration

## Integration Points

### Design Project Integration

**Shared Configuration:**
- Uses configuration from MyClub.Scorer.Infrastructure.Persistence.Design
- Inherits from MyClubDbContextFactoryBase for consistency
- Leverages environment-aware configuration loading

**Factory Pattern:**
- Implements provider-specific DbContext factory
- Configures SQL Server-specific options
- Sets migration assembly for proper migration storage

### Application Integration

**Runtime Usage:**
- Application configures SQL Server provider at startup
- Connection string loaded from application configuration
- DbContext instances created with SQL Server options

**Performance Monitoring:**
- SQL Server performance counters integration
- Query execution monitoring and logging
- Connection pool metrics and optimization

## Benefits

### 🏢 **Enterprise Database Support**
- Full Microsoft SQL Server feature support
- High-performance enterprise database capabilities
- Advanced security and compliance features
- Scalability for large football organizations

### 👨‍💻 **Professional Development**
- LocalDB integration for development productivity
- Comprehensive migration management and versioning
- SQL script generation for deployment automation
- Visual Studio and Azure DevOps integration

### 🚀 **Production Readiness**
- Enterprise-grade database engine support
- High availability and disaster recovery capabilities
- Advanced monitoring and management tools
- Azure SQL Database cloud deployment options

### ? **Performance Optimization**
- SQL Server-specific query optimizations
- Advanced indexing strategies for football data
- Connection pooling and resource management
- In-memory processing capabilities for analytics

### ??? **Security & Compliance**
- Enterprise security features and encryption
- Role-based access control and auditing
- Compliance with industry standards
- Data protection and privacy controls

## Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| **Microsoft.EntityFrameworkCore.SqlServer** | 10.0.0-preview.6 | SQL Server provider for EF Core |
| **Microsoft.EntityFrameworkCore.Tools** | 10.0.0-preview.6 | EF Core command-line tools |
| **Microsoft.EntityFrameworkCore.Design** | 10.0.0-preview.6 | Design-time services |

## Related Projects

- **MyClub.Scorer.Infrastructure.Persistence.Design**: Shared design-time factory and configuration
- **MyClub.Scorer.Infrastructure.Persistence**: Core persistence layer with ScorerDbContext
- **MyClub.Scorer.Infrastructure.Migrations.Sqlite**: SQLite-specific migrations for development
- **MyClub.Scorer.Domain**: Domain entities that define the database schema

## Deployment Considerations

### Environment Requirements

**Development:**
- SQL Server LocalDB (included with Visual Studio)
- .NET 10 SDK for tooling support
- Windows development environment recommended

**Production:**
- Microsoft SQL Server 2019+ or Azure SQL Database
- Sufficient database size and performance tier
- Network connectivity and firewall configuration
- Backup and recovery infrastructure

### Security Configuration

**Authentication:**
- Windows Authentication for development (LocalDB)
- SQL Server Authentication for production deployment
- Azure Active Directory for Azure SQL Database
- Service account configuration for application access

**Data Protection:**
- Transparent Data Encryption (TDE) for data at rest
- SSL/TLS encryption for data in transit
- Column-level encryption for sensitive data
- Audit logging for compliance requirements

---

This project provides the essential SQL Server-specific migration infrastructure for the MyClub football scoring system, enabling professional database management with enterprise-grade features, performance optimization, and production-ready deployment capabilities.