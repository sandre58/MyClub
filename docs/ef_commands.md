# Entity Framework Core - Commands Guide

## Basic Commands

### Tools Installation/Update
```bash
# Install EF Core CLI globally
dotnet tool install --global dotnet-ef

# Update EF Core CLI
dotnet tool update --global dotnet-ef

# Restore local tools (from dotnet-tools.json)
dotnet tool restore
```

## Migration Management

### Create a migration
```bash
# Sqlite
dotnet ef migrations add InitSchema --project src/Scorer/MyClub.Scorer.Infrastructure.Migrations.Sqlite

# SQL Server
dotnet ef migrations add InitSchema --project src/Scorer/MyClub.Scorer.Infrastructure.Migrations.SqlServer
```

### Apply migrations
```bash
# Apply all pending migrations
dotnet ef database update

# Apply up to a specific migration
dotnet ef database update AddUserTable

# Revert to a previous migration
dotnet ef database update InitialCreate

# Revert to initial state (remove all migrations)
dotnet ef database update 0
```

### Manage migrations
```bash
# List all migrations
dotnet ef migrations list

# Remove the last migration (not applied)
dotnet ef migrations remove

# Remove a specific migration (not applied)
dotnet ef migrations remove --force

# Generate SQL script for migrations
dotnet ef migrations script

# SQL script for specific migration
dotnet ef migrations script InitialCreate AddUserTable

# SQL script for all migrations
dotnet ef migrations script --idempotent
```

## Database Management

### Create/Drop database
```bash
# Create the database
dotnet ef database ensure-created

# Drop the database
dotnet ef database drop

# Drop without confirmation
dotnet ef database drop --force
```

### Database information
```bash
# Display connection information
dotnet ef dbcontext info

# List all DbContexts
dotnet ef dbcontext list

# Generate model from existing database (Scaffold)
dotnet ef dbcontext scaffold "Server=localhost;Database=MyDb;Trusted_Connection=true;" Microsoft.EntityFrameworkCore.SqlServer
```

### Deployment scripts
```bash
# Generate idempotent script for production
dotnet ef migrations script --idempotent --output migrations.sql

# Script for specific migration
dotnet ef migrations script 20231201000000_InitialCreate 20231215000000_AddUserTable --output update.sql
```

## Best Practices

### Recommended workflow
```bash
# 1. Modify the model
# 2. Create the migration
dotnet ef migrations add AddNewFeature

# 3. Review the generated migration
# 4. Test locally
dotnet ef database update

# 5. Generate script for production
dotnet ef migrations script --idempotent --output deployment.sql
```

### Multiple environments
```bash
# Development (SQLite)
dotnet ef database update --context DevDbContext

# Test (SQL Server)
dotnet ef database update --context TestDbContext --configuration Test

# Production (PostgreSQL)
dotnet ef migrations script --context ProdDbContext --idempotent
```

## Troubleshooting

### Common issues
```bash
# Complete reset
dotnet ef database drop --force
dotnet ef migrations remove (repeat until complete removal)
dotnet ef migrations add InitialCreate
dotnet ef database update

# Force recreation
dotnet ef database drop
dotnet ef database ensure-created
dotnet ef database update

# Check configuration
dotnet ef dbcontext info
dotnet ef dbcontext list
```