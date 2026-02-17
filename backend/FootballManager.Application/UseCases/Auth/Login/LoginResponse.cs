using System;

namespace FootballManager.Application.UseCases.Auth.Login
{
    public class LoginResponse
    {
        public Guid UserId { get; }
        public string Email { get; }
        public string Role { get; }
        public string Token { get; }

        public LoginResponse(Guid userId, string email, string role, string token)
        {
            UserId = userId;
            Email = email;
            Role = role;
            Token = token;
        }
    }
}
