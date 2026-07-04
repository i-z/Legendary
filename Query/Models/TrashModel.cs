using System;
using System.Collections.Generic;
using System.Linq;
using Legendary.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace Legendary.Data.Models
{
    public class TrashModel
    {
        private readonly AppDbContext _context;

        public TrashModel(AppDbContext context)
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

        public void EmptySelected(int userId, List<int> bookIds, List<(int bookId, int chapter)> chapterIds, List<int> entryIds)
        {
            if (bookIds != null && bookIds.Count > 0)
            {
                var books = _context.Books.Where(b => b.userId == userId && b.isTrashed && bookIds.Contains(b.bookId));
                _context.Books.RemoveRange(books);
            }

            if (chapterIds != null && chapterIds.Count > 0)
            {
                foreach (var chapter in chapterIds)
                {
                    var ch = _context.Chapters.FirstOrDefault(c => c.bookId == chapter.bookId && c.chapter == chapter.chapter && c.isTrashed);
                    if (ch != null)
                    {
                        _context.Chapters.Remove(ch);
                    }
                }
            }

            if (entryIds != null && entryIds.Count > 0)
            {
                var entries = _context.Entries.Where(e => e.userId == userId && e.isTrashed && entryIds.Contains(e.entryId));
                _context.Entries.RemoveRange(entries);
            }

            _context.SaveChanges();
        }

        public void RestoreSelected(int userId, List<int> bookIds, List<(int bookId, int chapter)> chapterIds, List<int> entryIds)
        {
            if (bookIds != null && bookIds.Count > 0)
            {
                var books = _context.Books.Where(b => b.userId == userId && b.isTrashed && bookIds.Contains(b.bookId));
                foreach (var b in books)
                {
                    b.isTrashed = false;
                }
            }

            if (chapterIds != null && chapterIds.Count > 0)
            {
                foreach (var chapter in chapterIds)
                {
                    var ch = _context.Chapters.FirstOrDefault(c => c.bookId == chapter.bookId && c.chapter == chapter.chapter && c.isTrashed);
                    if (ch != null)
                    {
                        ch.isTrashed = false;
                    }
                }
            }

            if (entryIds != null && entryIds.Count > 0)
            {
                var entries = _context.Entries.Where(e => e.userId == userId && e.isTrashed && entryIds.Contains(e.entryId));
                foreach (var e in entries)
                {
                    e.isTrashed = false;
                }
            }

            _context.SaveChanges();
        }
    }
}
