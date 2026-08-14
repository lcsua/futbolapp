namespace FootballManager.Domain.Enums;

public enum PushFollowScopeType
{
    League = 1,
    Team = 2
}

public enum PushNotificationEventType
{
    ResultUpdated = 1,
    FixtureUpdated = 2,
    StandingsUpdated = 3,
    NewsPublished = 4
}
