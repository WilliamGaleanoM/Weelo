using Weelo.Models;
using System.Collections.Generic;
using System.Linq;

namespace Weelo
{
    public class UserRepository : IUserRepository
    {
        private readonly List<UserDTO> users = new();
        public UserRepository()
        {
            users.Add(new UserDTO { UserName = "william", Password = "William654321", Role = "manager" });
            users.Add(new UserDTO { UserName = "developer", Password = "@dev1234", Role = "developer" });
        }
        public UserDTO GetUser(UserModel userModel)
        {
            return users.Where(x => x.UserName.ToLower() == userModel.UserName.ToLower()
                && x.Password == userModel.Password).FirstOrDefault();
        }
    }
}
