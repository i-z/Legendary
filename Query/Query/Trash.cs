using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Query.Models;
using Query.Data;

namespace Query
{
    public class Trash
    {
        private readonly AppDbContext _context;

        public Trash(AppDbContext context)
        {
            _context = context;
        }

        public int GetCount(int userId)
        {
            return _context.Entries.Count(e => e.userId == userId && e.isTrashed)
                 //+ _context.Chapters.Count(c => c.userId == userId && c.isTrashed)
                 + _context.Books.Count(b => b.userId == userId && b.isTrashed);
        }

        public Tuple<List<Book>, List<Chapter>, List<Entry>> GetList(int userId)
        {
            var books = _context.Books
                .Where(b => b.userId == userId && b.isTrashed)
                .ToList();

            var chapters = _context.Chapters
                .Where(c => /*c.userId == userId &&*/ c.isTrashed)
                .ToList();

            var entries = _context.Entries
                .Where(e => e.userId == userId && e.isTrashed)
                .ToList();

            return Tuple.Create(books, chapters, entries);
        }

        public void Empty(int userId)
        {
            var books = _context.Books.Where(b => b.userId == userId && b.isTrashed);
            var chapters = _context.Chapters.Where(c => /*c.userId == userId && */ c.isTrashed);
            var entries = _context.Entries.Where(e => e.userId == userId && e.isTrashed);

            _context.Books.RemoveRange(books);
            _context.Chapters.RemoveRange(chapters);
            _context.Entries.RemoveRange(entries);

            _context.SaveChanges();
        }

        public void RestoreAll(int userId)
        {
            var books = _context.Books.Where(b => b.userId == userId && b.isTrashed);
            var chapters = _context.Chapters.Where(c => /*c.userId == userId && */ c.isTrashed);
            var entries = _context.Entries.Where(e => e.userId == userId && e.isTrashed);

            foreach (var b in books) b.isTrashed = false;
            foreach (var c in chapters) c.isTrashed = false;
            foreach (var e in entries) e.isTrashed = false;

            _context.SaveChanges();
        }
    }
}
