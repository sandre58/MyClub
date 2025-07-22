# ✅ MyClub — TODO List

Task tracking for the development of the **MyClub** modular sports club management suite.

---

## 🧱 Global Architecture

- [x] Write complete technical documentation (`README.md` per module)
- [ ] Setup CI/CD pipelines (github)
- [ ] Local deployment via Docker Compose
- [ ] Integration testing (API + Database)

---

## ⚙️ Core (Shared Kernel)

---

## 🏆 Scor'er

- [ ] Finalize competition `Tournament` type

### Services

- [ ] Add unit tests for match, round, tournament logic
- [ ] Create team (in CQRS command)
- [ ] Update team (in CQRS command)
- [ ] Delete team (in CQRS command)
- [ ] Create stadium (in CQRS command)
- [ ] Update stadium (in CQRS command)
- [ ] Delete stadium (in CQRS command)

#### League

- [ ] Create a league (in CQRS command)
- [ ] Calculate standing (Service)
    - Home ranking
    - Away ranking
    - Live ranking
- [x] Schedule matchdays (Service)
    - Needs League Format (Swiss System or Round Robin, Number of fixtures, etc...)

#### Round
- [ ] Determine fixture winner and loser
- [ ] Determine stage format
- [ ] Auto-generate fixtures and brackets

#### Tournament
