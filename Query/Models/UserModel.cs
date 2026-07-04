using System;
using System.Collections.Generic;
using System.Linq;
using Legendary.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace Legendary.Data.Models
{
    public class UserModel
    {
        private readonly AppDbContext _context;

        public UserModel(AppDbContext context)
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

        public User GetById(int userId)
        {
            return _context.Users.FirstOrDefault(u => u.userId == userId);
        }

        public List<User> GetUsers(int? excludeUserId = null)
        {
            var query = _context.Users.AsQueryable();
            if (excludeUserId.HasValue)
            {
                query = query.Where(u => u.userId != excludeUserId.Value);
            }

            return query
                .OrderByDescending(u => u.usertype)
                .ThenBy(u => u.name)
                .ThenBy(u => u.email)
                .ToList();
        }

        public bool EmailExists(string email)
        {
            return _context.Users.Any(u => u.email == email);
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
            return _context.Users.Any(u => u.usertype == 1);
        }

        public int GetAdminCount()
        {
            return _context.Users.Count(u => u.usertype == 1);
        }

        public List<int> DeleteUserAndData(int userId)
        {
            var entryIds = _context.Entries
                .Where(e => e.userId == userId)
                .Select(e => e.entryId)
                .ToList();

            var authTokens = _context.AuthTokens.Where(t => t.UserId == userId);
            var entries = _context.Entries.Where(e => e.userId == userId);
            var books = _context.Books.Where(b => b.userId == userId);
            var user = _context.Users.FirstOrDefault(u => u.userId == userId);

            _context.AuthTokens.RemoveRange(authTokens);
            _context.Entries.RemoveRange(entries);
            _context.Books.RemoveRange(books);
            if (user != null)
            {
                _context.Users.Remove(user);
            }

            _context.SaveChanges();
            return entryIds;
        }
    }
}
