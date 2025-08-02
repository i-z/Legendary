using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Query.Models;
using Query.Data;

namespace Query
{
    public class Books
    {
        private readonly AppDbContext _context;

        public Books(AppDbContext context)
        {
            _context = context;
        }

        public int CreateBook(int userId, string title, bool favorite, int sort = 0)
        {
            var book = new Book
            {
                userId = userId,
                title = title,
                favorite = favorite,
                sort = sort
            };

            _context.Books.Add(book);
            _context.SaveChanges();

            return book.bookId;
        }

        public void TrashBook(int userId, int bookId)
        {
            // Assuming Trash means delete here (or you can implement soft-delete logic)
            var book = _context.Books.FirstOrDefault(b => b.bookId == bookId && b.userId == userId);
            if (book != null)
            {
                _context.Books.Remove(book);
                _context.SaveChanges();
            }
        }

        public void DeleteBook(int userId, int bookId)
        {
            TrashBook(userId, bookId); // Same behavior
        }

        public void UpdateBook(int userId, int bookId, string title)
        {
            var book = _context.Books.FirstOrDefault(b => b.bookId == bookId && b.userId == userId);
            if (book != null)
            {
                book.title = title;
                _context.SaveChanges();
            }
        }

        public void UpdateBookFavorite(int userId, int bookId, bool favorite)
        {
            var book = _context.Books.FirstOrDefault(b => b.bookId == bookId && b.userId == userId);
            if (book != null)
            {
                book.favorite = favorite;
                _context.SaveChanges();
            }
        }

        public void UpdateBookSort(int userId, int bookId, int sort)
        {
            var book = _context.Books.FirstOrDefault(b => b.bookId == bookId && b.userId == userId);
            if (book != null)
            {
                book.sort = sort;
                _context.SaveChanges();
            }
        }

        public Book GetDetails(int userId, int bookId)
        {
            return _context.Books.FirstOrDefault(b => b.bookId == bookId && b.userId == userId);
        }

        public List<Book> GetList(int userId, int sort = 0)
        {
            var query = _context.Books.Where(b => b.userId == userId);

            return sort == 0
                ? query.OrderBy(b => b.sort).ToList()
                : query.OrderByDescending(b => b.sort).ToList();
        }
    }
}
