using Authorization.Exceptions;
using Authorization.Models;

namespace Authorization.Helpers.UserHelpers;

public static class UserHelpersUpdateUsers
{
    public static void UpdateUser(this User? user, User updateUser)
    {
        if (updateUser.Surname == null 
            && updateUser.Name == null 
            && updateUser.Email == null) 
            throw new HttpException(StatusCodes.Status400BadRequest,
                "Произошла неизвестная ошибка, повторите попытку");

        if (user == null) return;
        
        user.Email = updateUser.Email;
        user.Name = updateUser.Name;
        user.Surname = updateUser.Surname;
    }
}