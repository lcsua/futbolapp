using System;

namespace FootballManager.Application.UseCases.Leagues.CreateDivision
{
    public class CreateDivisionRequest
    {
        public Guid LeagueId { get; set; }
        public Guid UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Slug { get; set; }
        public string? Description { get; set; }

        /// <summary>
        /// When true, generated fixtures will not use kickoff times in
        /// [<see cref="KickoffRestrictionStart"/>, <see cref="KickoffRestrictionEnd"/>).
        /// </summary>
        public bool KickoffRestrictionEnabled { get; set; }

        public TimeOnly? KickoffRestrictionStart { get; set; }
        public TimeOnly? KickoffRestrictionEnd { get; set; }
    }
}
