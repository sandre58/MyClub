# MyClub.Localization

> Centralized localization resources for the MyClub sports management suite

![.NET](https://img.shields.io/badge/.NET-10-blue)
![Localization](https://img.shields.io/badge/Feature-i18n-green)
![RESX](https://img.shields.io/badge/Format-RESX-orange)
![French](https://img.shields.io/badge/Primary-Français-blue)

## Overview

**MyClub.Localization** provides centralized internationalization (i18n) resources for the MyClub sports management suite. This project contains strongly-typed resource classes generated from RESX files, enabling consistent multilingual support across all modules and applications.

## Architecture

This project serves as the central localization hub for the entire MyClub ecosystem:

```text
┌─────────────────────────────────────┐
│              Presentation           │ ← Uses Localized Strings
├─────────────────────────────────────┤
│            Application Layer        │ ← Uses Localized Messages
├─────────────────────────────────────┤
│              Domain Layer           │ ← Uses Enum Localization
├─────────────────────────────────────┤
│           Infrastructure Layer      │ ← Uses Error Messages
├─────────────────────────────────────┤
│             LOCALIZATION            │ ← This Project
│        (Resources & Translations)   │
└─────────────────────────────────────┘
```

## Features

### 🌍 **Comprehensive Localization**
- **General Resources**: UI labels, messages, and common terms
- **Domain Enums**: Localized enum values for domain entities
- **Strongly-Typed Access**: Auto-generated classes for type-safe resource access
- **Multi-Language Support**: Primary French with English fallback

### 📁 **Resource Organization**
- **MyClubResources**: General application strings and messages
- **MyClubEnumsResources**: Domain-specific enumeration localizations
- **Automatic Generation**: Strongly-typed classes from RESX files
- **Designer Integration**: Visual Studio RESX editor support

## Technologies

| Component | Purpose |
|-----------|---------|
| **RESX Files** | Resource storage format with Visual Studio integration |
| **ResXFileCodeGenerator** | Automatic strongly-typed class generation |
| **ResourceManager** | Runtime resource retrieval and culture management |
| **Framework** | .NET 10 Standard Library only |

## Resource Files

### MyClubResources.resx
Primary resource file containing general application strings:

```text
Key Examples:
- General UI: "Actions", "Name", "Description", "Settings"
- Navigation: "Home", "Teams", "Competitions", "Players"  
- Operations: "Add", "Edit", "Delete", "Save", "Cancel"
- Status: "Success", "Error", "Warning", "Information"
- Business Terms: "Match", "Stadium", "Competition", "Player"
```

### MyClubEnumsResources.resx
Specialized resource file for domain enumeration values:

```text
Key Examples:
- Positions: "PositionGoalKeeper", "PositionCenterBack", "PositionForward"
- Injuries: "InjuryTypeLeftKnee", "InjurySeverityMinor", "InjuryCategoryMuscular"
- Match States: "MatchStatePlayed", "MatchStatePostponed", "MatchStateCancelled"
- Competition Types: "CompetitionTypeLeague", "CompetitionTypeCup"
- Categories: "CategoryU10", "CategoryU21", "CategoryAdult"
```

## Usage Examples

### General Resources Access

```csharp
using MyClub.Localization;

// Direct access to resources
string addButtonText = MyClubResources.AddPlayer;           // "Ajouter un joueur"
string teamLabel = MyClubResources.Team;                   // "Équipe"
string matchesTitle = MyClubResources.Matches;             // "Matchs"
string settingsMenu = MyClubResources.Settings;            // "Paramètres"

// Formatted messages
string successMessage = MyClubResources.AddPlayerSuccess;  // "Le joueur a été ajouté avec succès..."
string conflictWarning = MyClubResources.HasConflictsWarning; // "Ce match a des conflits..."

// Pluralization and parameters
string matchCount = MyClubResources.XMatches;              // "1 match"
string matchCountPlural = MyClubResources.XMatchesPlural; // "# matchs"
```

### Enum Localization

```csharp
using MyClub.Localization;
using MyClub.Shared.Domain.Enums;

// Position localization
string goalkeeperText = MyClubEnumsResources.PositionGoalKeeper;        // "Gardien"
string centerBackText = MyClubEnumsResources.PositionCenterBack;        // "Défenseur central"
string forwardText = MyClubEnumsResources.PositionForward;              // "Attaquant"

// Match status localization  
string playedText = MyClubEnumsResources.MatchStatePlayed;              // "Joué"
string postponedText = MyClubEnumsResources.MatchStatePostponed;        // "Reporté"
string cancelledText = MyClubEnumsResources.MatchStateCancelled;        // "Annulé"

// Injury type localization
string leftKneeText = MyClubEnumsResources.InjuryTypeLeftKnee;          // "Genou gauche"
string muscularText = MyClubEnumsResources.InjuryCategoryMuscular;      // "Musculaire"
string minorText = MyClubEnumsResources.InjurySeverityMinor;            // "Mineure"
```

### Dynamic Culture Support

```csharp
using System.Globalization;
using MyClub.Localization;

// Set culture for localization
CultureInfo.CurrentUICulture = new CultureInfo("fr-FR");
string frenchText = MyClubResources.Players; // "Joueurs"

CultureInfo.CurrentUICulture = new CultureInfo("en-US");  
string englishText = MyClubResources.Players; // "Players" (if en.resx exists)

// Culture-specific formatting
MyClubResources.Culture = new CultureInfo("fr-FR");
string localizedMessage = MyClubResources.MatchForXPlace; // "Match pour la {0} place"
```

### Extension Methods for Enums

```csharp
// Extension method example for enum localization
public static string GetLocalizedName(this MatchStatus status)
{
    return status switch
    {
        MatchStatus.Played => MyClubEnumsResources.MatchStatePlayed,
        MatchStatus.Postponed => MyClubEnumsResources.MatchStatePostponed,
        MatchStatus.Cancelled => MyClubEnumsResources.MatchStateCancelled,
        _ => status.ToString()
    };
}

// Usage in UI
var match = new Match { Status = MatchStatus.Played };
string statusDisplay = match.Status.GetLocalizedName(); // "Joué"
```

### Resource Access Patterns

```csharp
// Resource manager access
ResourceManager rm = MyClubResources.ResourceManager;
string localizedValue = rm.GetString("Players", CultureInfo.CurrentUICulture);

// Parameterized resources
string playerAdded = string.Format(MyClubResources.PlayerXAdded, playerName);
string matchResult = string.Format(MyClubResources.MatchResult, homeTeam, awayTeam, score);

// Conditional resource loading
string GetMatchStatusText(MatchStatus status)
{
    string resourceKey = $"MatchState{status}";
    return MyClubEnumsResources.ResourceManager.GetString(resourceKey) ?? status.ToString();
}
```

## Project Structure

```text
MyClub.Localization/
├── MyClub.Localization.csproj          # Project file with RESX build rules
├── MyClubResources.resx                # General application resources (French)
├── MyClubResources.Designer.cs         # Auto-generated strongly-typed class
├── MyClubEnumsResources.resx           # Domain enums resources (French)
├── MyClubEnumsResources.Designer.cs    # Auto-generated strongly-typed class
└── Properties/
    └── AssemblyInfo.cs                 # Assembly metadata
```

## Integration with MyClub Modules

### Scorer Module
- Competition terminology: "Championnat", "Tournoi", "Phase de groupes"
- Match states: "Programmé", "En cours", "Terminé"
- Results: "Victoire", "Défaite", "Match nul"
- Positions: All player positions with French translations

### Team'up Module _(planned)_
- Player management: "Inscription", "Transfert", "Prêt"
- Staff roles: "Entraîneur", "Préparateur physique", "Médecin"
- Contract types: "Professionnel", "Amateur", "Stagiaire"

### Shared Components
- **UI Components**: Button labels, menu items, dialog messages
- **Validation**: Error messages and validation feedback
- **Notifications**: Success, warning, and error notifications
- **Data Display**: Grid headers, filter options, status indicators

## Localization Workflow

### Adding New Resources

1. **Open RESX File**: Use Visual Studio RESX editor
2. **Add Key-Value Pairs**: Create meaningful keys with French values
3. **Build Project**: Auto-generate strongly-typed classes
4. **Use in Code**: Access via generated static properties

```csharp
// Step 1: Add to MyClubResources.resx
// Key: "NewPlayerRegistration"
// Value: "Nouvelle inscription de joueur"

// Step 2: Use in code (after build)
string title = MyClubResources.NewPlayerRegistration;
```

### Adding Enum Localizations

1. **Identify Enum Values**: Determine which enums need localization
2. **Create Resource Keys**: Use pattern "EnumTypeName + EnumValue"
3. **Add French Translations**: Provide appropriate French terms
4. **Create Extension Methods**: Helper methods for easy access

```csharp
// For enum: public enum Position { GoalKeeper, CenterBack, Forward }
// Add to MyClubEnumsResources.resx:
// "PositionGoalKeeper" → "Gardien de but"
// "PositionCenterBack" → "Défenseur central" 
// "PositionForward" → "Attaquant"
```

### Adding Language Support
1. **Create Locale RESX**: Add new `.{culture}.resx` files
2. **Translate Resources**: Provide cultural translations
3. **Test Culture**: Verify resources load correctly for new culture
4. **Document Coverage**: Update supported languages documentation

## Benefits

### ✅ **Consistency**
- Centralized terminology across all modules
- Standardized domain-specific vocabulary  
- Uniform user experience across applications

### 🔧 **Maintainability**
- Single source of truth for all text resources
- Strongly-typed access prevents runtime errors
- Visual Studio integration for easy editing

### 🌍 **Internationalization**
- Ready for multi-language support
- Culture-aware resource loading
- Extensible translation framework

### ⚡ **Performance**
- Compiled resources for fast access
- Resource caching by ResourceManager
- No runtime string parsing required

### 🛡️ **Type Safety**
- IntelliSense support for all resource keys
- Compile-time verification of resource access
- Refactoring support across solution

## Dependencies

- **Framework**: .NET 10 Standard Library only
- **Tools**: Visual Studio RESX editor and code generator
- **Integration**: Used by all other MyClub projects

## Related Projects

- **MyClub.Shared.Domain**: Uses enum localizations for domain entities
- **MyClub.Shared.Application**: Uses general resources for validation messages
- **MyClub.Scorer.***: Uses competition and match related resources
- **MyClub.*.UI**: Uses all resources for user interface elements

---

This project provides the essential localization foundation for delivering MyClub applications in multiple languages while maintaining consistent terminology and professional user experience across all modules.
