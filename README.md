<div id="top"></div>

<!-- Logo -->
<p align="center">
  <img src="assets/myclub-logo.png" alt="MyClub logo" width="200"/>
</p>

<!-- Title -->
<h1 align="center">MyClub</h1>
<p align="center"><em>Modular Software Suite for Football Clubs</em></p>

[![Build][build-shield]][build-url]
[![Downloads][downloads-shield]][downloads-url]
[![Forks][forks-shield]][forks-url]
[![Stargazers][stars-shield]][stars-url]
[![Issues][issues-shield]][issues-url]
[![MIT License][license-shield]][license-url]

**MyClub** is a modular software suitedesigned to fully digitalize the managementof football clubs. From handling playerrosters and scheduling training sessions totracking team results and league standings,these applications provide comprehensivetools to efficiently run and organize asports club. Whether you're a coach,manager, or club administrator, theseapplications help streamline operations,improve communication, and enhance overallclub performance.

---

## 🔧 Technical Architecture

![Framework1][framework1-shield]
![Framework2][framework2-shield]
![Framework3][framework3-shield]
![Language][language-shield]

MyClub is built with a robust, scalable, and modular architecture designed to support both **offline-first usage** and **cloud-based integration**, while following modern software engineering principles like **Domain-Driven Design (DDD)**, **Clean Architecture**, and **CQRS**.

### 🧱 Architectural Layers

The solution is structured around several clear layers to ensure separation of concerns:

- **Domain Layer** (per module):  
  Contains the pure domain model—entities, value objects, aggregates, business rules, and domain events.  
  This layer has no dependencies on any external library or infrastructure. It reflects the *business logic* of each module (e.g., `Scorer`, `TeamUp`) and ensures domain integrity.

- **Application Layer**:  
  Contains use cases, commands, queries, handlers, validators, and interfaces for repositories or services.  
  This layer orchestrates domain logic and handles the flow of data between the UI and domain.  
  It follows the **CQRS** pattern to separate reads and writes.

- **Infrastructure Layer**:  
  Implements the interfaces defined in the application layer.  
  It handles data persistence (with EF Core), file storage, networking, and external services.  
  This layer is plug-and-play and can easily be swapped for another provider (e.g., switch from SQLite to PostgreSQL).

- **Presentation Layer**:  
  - **Desktop Client**: Built with **Avalonia UI**, it offers a rich, cross-platform experience with full offline capabilities.  
    The embedded backend allows local data persistence and interaction without requiring a connection.
  - **Web Client**: Developed using **Blazor WebAssembly**, it provides a lightweight interface primarily for visualization and consultation of competitions, standings, and results.

---

### ☁️ Backend & Offline Capabilities

The backend is built using **ASP.NET Core** and exposes a set of RESTful APIs for the web clients.  
For desktop users, the same logic is embedded into the Avalonia app using a self-hosted, lightweight in-process backend.  
This allows the application to be used fully offline, with local persistence through **EF Core (SQLite)**.

A synchronization mechanism (planned) will allow syncing local data with the central server once a connection is available.  
This ensures data consistency and enables seamless collaboration between online and offline users.

---

### 📦 Shared Core

A shared `Core` project is used across all modules. It contains:

- Shared interfaces and abstractions (`IEntity`, `IAggregateRoot`, `IRepository<T>`, etc.)
- Domain primitives (e.g., strongly typed IDs, enums, value objects)
- Common exceptions and base classes

This ensures consistency and eliminates duplication across modules while allowing each module to evolve independently.

---

### 🧪 Testing & Tooling

All business logic is covered by **unit tests** using:

- **xUnit** as the test framework  
- **Moq** and **AutoFixture** for mocking and auto-generation of data  
- **FluentAssertions** for expressive assertions  

Tests are organized per module and reflect the domain boundaries.

---

### 🛠 Tooling & Best Practices

- **AutoMapper** is used to transform domain models to DTOs or view models, ensuring clean separation.
- **FluentValidation** is used for input validation in the application layer.
- **Strongly Typed IDs** (value objects) prevent mixing domain concepts (e.g., `TeamId` vs `MatchId`) and improve type safety.

The architecture is built for **extensibility**, **testability**, and **modularity**, making it easy to introduce new modules (e.g., Training, Scouting, Licensing) without impacting the existing system.

---

## 🧩 Functional Modules

| Module     | Description                                                |
|------------|------------------------------------------------------------|
| [`Scor'er`](./src/Scorer/README.md)   | Competition creation & management (leagues, cups, tournaments) |
| `Team'up` _(planned)_   | Squad and staff management (players, coaches, teams)           |
| `Training` _(planned)_ | Training session builder and exercise planning         |
| `Licensing` _(planned)_ | Administrative tools and regulatory tracking            |

---

## 🔭 Roadmap Highlights

- 🔄 Data synchronization between offline and online apps
- 📊 Player and team performance analytics
- 📅 Training and planning tools
- 📱 Native mobile app (MAUI or Flutter)
- 🌍 Multi-club and federation mode

---

## 📜 License

This project is licensed under the MIT License – see [LICENSE](./LICENSE) for details.

---

## 🤝 Contributing

Coming soon – contribution guidelines, code style, and branching model.

---

## 📬 Contact

Developed by [Stéphane ANDRE].  
For questions or collaborations: `andre.cs2i@gmail.com`

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