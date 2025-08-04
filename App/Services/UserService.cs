using Legendary.Data.Models;

namespace Legendary.Services
{
    public class UserService : Service
    {
        private readonly UserModel _users;

        public UserService(UserModel users) : base(users)
        {
            _users = users;
        }


        public string Authenticate(string email, string password)
        {
            var encrypted = _users.GetPassword(email);
            if (!DecryptPassword(email, password, encrypted)) { return Error(); }
            {
                //password verified by Bcrypt
                var user = _users.AuthenticateUser(email, encrypted);
                if (user != null)
                {
                    User.LogIn(_users, user.userId, user.email, user.name, user.datecreated, "", user.usertype, user.photo);
                    User.Save(true);
                    return Success();
                }
            }
            return Error();
        }

        public string SaveAdminPassword(string password)
        {
            if (Server.resetPass == true)
            {
                var update = false; //security check
                var emailAddr = "";
                var adminId = 1;
                if (Server.resetPass == true)
                {
                    //securely change admin password
                    //get admin email address from database
                    emailAddr = _users.GetEmail(adminId);
                    if (emailAddr != "" && emailAddr != null) { update = true; }
                }
                if (update == true)
                {
                    _users.UpdatePassword(adminId, EncryptPassword(emailAddr, password));
                    Server.resetPass = false;
                }
                return Success();
            }
            Context.Response.StatusCode = 500;
            return "";
        }

        public string CreateAdminAccount(string name, string email, string password)
        {
            if (Server.hasAdmin == false && Server.environment == Server.Environment.development)
            {
                _users.CreateUser(new Legendary.Data.Models.User()
                {
                    name = name,
                    email = email,
                    password = EncryptPassword(email, password),
                    active = true,
                });
                Server.hasAdmin = true;
                Server.resetPass = false;
                return "success";
            }
            Context.Response.StatusCode = 500;
            return "";
        }

        public void LogOut()
        {
            User.LogOut();
        }

        public string EncryptPassword(string email, string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(email + Server.salt + password, Server.bcrypt_workfactor);

        }

        public bool DecryptPassword(string email, string password, string encrypted)
        {
            return BCrypt.Net.BCrypt.Verify(email + Server.salt + password, encrypted);
        }
    }
}