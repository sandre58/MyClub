# MyClub — Suite logicielle pour clubs de sport (Football)

## Présentation

**MyClub** est une suite logicielle modulaire dédiée à la gestion complète des clubs de football.  
Chaque module est indépendant et s’appuie sur un noyau commun pour garantir la cohérence et la réutilisabilité.

---

## Modules

- **Scor'er** : Création de compétitions entièrement personnalisables
- **Team'up** : Gestion d’effectif (joueurs, staff, etc.)
- **À venir** : Création d’entraînements, autres modules spécialisés

---

## Stack Technique

| Couche      | Technologies principales                                      |
|-------------|--------------------------------------------------------------|
| **Frontend**| - Avalonia (client lourd, gestion, offline)<br>- Blazor WebAssembly (visualisation web) |
| **Backend** | - ASP.NET Core (C#)<br>- API REST<br>- EF Core (persistence BDD)<br>- Backend embarqué pour Avalonia (offline) |

---

## Architecture

- **Domain-Driven Design (DDD)**
- **Clean Architecture**
- **CQRS**
- **FluentValidation**
- **AutoMapper**
- **Tests unitaires** : xUnit, FluentAssertions, Moq, AutoFixture
- **Bonnes pratiques** : modules indépendants, core partagé (`IEntity`, `IEntityId`, `IRepository`, etc.), Strongly Typed Id

---

## Spécificités

- Modules totalement indépendants
- Entités partagées : `Team`, `Stadium`, `Player`, etc.
- Noyau commun pour les abstractions et primitives

---

## Domaine Scor'er

### 🏆 Competition (Agrégat racine)

Une compétition peut être de 3 types :

| Type        | Description                                                                 |
|-------------|-----------------------------------------------------------------------------|
| **League**      | Championnat (ex : Ligue 1) avec journées (`Matchday`) et classement      |
| **Cup**         | Coupe à élimination directe (ex : Coupe de France), avec tours (`Round`) |
| **Tournament**  | Compétition multi-phases : groupes, ligue, élimination directe           |

#### Données de base d’une compétition

- Nom
- Logo
- Format des matchs par défaut
- Règles des matchs par défaut
- Liste des équipes réelles (ex: PSG, Lyon, etc...)
- Liste des stades

---

### Détails des entités principales

#### League

- Liste de journées (`Matchday`)
- Règle de classement
- Classement
- Points de pénalité
- Statuts des rangs de classement (ex: "Champion", "Relégué", etc.)

#### Cup

- Liste de tours (`Round`)
- Règle de qualification

#### Tournament

- Liste de phases (`Stage`)

#### Teams

- Nom
- Logo
- Stade
- Liste de joueurs
- Coachs

#### Stadiums

- Nom
- Surface
- Adresse

#### Matchday

- Représente une journée (ex : "Journée 3")
- Liste de matchs
- Date
- Heure par défaut

#### Round

- Liste des équipes (réelles ou virtuelles (ex: Vaiqueur du Match A, Perdant du match B, 3ème du Groupe A, etc...))
- Liste de fixtures
- Liste de RoundStage (Phase Aller, Match1, Match2, etc...)
- Format de match personnalisé (hérité du parent si null)
- Format (Match Aller-Retour, Meilleur des 5 matchs, Replay match, etc.)

#### Stage

- Liste de teams (réelles ou virtuelles (ex: Vaiqueur du Match A, Perdant du match B, 3ème du Groupe A, etc...))
- Format et règles personnalisés
- Types possibles : `GroupStage`, `Knockout`, `Championship`

#### Match (Agrégat)

- Date
- Règles
- Format
- État
- MatchOpponent (score, équipe)

#### Fixture

- Regroupe les matchs d'un tour (les matchs de tous les roundStage) entre 2 équipes (`TeamAId`, `TeamBId`)

#### GroupStage

- Liste d’équipes
- Liste des groupes

#### Knockout

- Identique à `Round`

#### Championship

- Identique à `League`

#### Standing

- Classement des équipes
- Liste de standingRow
- Ordre de tri (points, différence de buts, etc.) établi par la règle de classement
- Nombre de points par victoire, match nul, défaite établi par la règle de classement
- Les équipes peuvent être triées par head-to-head aussi
- Les équipes peuvent avoir des points de pénalité
- On peut calculer de manière incrémentale à chaque match joué ou de manière globale à n'importe quel moment
---

> **Remarque** :  
> Cette structure est conçue pour évoluer facilement avec de nouveaux modules et de nouveaux types de compétitions.