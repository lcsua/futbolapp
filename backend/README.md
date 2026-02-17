# FootballManager Backend

.NET 8 backend using Clean Architecture (pragmatic).

## Architecture
- Domain: business entities and rules
- Application: use cases, interfaces, authorization logic
- Infrastructure: EF Core, PostgreSQL, repositories
- API: HTTP controllers

## Key Concepts
- Multi-league system
- user_leagues is the source of truth for authorization
- All actions are scoped to a league
- Controllers are thin
- Use cases enforce rules

## Tech Stack
- .NET 8
- EF Core
- PostgreSQL
- UnitOfWork
