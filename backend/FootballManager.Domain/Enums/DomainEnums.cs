namespace FootballManager.Domain.Enums
{
    public enum PlayerPosition
    {
        GK,
        DEF,
        MID,
        FWD
    }

    public enum MatchStatus
    {
        SCHEDULED,
        IN_PROGRESS,
        COMPLETED,
        CANCELLED,
        POSTPONED,
        PLAYED
    }

    public enum MatchEventType
    {
        GOAL,
        YELLOW_CARD,
        RED_CARD,
        SUBSTITUTION,
        OWN_GOAL
    }

    public enum MatchIncidentType
    {
        Goal,
        YellowCard,
        RedCard,
        Injury,
        Substitution,
        Other
    }

    public enum UserRole
    {
        ADMIN,
        USER
    }
}
