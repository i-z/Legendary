using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Query.Models;
using Query.Data;

namespace Query
{
    public class Entries
    {
        private readonly AppDbContext _context;

        public Entries(AppDbContext context)
        {
            _context = context;
        }

        public int CreateEntry(int userId, int bookId, DateTime dateCreated, string title, string summary = "", int chapter = 0, int sort = 0)
        {
            var entry = new Entry
            {
                userId = userId,
                bookId = bookId,
                chapter = chapter,
                sort = sort,
                datecreated = dateCreated,
                datemodified = dateCreated,
                title = title,
                summary = summary
            };

            _context.Entries.Add(entry);
            _context.SaveChanges();

            return entry.entryId;
        }

        public int TrashEntry(int userId, int entryId)
        {
            var entry = _context.Entries.FirstOrDefault(e => e.entryId == entryId && e.userId == userId);
            if (entry != null)
            {
                _context.Entries.Remove(entry);
                _context.SaveChanges();
                return 1;
            }

            return 0;
        }

        public void RestoreEntry(int userId, int entryId)
        {
            // Only possible if soft-deleted support exists (e.g., isTrashed flag)
        }

        public void DeleteEntry(int userId, int entryId)
        {
            TrashEntry(userId, entryId); // Same behavior in this version
        }

        public Entry GetDetails(int userId, int entryId)
        {
            return _context.Entries.FirstOrDefault(e => e.entryId == entryId && e.userId == userId);
        }

        public Entry GetFirst(int userId, int bookId, int sort = 0)
        {
            var query = _context.Entries
                .Where(e => e.userId == userId && e.bookId == bookId);

            if (sort == 1)
                return query.OrderByDescending(e => e.sort).FirstOrDefault();

            return query.OrderBy(e => e.sort).FirstOrDefault();
        }

        public List<Entry> GetList(int userId, int bookId, int start = 1, int length = 50, int sort = 0)
        {
            var query = _context.Entries
                .Where(e => e.userId == userId && e.bookId == bookId);

            if (sort == 1)
                query = query.OrderByDescending(e => e.sort);
            else
                query = query.OrderBy(e => e.sort);

            return query
                .Skip(start - 1)
                .Take(length)
                .ToList();
        }

        public void UpdateBook(int userId, int entryId, int bookId)
        {
            var entry = _context.Entries.FirstOrDefault(e => e.entryId == entryId && e.userId == userId);
            if (entry != null)
            {
                entry.bookId = bookId;
                entry.datemodified = DateTime.UtcNow;
                _context.SaveChanges();
            }
        }

        public void UpdateChapter(int userId, int entryId, int chapter)
        {
            var entry = _context.Entries.FirstOrDefault(e => e.entryId == entryId && e.userId == userId);
            if (entry != null)
            {
                entry.chapter = chapter;
                entry.datemodified = DateTime.UtcNow;
                _context.SaveChanges();
            }
        }

        public void UpdateSummary(int userId, int entryId, string summary)
        {
            var entry = _context.Entries.FirstOrDefault(e => e.entryId == entryId && e.userId == userId);
            if (entry != null)
            {
                entry.summary = summary;
                entry.datemodified = DateTime.UtcNow;
                _context.SaveChanges();
            }
        }

        public void UpdateTitle(int userId, int entryId, string title)
        {
            var entry = _context.Entries.FirstOrDefault(e => e.entryId == entryId && e.userId == userId);
            if (entry != null)
            {
                entry.title = title;
                entry.datemodified = DateTime.UtcNow;
                _context.SaveChanges();
            }
        }

        public void Update(int entryId, int bookId, DateTime dateCreated, string title, string summary = "", int chapter = 0)
        {
            var entry = _context.Entries.FirstOrDefault(e => e.entryId == entryId);
            if (entry != null)
            {
                entry.bookId = bookId;
                entry.chapter = chapter;
                entry.datecreated = dateCreated;
                entry.datemodified = DateTime.UtcNow;
                entry.title = title;
                entry.summary = summary;
                _context.SaveChanges();
            }
        }
    }
}
