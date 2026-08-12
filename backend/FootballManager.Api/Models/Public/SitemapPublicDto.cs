using System;
using System.Collections.Generic;

namespace FootballManager.Api.Models.Public;

public class SitemapPublicDto
{
    public DateTime GeneratedAtUtc { get; set; }
    public List<SitemapLeagueDto> Leagues { get; set; } = new();
}

public class SitemapLeagueDto
{
    public string Slug { get; set; } = string.Empty;
    public DateTime? UpdatedAtUtc { get; set; }
    public List<SitemapTeamDto> Teams { get; set; } = new();
}

public class SitemapTeamDto
{
    public string Slug { get; set; } = string.Empty;
    public DateTime? UpdatedAtUtc { get; set; }
}
