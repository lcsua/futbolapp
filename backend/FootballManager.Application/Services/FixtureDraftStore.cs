using System;
using System.Collections.Concurrent;
using FootballManager.Application.Dtos;

namespace FootballManager.Application.Services;

public sealed class FixtureDraftStore : IFixtureDraftStore
{
    private readonly ConcurrentDictionary<Guid, FixtureDraftDto> _store = new();

    public void Set(Guid seasonId, FixtureDraftDto draft)
    {
        _store[seasonId] = draft;
    }

    public FixtureDraftDto? Get(Guid seasonId)
    {
        return _store.TryGetValue(seasonId, out var draft) ? draft : null;
    }

    public void Clear(Guid seasonId)
    {
        _store.TryRemove(seasonId, out _);
    }
}
