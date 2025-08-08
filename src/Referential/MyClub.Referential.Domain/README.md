# MyClub.Referential.Domain

> Domain layer for football reference data management with core entities and aggregates

![.NET](https://img.shields.io/badge/.NET-10-blue)
![Domain](https://img.shields.io/badge/Layer-Domain-green)
![DDD](https://img.shields.io/badge/Pattern-DDD-purple)
![Reference Data](https://img.shields.io/badge/Purpose-Reference%20Data-orange)

## Overview

**MyClub.Referential.Domain** contains the domain layer implementation for football reference data management within the MyClub ecosystem. This project provides the core domain entities and aggregates for managing fundamental football reference information such as teams, players, managers, and stadiums that are shared across different modules of the MyClub suite.

## Architecture

This project implements the Domain layer in Clean Architecture with Domain-Driven Design patterns for reference data:

```text
┌─────────────────────────────────────────┐
│            Presentation Layer           │
├─────────────────────────────────────────┤
│           Application Layer             │
├─────────────────────────────────────────┤
│         REFERENTIAL DOMAIN              │ ← This Project
│    ┌─────────┐ ┌──────────────────┐     │
│    │  Team   │ │     Stadium      │     │
│    │Aggregate│ │    Aggregate     │     │
│    └─────────┘ └──────────────────┘     │
│    ┌─────────┐ ┌──────────────────┐     │
│    │ Player  │ │     Manager      │     │
│    │Aggregate│ │    Aggregate     │     │
│    └─────────┘ └──────────────────┘     │
├─────────────────────────────────────────┤
│           Shared Domain                 │
│       (Base Classes & VOs)              │
├─────────────────────────────────────────┤
│           Shared Kernel                 │
│      (Primitives & Interfaces)          │
└─────────────────────────────────────────┘
```

## Features

### ⚽ **Core Football Reference Entities**
- **Team Aggregate**: Core team information with display properties
- **Player Aggregate**: Player reference data with personal information
- **Manager Aggregate**: Manager/coach reference data
- **Stadium Aggregate**: Venue reference information with ground details

### 🏗️ **Domain-Driven Design Implementation**
- **Aggregate Roots**: Well-defined aggregate boundaries for data consistency
- **Strongly-Typed IDs**: Type-safe entity identifiers with compile-time validation
- **Factory Methods**: Controlled entity creation with business rule validation
- **Value Objects**: Immutable data structures from shared domain

### 🔧 **Reference Data Management**
- **Shared Entities**: Reference data usable across multiple MyClub modules
- **Base Class Inheritance**: Leverages shared domain base classes for consistency
- **Minimal Business Logic**: Focus on identity and basic reference information
- **Cross-Module Compatibility**: Designed for use in Scorer, Team'up, and future modules

## Technologies

| Component | Version | Purpose |
|-----------|---------|---------|
| **JetBrains.Annotations** | 2025.2.0 | Code analysis and documentation annotations |
| **MyClub.Shared.Domain** | Project Reference | Shared domain base classes and value objects |
| **.NET** | 10.0 | Target framework for domain implementation |

## Project Structure

```text
MyClub.Referential.Domain/
├── MyClub.Referential.Domain.csproj              # Project file with domain dependencies
├── TeamAggregate/                                # Team reference aggregate
│   └── Team.cs                                   # Team aggregate root
├── PlayerAggregate/                              # Player reference aggregate
│   ├── Player.cs                                 # Player aggregate root
│   └── PlayerId.cs                               # Strongly-typed player identifier
├── ManagerAggregate/                             # Manager reference aggregate
│   ├── Manager.cs                                # Manager aggregate root
│   └── ManagerId.cs                              # Strongly-typed manager identifier
└── StadiumAggregate/                             # Stadium reference aggregate
    └── Stadium.cs                                # Stadium aggregate root
```

## Core Aggregates

### Team Aggregate

The Team aggregate represents football teams as reference entities for use across modules:

**Key Features:**
- **Aggregate Root**: `Team` entity with strongly-typed `TeamId`
- **Base Class**: Inherits from `TeamBase<TeamId>` for consistent team structure
- **Factory Creation**: Static `Create` method for controlled team instantiation
- **Display Properties**: Name and optional short name for team identification

**Implementation:**
```csharp
public class Team : TeamBase<TeamId>, IAggregateRoot
{
    private Team(TeamId id, string name, string? shortName = null)
        : base(id, name, shortName) { }

    public static Team Create(string name, string? shortName = null) 
        => new(TeamId.New(), name, shortName);
}
```

### Player Aggregate

The Player aggregate represents football players as reference entities:

**Key Features:**
- **Aggregate Root**: `Player` entity with strongly-typed `PlayerId`
- **Base Class**: Inherits from `Person<PlayerId>` for consistent person structure
- **Factory Creation**: Static `Create` method for controlled player instantiation
- **Personal Information**: First name and last name for player identification

**Implementation:**
```csharp
public class Player : Person<PlayerId>, IAggregateRoot
{
    private Player(PlayerId id, string firstName, string lastName)
        : base(id, firstName, lastName) { }

    public static Player Create(string firstName, string lastName) 
        => new(PlayerId.New(), firstName, lastName);
}
```

### Manager Aggregate

The Manager aggregate represents coaches and management staff as reference entities:

**Key Features:**
- **Aggregate Root**: `Manager` entity with strongly-typed `ManagerId`
- **Base Class**: Inherits from `Person<ManagerId>` for consistent person structure
- **Factory Creation**: Static `Create` method for controlled manager instantiation
- **Personal Information**: First name and last name for manager identification

**Implementation:**
```csharp
public class Manager : Person<ManagerId>, IAggregateRoot
{
    private Manager(ManagerId id, string firstName, string lastName)
        : base(id, firstName, lastName) { }

    public static Manager Create(string firstName, string lastName) 
        => new(ManagerId.New(), firstName, lastName);
}
```

### Stadium Aggregate

The Stadium aggregate represents football venues as reference entities:

**Key Features:**
- **Aggregate Root**: `Stadium` entity with strongly-typed `StadiumId`
- **Base Class**: Inherits from `StadiumBase<StadiumId>` for consistent stadium structure
- **Factory Creation**: Static `Create` method for controlled stadium instantiation
- **Venue Information**: Name and ground surface type for stadium identification

**Implementation:**
```csharp
public class Stadium : StadiumBase<StadiumId>, IAggregateRoot
{
    private Stadium(StadiumId id, string name, Ground ground)
        : base(id, name, ground) { }

    public static Stadium Create(string name, Ground ground) 
        => new(StadiumId.New(), name, ground);
}
```

## Strongly-Typed Identifiers

### PlayerId

**Type-Safe Player Identification:**
```csharp
public sealed record PlayerId(Guid Value) : EntityId<PlayerId>(Value);
```

### ManagerId

**Type-Safe Manager Identification:**
```csharp
public sealed record ManagerId(Guid Value) : EntityId<ManagerId>(Value);
```

### Shared Identifiers

**Other identifiers inherit from shared domain:**
- `TeamId`: Defined in MyClub.Shared.Domain.Teams
- `StadiumId`: Defined in MyClub.Shared.Domain.Stadiums

## Design Patterns

### Aggregate Pattern

**Aggregate Boundaries:**
- Each aggregate maintains its own consistency boundary
- No direct references between aggregates
- Communication through strongly-typed identifiers
- Clear ownership and lifecycle management

### Factory Pattern

**Controlled Creation:**
```csharp
// Team creation with validation
var team = Team.Create("Paris Saint-Germain", "PSG");

// Player creation with required information
var player = Player.Create("Kylian", "Mbappé");

// Manager creation with personal details
var manager = Manager.Create("Christophe", "Galtier");

// Stadium creation with ground information
var stadium = Stadium.Create("Parc des Princes", Ground.Grass);
```

### Value Object Pattern

**Immutable Data Structures:**
- Leverages value objects from MyClub.Shared.Domain
- DisplayName for consistent naming across entities
- Address for geographic information
- Country for nationality and location data

## Domain Rules

### Entity Creation Rules

**Team Creation:**
- Team name is required and cannot be empty
- Short name is optional but recommended for display
- Each team receives a unique strongly-typed identifier

**Player Creation:**
- First name and last name are both required
- Names cannot be empty or whitespace-only
- Each player receives a unique strongly-typed identifier

**Manager Creation:**
- First name and last name are both required
- Names cannot be empty or whitespace-only
- Each manager receives a unique strongly-typed identifier

**Stadium Creation:**
- Stadium name is required and cannot be empty
- Ground surface type must be specified
- Each stadium receives a unique strongly-typed identifier

### Business Invariants

**Reference Data Integrity:**
- All entities must have valid, non-empty names
- Strongly-typed IDs prevent identifier confusion
- Factory methods ensure proper entity initialization
- EF Core constructors are private to enforce controlled creation

## Integration Points

### Shared Domain Integration

**Base Class Usage:**
- `TeamBase<TId>`: Provides common team properties and behavior
- `Person<TId>`: Provides common person properties for players and managers
- `StadiumBase<TId>`: Provides common stadium properties and behavior

**Value Object Integration:**
- `DisplayName`: Consistent naming across all entities
- `Address`: Geographic location information for stadiums
- `Country`: Nationality and location enumeration

### Cross-Module Usage

**Scorer Module Integration:**
- Team references in competitions and matches
- Player references in match events and lineups
- Manager references in team management
- Stadium references in match venues

**Team'up Module Integration (Planned):**
- Player management with detailed profiles
- Team roster and squad management
- Manager assignment and responsibilities
- Stadium allocation and scheduling

### Repository Pattern

**Domain Repository Contracts:**
```csharp
// Repository interfaces would be defined for each aggregate
public interface ITeamRepository : IRepository<Team, TeamId> { }
public interface IPlayerRepository : IRepository<Player, PlayerId> { }
public interface IManagerRepository : IRepository<Manager, ManagerId> { }
public interface IStadiumRepository : IRepository<Stadium, StadiumId> { }
```

## Benefits

### ✅ **Reference Data Consistency**
- Centralized reference entities used across multiple modules
- Strongly-typed identifiers prevent data corruption
- Consistent base class implementation ensures uniform behavior
- Factory methods enforce business rules and data validation

### 🔧 **Domain-Driven Design**
- Clear aggregate boundaries with well-defined responsibilities
- Proper encapsulation with private constructors and public factories
- Type-safe operations with strongly-typed identifiers
- Clean separation of concerns between reference and transactional data

### 🗄️ **Cross-Module Compatibility**
- Reference entities designed for use across MyClub modules
- Shared domain base classes ensure consistent structure
- Minimal dependencies enable easy integration
- Stable contracts for long-term module compatibility

### ⚡ **Development Productivity**
- Simple, focused domain model for reference data
- Clear factory methods for entity creation
- Strong typing prevents common programming errors
- JetBrains annotations improve code analysis and IDE support

### 🛡️ **Type Safety & Validation**
- Strongly-typed identifiers prevent ID confusion
- Factory methods ensure proper entity initialization
- Compile-time validation of entity relationships
- Clear business rules embedded in domain logic

## Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| **JetBrains.Annotations** | 2025.2.0 | Code analysis and documentation |
| **MyClub.Shared.Domain** | Project Reference | Base classes and value objects |

## Related Projects

- **MyClub.Shared.Domain**: Base classes and shared value objects
- **MyClub.Shared.Kernel**: Core primitives and interfaces
- **MyClub.Scorer.Domain**: Uses referential entities for competition management
- **MyClub.Team'up.Domain** (Planned): Extended player and team management

## Usage Examples

### Entity Creation

**Creating Reference Entities:**
```csharp
// Create a new team
var psg = Team.Create("Paris Saint-Germain", "PSG");

// Create a new player
var mbappe = Player.Create("Kylian", "Mbappé");

// Create a new manager
var galtier = Manager.Create("Christophe", "Galtier");

// Create a new stadium
var parcDesPrinces = Stadium.Create("Parc des Princes", Ground.Grass);
```

### Cross-Module References

**Using in Scorer Module:**
```csharp
// Reference team in competition
var competition = Competition.Create("Ligue 1");
competition.AddTeam(psg.Id); // Use strongly-typed TeamId

// Reference stadium in match
var match = Match.Create(homeTeam: psg.Id, awayTeam: marseille.Id);
match.SetVenue(parcDesPrinces.Id); // Use strongly-typed StadiumId
```

### Repository Usage

**Repository Implementation:**
```csharp
public class TeamRepository : ITeamRepository
{
    public async Task<Team?> GetByIdAsync(TeamId id)
    {
        // Implementation for retrieving team by strongly-typed ID
    }
    
    public async Task<IEnumerable<Team>> GetAllAsync()
    {
        // Implementation for retrieving all teams
    }
}
```

## Future Enhancements

### Extended Reference Data

**Planned Additions:**
- Extended player information (positions, statistics)
- Team hierarchies and affiliations
- Stadium capacity and facility details
- Manager qualifications and history

### Advanced Domain Logic

**Business Rule Enhancements:**
- Player eligibility validation
- Team capacity constraints
- Stadium availability management
- Manager certification requirements

### Integration Capabilities

**Cross-Module Features:**
- Player transfer management
- Team roster synchronization
- Stadium booking coordination
- Manager assignment workflows

---

This project provides the essential reference data foundation for the MyClub ecosystem, enabling consistent entity management across all modules while maintaining clean domain boundaries and type safety through Domain-Driven Design principles.