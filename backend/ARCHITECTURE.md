# Architecture Decisions

## Clean Architecture
- Domain has no dependencies
- Application orchestrates use cases
- Infrastructure implements interfaces
- API only coordinates HTTP

## Multi-League Model
- user_leagues defines access and visibility
- All use cases receive userId + leagueId
- Authorization is enforced in Application layer

## What we avoid
- No MediatR
- No CQRS
- No domain events (for now)
