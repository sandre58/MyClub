# MyClub.Scorer.Domain

> Domain layer for football scoring and competition management in the MyClub sports management suite

![.NET](https://img.shields.io/badge/.NET-10-blue)
![DDD](https://img.shields.io/badge/Pattern-DDD-green)
![Clean Architecture](https://img.shields.io/badge/Architecture-Clean-brightgreen)
![CQRS](https://img.shields.io/badge/Pattern-CQRS-orange)
![Football](https://img.shields.io/badge/Domain-Football-red)

## 🎯 Overview

**MyClub.Scorer.Domain** is the core domain layer for football competition management within the MyClub sports management suite. This project implements advanced Domain-Driven Design (DDD) principles to model complex football tournament structures, match management, and scoring systems used by professional leagues worldwide.

The domain provides comprehensive support for all major football competition formats including leagues, cups, tournaments, and complex multi-stage competitions like the FIFA World Cup and UEFA Champions League.

## 🏗️ Architecture

This project represents the domain layer in Clean Architecture with CQRS support, containing rich business logic for football competition management:

```text
┌─────────────────────────────────────────┐
│              Presentation               │
├─────────────────────────────────────────┤
│            Application Layer            │
│            (CQRS Handlers)              │
├─────────────────────────────────────────┤
│        🎯 SCORER DOMAIN 🎯             │ ← This Project
│      (Football Competition Logic)       │   
├─────────────────────────────────────────┤
│           Infrastructure Layer          │
│        (Entity Framework Core)          │
├─────────────────────────────────────────┤
│           Shared Domain & Kernel        │
└─────────────────────────────────────────┘
```

## ⚡ Core Features

### 🏆 **Competition Management**
- **Multi-Format Support**: League, Cup, Tournament with complex stage structures
- **Team Organization**: Polymorphic team references for flexible tournament brackets
- **Stadium Management**: Venue assignment and capacity management
- **Configuration System**: Rich configuration for rules, formats, and labels

### ⚽ **Match Management**
- **Complete Match Lifecycle**: From scheduling to final result with live updates
- **Event Tracking**: Goals, cards, substitutions, penalty shootouts
- **Match Formats**: Regular time, extra time, penalty shootouts
- **Real-time Updates**: Live scoring and event management

### 🏟️ **Tournament Structures**
- **Stage-based Competitions**: Group stages, knockout phases, championship rounds
- **Round Management**: Single elimination, home-and-away, best-of series, replays
- **Virtual Team References**: "Winner of Match A", "3rd place Group B" placeholders
- **Automatic Progression**: Teams advance automatically based on results

### 📅 **Scheduling & Organization**
- **Matchday Management**: Round-robin scheduling with intelligent algorithms
- **Fixture Organization**: Team pairings and venue assignments
- **Standing Labels**: Visual categorization of league positions (Champion, Relegation, etc.)
- **Penalty Management**: Disciplinary points affecting final standings

## 🛠️ Technologies

| Dependency | Purpose |
|------------|---------|
| **MyClub.Shared.Domain** | Shared domain primitives and abstractions |
| **MyClub.Shared.Kernel** | Core DDD building blocks and utilities |
| **Framework** | .NET 10 with record types and pattern matching |

## 🏗️ Domain Architecture

### Aggregate Roots

```csharp
// Competition aggregate - supports League, Cup, Tournament
public abstract class Competition : AuditableEntity<CompetitionId>, IAggregateRoot
{
    public DisplayName Name { get; protected set; }
    public CompetitionType Type { get; }
    public MatchFormat MatchFormat { get; set; }
    public MatchRules Rules { get; set; }
    
    // Polymorphic behavior for different competition types
    public abstract void ScheduleMatches();
    public abstract Standing GetStanding();
}

// Match aggregate - complete match with events and lifecycle
public sealed class Match : AuditableEntity<MatchId>, IMatch, IAggregateRoot
{
    public MatchOpponent Home { get; private set; }
    public MatchOpponent Away { get; private set; }
    public MatchStatus Status { get; private set; }
    public MatchFormat Format { get; private set; }
    
    // Rich behavior for match management
    public void Start();
    public void AddGoal(TeamReference team, PlayerId? scorer = null);
    public void AddCard(TeamReference team, PlayerId player, CardColor color);
    public void Complete();
}

// Round aggregate - manages knockout tournament rounds
public class Round : AuditableEntity<RoundId>, IAggregateRoot
{
    public DisplayName Name { get; }
    public RoundFormat Format { get; private set; }
    public IReadOnlyCollection<Fixture> Fixtures { get; }
    public IReadOnlyCollection<RoundStage> Stages { get; }
    
    // Complex round management
    public Result<Fixture> AddFixture(TeamReference team1, TeamReference team2);
    public void UpdateMatchFormat(MatchFormat matchFormat);
}

// Stage aggregate - manages tournament phases
public abstract class Stage : AuditableEntity<StageId>, IAggregateRoot
{
    public DisplayName Name { get; }
    public StageType Type { get; }
    public MatchFormat MatchFormat { get; set; }
    public MatchRules Rules { get; set; }
    
    // Specialized by GroupStage, KnockoutStage, ChampionshipStage
}
```

### Competition Types

```csharp
// League - traditional round-robin competition
public sealed class League : Competition, IChampionship
{
    public StandingRuleSet StandingRules { get; set; }
    public StandingLabels Labels { get; set; }
    public IReadOnlyCollection<MatchdayId> Matchdays { get; }
    
    // League-specific behavior
    public Standing GetStanding();
    public void AddPenalty(TeamId teamId, int points);
}

// Cup - knockout elimination tournament
public sealed class Cup : Competition, IKnockout
{
    public IReadOnlyCollection<RoundId> Rounds { get; }
    
    // Cup-specific behavior
    public Result<RoundId> AddRound(RoundId roundId);
    public void Clear();
}

// Tournament - complex multi-stage competition
public sealed class Tournament : Competition
{
    public IReadOnlyCollection<StageId> Stages { get; }
    
    // Tournament-specific behavior
    public Result<StageId> AddStage(StageId stageId);
    public void Clear();
}
```

### Advanced Match Events

```csharp
// Polymorphic match events
public abstract class MatchEvent : Entity<MatchEventId>
{
    public int? Minute { get; protected set; }
    public string? Description { get; set; }
}

public class Goal : MatchEvent
{
    public GoalType Type { get; private set; }
    public PlayerId? ScorerId { get; private set; }
    public PlayerId? AssistId { get; private set; }
    
    public static Goal Create(GoalType type, int? minute = null, PlayerId? scorer = null);
}

public class Card : MatchEvent
{
    public PlayerId PlayerId { get; private set; }
    public CardColor Color { get; private set; }
    public CardReason Reason { get; private set; }
    
    public static Card Create(PlayerId playerId, CardColor color, CardReason reason);
}

public class PenaltyShootout : Entity<PenaltyShootoutId>
{
    public PlayerId? TakerId { get; set; }
    public PenaltyShootoutOutcome Result { get; set; }
    
    public static PenaltyShootout Create(PlayerId? takerId = null, PenaltyShootoutOutcome result = PenaltyShootoutOutcome.None);
}
```

### Round Format System

```csharp
// Polymorphic round formats
public abstract record RoundFormat(PeriodFormat RegulationTime, PeriodFormat? ExtraTime = null, int? NumberOfPenaltyShootouts = null)
{
    public abstract RoundFormatType Type { get; }
    public abstract bool AllowDraw();
}

// Single elimination (World Cup knockout)
public record SingleFormat(PeriodFormat RegulationTime, PeriodFormat? ExtraTime = null, int? NumberOfPenaltyShootouts = null) 
    : RoundFormat(RegulationTime, ExtraTime, NumberOfPenaltyShootouts)
{
    public override RoundFormatType Type => RoundFormatType.Single;
    public override bool AllowDraw() => false;
}

// Home and away (Champions League knockout)
public record HomeAndAwayFormat(PeriodFormat RegulationTime, PeriodFormat? ExtraTime = null, bool UseAwayGoals = false, int? NumberOfPenaltyShootouts = null) 
    : RoundFormat(RegulationTime, ExtraTime, NumberOfPenaltyShootouts)
{
    public override RoundFormatType Type => RoundFormatType.HomeAndAway;
    public override bool AllowDraw() => true;
}

// Best-of series (American playoffs style)
public record BestOfFormat(int MaxGames, bool[] InvertTeamsByStage, PeriodFormat RegulationTime, PeriodFormat? ExtraTime = null, int? NumberOfPenaltyShootouts = null) 
    : RoundFormat(RegulationTime, ExtraTime, NumberOfPenaltyShootouts)
{
    public override RoundFormatType Type => RoundFormatType.BestOf;
    public override bool AllowDraw() => false;
}
```

### Virtual Team System

```csharp
// Polymorphic team references for tournament brackets
public abstract record TeamReference;

public sealed record ConcreteTeamReference(TeamId Id) : TeamReference;

// Virtual team references for tournament progression
public record VirtualTeamReference : TeamReference;

// "Winner of Quarter-Final 1"
public record FixtureResultReference(RoundId RoundId, FixtureId FixtureId, VirtualTeamType Type) : VirtualTeamReference;

// "3rd place Group A" 
public record GroupRankReference(StageId StageId, GroupId GroupId, int Rank) : VirtualTeamReference;

// "Champion of League"
public record ChampionshipRankReference(StageId StageId, int Rank) : VirtualTeamReference;
```

### Scheduling Strategy System

```csharp
// Strategy pattern for match scheduling
public interface IMatchdayScheduleStrategy
{
    IEnumerable<MatchdayDefinition> GenerateSchedule(IEnumerable<TeamReference> teams);
    int GetMatchdayCount(int teamCount);
    int GetMaxMatchesPerMatchday(int teamCount);
}

// Round-robin scheduling (Premier League style)
public class RoundRobinStrategy : IMatchdayScheduleStrategy
{
    public RoundRobinStrategy(int matchesPerPair, bool[] invertTeamsByStage);
    public static RoundRobinStrategy Default => new(2); // Double round-robin
    
    public IEnumerable<MatchdayDefinition> GenerateSchedule(IEnumerable<TeamReference> teams);
    public int GetMatchdayCount(int teamCount) => (teamCount % 2 == 0 ? teamCount - 1 : teamCount) * MatchesPerPair;
}
```

## 💡 Usage Examples

### Creating a Premier League Season

```csharp
// Create league competition
var premierLeague = League.Create("Premier League 2024-25");

// Configure standing labels (Champion, European qualification, Relegation)
var labels = new StandingLabels()
    .Add(1, "gold", "Champion", "CHAMP", "Premier League Champion")
    .Add(new Interval<int>(2, 4), "green", "Champions League", "UCL", "Champions League Qualification")
    .Add(new Interval<int>(5, 6), "blue", "Europa League", "UEL", "Europa League Qualification")
    .Add(7, "lightblue", "Conference League", "UECL", "UEFA Conference League")
    .Add(new Interval<int>(18, 20), "red", "Relegation", "REL", "Relegated to Championship");

premierLeague.Labels = labels;

// Add teams
var teams = new[] { "Manchester City", "Arsenal", "Liverpool", "Manchester United" /* ... */ }
    .Select(name => Team.Create(name, name.Substring(0, 3).ToUpper()))
    .ToList();

foreach (var team in teams)
{
    premierLeague.AddTeam(team.Id.ToReference());
}

// Generate round-robin schedule (38 matchdays)
var strategy = RoundRobinStrategy.Default; // Double round-robin
var schedule = strategy.GenerateSchedule(premierLeague.Teams);

foreach (var matchdayDef in schedule)
{
    var matchday = Matchday.Create(DateTime.Now.AddWeeks(matchdayIndex), $"Matchday {matchdayIndex + 1}");
    
    foreach (var fixtureDef in matchdayDef.Fixtures)
    {
        var match = Match.Create(matchday.Date, fixtureDef.HomeTeam, fixtureDef.AwayTeam);
        matchday.AddMatch(match.Id);
    }
    
    premierLeague.AddMatchday(matchday.Id);
}
```

### Managing a Champions League Tournament

```csharp
// Create tournament with multiple stages
var championsLeague = Tournament.Create("UEFA Champions League 2024-25");

// Group stage (8 groups of 4 teams)
var groupStage = GroupStage.Create(null, "Group Stage");
for (char groupLetter = 'A'; groupLetter <= 'H'; groupLetter++)
{
    var groupTeams = GetTeamsForGroup(groupLetter); // 4 teams per group
    groupStage.AddGroup(groupTeams, $"Group {groupLetter}", groupLetter.ToString());
}
championsLeague.AddStage(groupStage.Id);

// Knockout stage with various round formats
var knockoutStage = KnockoutStage.Create(groupStage.Id, "Knockout Stage");

// Round of 16 (home and away)
var round16 = Round.Create(knockoutStage.Id, new HomeAndAwayFormat(PeriodFormat.Default, PeriodFormat.ExtraTime), "Round of 16", "R16");
// Add fixtures like "Group A Winner vs Group B Runner-up"
for (int i = 0; i < 8; i++)
{
    var groupWinner = new GroupRankReference(groupStage.Id, groups[i].Id, 1);
    var groupRunnerUp = new GroupRankReference(groupStage.Id, groups[i + 8].Id, 2);
    round16.AddFixture(groupWinner, groupRunnerUp);
}
knockoutStage.AddRound(round16.Id);

// Quarter-finals, Semi-finals, Final (single matches)
var quarterFinals = Round.Create(knockoutStage.Id, new SingleFormat(PeriodFormat.Default, PeriodFormat.ExtraTime), "Quarter-Finals", "QF");
for (int i = 0; i < 4; i++)
{
    var winner1 = new FixtureResultReference(round16.Id, round16Fixtures[i].Id, VirtualTeamType.Winner);
    var winner2 = new FixtureResultReference(round16.Id, round16Fixtures[i + 4].Id, VirtualTeamType.Winner);
    quarterFinals.AddFixture(winner1, winner2);
}
```

### Live Match Management

```csharp
// Create and manage a live match
var match = Match.Create(DateTime.Now, homeTeam.ToReference(), awayTeam.ToReference());

// Start the match
match.Start(); // Status: InProgress

// Add events during the match
match.Home.AddGoal(15); // Goal at 15th minute
match.Away.AddCard(Card.Create(playerId, CardColor.Yellow, CardReason.Foul));
match.Home.AddGoal(Goal.Create(GoalType.Penalty, 67, penaltyTaker.Id));

// Handle penalty shootout
if (match.RequiresPenaltyShootout())
{
    // Home team penalties
    match.Home.AddPenaltyShootout(PenaltyShootoutOutcome.Succeeded, player1.Id);
    match.Home.AddPenaltyShootout(PenaltyShootoutOutcome.Failed, player2.Id);
    
    // Away team penalties
    match.Away.AddPenaltyShootout(PenaltyShootoutOutcome.Succeeded, player3.Id);
    match.Away.AddPenaltyShootout(PenaltyShootoutOutcome.Succeeded, player4.Id);
}

// Complete the match
match.Complete(); // Status: Played

// Get final result
var homeScore = match.Home.Score; // Regular time goals
var awayScore = match.Away.Score;
var homeShootoutScore = match.Home.GetShootoutScore(); // Penalty shootout
var awayShootoutScore = match.Away.GetShootoutScore();
```

### Advanced Tournament Bracket Management

```csharp
// World Cup style tournament
var worldCup = Tournament.Create("FIFA World Cup 2026");

// Group stage (8 groups of 4 teams)
var groupStage = GroupStage.Create(null, "Group Stage");
// ... configure groups ...

// Round of 16 with automatic team progression
var round16 = Round.Create(groupStage.Id, new SingleFormat(PeriodFormat.Default, PeriodFormat.ExtraTime), "Round of 16");

// Automatic bracket generation using virtual references
var fixtures = new[]
{
    (new GroupRankReference(groupStage.Id, groupA.Id, 1), new GroupRankReference(groupStage.Id, groupB.Id, 2)), // A1 vs B2
    (new GroupRankReference(groupStage.Id, groupC.Id, 1), new GroupRankReference(groupStage.Id, groupD.Id, 2)), // C1 vs D2
    // ... continue for all 16 fixtures
};

foreach (var (team1, team2) in fixtures)
{
    round16.AddFixture(team1, team2);
}

// Quarter-finals automatically reference Round of 16 winners
var quarterFinals = Round.Create(round16.Id, new SingleFormat(PeriodFormat.Default, PeriodFormat.ExtraTime), "Quarter-Finals");
for (int i = 0; i < 4; i++)
{
    var winner1 = new FixtureResultReference(round16.Id, round16Fixtures[i * 2].Id, VirtualTeamType.Winner);
    var winner2 = new FixtureResultReference(round16.Id, round16Fixtures[i * 2 + 1].Id, VirtualTeamType.Winner);
    quarterFinals.AddFixture(winner1, winner2);
}

// 3rd place playoff using semi-final losers
var thirdPlacePlayoff = Round.Create(semiFinals.Id, new SingleFormat(PeriodFormat.Default, PeriodFormat.ExtraTime), "3rd Place Playoff");
var semifinalLoser1 = new FixtureResultReference(semiFinals.Id, semifinal1.Id, VirtualTeamType.Loser);
var semifinalLoser2 = new FixtureResultReference(semiFinals.Id, semifinal2.Id, VirtualTeamType.Loser);
thirdPlacePlayoff.AddFixture(semifinalLoser1, semifinalLoser2);
```

## 📁 Project Structure

```
MyClub.Scorer.Domain/
??? MyClub.Scorer.Domain.csproj           # Minimal domain dependencies
??? CompetitionAggregate/                 # Competition management
?   ??? Competition.cs                    # Abstract competition base
?   ??? League.cs                        # League competition (Premier League style)
?   ??? Cup.cs                           # Cup competition (FA Cup style)
?   ??? Tournament.cs                    # Tournament competition (World Cup style)
?   ??? CompetitionType.cs               # Competition type enumeration
?   ??? Teams/                           # Team management
?   ?   ??? Team.cs                      # Team entity with players and staff
?   ?   ??? Player.cs                    # Player entity
?   ?   ??? Manager.cs                   # Manager/coach entity
?   ?   ??? VirtualTeamReference.cs      # Virtual team system for brackets
?   ?   ??? VirtualTeamType.cs           # Winner/Loser enumeration
?   ??? Stadiums/                        # Stadium management
?   ?   ??? Stadium.cs                   # Stadium entity
?   ??? Configurations/                  # Competition configuration
?   ?   ??? MatchFormat.cs               # Match timing and rules
?   ?   ??? MatchRules.cs                # Match regulations
?   ?   ??? PeriodFormat.cs              # Period duration configuration
?   ?   ??? StandingLabels.cs            # League position categorization
?   ?   ??? StandingLabel.cs             # Individual standing label
?   ??? Repositories/                    # Repository contracts
?       ??? ICompetitionRepository.cs    # Competition persistence interface
??? MatchAggregate/                      # Match management
?   ??? Match.cs                         # Match aggregate root
?   ??? MatchOpponent.cs                 # Team in match context
?   ??? MatchEvents/                     # Match event system
?   ?   ??? MatchEvent.cs                # Abstract match event
?   ?   ??? Goal.cs                      # Goal event
?   ?   ??? Card.cs                      # Card event
?   ?   ??? PenaltyShootout.cs           # Penalty shootout event
?   ??? Repositories/                    # Repository contracts
?       ??? IMatchRepository.cs          # Match persistence interface
??? RoundAggregate/                      # Tournament round management
?   ??? Round.cs                         # Round aggregate root
?   ??? RoundStage.cs                    # Round stage (legs, games)
?   ??? Fixture.cs                       # Team pairing
?   ??? Format/                          # Round format system
?   ?   ??? RoundFormat.cs               # Abstract round format
?   ?   ??? SingleFormat.cs              # Single elimination
?   ?   ??? HomeAndAwayFormat.cs         # Home and away legs
?   ?   ??? BestOfFormat.cs              # Best-of series
?   ?   ??? ReplayFormat.cs              # Replay if drawn
?   ?   ??? RoundFormatType.cs           # Format enumeration
?   ??? Repositories/                    # Repository contracts
?       ??? IRoundRepository.cs          # Round persistence interface
??? StageAggregate/                      # Tournament stage management
?   ??? Stage.cs                         # Abstract stage base
?   ??? GroupStage.cs                    # Group stage (World Cup groups)
?   ??? KnockoutStage.cs                 # Knockout stage
?   ??? ChampionshipStage.cs             # Championship stage (league format)
?   ??? Group.cs                         # Individual group in group stage
?   ??? StageType.cs                     # Stage type enumeration
?   ??? Repositories/                    # Repository contracts
?       ??? IStageRepository.cs          # Stage persistence interface
??? MatchdayAggregate/                   # Matchday scheduling
?   ??? Matchday.cs                      # Matchday aggregate root
?   ??? Services/                        # Scheduling strategies
?   ?   ??? IMatchdayScheduleStrategy.cs # Strategy interface
?   ?   ??? RoundRobinStrategy.cs        # Round-robin scheduling
?   ??? Repositories/                    # Repository contracts
?       ??? IMatchdayRepository.cs       # Matchday persistence interface
??? Primitives/                          # Domain primitives and interfaces
    ??? MatchScope.cs                    # Base for match containers
    ??? ITeamsContainer.cs               # Team container interface
    ??? IChampionship.cs                 # Championship interface
    ??? IKnockout.cs                     # Knockout interface
```

## ✨ Benefits

### ⚽ **Professional Football Modeling**
- Complete support for all major competition formats (League, Cup, Tournament)
- Authentic match lifecycle with live event tracking
- Virtual team references for complex tournament brackets
- Industry-standard scheduling algorithms

### 🎯 **Domain-Driven Design Excellence**
- Rich aggregate roots with strong invariants
- Polymorphic competition and round format systems
- Value objects for complex configurations
- Domain events for integration and auditing

### 🔧 **Competition Flexibility**
- From local amateur leagues to FIFA World Cup complexity
- Configurable rules, formats, and standing calculations
- Support for any tournament bracket structure
- Real-time match and tournament management

### ⚡ **Type Safety & Performance**
- Strongly-typed identifiers prevent ID confusion
- Immutable value objects and records for thread safety
- Efficient algorithms for scheduling and bracket management
- Optimized for Entity Framework Core integration

### ✅ **Real-World Proven**
- Based on actual FIFA, UEFA, and national league formats
- Supports all major tournament structures used globally
- Professional-grade match event tracking
- Scalable from youth leagues to World Cup level

## ??? Design Principles

### Domain-Driven Design
- **Ubiquitous Language**: Football terminology throughout
- **Rich Aggregates**: Complex business logic encapsulated
- **Value Objects**: Immutable configuration and reference objects
- **Domain Events**: Decoupled integration and side effects

### Clean Architecture
- **Dependency Inversion**: Domain defines contracts, infrastructure implements
- **Framework Independence**: Pure business logic without infrastructure coupling
- **Testability**: Rich domain models easily unit tested

### Football Domain Expertise
- **Competition Formats**: All major international tournament structures
- **Match Management**: Complete lifecycle from scheduling to final result
- **Team Management**: Players, managers, and staff organization
- **Scheduling**: Professional-grade algorithms for fixture generation

## ?? Testing Strategy

### Unit Testing Rich Domain Logic

```csharp
[Fact]
public void League_AddTeam_Should_Add_Team_Successfully()
{
    // Arrange
    var league = League.Create("Test League");
    var team = Team.Create("Manchester United", "MUN");
    
    // Act
    var result = league.AddTeam(team.Id.ToReference());
    
    // Assert
    result.IsSuccess.Should().BeTrue();
    league.Teams.Should().Contain(team.Id.ToReference());
}

[Fact]
public void Match_AddGoal_Should_Update_Score()
{
    // Arrange
    var match = Match.Create(DateTime.Now, homeTeam.ToReference(), awayTeam.ToReference());
    
    // Act
    match.Home.AddGoal(15); // Goal at 15th minute
    
    // Assert
    match.Home.Score.Should().Be(1);
    match.Home.Goals.Should().HaveCount(1);
    match.Home.Goals.First().Minute.Should().Be(15);
}

[Fact]
public void RoundRobinStrategy_GenerateSchedule_Should_Create_Correct_Matchdays()
{
    // Arrange
    var teams = Enumerable.Range(1, 4)
        .Select(i => TeamId.New().ToReference())
        .ToList();
    var strategy = new RoundRobinStrategy(2); // Double round-robin
    
    // Act
    var schedule = strategy.GenerateSchedule(teams).ToList();
    
    // Assert
    schedule.Should().HaveCount(6); // (4-1) * 2 = 6 matchdays
    schedule.SelectMany(md => md.Fixtures).Should().HaveCount(12); // 4*3 = 12 total matches
}
```

### Integration Testing with Complex Scenarios

```csharp
[Fact]
public void WorldCup_Tournament_Should_Handle_Complete_Bracket_Progression()
{
    // Arrange - Create World Cup with 32 teams
    var worldCup = CreateWorldCupTournament();
    
    // Act - Simulate group stage
    SimulateGroupStageMatches(worldCup);
    var groupWinners = GetGroupWinners(worldCup);
    
    // Assert - Verify 16 teams qualified for knockout
    groupWinners.Should().HaveCount(16);
    
    // Act - Simulate knockout rounds
    SimulateKnockoutRounds(worldCup);
    
    // Assert - Verify final winner
    var champion = GetTournamentWinner(worldCup);
    champion.Should().NotBeNull();
}
```

## ?? Dependencies

- **MyClub.Shared.Domain**: Shared domain primitives and abstractions
- **MyClub.Shared.Kernel**: Core DDD building blocks
- **Framework**: .NET 10 with advanced language features

## ?? Related Projects

- **MyClub.Shared.Domain**: Foundation domain primitives used by this project
- **MyClub.Shared.Kernel**: Core DDD patterns and utilities
- **MyClub.Scorer.Application**: CQRS handlers using this domain
- **MyClub.Scorer.Infrastructure.Persistence**: Entity Framework implementation

---

**MyClub.Scorer.Domain** provides the complete foundation for building professional-grade football competition management systems, from local amateur leagues to FIFA World Cup level tournaments. The rich domain model, proven DDD patterns, and authentic football expertise make it suitable for any serious sports management application.