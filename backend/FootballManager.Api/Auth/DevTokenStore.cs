using System;
using System.Collections.Concurrent;
using FootballManager.Application.Interfaces;

namespace FootballManager.Api.Auth
{
    public class DevTokenStore : IDevTokenStore
    {
        private readonly ConcurrentDictionary<string, Guid> _tokenToUserId = new();

        public void Register(Guid userId, string token)
        {
            _tokenToUserId[token] = userId;
        }

        public Guid? GetUserId(string token)
        {
            return _tokenToUserId.TryGetValue(token, out var userId) ? userId : null;
        }
    }
}
