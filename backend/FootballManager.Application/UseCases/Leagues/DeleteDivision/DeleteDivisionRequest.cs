using System;

namespace FootballManager.Application.UseCases.Leagues.DeleteDivision;

public class DeleteDivisionRequest
{
    public Guid LeagueId { get; set; }
    public Guid DivisionId { get; set; }
    public Guid UserId { get; set; }
}
