using System;

namespace FootballManager.Application.Exceptions
{
    public class ForbiddenAccessException : Exception
    {
        public ForbiddenAccessException(string message) : base(message)
        {
        }
    }
}
