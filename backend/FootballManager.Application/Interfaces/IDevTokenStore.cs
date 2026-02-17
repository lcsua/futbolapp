using System;

namespace FootballManager.Application.Interfaces
{
    /// <summary>
    /// Development-only token store. Replace with JWT/OAuth validation in production.
    /// </summary>
    public interface IDevTokenStore
    {
        void Register(Guid userId, string token);
        Guid? GetUserId(string token);
    }
}
