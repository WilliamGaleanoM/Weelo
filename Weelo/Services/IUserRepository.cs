using Weelo.Models;

namespace Weelo
{
    public interface IUserRepository
    {
        UserDTO GetUser(UserModel userModel);
    }
}
