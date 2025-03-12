using Microsoft.AspNetCore.Mvc;
using UserService.Application.DTOs;
using UserService.Application.Interfaces;
using UserService.Domain.Entities;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using UserService.Application.Exceptions;

namespace UserService.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        try
        {
            var token = await _authService.LoginAsync(dto);
            return Ok(new { Token = token });
        }
        catch (AuthException ex)
        {
            return Unauthorized(new { Error = ex.Message });
        }
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] UserDto dto)
    {
        try
        {
            var user = await _authService.RegisterAsync(dto);
            return Ok(new { user.Id, user.Email });
        }
        catch (AuthException ex)
        {
            return Conflict(new { Error = ex.Message });
        }
    }

    [HttpPost("confirm-email")]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmEmail([FromQuery] string token)
    {
        try
        {
            await _authService.ConfirmEmailAsync(token);
            return Ok(new { Message = "Email confirmed successfully" });
        }
        catch (AuthException ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }
}