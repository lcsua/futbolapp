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
        PLAYED,
        POSTPONED,
        CANCELLED
    }

    public enum MatchEventType
    {
        GOAL,
        YELLOW_CARD,
        RED_CARD,
        SUBSTITUTION,
        OWN_GOAL
    }

    public enum UserRole
    {
        ADMIN,
        USER
    }
}
