using System;
using FootballManager.Application.Dtos;

namespace FootballManager.Application.Services;

public interface IFixtureDraftStore
{
    void Set(Guid seasonId, FixtureDraftDto draft);
    FixtureDraftDto? Get(Guid seasonId);
    void Clear(Guid seasonId);
}
