using System;
using System.IO;
using System.Net;
using System.Text;
using System.Security.Cryptography;
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
                    User.SetContentKey(GenerateContentKey(user.email, password));
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
                    usertype = 1,
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

        public string CreateUser(string name, string email, string password, bool isAdmin = false)
        {
            if (!CheckSecurity()) { return AccessDenied(); }
            if (User.userType != 1) { return AccessDenied(); }

            name = (name ?? "").Trim();
            email = (email ?? "").Trim().ToLower();

            if (name == "") { return BadRequest("You must provide a name"); }
            if (email == "") { return BadRequest("You must provide an email address"); }
            if (password == null || password.Length < 8) { return BadRequest("Password must be at least 8 characters long"); }
            if (_users.EmailExists(email)) { return BadRequest("A user with that email already exists"); }

            _users.CreateUser(new Legendary.Data.Models.User()
            {
                usertype = (short)(isAdmin ? 1 : 0),
                name = name,
                email = email,
                password = EncryptPassword(email, password),
                active = true
            });

            return Success();
        }

        public string GetUsers()
        {
            if (!CheckSecurity()) { return AccessDenied(); }
            if (User.userType != 1) { return AccessDenied(); }

            var users = _users.GetUsers(User.userId);
            var html = new StringBuilder();
            html.Append("<div class=\"row message hide\"><span></span></div>");

            if (users.Count == 0)
            {
                html.Append("<div class=\"row pad\">No other users found.</div>");
                return html.ToString();
            }

            users.ForEach((user) =>
            {
                var role = user.usertype == 1 ? "Administrator" : "User";
                var safeName = WebUtility.HtmlEncode(user.name);
                var safeEmail = WebUtility.HtmlEncode(user.email);
                var safeRole = WebUtility.HtmlEncode(role);
                html.Append("<div class=\"row hover pad\">");
                html.Append("<div class=\"col\">");
                html.Append("<strong>" + safeName + "</strong> <span class=\"faded\">(" + safeEmail + ")</span>");
                html.Append("</div>");
                html.Append("<div class=\"col right text-right\">");
                html.Append("<span class=\"faded pad-right-sm\">" + safeRole + "</span>");
                html.Append("<a href=\"javascript:\" class=\"button delete btn-delete-user\" data-userid=\"" + user.userId + "\" data-name=\"" + safeName + "\">Delete</a>");
                html.Append("</div>");
                html.Append("</div>");
            });

            return html.ToString();
        }

        public string DeleteUser(int userId)
        {
            if (!CheckSecurity()) { return AccessDenied(); }
            if (User.userType != 1) { return AccessDenied(); }
            if (userId <= 0) { return BadRequest("Invalid user"); }
            if (userId == User.userId) { return BadRequest("You cannot delete your own account"); }

            var target = _users.GetById(userId);
            if (target == null) { return BadRequest("User not found"); }
            if (target.usertype == 1 && _users.GetAdminCount() <= 1)
            {
                return BadRequest("You cannot delete the last administrator");
            }

            var entryIds = _users.DeleteUserAndData(userId);
            entryIds.ForEach((entryId) =>
            {
                var folder = Server.MapPath("/Content/files/" + entryId + "/");
                if (Directory.Exists(folder))
                {
                    Directory.Delete(folder, true);
                }
            });

            return Success();
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
            if (string.IsNullOrWhiteSpace(encrypted) || !encrypted.StartsWith("$2"))
            {
                return false;
            }

            try
            {
                return BCrypt.Net.BCrypt.Verify(email + Server.salt + password, encrypted);
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        private string GenerateContentKey(string email, string password)
        {
            var bytes = Encoding.UTF8.GetBytes(email + "|" + Server.salt + "|" + password);
            var hash = SHA256.HashData(bytes);
            return Convert.ToBase64String(hash);
        }
    }
}