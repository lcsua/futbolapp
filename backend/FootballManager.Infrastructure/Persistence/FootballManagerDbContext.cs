using FootballManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace FootballManager.Infrastructure.Persistence
{
    public class FootballManagerDbContext : DbContext
    {
        public FootballManagerDbContext(DbContextOptions<FootballManagerDbContext> options) : base(options)
        {
        }

        public DbSet<League> Leagues { get; set; }
        public DbSet<Season> Seasons { get; set; }
        public DbSet<Division> Divisions { get; set; }
        public DbSet<DivisionSeason> DivisionSeasons { get; set; }
        public DbSet<Team> Teams { get; set; }
        public DbSet<TeamDivisionSeason> TeamDivisionSeasons { get; set; }
        public DbSet<Player> Players { get; set; }
        public DbSet<Field> Fields { get; set; }
        public DbSet<FieldAvailability> FieldAvailabilities { get; set; }
        public DbSet<FieldBlackout> FieldBlackouts { get; set; }
        public DbSet<MatchRule> MatchRules { get; set; }
        public DbSet<CompetitionRule> CompetitionRules { get; set; }
        public DbSet<CompetitionMatchDay> CompetitionMatchDays { get; set; }
        public DbSet<Fixture> Fixtures { get; set; }
        public DbSet<Result> Results { get; set; }
        public DbSet<MatchEvent> MatchEvents { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<UserLeague> UserLeagues { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Standing> Standings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Apply all configurations from the current assembly
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

            // Global conventions (optional, e.g. for snake_case if I used a library, but I'll do explicit mapping)
            // But I'll do explicit for now to be safe and faithful to the SQL.
        }
    }
}
