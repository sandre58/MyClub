<div id="top"></div>

<!-- Logo -->
<p align="center">
  <img src="assets/myclub-logo.png" alt="MyClub logo" width="200"/>
</p>

<!-- Title -->
<h1 align="center">MyClub</h1>
<p align="center"><em>Enterprise Modular Software Suite for Football Clubs</em></p>

[![Build][build-shield]][build-url]
[![Downloads][downloads-shield]][downloads-url]
[![Forks][forks-shield]][forks-url]
[![Stargazers][stars-shield]][stars-url]
[![Issues][issues-shield]][issues-url]
[![MIT License][license-shield]][license-url]

**MyClub** is an enterprise-grade modular software suite designed to fully digitalize the management of football clubs. From handling player rosters and scheduling training sessions to tracking team results and league standings, these applications provide comprehensive tools to efficiently run and organize a sports club. Whether you're a coach, manager, or club administrator, these applications help streamline operations, improve communication, and enhance overall club performance.

---

## 🔧 Technical Architecture

![Framework1][framework1-shield]
![Framework2][framework2-shield]
![Framework3][framework3-shield]
![Language][language-shield]
![Enterprise][enterprise-shield]
![Performance][performance-shield]

MyClub is built with a robust, scalable, and enterprise-ready modular architecture designed to support both **offline-first usage** and **cloud-based integration**, while following modern software engineering principles like **Domain-Driven Design (DDD)**, **Clean Architecture**, **CQRS**, and **Enterprise Resilience Patterns**.

### 🧱 Architectural Layers

The solution is structured around several clear layers to ensure separation of concerns and enterprise-grade reliability:

- **Domain Layer** (per module):  
  Contains the pure domain model—entities, value objects, aggregates, business rules, and domain events.  
  This layer has no dependencies on any external library or infrastructure. It reflects the *business logic* of each module (e.g., `Scorer`, `TeamUp`) and ensures domain integrity with strongly-typed IDs and rich domain models.

- **Application Layer**:  
  Contains use cases, commands, queries, handlers, validators, and interfaces for repositories or services.  
  This layer orchestrates domain logic and handles the flow of data between the UI and domain.  
  It follows the **CQRS** pattern to separate reads and writes, with comprehensive **MediatR** pipeline behaviors for logging, validation, caching, and performance monitoring.

- **Infrastructure Layer**:  
  Implements the interfaces defined in the application layer with enterprise-grade features.  
  It handles data persistence (with EF Core), file storage, networking, and external services.  
  **NEW**: Includes advanced **error handling**, **circuit breaker patterns**, **connection resilience**, and **high-performance logging** with LoggerMessage delegates.
  This layer is plug-and-play and can easily be swapped for another provider (e.g., switch from SQLite to PostgreSQL).

- **Presentation Layer**:  
  - **Desktop Client**: Built with **Avalonia UI**, it offers a rich, cross-platform experience with full offline capabilities.  
    The embedded backend allows local data persistence and interaction without requiring a connection.
  - **Web Client**: Developed using **Blazor WebAssembly**, it provides a lightweight interface primarily for visualization and consultation of competitions, standings, and results.

---

### 🛡️ Enterprise Reliability & Performance

**NEW Enterprise Features:**

- **Circuit Breaker Pattern**: Automatic failure detection and recovery with state management (Closed/Open/HalfOpen)
- **Connection Resilience**: Real-time database health monitoring with automatic recovery
- **Intelligent Retry Policies**: Exponential backoff with transient failure detection and jitter support
- **High-Performance Logging**: Zero-allocation logging using LoggerMessage delegates for production environments
- **Error Handling**: Centralized error management with configurable strategies and detailed monitoring
- **Performance Monitoring**: Built-in operation timing, success rate tracking, and bottleneck detection

### ☁️ Backend & Offline Capabilities

