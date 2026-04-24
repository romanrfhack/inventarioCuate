using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RefaccionariaCuate.Api.Contracts.Auth;
using RefaccionariaCuate.Application.Abstractions.Auth;
using RefaccionariaCuate.Infrastructure.Persistence;
using RefaccionariaCuate.Infrastructure.Services;

namespace RefaccionariaCuate.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(ApplicationDbContext dbContext, IJwtTokenGenerator jwtTokenGenerator) : ControllerBase
{
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.SingleOrDefaultAsync(x => x.UserName == request.UserName && x.Active, cancellationToken);
        if (user is null || !PasswordHasher.Verify(request.Password, user.PasswordHash))
        {
            return Unauthorized(new { message = "Credenciales inválidas" });
        }

        var token = jwtTokenGenerator.Generate(user);
        return Ok(new
        {
            accessToken = token,
            user = new { user.UserName, user.FullName, user.Role }
        });
    }
}
