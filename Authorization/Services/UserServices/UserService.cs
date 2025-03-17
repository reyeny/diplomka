using Authorization.Context;
using Authorization.Dto;
using Authorization.Exceptions;
using Authorization.Helpers.UserHelpers;
using Authorization.Utilities.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Authorization.Services.UserServices;

public class UserService(UnchainMeDbContext dbContext) : IUserService
{
    public async Task<UserDto?> GetUserByIdAsync(string userId)
    {
        if (userId is null)
            throw new HttpException(StatusCodes.Status400BadRequest,
                "Неверно ввели данные");
        
        var userDto = await dbContext.Users.FirstOrDefaultAsync(user => user.Id == userId);
        return userDto?.UserToUserDto();
    }

    public async Task<List<UserDto?>> GetAllUserAsync()
    {
        var allUsers = await dbContext.Users.ToListAsync();
        return allUsers.UsersToUserDto();
    }

    public async Task<UserDto> PostUserAsync(UserDto userDto)
    {
        if (userDto is null)
            throw new HttpException(StatusCodes.Status400BadRequest,
                "Что-то пошло не так");
        
        var user = userDto.UserDtoToUser();

        if (await dbContext.Users.FindAsync(user.Email) != null)
            throw new HttpException(StatusCodes.Status400BadRequest,
                "Пользователь с такими данными существует ");

        await dbContext.Users.AddAsync(user);
        await dbContext.SaveChangesAsync();

        return user.UserToUserDto()!;
    }

    public async Task DeleteUserAsync(string userId)
    {
        if (userId is null)
            throw new HttpException(StatusCodes.Status400BadRequest,
                "Неверно ввели данные");

        var user = await dbContext.Users.FindAsync(userId);
        
        if (user == null)
            throw new HttpException(StatusCodes.Status400BadRequest,
                "Такой пользователь не существует");

        dbContext.Users.Remove(user);
        await dbContext.SaveChangesAsync();
    }

    public async Task<UserDto?> PutUserAsync(UserDto userDto)
    {
        if (userDto is null)
            throw new HttpException(StatusCodes.Status400BadRequest,
                "Что-то пошло не так");
        
        if (await dbContext.Users.FindAsync(userDto.Email) != null)
            throw new HttpException(StatusCodes.Status400BadRequest,
                "Пользователь с такими данными существует ");
        
        var user = await dbContext.Users.FindAsync(userDto.Id);
        
        if (user == null)
            throw new HttpException(StatusCodes.Status400BadRequest,
                "Такой пользователь не существует");
        
        var updateUser = userDto.UserDtoToUser();
        user.UpdateUser(updateUser);

        dbContext.Users.Update(user);
        await dbContext.SaveChangesAsync();

        return user.UserToUserDto();
    }
}