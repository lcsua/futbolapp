using System;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Domain.Entities;
using FootballManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FootballManager.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly FootballManagerDbContext _context;

        public UserRepository(FootballManagerDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        }

        public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email.Trim().ToLower(), cancellationToken);
        }

        public async Task<User?> GetByEmailAndPasswordAsync(string email, string password, CancellationToken cancellationToken = default)
        {
            var normalizedEmail = email.Trim();
            var normalizedPassword = password.Trim();

            return await _context.Users
                .FromSqlRaw(
                    @"SELECT * 
                      FROM users 
                      WHERE lower(email) = lower(@email)
                        AND password_hash = crypt(@password, password_hash)
                      LIMIT 1",
                    new NpgsqlParameter("email", normalizedEmail),
                    new NpgsqlParameter("password", normalizedPassword))
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task AddAsync(User user, CancellationToken cancellationToken = default)
        {
            await _context.Users.AddAsync(user, cancellationToken);
        }

        public async Task<string> HashPasswordAsync(string password, CancellationToken cancellationToken = default)
        {
            var hash = await _context.Database
                .SqlQuery<string>($"SELECT crypt({password.Trim()}, gen_salt('bf')) AS \"Value\"")
                .FirstAsync(cancellationToken);

            if (string.IsNullOrWhiteSpace(hash))
                throw new InvalidOperationException("Could not hash password.");

            return hash;
        }

        public async Task SetPasswordAsync(Guid userId, string password, CancellationToken cancellationToken = default)
        {
            await _context.Database.ExecuteSqlRawAsync(
                @"UPDATE users
                  SET password_hash = crypt(@password, gen_salt('bf')),
                      updated_at = NOW()
                  WHERE id = @id",
                new object[]
                {
                    new NpgsqlParameter("password", password.Trim()),
                    new NpgsqlParameter("id", userId)
                },
                cancellationToken);
        }
    }
}
