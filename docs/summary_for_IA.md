# MyClub — Suite logicielle enterprise pour clubs de sport (Football)

## 🏆 Présentation

**MyClub** est une suite logicielle modulaire **enterprise-grade** dédiée à la gestion complète des clubs de football. Chaque module est indépendant et s'appuie sur une infrastructure partagée robuste pour garantir la cohérence, la réutilisabilité, et la fiabilité en production.

Le projet suit les principes de **Domain-Driven Design (DDD)**, **Clean Architecture**, et **CQRS** avec des patterns de résilience enterprise pour assurer une haute disponibilité et des performances optimales.

---

## 🚀 Architecture Enterprise

### **Couches Architecturales Principales**

| Couche | Technologies / Patterns | Caractéristiques |
|--------|-------------------------|-------------------|
| **Domain** | DDD, Strongly-Typed IDs, Aggregates | Pure business logic, aucune dépendance externe |
| **Application** | CQRS, MediatR, FluentValidation, AutoMapper | Orchestration avec behaviors avancés (logging, caching, performance) |
| **Infrastructure** | EF Core, Circuit Breaker, Error Handling, Resilience | Patterns enterprise avec gestion d'erreurs et monitoring |
| **Presentation** | Avalonia (desktop), Blazor WebAssembly (web) | Support offline-first et cloud-based |

### **🛡️ Fonctionnalités Enterprise Nouvelles**

- **Circuit Breaker Pattern** : Détection automatique des pannes avec états (Closed/Open/HalfOpen)
- **Connection Resilience** : Monitoring de santé des connexions en temps réel
- **Retry Policies** : Exponential backoff intelligent avec détection transient failures
- **High-Performance Logging** : LoggerMessage delegates pour logging zero-allocation
- **Error Handling Centralisé** : Stratégies configurables avec monitoring détaillé
- **Performance Monitoring** : Tracking des opérations et détection des goulots d'étranglement

---

## 📦 Infrastructure Partagée

### **Projets Core Partagés (17 projets totaux)**

```
MyClub/
├── src/Shared/                          # Infrastructure partagée enterprise
│   ├── MyClub.Shared.Kernel/           # Primitives domain, abstractions
│   ├── MyClub.Shared.Domain/           # Entités partagées avec audit
│   ├── MyClub.Shared.Application/      # CQRS, MediatR behaviors
│   ├── MyClub.Shared.Infrastructure.Persistence/  # Patterns enterprise + resilience
│   ├── MyClub.Shared.Infrastructure.Events/       # Domain events avec MediatR
│   └── MyClub.Localization/            # Support international
├── src/Referential/                     # Données de référence
│   └── MyClub.Referential.Domain/      # Teams, Players, Stadiums, Managers
├── src/Scorer/                          # Module compétitions (PRODUCTION READY)
│   ├── MyClub.Scorer.Domain/           # Modèle domain riche
│   ├── MyClub.Scorer.Application/      # Handlers CQRS
│   ├── MyClub.Scorer.Infrastructure.Persistence/     # EF Core + enterprise
│   ├── MyClub.Scorer.Infrastructure.Migrations.Sqlite/    # Migrations dev
│   └── MyClub.Scorer.Infrastructure.Migrations.SqlServer/ # Migrations prod
└── tests/                               # Suite de tests complète
    ├── MyClub.Shared.Tests/
    ├── MyClub.Scorer.Domain.Tests/
    └── MyClub.Scorer.Infrastructure.Persistence.Tests/
```

### **Patterns Enterprise Implémentés**

```
┌─────────────────────────────────────────┐
│           SHARED INFRASTRUCTURE         │
│  ┌─────────────┐ ┌────────────────────┐ │
│  │ Error       │ │  Connection        │ │
│  │ Handling    │ │  Resilience        │ │
│  └─────────────┘ └────────────────────┘ │
│  ┌─────────────┐ ┌────────────────────┐ │
│  │ CQRS        │ │  Performance       │ │
│  │ Patterns    │ │  Monitoring        │ │
│  └─────────────┘ └────────────────────┘ │
│  ┌─────────────┐ ┌────────────────────┐ │
│  │ Domain      │ │  EF Core           │ │
│  │ Events      │ │  Extensions        │ │
│  └─────────────┘ └────────────────────┘ │
└─────────────────────────────────────────┘
```

---

## 🧩 Modules Fonctionnels

| Module | Description | Status | Fonctionnalités Clés |
|--------|-------------|--------|----------------------|
| **Scor'er** | Gestion de compétitions | ✅ **Production Ready** | Multi-formats, brackets complexes, resilience enterprise |
| **Team'up** | Gestion effectifs | 🚧 **Planifié** | Joueurs, staff, équipes |
| **Training** | Planification entraînements | 🚧 **Planifié** | Sessions, exercices |
| **Licensing** | Outils administratifs | 🚧 **Planifié** | Régulation, licences |

---

## 🏆 Domaine Scor'er (Module Production)

### **Competition (Agrégat racine)**

