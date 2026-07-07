# MyClub.Shared.Application.Abstractions

> Abstractions for the shared application layer in the MyClub sports management suite

![.NET](https://img.shields.io/badge/.NET-10-blue)
![CQRS](https://img.shields.io/badge/Pattern-CQRS-green)
![Clean Architecture](https://img.shields.io/badge/Architecture-Clean-brightgreen)

## Overview

**MyClub.Shared.Application.Abstractions** provides core interfaces and base types for the application layer of MyClub. It defines contracts for commands, queries, handlers, and pipeline behaviors, enabling modularity and testability across all application modules.

## Features

- **CQRS Abstractions**: Interfaces for commands, queries, and their handlers.
- **Pipeline Behaviors**: Contracts for cross-cutting concerns (validation, logging, caching).
- **Result Pattern**: Standardized result types for functional error handling.
- **Extensibility**: Designed for easy extension and implementation in concrete application projects.

## Usage

Reference this project in your application layer to implement custom commands, queries, and behaviors. It is intended to be used with MediatR and other shared infrastructure components.

## Related Projects

- **MyClub.Shared.Application**: Implements these abstractions with MediatR and pipeline behaviors.
- **MyClub.Shared.Domain**: Shared domain entities and value objects.
- **MyClub.Shared.Kernel**: Core primitives and interfaces.

## License

Distributed under the MIT License.
