# MyClub.Scorer.Application

> Application layer for football scoring and competition management using CQRS pattern

![.NET](https://img.shields.io/badge/.NET-10-blue)
![CQRS](https://img.shields.io/badge/Pattern-CQRS-green)
![MediatR](https://img.shields.io/badge/MediatR-13.0-orange)
![FluentValidation](https://img.shields.io/badge/FluentValidation-12.0-red)
![Clean Architecture](https://img.shields.io/badge/Architecture-Clean-brightgreen)

## 🎯 Overview

**MyClub.Scorer.Application** is the application layer for the football scoring module within the MyClub sports management suite. This project implements the **CQRS (Command Query Responsibility Segregation)** pattern using **MediatR** to orchestrate business operations, handle validation, and coordinate between the presentation layer and the rich domain model.

The application layer translates user intentions into domain operations while maintaining separation of concerns and providing cross-cutting functionality like validation, logging, and performance monitoring.

## 🏗️ Architecture

This project follows **Clean Architecture** principles with CQRS implementation:

```text
┌─────────────────────────────────────────┐
│              Presentation               │
│             (Desktop UI)                │
├─────────────────────────────────────────┤
│         🎯 APPLICATION LAYER 🎯        │ ← This Project
│  ┌─────────┐ ┌────────────────────────┐ │
│  │ Commands│ │      Handlers          │ │
│  │ (Write) │ │   (Orchestration)      │ │
│  └─────────┘ └────────────────────────┘ │
│  ┌─────────┐ ┌────────────────────────┐ │
│  │ Queries │ │     Services           │ │
│  │ (Read)  │ │   (Domain Logic)       │ │
│  └─────────┘ └────────────────────────┘ │
│  ┌─────────┐ ┌────────────────────────┐ │
│  │Validator│ │     Mappings           │ │
│  │(FluentV)│ │   (AutoMapper)         │ │
│  └─────────┘ └────────────────────────┘ │
├─────────────────────────────────────────┤
│            SCORER DOMAIN LAYER          │
│        (Rich Business Logic)            │
├─────────────────────────────────────────┤
│          INFRASTRUCTURE LAYER           │
│         (Entity Framework Core)         │
└─────────────────────────────────────────┘
```

## ⚡ Core Features

### 🏆 **Competition Management**
- **Team Operations**: Add, update, delete teams in competitions
- **Competition Configuration**: Manage rules, formats, and settings
- **Stadium Assignment**: Venue management for teams and matches
- **Validation**: Comprehensive business rule validation

### 📅 **Matchday Generation**
- **Intelligent Scheduling**: Automatic matchday generation using domain strategies
- **Team Reference Support**: Handle both concrete and virtual teams
- **Flexible Configuration**: Support various competition formats
- **Service Abstraction**: Clean interfaces for scheduling algorithms

### 🔧 **CQRS Implementation**
- **Command Pattern**: Write operations with business logic orchestration
- **Query Pattern**: Read operations optimized for specific use cases
- **Handler Pattern**: Centralized request processing with MediatR
- **Result Pattern**: Functional error handling and response management

### 🛠️ **Cross-Cutting Concerns**
- **Validation Pipeline**: FluentValidation integration for all commands
- **Mapping**: AutoMapper profiles for object transformation
- **Error Handling**: Consistent error responses and failure management
- **Performance**: Optimized operations with async/await patterns

## 🛠️ Technologies

| Dependency | Version | Purpose |
|------------|---------|---------|
| **MyClub.Shared.Application** | - | Base CQRS patterns and behaviors |
| **MyClub.Scorer.Domain** | - | Rich domain model and business logic |
| **MediatR** | 13.0+ | CQRS implementation and request mediation |
| **FluentValidation** | 12.0+ | Command and query validation |
| **AutoMapper** | Latest | Object-to-object mapping |
| **Framework** | .NET 10 | Modern C# features and performance |

## 🏗️ Application Architecture

### Command Pattern Implementation

```csharp
// Abstract command base from shared application layer
public record AddTeamCommand(
    Guid CompetitionId, 
    string Name, 
    string? ShortName = null, 
    byte[]? Logo = null, 
    Guid? StadiumId = null
) : CreateCommand;

// Command handler with full orchestration
public class AddTeamCommandHandler : IRequestHandler<AddTeamCommand, Result<Guid>>
{
    private readonly ICompetitionRepository _competitionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public async Task<Result<Guid>> Handle(AddTeamCommand command, CancellationToken cancellationToken)
    {
        // 1. Load aggregate
        var competition = _competitionRepository.GetById(competitionId);
        if (competition is null)
            return Failures.NotFound<Guid>(command.CompetitionId.ToString());

        // 2. Business validation
        if (competition.HasSimilarTeams(command.Name))
            return Failures.NameAlreadyExists<Guid>(command.Name);

        // 3. Domain operation
        var team = _mapper.Map<Team>(command);
        var result = competition.AddTeam(team);
        
        if (result.IsFailure)
            return Result.Fail<Guid>(result);

        // 4. Persistence
        _competitionRepository.Update(competition);
        await _unitOfWork.CommitAsync(cancellationToken);

        return Result.Success(team.Id.Value);
    }
}
```

### Validation System

```csharp
// FluentValidation for comprehensive validation
public sealed class AddTeamCommandValidator : AbstractValidator<AddTeamCommand>
{
    public AddTeamCommandValidator()
    {
        RuleFor(x => x.CompetitionId)
            .NotEmpty()
            .WithMessage("CompetitionId is required.");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Team name is required.")
            .MaximumLength(100)
            .WithMessage("Team name must be at most 100 characters.");

        RuleFor(x => x.ShortName)
            .MaximumLength(10)
            .WithMessage("Short name must be at most 10 characters.")
            .When(x => !string.IsNullOrEmpty(x.ShortName));

        RuleFor(x => x.StadiumId)
            .NotEmpty()
            .WithMessage("Stadium ID must be valid when specified.")
            .When(x => x.StadiumId.HasValue);
    }
}
```

### Service Interfaces

```csharp
// Domain service interfaces for complex operations
public interface IMatchdaysGeneratorService
{
    /// <summary>
    /// Generates a complete set of matchdays for the given teams using appropriate scheduling strategy.
    /// </summary>
    /// <param name="teams">The teams participating in the competition.</param>
    /// <returns>A collection of matchdays with organized fixtures.</returns>
    IReadOnlyCollection<Matchday> GenerateMatchdays(IReadOnlyCollection<TeamReference> teams);
}

// Supporting data structures
public record GeneratedMatchday(Matchday Matchday, IReadOnlyCollection<Match> Matches);

public interface IMatchdayNameProvider
{
    /// <summary>
    /// Provides appropriate names for matchdays based on competition format and index.
    /// </summary>
    /// <param name="competitionType">The type of competition (League, Cup, Tournament).</param>
    /// <param name="matchdayIndex">The zero-based index of the matchday.</param>
    /// <returns>A formatted name for the matchday.</returns>
    string GetMatchdayName(CompetitionType competitionType, int matchdayIndex);
}
```

### Mapping Profiles

```csharp
// AutoMapper profiles for object transformations
public class TeamMappingProfile : Profile
{
    public TeamMappingProfile()
    {
        // Command to domain entity mapping
        CreateMap<AddTeamCommand, Team>()
            .ConstructUsing(cmd => Team.Create(cmd.Name, cmd.ShortName))
            .ForMember(dest => dest.Logo, opt => opt.MapFrom(src => src.Logo))
            .ForMember(dest => dest.StadiumId, opt => opt.MapFrom(src => 
                src.StadiumId.HasValue ? EntityId.From<StadiumId>(src.StadiumId.Value) : null));

        // Update command to domain entity mapping
        CreateMap<UpdateTeamCommand, Team>()
            .ForMember(dest => dest.DisplayName, opt => opt.MapFrom(src => 
                new DisplayName(src.Name, src.ShortName)))
            .ForMember(dest => dest.Logo, opt => opt.MapFrom(src => src.Logo))
            .ForMember(dest => dest.StadiumId, opt => opt.MapFrom(src => 
                src.StadiumId.HasValue ? EntityId.From<StadiumId>(src.StadiumId.Value) : null));
    }
}
```

## 💡 Usage Examples

### Adding a Team to Competition

```csharp
// In presentation layer (UI or API controller)
public async Task<Result<Guid>> AddTeamAsync(Guid competitionId, string teamName, string shortName)
{
    var command = new AddTeamCommand(
        CompetitionId: competitionId,
        Name: teamName,
        ShortName: shortName,
        Logo: null,
        StadiumId: null
    );

    // MediatR handles validation, execution, and response
    var result = await _mediator.Send(command);
    
    if (result.IsSuccess)
    {
        // Team added successfully - result.Value contains the new team ID
        await ShowSuccessMessage($"Team '{teamName}' added successfully!");
        return result;
    }
    else
    {
        // Handle validation or business rule failures
        await ShowErrorMessage(result.Error);
        return result;
    }
}
```

### Generating Matchdays for League

```csharp
// Using the matchday generation service
public class LeagueSchedulingService
{
    private readonly IMatchdaysGeneratorService _matchdaysGenerator;
    private readonly ICompetitionRepository _competitionRepository;

    public async Task<Result> GenerateLeagueScheduleAsync(Guid leagueId)
    {
        // Load the league competition
        var league = _competitionRepository.GetById(EntityId.From<CompetitionId>(leagueId)) as League;
        if (league is null)
            return Failures.NotFound(leagueId.ToString());

        // Generate matchdays using domain strategy
        var teams = league.Teams;
        var matchdays = _matchdaysGenerator.GenerateMatchdays(teams);

        // Add generated matchdays to the league
        foreach (var matchday in matchdays)
        {
            var addResult = league.AddMatchday(matchday.Id);
            if (addResult.IsFailure)
                return addResult;
        }

        // Persist changes
        _competitionRepository.Update(league);
        await _unitOfWork.CommitAsync();

        return Result.Success();
    }
}
```

### Updating Team Information

```csharp
// Update team command and handling
public async Task<Result> UpdateTeamAsync(Guid teamId, string newName, string newShortName, byte[] newLogo)
{
    var command = new UpdateTeamCommand(
        TeamId: teamId,
        Name: newName,
        ShortName: newShortName,
        Logo: newLogo,
        StadiumId: null // Keep existing stadium
    );

    var result = await _mediator.Send(command);
    
    if (result.IsSuccess)
    {
        // Team updated successfully
        await RefreshTeamDisplay();
        return Result.Success();
    }
    else
    {
        // Handle update failures
        await DisplayValidationErrors(result.Error);
        return result;
    }
}
```

### Advanced Competition Management

```csharp
// Complex competition setup with multiple operations
public class CompetitionSetupService
{
    private readonly IMediator _mediator;
    private readonly IMatchdaysGeneratorService _matchdaysGenerator;

    public async Task<Result> SetupPremierLeagueAsync()
    {
        // 1. Create league competition (would use CreateLeagueCommand)
        var leagueResult = await CreateLeagueAsync("Premier League 2024-25");
        if (leagueResult.IsFailure) return leagueResult;

        var leagueId = leagueResult.Value;

        // 2. Add all 20 teams
        var premierLeagueTeams = new[]
        {
            ("Manchester City", "MCI"),
            ("Arsenal", "ARS"),
            ("Liverpool", "LIV"),
            ("Manchester United", "MUN"),
            // ... continue for all 20 teams
        };

        foreach (var (name, shortName) in premierLeagueTeams)
        {
            var addTeamCommand = new AddTeamCommand(leagueId, name, shortName);
            var teamResult = await _mediator.Send(addTeamCommand);
            
            if (teamResult.IsFailure)
                return Result.Fail($"Failed to add team {name}: {teamResult.Error}");
        }

        // 3. Generate complete season schedule (38 matchdays)
        var scheduleResult = await GenerateLeagueScheduleAsync(leagueId);
        if (scheduleResult.IsFailure) return scheduleResult;

        // 4. Configure standing labels (Champion, European qualification, Relegation)
        var labelsResult = await ConfigureStandingLabelsAsync(leagueId);
        if (labelsResult.IsFailure) return labelsResult;

        return Result.Success();
    }
}
```

## 📁 Project Structure

```
MyClub.Scorer.Application/
├── MyClub.Scorer.Application.csproj    # Project dependencies and configuration
├── Competitions/                       # Competition management operations
│   ├── Commands/                       # Write operations for competitions
│   │   ├── AddTeam/                   # Add team to competition
│   │   │   ├── AddTeamCommand.cs      # Command definition
│   │   │   ├── AddTeamCommandHandler.cs # Command handler implementation
│   │   │   └── AddTeamCommandValidator.cs # FluentValidation rules
│   │   ├── UpdateTeam/                # Update team information
│   │   │   ├── UpdateTeamCommand.cs   # Update command definition
│   │   │   └── UpdateTeamCommandHandler.cs # Update handler
│   │   └── DeleteTeam/                # Remove team from competition
│   │       ├── DeleteTeamCommand.cs   # Delete command definition
│   │       └── DeleteTeamCommandHandler.cs # Delete handler
│   │   ├── CreateCompetition/         # Create new competitions (planned)
│   │   ├── UpdateCompetition/         # Modify competition settings (planned)
│   │   └── DeleteCompetition/         # Archive competitions (planned)
│   ├── Queries/                       # Read operations for competitions (planned)
│   │   ├── GetCompetition/           # Get competition by ID
│   │   ├── GetCompetitions/          # List competitions with filtering
│   │   └── GetCompetitionTeams/      # Get teams in competition
│   ├── Mappings/                     # AutoMapper configuration
│   │   ├── TeamMappingProfile.cs     # Team mapping rules
│   │   ├── CompetitionMappingProfile.cs # Competition mapping (planned)
│   │   └── StadiumMappingProfile.cs  # Stadium mapping (planned)
│   └── Services/                     # Competition-specific services (planned)
│       ├── ICompetitionService.cs    # Business operations interface
│       └── CompetitionService.cs     # Complex business logic
├── Matchdays/                        # Matchday and scheduling operations
│   ├── Commands/                     # Write operations for matchdays (planned)
│   │   ├── CreateMatchday/          # Create individual matchdays
│   │   ├── UpdateMatchday/          # Modify matchday information
│   │   └── GenerateMatchdays/       # Generate complete schedules
│   ├── Queries/                     # Read operations for matchdays (planned)
│   │   ├── GetMatchday/            # Get matchday by ID
│   │   ├── GetMatchdays/           # List matchdays with filtering
│   │   └── GetUpcomingMatchdays/   # Get upcoming scheduled matchdays
│   └── Services/                   # Matchday-specific services
│       ├── IMatchdaysGeneratorService.cs # Schedule generation interface
│       ├── MatchdaysGeneratorService.cs  # Schedule generation implementation
│       ├── IMatchdayNameProvider.cs      # Naming strategy interface
│       └── MatchdayNameProvider.cs       # Competition-specific naming
├── Matches/                         # Match management operations (planned)
│   ├── Commands/                    # Write operations for matches
│   │   ├── CreateMatch/            # Create new matches
│   │   ├── StartMatch/             # Begin match play
│   │   ├── AddGoal/                # Record goals during matches
│   │   ├── AddCard/                # Record disciplinary actions
│   │   ├── CompleteMatch/          # Finalize match results
│   │   └── PostponeMatch/          # Handle match postponements
│   ├── Queries/                    # Read operations for matches
│   │   ├── GetMatch/               # Get match details
│   │   ├── GetMatches/             # List matches with filtering
│   │   ├── GetLiveMatches/         # Get currently playing matches
│   │   └── GetMatchEvents/         # Get match event timeline
│   └── Services/                   # Match-specific services
│       ├── IMatchService.cs        # Match business operations
│       └── ILiveMatchService.cs    # Real-time match updates
├── Rounds/                         # Tournament round management (planned)
│   ├── Commands/                   # Write operations for rounds
│   │   ├── CreateRound/           # Create tournament rounds
│   │   ├── AddFixture/            # Add team pairings
│   │   └── AdvanceTeams/          # Progress teams to next round
│   ├── Queries/                   # Read operations for rounds
│   │   ├── GetRound/              # Get round information
│   │   ├── GetFixtures/           # Get round fixtures
│   │   └── GetBracket/            # Get tournament bracket
│   └── Services/                  # Round-specific services
│       ├── IBracketService.cs     # Tournament bracket management
│       └── IVirtualTeamResolver.cs # Resolve virtual team references
├── Stages/                        # Tournament stage management (planned)
│   ├── Commands/                  # Write operations for stages
│   │   ├── CreateStage/          # Create tournament stages
│   │   ├── AddGroup/             # Create groups in group stages
│   │   └── ConfigureStage/       # Configure stage settings
│   ├── Queries/                  # Read operations for stages
│   │   ├── GetStage/             # Get stage information
│   │   ├── GetGroups/            # Get groups in stage
│   │   └── GetStandings/         # Get stage standings
│   └── Services/                 # Stage-specific services
│       ├── IStageService.cs      # Stage business operations
│       └── IGroupService.cs      # Group management within stages
└── Common/                       # Shared application components
    ├── Behaviors/               # MediatR pipeline behaviors (inherited from shared)
    ├── Extensions/              # Extension methods and utilities
    ├── Exceptions/              # Application-specific exceptions
    ├── DTOs/                    # Data transfer objects
    │   ├── CompetitionDto.cs    # Competition data structures
    │   ├── TeamDto.cs           # Team data structures
    │   ├── MatchDto.cs          # Match data structures
    │   └── StandingDto.cs       # Standing data structures
    └── DependencyInjection/         # Service registration
        └── ServiceCollectionExtensions.cs # IoC container configuration
```

## ✨ Benefits

### ✅ **CQRS Excellence**
- **Separation of Concerns**: Clear distinction between read and write operations
- **Scalability**: Optimized queries and commands for specific use cases
- **Maintainability**: Single responsibility handlers with focused logic
- **Testability**: Isolated units with clear input/output contracts

### 🛡️ **Validation & Error Handling**
- **Comprehensive Validation**: FluentValidation rules for all operations
- **Functional Error Handling**: Result pattern for robust error management
- **Business Rule Enforcement**: Domain validation in addition to input validation
- **User-Friendly Messages**: Clear error messages for presentation layer

### ⚽ **Football Domain Expertise**
- **Professional Operations**: Based on real football management requirements
- **Competition Flexibility**: Support for all major competition formats
- **Team Management**: Complete team lifecycle from creation to deletion
- **Scheduling Intelligence**: Sophisticated matchday generation algorithms

### 🚀 **Performance & Scalability**
- **Async Operations**: Non-blocking operations with cancellation support
- **Efficient Mapping**: AutoMapper for optimized object transformations
- **Memory Management**: Minimal allocations with modern C# patterns
- **Caching Ready**: Prepared for query result caching (inherited from shared)

### 💻 **Developer Experience**
- **Strong Typing**: Type-safe operations with compile-time verification
- **IntelliSense Support**: Rich IDE experience with comprehensive documentation
- **Consistent Patterns**: Standardized approach across all operations
- **Extension Points**: Clear interfaces for custom business logic

## ??? Design Principles

### Clean Architecture
- **Dependency Inversion**: Application layer defines interfaces, infrastructure implements
- **Single Responsibility**: Each handler focuses on one specific operation
- **Open/Closed**: Extensible through new commands/queries without modifying existing code

### CQRS Pattern
- **Command Responsibility**: Handle write operations with business logic
- **Query Responsibility**: Optimized read operations for specific presentation needs
- **Handler Isolation**: Each request type has dedicated handler
- **Result Pattern**: Consistent response handling across all operations

### Domain-Driven Design
- **Rich Domain Model**: Application orchestrates rich domain operations
- **Ubiquitous Language**: Football terminology throughout application layer
- **Business Logic Encapsulation**: Domain logic stays in domain, orchestration in application
- **Aggregate Consistency**: Unit of work ensures transactional consistency

## ?? Testing Strategy

### Unit Testing Application Logic

```csharp
[Fact]
public async Task AddTeamCommandHandler_WithValidCommand_ShouldAddTeamSuccessfully()
{
    // Arrange
    var competition = League.Create("Test League");
    var command = new AddTeamCommand(competition.Id.Value, "Manchester United", "MUN");
    
    _competitionRepository.Setup(x => x.GetById(It.IsAny<CompetitionId>()))
                         .Returns(competition);
    
    // Act
    var result = await _handler.Handle(command, CancellationToken.None);
    
    // Assert
    result.IsSuccess.Should().BeTrue();
    result.Value.Should().NotBeEmpty();
    _competitionRepository.Verify(x => x.Update(competition), Times.Once);
    _unitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
}

[Fact]
public async Task AddTeamCommandHandler_WithDuplicateName_ShouldReturnFailure()
{
    // Arrange
    var competition = League.Create("Test League");
    competition.AddTeam(Team.Create("Manchester United", "MUN"));
    
    var command = new AddTeamCommand(competition.Id.Value, "Manchester United", "MAN");
    
    _competitionRepository.Setup(x => x.GetById(It.IsAny<CompetitionId>()))
                         .Returns(competition);
    
    // Act
    var result = await _handler.Handle(command, CancellationToken.None);
    
    // Assert
    result.IsFailure.Should().BeTrue();
    result.Error.Should().Contain("already exists");
}
```

### Integration Testing with MediatR

```csharp
[Fact]
public async Task AddTeam_FullPipeline_ShouldValidateAndExecute()
{
    // Arrange - Setup MediatR pipeline with validation
    var services = new ServiceCollection();
    services.AddMediatR(typeof(AddTeamCommandHandler));
    services.AddValidatorsFromAssembly(typeof(AddTeamCommandValidator).Assembly);
    services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
    
    var serviceProvider = services.BuildServiceProvider();
    var mediator = serviceProvider.GetService<IMediator>();
    
    var invalidCommand = new AddTeamCommand(Guid.Empty, "", null);
    
    // Act & Assert - Validation should fail
    var exception = await Assert.ThrowsAsync<ValidationException>(() => 
        mediator.Send(invalidCommand));
    
    exception.Errors.Should().NotBeEmpty();
}
```

## ?? Dependencies

- **MyClub.Shared.Application**: Base CQRS patterns and pipeline behaviors
- **MyClub.Scorer.Domain**: Rich domain model and business logic
- **MediatR**: Request/response mediation and CQRS implementation
- **FluentValidation**: Comprehensive validation framework
- **AutoMapper**: Object-to-object mapping

## ?? Related Projects

- **MyClub.Shared.Application**: Foundation CQRS patterns used by this application
- **MyClub.Scorer.Domain**: Rich domain model orchestrated by this application layer
- **MyClub.Scorer.Infrastructure.Persistence**: Repository implementations for this application
- **MyClub.Scorer.Presentation**: UI layer consuming this application's operations

---

**MyClub.Scorer.Application** provides a professional-grade application layer for football competition management, implementing modern patterns like CQRS, comprehensive validation, and clean separation of concerns. It serves as the orchestration layer between rich domain logic and presentation requirements, ensuring robust and maintainable football management operations.