Trois types de compétitions avec gestion enterprise :

| Type | Description | Fonctionnalités Enterprise |
|------|-------------|----------------------------|
| **League** | Championnat avec journées (`Matchday`) et classement | Circuit breaker pour calculs standings |
| **Cup** | Coupe à élimination directe avec tours (`Round`) | Retry policies pour brackets complexes |
| **Tournament** | Multi-phases : groupes, ligue, élimination directe | Health monitoring pour opérations critiques |

### **Entités Principales avec Patterns Enterprise**

#### **Match (Agrégat)**
- Date, Règles, Format, État
- MatchOpponent (score, équipe)
- **Enterprise** : Error handling pour événements match, logging haute performance

#### **Teams & TeamReference**
- Support équipes réelles et virtuelles (ex: "Vainqueur du Match A")
- **Enterprise** : Converters JSON polymorphes, gestion resilience

#### **Standing & Ranking**
- Calcul incrémental ou global
- Rules head-to-head, penalty points
- **Enterprise** : Performance monitoring, circuit breaker protection

#### **Fixtures & Scheduling**
- Organisation matchs par round/stage
- **Enterprise** : Retry policies pour génération brackets

---

## 🛠 Stack Technique Enterprise

### **Core Technologies (.NET 10)**
- **Entity Framework Core 10.0** : ORM avancé avec patterns enterprise
- **MediatR 13.0** : CQRS et mediator pattern
- **FluentValidation 12.0** : Validation comprehensive
- **AutoMapper** : Mapping object-to-object optimisé

### **Enterprise Infrastructure**
- **Microsoft.Extensions.Logging** : Logging structuré haute performance
- **Microsoft.Extensions.HealthChecks** : Monitoring santé application
- **MyNet.Humanizer 5.0** : Manipulation strings et formatage

### **Resilience & Performance**
- **Circuit Breaker Pattern** : Protection contre pannes en cascade
- **LoggerMessage Delegates** : Logging zero-allocation production
- **Connection Health Monitoring** : Surveillance connectivité temps réel
- **Multi-Database Support** : SQLite (dev), SQL Server, PostgreSQL (prod)

### **Testing & Quality (xUnit, Moq, FluentAssertions)**
- **Unit Tests** : Logic domain avec mocking avancé
- **Integration Tests** : Repositories avec bases in-memory
- **Performance Tests** : Circuit breaker, retry policies, error handling
- **Architecture Tests** : Validation règles dépendances

---

## 🚀 Capacités Enterprise

### **🛡️ Fiabilité Production**
- **Circuit Breaker** : États managés pour prévention pannes cascade
- **Intelligent Retry** : Exponential backoff avec détection transient failures
- **Health Monitoring** : Surveillance connectivité base de données temps réel
- **Error Recovery** : Stratégies configurables par environnement

### **📊 Performance & Observabilité**
- **High-Performance Logging** : Zero-allocation avec LoggerMessage delegates
- **Performance Metrics** : Timing opérations et tracking taux succès
- **Health Check Integration** : Endpoints ASP.NET Core pour monitoring
- **Query Optimization** : Includes EF Core et indexes optimisés domain football

### **⚙️ Flexibilité Déploiement**
- **Multi-Database** : SQLite, SQL Server, PostgreSQL, MySQL
- **Configuration Environnement** : Settings dev/staging/production
- **Connection Pooling** : Gestion connexions optimisée
- **Migration Management** : Évolution schéma automatisée avec rollback

---

## 🔍 Spécificités Techniques pour IA

### **Patterns Architecturaux Clés**
- **Strongly Typed IDs** : `CompetitionId`, `TeamId`, `MatchId` pour sécurité types
- **Value Converters EF** : JSON polymorphe pour TeamReference, RoundFormat
- **Join Entities** : Relations many-to-many explicites (`CompetitionTeam`, `StageTeam`)
- **Domain Events** : Publishing événements avec MediatR pour découplage
- **Result Pattern** : Gestion erreurs fonctionnelle sans exceptions

### **Conventions Base de Données**
- **Snake Case** : Noms colonnes automatiques (ex: `team_id`, `match_date`)
- **Pluralization** : Noms tables automatiques (ex: `Competitions`, `Matches`)
- **FK Naming** : Conventions foreign keys cohérentes

### **Gestion Erreurs Enterprise**
- **Transient Failure Detection** : Identification automatique erreurs retry-ables
- **Exponential Backoff** : Délais retry intelligents avec jitter
- **Circuit Breaker States** : Closed → Open → HalfOpen avec timeouts configurables
- **Structured Logging** : EventId organisés pour monitoring et alerting

---

> **Notes pour IA** : 
> - Architecture enterprise mature avec 17 projets organisés
> - Module Scorer prêt production avec resilience patterns
> - Infrastructure partagée robuste pour extension futures modules
> - Patterns DDD/CQRS/Clean Architecture strictement appliqués
> - Support multi-databases avec migrations provider-specific
> - Testing comprehensive couvrant toutes couches architecturales