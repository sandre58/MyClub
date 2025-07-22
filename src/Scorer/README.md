<div id="top"></div>

<!-- Logo -->
<p align="center">
  <img src="../../assets/scorer-logo.png" alt="Scor'er logo" width="200"/>
</p>

<!-- Title -->
<h1 align="center">Scor'er</h1>

> Part of the [MyClub](../../README.md) suite – modular software for football clubs.

![Module Status](https://img.shields.io/badge/module-active-brightgreen)
![Domain Driven Design](https://img.shields.io/badge/DDD-CQRS-blue)
![Test Coverage](https://img.shields.io/badge/tests-100%25-green)

**Scor'er** is the core module of MyClub responsible for creating, configuring, and managing football competitions.

It supports various competition types (league, cup, tournament), handles rules, match formats, progression logic, standings, and more.

---

## 🧠 Domain Overview

### Supported Competition Types

| Type         | Description                                                         |
|--------------|---------------------------------------------------------------------|
| `League`     | Round-robin championship with standings (e.g. Ligue 1)              |
| `Cup`        | Knockout tournament with rounds and qualifications (e.g. FA Cup)    |
| `Tournament` | Multi-stage structure with group stages, playoffs, finals, etc.     |

---

### Main Aggregates & Entities

#### `Competition` (Aggregate Root)

- ID, Name, Logo
- Default Match Format & Rules
- Participating Teams
- Stadiums
- Type (`League`, `Cup`, `Tournament`)
- Child entities depending on the type:
  - `Matchdays` (League)
  - `Rounds` (Cup)
  - `Stages` (Tournament)

#### `Team`

- Name, Logo
- Stadium
- Players, Coaches

#### `Match`

- Date, Time, Status
- Format & Rules
- `MatchOpponent` (Team & Score)
- MatchResult (Win/Draw/Loss)

#### `Round` / `Stage`

- Virtual & Real Teams (e.g. "Winner of Match A")
- Match Format (best of X, home/away, replay)
- `Fixtures` (links between teams and match sequence)
- Sub-phases (`RoundStage`): match legs or phases

#### `Standing`

- Standings Table with rules (points, tiebreakers)
- Ranking status (e.g. Champion, Relegated)
- Supports penalties, head-to-head rules

---

## ⚙️ Features

- ✅ Custom match formats per round or stage
- ✅ Virtual teams for dynamic progression
- ✅ Supports multi-phase tournaments
- ✅ Standings & ranking rule engine
- 🔄 Sync planned with shared infrastructure
- 🔬 Testable domain with pure logic

---

## 🧪 Tests

Unit tests are written using:

- `xUnit` for orchestration
- `FluentAssertions` for readable assertions
- `AutoFixture` for rich test data
- `Moq` for mocking external dependencies

---

## 🧩 Integration

This module depends on the shared `Core` library (`IEntity`, `IRepository`, typed IDs)  
and can be used independently in both the desktop and web apps of MyClub.

---

## 🚧 TODO

- [ ] Dynamic match scheduling & constraints

---

## 📬 Contact

Maintained by [Stéphane ANDRE].  
Feel free to open issues or propose features in the [main repository](https://github.com/sandre58/MyClub).
