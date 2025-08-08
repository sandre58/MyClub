# MyClub.Scorer.Infrastructure.Persistence.Design

> Entity Framework Core design-time factory for database migrations and tooling support

![.NET](https://img.shields.io/badge/.NET-10-blue)
![EF Core](https://img.shields.io/badge/EF%20Core-10.0-green)
![Design Time](https://img.shields.io/badge/Purpose-Design%20Time-orange)
![Migrations](https://img.shields.io/badge/Feature-Migrations-purple)

## Overview

**MyClub.Scorer.Infrastructure.Persistence.Design** provides design-time support for Entity Framework Core tooling, specifically for database migrations and schema management. This project contains the necessary infrastructure to enable EF Core tools to create database contexts during design-time operations such as generating migrations, updating databases, and scaffolding.

## Architecture

This project serves as the design-time bridge between EF Core tooling and the persistence layer:

```text
┌─────────────────────────────────────────┐
│          EF Core Tools                  │ ← dotnet ef commands
│      (Migration Generation)             │
├─────────────────────────────────────────┤
│          DESIGN PROJECT                 │ ← This Project
│     (DbContext Factory & Config)        │
├─────────────────────────────────────────┤
│        Migrations Projects              │ ← Provider-specific migrations
│     (Sqlite / SqlServer)                │
├─────────────────────────────────────────┤
│       Persistence Layer                 │ ← Core DbContext
│        (ScorerDbContext)                │
├─────────────────────────────────────────┤
│          Domain Layer                   │ ← Domain entities
└─────────────────────────────────────────┘
```

## Features

### 🎨 **Design-Time DbContext Factory**
- **Abstract Base Factory**: `MyClubDbContextFactoryBase` for provider-agnostic design-time support
- **Configuration Loading**: Automatic configuration file discovery and loading
- **Environment Support**: Development and production configuration management
- **Provider Abstraction**: Abstract pattern for multiple database providers

### ⚙️ **Configuration Management**
- **JSON Configuration**: `appsettings.json` and `appsettings.Development.json`
- **Connection Strings**: Multi-provider database connection management
- **Environment Variables**: Support for environment-based configuration
- **Path Resolution**: Intelligent configuration file discovery

### 🗄️ **Database Provider Support**
- **SQLite**: Development and testing database support
- **SQL Server**: Production database support with LocalDB for development
- **Provider Abstraction**: Extensible pattern for additional database providers
- **Migration Assembly**: Proper assembly configuration for migrations

## Technologies

| Component | Version | Purpose |
|-----------|---------|---------|
| **Microsoft.EntityFrameworkCore.Tools** | 10.0.0-preview.6 | EF Core command-line tools |
| **Microsoft.EntityFrameworkCore.Design** | 10.0.0-preview.6 | Design-time services for EF Core |
| **Microsoft.Extensions.Configuration** | 10.0.0-preview.6 | Configuration framework |
| **Microsoft.Extensions.Configuration.Json** | 10.0.0-preview.6 | JSON configuration provider |
| **Microsoft.Extensions.Configuration.EnvironmentVariables** | 10.0.0-preview.6 | Environment variable configuration |

## Project Structure

```text
MyClub.Scorer.Infrastructure.Persistence.Design/
├── MyClub.Scorer.Infrastructure.Persistence.Design.csproj  # Project file with design-time packages
├── MyClubDbContextFactoryBase.cs                          # Abstract base factory for DbContext creation
└── config/                                                # Configuration files directory
    ├── appsettings.json                                   # Production configuration
    └── appsettings.Development.json                       # Development configuration
```

## Core Components

### MyClubDbContextFactoryBase

The abstract base class that implements `IDesignTimeDbContextFactory<ScorerDbContext>` and provides the foundation for database provider-specific factories:

**Key Features:**
- **Abstract Provider Pattern**: Extensible design for multiple database providers
- **Configuration Loading**: Automatic discovery and loading of configuration files
- **Environment Awareness**: Support for development and production configurations
- **Path Resolution**: Intelligent configuration file location strategies

**Abstract Methods:**
- `ProviderName`: Specifies the database provider name for connection string lookup
- `ConfigureOptions`: Provider-specific DbContext options configuration

### Configuration System

The configuration system provides flexible and environment-aware database connection management:

**Configuration Files:**
- `appsettings.json`: Base configuration with production connection strings
- `appsettings.Development.json`: Development-specific overrides

**Configuration Structure:**
```json
{
  "ConnectionStrings": {
    "SQLServer": "Server=(localdb)\\MSSQLLocalDB;Database=MyClub_Dev;Trusted_Connection=True;",
    "Sqlite": "Data Source=Data\\MyClub_Dev.db"
  }
}
```

## Usage Scenarios

### Migration Generation

The design project enables EF Core tools to generate migrations for different database providers:

```bash
# Generate migration for SQLite
dotnet ef migrations add InitialCreate \
  --project src/Scorer/MyClub.Scorer.Infrastructure.Migrations.Sqlite \
  --startup-project src/Scorer/MyClub.Scorer.Infrastructure.Persistence.Design

# Generate migration for SQL Server
dotnet ef migrations add InitialCreate \
  --project src/Scorer/MyClub.Scorer.Infrastructure.Migrations.SqlServer \
  --startup-project src/Scorer/MyClub.Scorer.Infrastructure.Persistence.Design
```

### Database Updates

Apply migrations to update database schemas:

```bash
# Update SQLite database
dotnet ef database update \
  --project src/Scorer/MyClub.Scorer.Infrastructure.Migrations.Sqlite \
  --startup-project src/Scorer/MyClub.Scorer.Infrastructure.Persistence.Design

# Update SQL Server database
dotnet ef database update \
  --project src/Scorer/MyClub.Scorer.Infrastructure.Migrations.SqlServer \
  --startup-project src/Scorer/MyClub.Scorer.Infrastructure.Persistence.Design
```

### Schema Inspection

Inspect and script database schemas:

```bash
# Generate SQL script for SQLite
dotnet ef migrations script \
  --project src/Scorer/MyClub.Scorer.Infrastructure.Migrations.Sqlite \
  --startup-project src/Scorer/MyClub.Scorer.Infrastructure.Persistence.Design
```

## Configuration Management

### Environment-Based Configuration

The design project supports environment-specific configuration through:

**Environment Detection:**
- `DOTNET_ENVIRONMENT` environment variable
- Defaults to "Development" if not specified
- Loads `appsettings.{Environment}.json` if available

**Configuration Hierarchy:**
1. Base `appsettings.json`
2. Environment-specific `appsettings.{Environment}.json`
3. Environment variables (highest priority)

### Connection String Management

**Development Configuration:**
```json
{
  "ConnectionStrings": {
    "SQLServer": "Server=(localdb)\\MSSQLLocalDB;Database=MyClub_Dev;Trusted_Connection=True;",
    "Sqlite": "Data Source=Data\\MyClub_Dev.db"
  }
}
```

**Production Configuration:**
```json
{
  "ConnectionStrings": {
    "SQLServer": "Server=(localdb)\\MSSQLLocalDB;Database=MyClub_Prod;Trusted_Connection=True;",
    "Sqlite": "Data Source=Data\\MyClub_Prod.db"
  }
}
```

### Configuration File Discovery

The factory implements intelligent configuration file discovery:

**Discovery Strategy:**
1. **Current Directory**: Look for `config/` subdirectory
2. **Design Project Path**: Navigate to Design project from Migrations project
3. **Fallback**: Use current directory with `config/` subdirectory

**Path Resolution Logic:**
```csharp
private static string FindConfigDirectory()
{
    var currentDirectory = Directory.GetCurrentDirectory();
    
    // Strategy 1: Current directory config
    var configPath = Path.Combine(currentDirectory, "config");
    if (Directory.Exists(configPath))
        return configPath;
    
    // Strategy 2: Design project relative path
    var designProjectPath = Path.Combine(currentDirectory, "..", 
        "MyClub.Scorer.Infrastructure.Persistence.Design", "config");
    return Directory.Exists(designProjectPath) ? 
        Path.GetFullPath(designProjectPath) :
        Path.Combine(currentDirectory, "config");
}
```

## Provider Implementation Pattern

### Abstract Base Implementation

The `MyClubDbContextFactoryBase` provides a template for provider-specific implementations:

```csharp
public abstract class MyClubDbContextFactoryBase : IDesignTimeDbContextFactory<ScorerDbContext>
{
    protected abstract string ProviderName { get; }
    
    public ScorerDbContext CreateDbContext(string[] args)
    {
        var configuration = LoadConfiguration();
        var connectionString = configuration.GetConnectionString(ProviderName) ?? 
            throw new InvalidOperationException($"Missing {ProviderName} connection string.");
        
        var optionsBuilder = new DbContextOptionsBuilder<ScorerDbContext>();
        ConfigureOptions(optionsBuilder, connectionString);
        
        return new(optionsBuilder.Options);
    }
    
    protected abstract void ConfigureOptions(
        DbContextOptionsBuilder<ScorerDbContext> optionsBuilder, 
        string connectionString);
}
```

### Provider-Specific Implementations

**SQLite Implementation:**
```csharp
public class MyClubDbContextFactory : MyClubDbContextFactoryBase
{
    protected override string ProviderName => "Sqlite";
    
    protected override void ConfigureOptions(
        DbContextOptionsBuilder<ScorerDbContext> optionsBuilder, 
        string connectionString)
        => optionsBuilder.UseSqlite(connectionString, 
            x => x.MigrationsAssembly(Assembly.GetExecutingAssembly()));
}
```

**SQL Server Implementation:**
```csharp
public class MyClubDbContextFactory : MyClubDbContextFactoryBase
{
    protected override string ProviderName => "SQLServer";
    
    protected override void ConfigureOptions(
        DbContextOptionsBuilder<ScorerDbContext> optionsBuilder, 
        string connectionString)
        => optionsBuilder.UseSqlServer(connectionString, 
            x => x.MigrationsAssembly(Assembly.GetExecutingAssembly()));
}
```

## Benefits

### 🎯 **Design-Time Support**
- Enables EF Core tooling to function without a running application
- Provides necessary DbContext instances for migration generation
- Supports multiple database providers through abstraction

### ⚙️ **Configuration Management**
- Centralized configuration for all database providers
- Environment-aware configuration loading
- Flexible configuration file discovery

### 🗄️ **Multi-Provider Support**
- Abstract pattern supports multiple database providers
- Consistent configuration across all providers
- Easy addition of new database providers

### ? **Development Workflow**
- Streamlined migration generation process
- Consistent tooling experience across providers
- Automated configuration loading

### ??? **Error Handling**
- Comprehensive error handling for missing configurations
- Clear error messages for configuration issues
- Fallback strategies for configuration discovery

## Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| **Microsoft.EntityFrameworkCore.Tools** | 10.0.0-preview.6 | EF Core command-line tools |
| **Microsoft.EntityFrameworkCore.Design** | 10.0.0-preview.6 | Design-time services |
| **Microsoft.Extensions.Configuration** | 10.0.0-preview.6 | Configuration framework |
| **Microsoft.Extensions.Configuration.Json** | 10.0.0-preview.6 | JSON configuration support |
| **Microsoft.Extensions.Configuration.EnvironmentVariables** | 10.0.0-preview.6 | Environment variable support |

## Related Projects

- **MyClub.Scorer.Infrastructure.Persistence**: Core persistence layer with ScorerDbContext
- **MyClub.Scorer.Infrastructure.Migrations.Sqlite**: SQLite-specific migrations
- **MyClub.Scorer.Infrastructure.Migrations.SqlServer**: SQL Server-specific migrations
- **MyClub.Scorer.Infrastructure.Persistence.Tests**: Tests requiring design-time factory

## Integration Points

### Migration Projects

The design project integrates with provider-specific migration projects:

**SQLite Migration Project:**
- Uses `MyClubDbContextFactoryBase` for design-time support
- Configures SQLite-specific options and migration assembly
- Stores migrations in provider-specific assembly

**SQL Server Migration Project:**
- Uses `MyClubDbContextFactoryBase` for design-time support
- Configures SQL Server-specific options and migration assembly
- Stores migrations in provider-specific assembly

### Testing Integration

The persistence tests project references the design project for:
- Test database context creation
- Configuration loading during tests
- Design-time factory validation

---

This project provides the essential design-time infrastructure for Entity Framework Core tooling, enabling seamless database migration management and schema development workflows across multiple database providers while maintaining configuration flexibility and environment awareness.