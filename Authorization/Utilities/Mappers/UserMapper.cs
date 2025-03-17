using Authorization.Dto;
using Authorization.Models;

namespace Authorization.Utilities.Mappers;

public static class UserMapper
{
    public static User UserDtoToUser(this UserDto userDto)
    {
        return new User
        {
            Id = userDto.Id!,
            Email = userDto.Email,
            Name = userDto.Name,
            Surname = userDto.Surname,
        };
    }

    public static UserDto? UserToUserDto(this User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            Name = user.Name,
            Surname = user.Surname
        };
    }
    
    public static List<UserDto?> UsersToUserDto(this IEnumerable<User> users)
    {
        return users.Select(user => user.UserToUserDto()).ToList();
    }
}