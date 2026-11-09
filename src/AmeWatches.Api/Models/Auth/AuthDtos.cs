using System.ComponentModel.DataAnnotations;

namespace AmeWatches.Api.Models.Auth;

public record LoginRequest(
    [Required, EmailAddress, StringLength(150)] string Email,
    [Required, StringLength(200)] string Password
);

public record LoginResponse(string Token, DateTime ExpiresAt, string FullName, string Email);

public enum LoginFailure
{
    InvalidCredentials = 1,
    LockedOut = 2
}

public record LoginResult(LoginResponse? Success, LoginFailure? Failure)
{
    public static LoginResult Ok(LoginResponse response) => new(response, null);
    public static LoginResult Fail(LoginFailure failure) => new(null, failure);
}
