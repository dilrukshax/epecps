using System.Security.Claims;
using Epecps.Application.DTOs.Auth;
using Epecps.Application.Exceptions;
using Epecps.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Epecps.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _authService.LoginAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (ConflictException ex)
        {
            return Conflict(new { code = ex.Code, message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("setup-password")]
    [AllowAnonymous]
    public async Task<IActionResult> SetupPassword([FromBody] SetupPasswordRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _authService.SetupPasswordAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (ConflictException ex)
        {
            return Conflict(new { code = ex.Code, message = ex.Message });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _authService.RegisterAsync(request, cancellationToken);
            return CreatedAtAction(nameof(Me), new { }, response);
        }
        catch (ConflictException ex)
        {
            return Conflict(new { code = ex.Code, message = ex.Message });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _authService.RefreshAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequestDto request, CancellationToken cancellationToken)
    {
        var userId = GetAuthenticatedUserId();
        await _authService.LogoutAsync(request, userId, cancellationToken);
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var userId = GetAuthenticatedUserId();
        var me = await _authService.GetCurrentUserAsync(userId, cancellationToken);
        return Ok(me);
    }

    private int GetAuthenticatedUserId()
    {
        var value = User.FindFirstValue("userId")
                    ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? User.FindFirstValue(ClaimTypes.Name);

        if (string.IsNullOrWhiteSpace(value) || !int.TryParse(value, out var userId))
        {
            throw new UnauthorizedAccessException("Authenticated user id is missing from token.");
        }

        return userId;
    }
}
