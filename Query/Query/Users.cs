using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Query.Models;
using Query.Data;

namespace Query
{
    public class UsersService
    {
        private readonly AppDbContext _context;

        public UsersService(AppDbContext context)
        {
            _context = context;
        }

        public int CreateUser(User user)
        {
            _context.Users.Add(user);
            _context.SaveChanges();
            return user.userId;
        }

        public User AuthenticateUser(string email, string password)
        {
            return _context.Users
                .FirstOrDefault(u => u.email == email && u.password == password && u.active);
        }

        public User AuthenticateUser(string token)
        {
            var now = DateTime.UtcNow;
            return _context.AuthTokens
                .Include(t => t.User)
                .Where(t => t.Token == token && t.ExpiresAt > now && t.User.active)
                .Select(t => t.User)
                .FirstOrDefault();
        }

        public string CreateAuthToken(int userId, int expireDays = 30)
        {
            var token = Guid.NewGuid().ToString("N");
            var authToken = new AuthToken
            {
                UserId = userId,
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddDays(expireDays)
            };
            _context.AuthTokens.Add(authToken);
            _context.SaveChanges();
            return token;
        }

        public void UpdatePassword(int userId, string password)
        {
            var user = _context.Users.Find(userId);
            if (user != null)
            {
                user.password = password;
                _context.SaveChanges();
            }
        }

        public string GetEmail(int userId)
        {
            return _context.Users
                .Where(u => u.userId == userId)
                .Select(u => u.email)
                .FirstOrDefault();
        }

        public string GetPassword(string email)
        {
            return _context.Users
                .Where(u => u.email == email)
                .Select(u => u.password)
                .FirstOrDefault();
        }

        public void UpdateEmail(int userId, string email)
        {
            var user = _context.Users.Find(userId);
            if (user != null)
            {
                user.email = email;
                _context.SaveChanges();
            }
        }

        public bool HasPasswords()
        {
            return _context.Users.Any(u => !string.IsNullOrEmpty(u.password));
        }

        public bool HasAdmin()
        {
            // Assuming usertype admin is some specific value, e.g., 1
            return _context.Users.Any(u => u.usertype == 1);
        }
    }
}
