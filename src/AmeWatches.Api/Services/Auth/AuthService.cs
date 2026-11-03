using AmeWatches.Api.Data.Entities;
using AmeWatches.Api.Models.Auth;
using Microsoft.AspNetCore.Identity;

namespace AmeWatches.Api.Services.Auth;

public interface IAuthService
{
    Task<LoginResult> Login(LoginRequest request);
}

public class AuthService : IAuthService
{
    private readonly UserManager<AppUser> _users;
    private readonly ITokenService _tokens;
    private readonly ILogger<AuthService> _logger;

    public AuthService(UserManager<AppUser> users, ITokenService tokens, ILogger<AuthService> logger)
    {
        _users = users;
        _tokens = tokens;
        _logger = logger;
    }

    public async Task<LoginResult> Login(LoginRequest request)
    {
        var user = await _users.FindByEmailAsync(request.Email.Trim());

        if (user is null)
        {
            _users.PasswordHasher.HashPassword(new AppUser(), request.Password);
            return LoginResult.Fail(LoginFailure.InvalidCredentials);
        }

        if (await _users.IsLockedOutAsync(user))
        {
            _logger.LogWarning("Hyrje e refuzuar: llogaria {Email} është e mbyllur përkohësisht.", user.Email);
            return LoginResult.Fail(LoginFailure.LockedOut);
        }

        if (!await _users.CheckPasswordAsync(user, request.Password))
        {
            await _users.AccessFailedAsync(user);
            _logger.LogWarning("Fjalëkalim i gabuar për {Email}.", user.Email);
            return LoginResult.Fail(LoginFailure.InvalidCredentials);
        }

        var roles = await _users.GetRolesAsync(user);
        if (!roles.Contains(Roles.Admin))
            return LoginResult.Fail(LoginFailure.InvalidCredentials);

        await _users.ResetAccessFailedCountAsync(user);
        var (token, expires) = _tokens.Create(user, roles);
        return LoginResult.Ok(new LoginResponse(token, expires, user.FullName, user.Email!));
    }
}
