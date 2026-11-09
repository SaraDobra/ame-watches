using AmeWatches.Api.Models.Auth;
using AmeWatches.Api.Services.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AmeWatches.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    public AuthController(IAuthService auth) => _auth = auth;

    [HttpPost("login")]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _auth.Login(request);

        return result.Failure switch
        {
            null => Ok(result.Success),
            LoginFailure.LockedOut => StatusCode(StatusCodes.Status423Locked, new { message = "Llogaria është e mbyllur përkohësisht pas shumë përpjekjeve. Provoni pas 15 minutash." }),
            _ => Unauthorized(new { message = "Email-i ose fjalëkalimi nuk është i saktë." })
        };
    }
}
