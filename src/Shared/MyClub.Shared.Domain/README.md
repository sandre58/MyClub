# MyClub.Shared.Domain

> Shared domain layer for the MyClub sports management suite

![.NET](https://img.shields.io/badge/.NET-10-blue)
![DDD](https://img.shields.io/badge/Pattern-DDD-green)
![Clean Architecture](https://img.shields.io/badge/Architecture-Clean-brightgreen)
![Value Objects](https://img.shields.io/badge/Design-ValueObjects-orange)

## Overview

**MyClub.Shared.Domain** contains the shared domain layer components for the MyClub sports management suite. This project implements Domain-Driven Design (DDD) principles and provides reusable domain entities, value objects, enumerations, and domain services that are used across all sports management modules.

## Architecture

This project represents the domain layer in Clean Architecture, containing the core business logic shared across all MyClub modules:

```
┌─────────────────────────────────────────┐
│              Presentation               │
├─────────────────────────────────────────┤
│            Application Layer            │
├─────────────────────────────────────────┤
│         🏆 DOMAIN LAYER 🏆             │ ← This Project
│         (Shared Domain Concepts)        │   
├─────────────────────────────────────────┤
│           Infrastructure Layer          │
├─────────────────────────────────────────┤
│             Shared Kernel               │
└─────────────────────────────────────────┘
```
## Core Features

### 🏃‍♂️ **Domain Entities**
- **Person Hierarchy**: Abstract base classes for players, coaches, staff
- **Team Abstractions**: Polymorphic team references for flexible competition structures
- **Match Contracts**: Interfaces for match entities and lifecycle management
- **Stadium Entities**: Stadium management with ground information

### 📊 **Value Objects**
- **Contact Information**: Email, phone, and contact value objects with validation
- **Display Names**: Rich display name objects with short name support
- **Reference Objects**: Generic reference value objects for external systems

### 📋 **Domain Enumerations**
- **Match Management**: Status, outcomes, result types, goal types
- **Card System**: Colors, reasons for card issuance
- **Competition Elements**: Ground types, penalty outcomes, standing columns

### 📈 **Standings System**
- **Advanced Standing Calculation**: Rule-based ranking and statistics
- **Configurable Comparers**: Flexible comparison logic for different sports
- **Real-time Updates**: Incremental and full standings recalculation

## Technologies

| Dependency | Purpose |
|------------|---------|
| **MyClub.Shared.Kernel** | Core domain primitives and abstractions |
| **Framework** | .NET 10 Standard Library only |

## Domain Components

### Persons

```csharp
// Abstract base for all person entities
public abstract class Person<TId> : AuditableEntity<TId>, IPerson
    where TId : EntityId<TId>
{
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public Country? Country { get; set; }
    public GenderType Gender { get; set; }
    public string? LicenseNumber { get; set; }
    public string? Email { get; set; }
    
    public void Rename(string firstName, string lastName);
    public int CompareTo(IPerson? other);
    public bool IsSimilar(IPerson? obj);
}

// Person interface contract
public interface IPerson : IComparable<IPerson>
{
    string FirstName { get; }
    string LastName { get; }
    Country? Country { get; }
}
```
### Teams

```csharp
// Strongly-typed team identifier
public sealed record TeamId(Guid Value) : EntityId<TeamId>(Value)
{
    public static implicit operator ConcreteTeamReference(TeamId id);
    public static implicit operator TeamId(ConcreteTeamReference reference);
}

// Polymorphic team reference system
public abstract record TeamReference;

public sealed record ConcreteTeamReference(TeamId Id) : TeamReference
{
    public static implicit operator ConcreteTeamReference(TeamId id);
    public static implicit operator TeamId(ConcreteTeamReference value);
}
```

```csharp
// Team contract
public interface ITeam
{
    TeamId Id { get; }
    string Name { get; }
    bool IsSimilar(ITeam? obj);
}
```
### Value Objects

```csharp
// Display name with short name support
public sealed class DisplayName : ValueObject, ISimilar<DisplayName>, IComparable<DisplayName>
{
    public string Name { get; }
    public string ShortName { get; }
    
    public DisplayName(string name, string? shortName = null);
    
    // Implicit conversions
    public static implicit operator string(DisplayName displayName);
    public static implicit operator DisplayName(string name);
}

// Contact information base
public record Contact(string Value, string? Label = null, bool IsDefault = false);

// Email with validation
public record Email(string Value, string? Label = null, bool IsDefault = false) 
    : Contact(Value.IsRequiredOrThrow().IsEmailAddressOrThrow(), Label, IsDefault);

// Phone number
public record Phone(string Value, string? Label = null, bool IsDefault = false) 
    : Contact(Value.IsRequiredOrThrow(), Label, IsDefault);
### Matches

```csharp
// Match contract
public interface IMatch
{
    MatchId Id { get; }
    DateTime Date { get; }
    MatchStatus Status { get; }
    bool HasResult();
    IEnumerable<TeamReference> GetTeams();
    bool Participate(TeamReference team);
}

// Match status enumeration
public enum MatchStatus
{
    None,
    InProgress,
    Suspended,
    Played,
    Postponed,
    Cancelled
}
```
```

```csharp
// Match outcome types
public enum MatchOutcome
{
    None,
    Won,
    Drawn,
    Lost
}
```
### Standings System

```csharp
// Complete standings table
public class Standing : IReadOnlyCollection<StandingRow>
{
    public StandingRuleSet Rules { get; }
    public IEnumerable<StandingRow> SortedRows { get; }
    public int Count { get; }
    
    public int GetRank(TeamReference team);
    public StandingRow GetRow(TeamReference team);
    public T? GetColumn<T>(TeamReference team, string column);
    
    public void ApplyMatch(IMatch match);
    public void ApplyMatches(IEnumerable<IMatch> matches);
    public void ComputeAll(IEnumerable<IMatch> matches);
}

// Individual standing row
public class StandingRow : IStandingRow
{
    public TeamReference Team { get; }
    public int Points { get; }
    public int PenaltyPoints { get; }
    public int TotalPoints { get; }
    
    public int Get(StandingColumnType column);
    public T? Get<T>(string column);
    
    public void ApplyMatch(IMatch match, StandingRuleSet rules);
    public void Compute(IEnumerable<IMatch> matches, StandingRuleSet rules);
}
```
### Domain Enumerations

```csharp
// Match-related enums
public enum MatchResultType { Win, Draw, Loss }
public enum GoalType { Regular, Penalty, OwnGoal, FreeKick }
public enum Ground { Home, Away, Neutral }

// Card system
public enum CardColor { Yellow, Red }
public enum CardReason { Foul, Unsporting, Dissent, Delaying, /* ... */ }
```

```

```csharp
// Standing system
public enum StandingColumnType 
{ 
    Played, Won, Drawn, Lost, GoalsFor, GoalsAgainst, 
    GoalsDifference, Points, PenaltyPoints 
}
```
## Usage Examples

### Creating Person Entities

```csharp
// Define a specific person type
public class Player : Person<PlayerId>
{
    private Player() { } // EF Core constructor
    
    private Player(PlayerId id, string firstName, string lastName, Position position) 
        : base(id, firstName, lastName)
    {
        Position = position;
    }
    
    public Position Position { get; private set; }
    
    public static Player Create(string firstName, string lastName, Position position) 
        => new(PlayerId.New(), firstName, lastName, position);
    
    public void ChangePosition(Position newPosition)
    {
        if (Position != newPosition)
        {
            Position = newPosition;
            AddDomainEvent(new PlayerPositionChangedEvent(Id, Position, newPosition));
        }
    }
}

// Usage
var player = Player.Create("Kylian", "Mbappé", Position.Forward);
player.Country = Country.France;
player.Email = "kmbappe@psg.fr";
```
### Working with Team References

```csharp
// Concrete team usage
var teamId = TeamId.New();
ConcreteTeamReference concreteTeam = teamId; // Implicit conversion
TeamId extractedId = concreteTeam; // Implicit conversion back

// Polymorphic collections
var participants = new List<TeamReference>
{
    new ConcreteTeamReference(psgId),
    new ConcreteTeamReference(marseilleId),
    // Could also include virtual teams like "Winner of Semi-Final A"
};
```

```csharp
// Type-safe operations
foreach (var team in participants)
{
    if (team is ConcreteTeamReference concrete)
    {
        var teamId = concrete.Id;
        // Process concrete team
    }
}
```
### Using Value Objects

```csharp
// Display names with automatic short names
var teamName = new DisplayName("Paris Saint-Germain");
Console.WriteLine(teamName.Name);      // "Paris Saint-Germain"
Console.WriteLine(teamName.ShortName); // "PSG" (auto-generated from initials)

// Custom short name
var stadiumName = new DisplayName("Parc des Princes", "PdP");

// Similarity comparison
var otherName = new DisplayName("paris saint-germain"); // Case insensitive
bool similar = teamName.IsSimilar(otherName); // true

// Contact information with validation
var emails = new List<Email>
{
    new Email("coach@psg.fr", "Professional", isDefault: true),
    new Email("personal@gmail.com", "Personal")
};

var phones = new List<Phone>
{
    new Phone("+33 1 47 43 71 71", "Office"),
    new Phone("+33 6 12 34 56 78", "Mobile", isDefault: true)
};
```
### Standings Management

```csharp
// Create standings for a league
var teams = new[] { psgId, marseilleId, lyonId, lensId }.Select(id => (TeamReference)new ConcreteTeamReference(id));
```

```csharp
var rules = StandingRuleSet.Default; // 3 points for win, 1 for draw
var standing = new Standing(teams, rules);

// Apply match results
var matches = GetCompletedMatches();
standing.ComputeAll(matches); // Full recalculation

// Get current rankings
foreach (var row in standing.SortedRows.Take(3)) // Top 3 teams
{
    var rank = standing.GetRank(row.Team);
    var points = row.TotalPoints;
    var goalDiff = standing.GetColumn(row.Team, StandingColumnType.GoalsDifference);
    
    Console.WriteLine($"{rank}. {GetTeamName(row.Team)} - {points} pts (GD: {goalDiff:+0;-0;0})");
}

// Real-time updates
var newMatch = GetLatestMatch();
if (newMatch.HasResult())
{
    standing.ApplyMatch(newMatch); // Incremental update
}
```
### Match Status Management

```csharp
public class Match : AuditableEntity<MatchId>, IMatch
{
    public MatchStatus Status { get; private set; } = MatchStatus.None;
    public TeamReference HomeTeam { get; private set; }
    public TeamReference AwayTeam { get; private set; }
    
    public void Start()
    {
        if (Status == MatchStatus.None)
        {
            Status = MatchStatus.InProgress;
            AddDomainEvent(new MatchStartedEvent(Id, HomeTeam, AwayTeam));
        }
    }
    
    public void Complete(int homeScore, int awayScore)
    {
        if (Status == MatchStatus.InProgress)
        {
            Status = MatchStatus.Played;
            SetScore(homeScore, awayScore);
            AddDomainEvent(new MatchCompletedEvent(Id, homeScore, awayScore));
        }
    }
    
    public void Postpone(DateTime newDate)
    {
        if (Status is MatchStatus.None or MatchStatus.InProgress)
        {
            Status = MatchStatus.Postponed;
            AddDomainEvent(new MatchPostponedEvent(Id, Date, newDate));
        }
    }
    
    public bool HasResult() => Status == MatchStatus.Played;
    
    public IEnumerable<TeamReference> GetTeams() => [HomeTeam, AwayTeam];
    
    public bool Participate(TeamReference team) => 
        HomeTeam.Equals(team) || AwayTeam.Equals(team);
}
```
## Project Structure

```
MyClub.Shared.Domain/
├── MyClub.Shared.Domain.csproj        # Minimal project dependencies
├── Persons/                           # Person domain entities
│   ├── Person.cs                      # Abstract person base class
│   └── IPerson.cs                     # Person interface contract
├── Teams/                             # Team domain abstractions
│   ├── TeamId.cs                      # Strongly-typed team identifier
│   ├── TeamReference.cs               # Polymorphic team reference system
│   ├── TeamBase.cs                    # Base team implementation
│   └── ITeam.cs                       # Team interface contract
├── Matches/                           # Match domain contracts
│   ├── MatchId.cs                     # Strongly-typed match identifier
│   └── IMatch.cs                      # Match interface contract
├── Stadiums/                          # Stadium domain abstractions
│   ├── StadiumId.cs                   # Strongly-typed stadium identifier
│   ├── StadiumBase.cs                 # Base stadium implementation
│   └── IStadium.cs                    # Stadium interface contract
├── ValueObjects/                      # Domain value objects
│   ├── DisplayName.cs                 # Name with short name support
│   ├── Contact.cs                     # Base contact information
│   ├── Email.cs                       # Email with validation
│   ├── Phone.cs                       # Phone number value object
│   └── Reference.cs                   # Generic reference object
├── Enums/                            # Domain enumerations
│   ├── MatchStatus.cs                # Match lifecycle status
│   ├── MatchOutcome.cs               # Match result outcomes
│   ├── MatchResultType.cs            # Win/Draw/Loss results
│   ├── GoalType.cs                   # Types of goals scored
│   ├── CardColor.cs                  # Card colors (Yellow/Red)
│   ├── CardReason.cs                 # Reasons for cards
│   ├── Ground.cs                     # Home/Away/Neutral
│   ├── PenaltyShootoutOutcome.cs     # Penalty shootout results
│   └── StandingColumnType.cs         # Standing table column types
├── Standings/                        # Standing calculation system
│   ├── Standing.cs                   # Complete standings table
│   ├── StandingRow.cs               # Individual standing row
│   ├── IStandingRow.cs              # Standing row interface
│   ├── Rules/                       # Standing rule system
│   │   ├── StandingRuleSet.cs       # Complete rule configuration
│   │   ├── StandingRuleSetBuilder.cs # Builder for rule sets
│   │   ├── StandingColumn.cs        # Column definition
│   │   └── IStandingColumn.cs       # Column interface
│   ├── Comparers/                   # Standing comparison logic
│   │   ├── StandingComparer.cs      # Main comparer implementation
│   │   ├── StandingComparerBuilder.cs # Builder for comparers
│   │   ├── IStandingComparer.cs     # Comparer interface
│   │   └── IStandingContextualComparer.cs # Context-aware comparer
│   └── Services/                    # Standing domain services
│       ├── IStandingCalculator.cs   # Standing calculation interface
│       └── StandingCalculator.cs    # Standing calculation service
└── Extensions/                      # Domain utility extensions
    └── PersonExtensions.cs          # Person name formatting utilities
```
## Integration with MyClub Modules

### Scorer Module

```csharp
// Uses shared domain for competitions
public class Competition : AuditableEntity<CompetitionId>
{
    public DisplayName Name { get; private set; }
    public List<ConcreteTeamReference> Teams { get; private set; }
    public List<StadiumId> Stadiums { get; private set; }
}

// Matches inherit from shared contract
public class Match : AuditableEntity<MatchId>, IMatch
{
    public MatchStatus Status { get; private set; }
    public TeamReference HomeTeam { get; private set; }
    public TeamReference AwayTeam { get; private set; }
}
```
### Team'up Module _(planned)_

```csharp
// Players extend shared person base
public class Player : Person<PlayerId>
{
    public Position Position { get; private set; }
    public TeamId? CurrentTeam { get; private set; }
}

// Coaches also extend person base
public class Coach : Person<CoachId>
{
    public CoachingLicense License { get; private set; }
    public List<TeamId> Teams { get; private set; }
}
```
## Benefits

### ✅ **Domain Purity**
- Zero infrastructure dependencies
- Framework-agnostic domain logic
- Clear separation of business concerns

### ✅ **Type Safety**
- Strongly-typed identifiers prevent ID confusion
- Value objects with built-in validation
- Compile-time verification of domain contracts

### ✅ **Flexibility**
- Polymorphic team references support complex competitions
- Configurable standing calculation rules
- Extensible enumeration system

### ✅ **Consistency**
- Shared domain concepts across all modules
- Uniform person and team abstractions
- Standardized value objects and contracts

### ✅ **Performance**
- Incremental standings updates
- Efficient comparison implementations
- Optimized for Entity Framework integration

## Design Principles

### Domain-Driven Design
- **Ubiquitous Language**: Domain concepts reflect business terminology
- **Value Objects**: Immutable objects with value-based equality
- **Domain Services**: Complex business operations
- **Aggregate Boundaries**: Clear consistency boundaries

### Clean Architecture
- **Dependency Inversion**: Domain defines contracts, infrastructure implements
- **Framework Independence**: No coupling to specific technologies
- **Testability**: Pure domain logic without external dependencies

### Sports Domain Modeling
- **Competition Flexibility**: Support for various tournament formats
- **Team Polymorphism**: Handle both concrete and virtual teams
- **Match Lifecycle**: Complete match status management
- **Standing Systems**: Configurable ranking and comparison logic

## Validation Rules

### Business Invariants
- **Person names** must be non-empty strings
- **Email addresses** must pass format validation
- **Team references** ensure type safety
- **Match status transitions** follow valid state machine rules

### Domain Constraints

```csharp
// Person validation
public void Rename(string firstName, string lastName)
{
    FirstName = firstName.IsRequiredOrThrow();  // Domain validation
    LastName = lastName.IsRequiredOrThrow();
}

// Email validation
public record Email(string Value, string? Label = null, bool IsDefault = false) 
    : Contact(Value.IsRequiredOrThrow().IsEmailAddressOrThrow(), Label, IsDefault);

// Standing rules validation
public StandingRuleSet(IReadOnlyDictionary<MatchResultType, int> pointsByMatchResult)
{
    PointsByMatchResult = pointsByMatchResult.IsRequiredOrThrow();
    // Additional rule validations...
}
```
## Testing Strategies

### Unit Testing Domain Logic

```csharp
[Fact]
public void Person_Rename_Should_Update_Names()
{
    // Arrange
    var player = TestPlayer.Create("John", "Doe");
```
    
    // Act
    player.Rename("Jane", "Smith");
    
    // Assert
    player.FirstName.Should().Be("Jane");
    player.LastName.Should().Be("Smith");
}

[Fact]
public void Standing_ApplyMatch_Should_Update_Points()
{
    // Arrange
    var teams = new[] { teamA, teamB };
    var standing = new Standing(teams);
    var match = CreateMatch(teamA, teamB, homeScore: 2, awayScore: 1);
    
    // Act
    standing.ApplyMatch(match);
    
    // Assert
    standing.GetRow(teamA).Points.Should().Be(3); // Win = 3 points
    standing.GetRow(teamB).Points.Should().Be(0); // Loss = 0 points
}
```
### Domain Event Testing

```csharp
[Fact]
public void Match_Complete_Should_Raise_Domain_Event()
{
    // Arrange
    var match = Match.Create(DateTime.Now, teamA, teamB);
    match.Start();
    
    // Act
    match.Complete(homeScore: 1, awayScore: 0);
    
    // Assert
    var domainEvent = match.DomainEvents.OfType<MatchCompletedEvent>().Single();
    domainEvent.MatchId.Should().Be(match.Id);
    domainEvent.HomeScore.Should().Be(1);
    domainEvent.AwayScore.Should().Be(0);
}
```
## Dependencies

- **MyClub.Shared.Kernel**: Core domain primitives and abstractions
- **Framework**: .NET 10 Standard Library only

## Related Projects

- **MyClub.Shared.Kernel**: Foundation primitives used by this domain
- **MyClub.Scorer.Domain**: Scorer-specific domain logic (uses this shared domain)
- **MyClub.TeamUp.Domain**: Team management domain logic (planned)
- **MyClub.*.Infrastructure**: Infrastructure implementations of domain contracts

---

This project provides the essential shared domain foundation for implementing consistent, type-safe, and maintainable sports management functionality across the entire MyClub suite using proven Domain-Driven Design patterns and practices.
