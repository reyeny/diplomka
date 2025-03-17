using Authorization.Dto;
using Authorization.Models;

namespace Authorization.Services.UserServices;

public interface IUserService
{
    Task<UserDto?> GetUserByIdAsync(string userId);
    Task<List<UserDto?>> GetAllUserAsync();
    Task<UserDto> PostUserAsync(UserDto userDto);
    Task DeleteUserAsync(string userId);
    Task<UserDto?> PutUserAsync(UserDto userDto);
}