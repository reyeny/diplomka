using Authorization.Dto;
using Authorization.Services.UserServices;
using Microsoft.AspNetCore.Mvc;
using Authorization.Services.AuthenticationServices;
using Microsoft.AspNetCore.Authorization;

namespace Authorization.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController(
    IAuthenticationService authenticationService,
    IUserService userService)
    : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("Login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request) =>
        Ok(await authenticationService.Login(request));

    [AllowAnonymous]
    [HttpPost("Register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request) =>
        Ok(await authenticationService.Register(request));

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<UserDto>> Post(UserDto userDto) =>
        Ok(await userService.PostUserAsync(userDto));

    [Authorize]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<IEnumerable<UserDto>>> Get(string id) =>
        Ok(await userService.GetUserByIdAsync(id));
    
    [Authorize]
    [HttpPut]
    public async Task<ActionResult<UserDto>> Put(UserDto userDto) =>
        Ok(await userService.PutUserAsync(userDto));

    [Authorize]
    [HttpDelete]
    public async Task<ActionResult> Delete(string id)
    {
        await userService.DeleteUserAsync(id);
        return Ok("Объект был удален");
    }

}