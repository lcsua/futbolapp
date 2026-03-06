namespace FootballManager.Application.UseCases.Matches.UpdateMatchResult;

public sealed class UpdateMatchResultRequest
{
    public int HomeScore { get; set; }
    public int AwayScore { get; set; }
    public string Status { get; set; }
}
