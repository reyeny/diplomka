using Authorization.Dto;
using Authorization.Models;

namespace Authorization.Mappers;

public static class UserMapper
{
    public static UserDto UserToUserDto(this User user) => new UserDto
    {
        Id = user.Id,
        Email = user.Email!,
        Name = user.Name!,
        Surname = user.Surname!
    };
}