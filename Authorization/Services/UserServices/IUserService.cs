using Authorization.Dto;

namespace Authorization.Services.UserServices;

public interface IUserService
{
    Task<UserDto?> GetUserByIdAsync(string userId);
    Task<UserDto> PostUserAsync(UserDto userDto);
    Task DeleteUserAsync(string userId);
    Task<UserDto?> PutUserAsync(UserDto userDto);
}