The backend is built using **ASP.NET Core** and exposes a set of RESTful APIs for the web clients.  
For desktop users, the same logic is embedded into the Avalonia app using a self-hosted, lightweight in-process backend.  
This allows the application to be used fully offline, with local persistence through **EF Core (SQLite)**.

**Enterprise Data Management:**
- Multi-database provider support (SQLite, SQL Server, PostgreSQL, MySQL)
- Advanced Entity Framework Core patterns with custom value converters
- Sophisticated join entity management for complex relationships
- Database conventions for consistent naming (snake_case, pluralization, FK naming)

A synchronization mechanism (planned) will allow syncing local data with the central server once a connection is available.  
This ensures data consistency and enables seamless collaboration between online and offline users.

---

### 📦 Shared Infrastructure

A comprehensive shared infrastructure provides enterprise-grade capabilities across all modules:

#### **Core Shared Projects:**

- **MyClub.Shared.Kernel**: Core domain primitives, strongly-typed IDs, and base abstractions
- **MyClub.Shared.Domain**: Shared domain entities and value objects with auditing capabilities
- **MyClub.Shared.Application**: CQRS patterns, MediatR behaviors, and application service abstractions
- **MyClub.Shared.Infrastructure.Persistence**: Enterprise persistence patterns with error handling and resilience
- **MyClub.Shared.Infrastructure.Events**: Domain event dispatching with MediatR integration
- **MyClub.Localization**: Comprehensive localization support for international deployments

#### **Enterprise Infrastructure Features:**

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

This ensures consistency and eliminates duplication across modules while providing enterprise-grade reliability, performance, and observability.

---

### 🧪 Testing & Quality Assurance

Comprehensive testing strategy covering all architectural layers:

- **Unit Tests**: Domain logic testing with **xUnit**, **Moq**, and **FluentAssertions**
- **Integration Tests**: Repository and infrastructure testing with in-memory databases
- **Performance Tests**: Circuit breaker, retry policy, and error handling validation
- **Architecture Tests**: Dependency rule enforcement and layer separation validation

**Testing Projects:**
- `MyClub.Shared.Tests`: Shared infrastructure and kernel testing
- `MyClub.Scorer.Domain.Tests`: Scorer domain logic testing
- `MyClub.Scorer.Infrastructure.Persistence.Tests`: Persistence layer testing

---

### 🛠 Enterprise Tooling & Best Practices

- **AutoMapper**: Transform domain models to DTOs with optimized mappings
- **FluentValidation**: Comprehensive input validation in the application layer
- **Strongly Typed IDs**: Prevent mixing domain concepts with compile-time safety
- **Value Converters**: JSON serialization for complex domain objects and polymorphic entities
- **Database Conventions**: Automatic snake_case naming, pluralization, and FK conventions
- **Health Checks**: ASP.NET Core integration for monitoring and alerting
- **Structured Logging**: High-performance logging with EventId organization

The architecture is built for **extensibility**, **testability**, **modularity**, and **enterprise deployment**, making it easy to introduce new modules without impacting the existing system.

---

## 🧩 Functional Modules

| Module                              | Description                                                    | Status |
|-------------------------------------|----------------------------------------------------------------|--------|
| [`Scor'er`](./src/Scorer/README.md) | Competition creation & management (leagues, cups, tournaments) | ✅ **Production Ready** |
| `Team'up` _(planned)_               | Squad and staff management (players, coaches, teams)           | 🚧 **Planned** |
| `Training` _(planned)_              | Training session builder and exercise planning                 | 🚧 **Planned** |
| `Licensing` _(planned)_             | Administrative tools and regulatory tracking                   | 🚧 **Planned** |

### 🏆 Scorer Module Features

**Competition Management:**
- **Multi-format Competitions**: Leagues, Cups, and Tournaments with complex bracket support
- **Team Management**: Concrete and virtual team references for tournament progression
- **Match System**: Comprehensive match tracking with events, goals, cards, and penalty shootouts
- **Standing Calculation**: Advanced standing rules with head-to-head comparison and penalty points

