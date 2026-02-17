using System;

namespace FootballManager.Application.Exceptions
{
    public class LeagueAlreadyExistsException : Exception
    {
        public LeagueAlreadyExistsException(string name) 
            : base($"A league with the name '{name}' already exists.")
        {
        }
    }
}
