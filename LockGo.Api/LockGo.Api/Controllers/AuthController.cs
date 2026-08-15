using LockGo.Application.DTOs;
using LockGo.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LockGo.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>Auto-signs the new account in — the response carries a token, same as /signin.</summary>
    [HttpPost("signup")]
    [ProducesResponseType<AuthResponseDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthResponseDto>> SignUp([FromBody] SignUpRequest request, CancellationToken ct)
    {
        var result = await _authService.SignUpAsync(request, ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPost("signin")]
    [ProducesResponseType<AuthResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponseDto>> SignIn([FromBody] SignInRequest request, CancellationToken ct)
    {
        var result = await _authService.SignInAsync(request, ct);
        return Ok(result);
    }
}