**Technical Capabilities:**
- **Multi-database Support**: SQLite (development), SQL Server, PostgreSQL production deployment
- **Advanced Entity Framework**: Complex value converters, join entities, and inheritance mapping
- **Enterprise Reliability**: Circuit breaker protection, retry policies, and connection monitoring
- **Performance Optimized**: High-performance logging, query optimization, and connection pooling

---

## 🏗️ Project Structure

```
MyClub/
├── src/
│   ├── Shared/                                    # Shared infrastructure and libraries
│   │   ├── MyClub.Shared.Kernel/                 # Core primitives and abstractions
│   │   ├── MyClub.Shared.Domain/                 # Shared domain entities and value objects
│   │   ├── MyClub.Shared.Application/            # CQRS patterns and MediatR behaviors
│   │   ├── MyClub.Shared.Infrastructure.Persistence/ # Enterprise persistence patterns
│   │   ├── MyClub.Shared.Infrastructure.Events/  # Domain event infrastructure
│   │   ├── MyClub.Localization/                  # Internationalization support
│   │   └── MyClub.CrossCutting/                  # Cross-cutting concerns
│   ├── Referential/                              # Reference data management
│   │   └── MyClub.Referential.Domain/            # Teams, players, stadiums, managers
│   └── Scorer/                                   # Competition management module
│       ├── MyClub.Scorer.Domain/                 # Rich domain model with DDD patterns
│       ├── MyClub.Scorer.Application/            # CQRS handlers and use cases
│       ├── MyClub.Scorer.Infrastructure.Persistence/ # EF Core with enterprise features
│       ├── MyClub.Scorer.Infrastructure.Persistence.Design/ # Design-time EF tools
│       ├── MyClub.Scorer.Infrastructure.Migrations.Sqlite/ # SQLite migrations
│       └── MyClub.Scorer.Infrastructure.Migrations.SqlServer/ # SQL Server migrations
└── tests/                                        # Comprehensive test suite
    ├── MyClub.Shared.Tests/                      # Shared infrastructure tests
    ├── MyClub.Scorer.Domain.Tests/               # Domain logic tests
    └── MyClub.Scorer.Infrastructure.Persistence.Tests/ # Persistence layer tests
```

---

## 🚀 Enterprise Features

### 🛡️ **Production-Ready Reliability**
- **Circuit Breaker Pattern**: Prevents cascade failures during database outages
- **Intelligent Retry Logic**: Exponential backoff for transient failure recovery
- **Connection Health Monitoring**: Real-time database connectivity tracking
- **Error Recovery Strategies**: Configurable policies for different failure scenarios

### 📊 **Performance & Observability**
- **High-Performance Logging**: LoggerMessage delegates for zero-allocation logging
- **Performance Metrics**: Operation timing and success rate tracking
- **Health Check Integration**: ASP.NET Core health endpoints for monitoring
- **Query Optimization**: EF Core includes and indexes optimized for football domain

### ⚙️ **Deployment Flexibility**
- **Multi-Database Support**: SQLite, SQL Server, PostgreSQL, MySQL
- **Environment Configuration**: Development, staging, and production-specific settings
- **Connection Pooling**: Optimized database connection management
- **Migration Management**: Automated schema evolution with rollback support

### 🌐 **Enterprise Integration**
- **Domain Events**: Loosely coupled integration between modules
- **CQRS Architecture**: Separate read and write models for scalability
- **Clean Architecture**: Testable, maintainable, and technology-agnostic design
- **Modular Design**: Independent module deployment and evolution

---

## 🔭 Roadmap Highlights

### **Phase 1: Foundation** ✅ **Complete**
- Core domain modeling with DDD patterns
- CQRS implementation with MediatR
- Enterprise persistence infrastructure
- Circuit breaker and error handling

### **Phase 2: Enhanced Reliability** ✅ **Complete**
- High-performance logging implementation
- Connection resilience patterns
- Comprehensive health monitoring
- Multi-database provider support

