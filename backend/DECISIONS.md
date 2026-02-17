# Technical Decisions

- EF Core used for persistence
- Dapper allowed only for read/reporting
- UnitOfWork used to control transactions
- SaveChanges is never called inside repositories