### **Phase 3: Advanced Features** 🚧 **In Progress**
- Data synchronization between offline and online apps
- Player and team performance analytics
- Training and planning tools
- Advanced reporting and dashboards

### **Phase 4: Mobile & Cloud** 🚧 **Planned**
- Native mobile app (MAUI or Flutter)
- Multi-club and federation mode
- Cloud-native deployment patterns
- Advanced security and compliance features

---

## 📦 Dependencies & Technologies

### **Core Technologies**
- **.NET 10**: Latest framework with performance improvements
- **Entity Framework Core 10.0**: Advanced ORM with enterprise patterns
- **MediatR 13.0**: CQRS and mediator pattern implementation
- **AutoMapper**: Object-to-object mapping
- **FluentValidation 12.0**: Validation framework

### **Enterprise Infrastructure**
- **Microsoft.Extensions.Logging**: High-performance structured logging
- **Microsoft.Extensions.HealthChecks**: Application health monitoring
- **Microsoft.Extensions.DependencyInjection**: Built-in IoC container
- **MyNet.Humanizer 5.0**: String manipulation and formatting

### **Testing & Quality**
- **xUnit**: Primary testing framework
- **Moq**: Mocking framework for unit tests
- **FluentAssertions**: Expressive assertion library
- **Microsoft.EntityFrameworkCore.InMemory**: In-memory database for testing

---

## 📜 License

This project is licensed under the MIT License – see [LICENSE](./LICENSE) for details.

---

## 🤝 Contributing

We welcome contributions to MyClub! Please read our contribution guidelines for:

- Code style and standards
- Pull request process
- Issue reporting
- Feature request process

**Development Setup:**
1. Install .NET 10 SDK
2. Clone the repository
3. Run `dotnet restore` in the root directory
4. Run `dotnet build` to ensure everything compiles
5. Run `dotnet test` to execute the test suite

---

## 📬 Contact

Developed by [Stéphane ANDRE].  
For questions or collaborations: `andre.cs2i@gmail.com`

**Enterprise Support:**
For enterprise deployment, custom development, or support contracts, please contact us directly.

<!-- MARKDOWN LINKS & IMAGES -->
<!-- https://www.markdownguide.org/basic-syntax/#reference-style-links -->
[language-shield]: https://img.shields.io/github/languages/top/sandre58/MyClub
[language-url]: https://github.com/sandre58/MyClub
[forks-shield]: https://img.shields.io/github/forks/sandre58/MyClub?style=for-the-badge
[forks-url]: https://github.com/sandre58/MyClub/network/members
[stars-shield]: https://img.shields.io/github/stars/sandre58/MyClub?style=for-the-badge
[stars-url]: https://github.com/sandre58/MyClub/stargazers
[issues-shield]: https://img.shields.io/github/issues/sandre58/MyClub?style=for-the-badge
[issues-url]: https://github.com/sandre58/MyClub/issues
[license-shield]: https://img.shields.io/github/license/sandre58/MyClub?style=for-the-badge
[license-url]: https://github.com/sandre58/MyClub/blob/main/LICENSE
[build-shield]: https://img.shields.io/github/actions/workflow/status/sandre58/MyClub/ci.yml?logo=github&label=CI&style=for-the-badge
[build-url]: https://github.com/sandre58/MyClub/actions
[downloads-shield]: https://img.shields.io/github/downloads/sandre58/MyClub/total?style=for-the-badge
[downloads-url]: https://github.com/sandre58/MyClub/releases
[framework1-shield]: https://img.shields.io/badge/.NET-8.0-purple
[framework2-shield]: https://img.shields.io/badge/.NET-9.0-purple
[framework3-shield]: https://img.shields.io/badge/.NET-10.0-purple
[enterprise-shield]: https://img.shields.io/badge/Grade-Enterprise-gold
[performance-shield]: https://img.shields.io/badge/Performance-High-brightgreen