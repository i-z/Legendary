using System;
using System.Collections.Generic;
using System.Linq;
using Legendary.Data.Context;

namespace Legendary.Data.Models
{
    public class EntryModel
    {
        private readonly AppDbContext _context;

        public EntryModel(AppDbContext context)
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
                entry.isTrashed = true;
                entry.datemodified = DateTime.UtcNow;
                _context.SaveChanges();
            }

            return _context.Entries.Count(e => e.userId == userId && e.isTrashed);
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
            return _context.Entries.FirstOrDefault(e => e.entryId == entryId && e.userId == userId && !e.isTrashed);
        }

        public Entry GetFirst(int userId, int bookId, int sort = 0)
        {
            var query = _context.Entries
                .Where(e => e.userId == userId && e.bookId == bookId && !e.isTrashed);

            switch (sort)
            {
                case 1: // byNewest
                    return query.OrderByDescending(e => e.datecreated).FirstOrDefault();
                case 2: // byOldest
                    return query.OrderBy(e => e.datecreated).FirstOrDefault();
                case 3: // byTitle
                    return query.OrderBy(e => e.title).FirstOrDefault();
                case 0: // byChapter
                default:
                    return query
                        .OrderBy(e => e.chapter)
                        .ThenBy(e => e.sort)
                        .ThenByDescending(e => e.datecreated)
                        .FirstOrDefault();
            }
        }

        public List<Entry> GetList(int userId, int bookId, int start = 1, int length = 50, int sort = 0)
        {
            var query = _context.Entries
                .Where(e => e.userId == userId && e.bookId == bookId && !e.isTrashed);

            switch (sort)
            {
                case 1: // byNewest
                    query = query.OrderByDescending(e => e.datecreated);
                    break;
                case 2: // byOldest
                    query = query.OrderBy(e => e.datecreated);
                    break;
                case 3: // byTitle
                    query = query.OrderBy(e => e.title);
                    break;
                case 0: // byChapter
                default:
                    query = query
                        .OrderBy(e => e.chapter)
                        .ThenBy(e => e.sort)
                        .ThenByDescending(e => e.datecreated);
                    break;
            }

            return query
                .Skip(start - 1)
                .Take(length)
                .ToList();
        }

        public void UpdateBook(int userId, int entryId, int bookId)
        {
            var entry = _context.Entries.FirstOrDefault(e => e.entryId == entryId && e.userId == userId && !e.isTrashed);
            if (entry != null)
            {
                entry.bookId = bookId;
                entry.datemodified = DateTime.UtcNow;
                _context.SaveChanges();
            }
        }

        public void UpdateChapter(int userId, int entryId, int chapter)
        {
            var entry = _context.Entries.FirstOrDefault(e => e.entryId == entryId && e.userId == userId && !e.isTrashed);
            if (entry != null)
            {
                entry.chapter = chapter;
                entry.datemodified = DateTime.UtcNow;
                _context.SaveChanges();
            }
        }

        public void UpdateSummary(int userId, int entryId, string summary)
        {
            var entry = _context.Entries.FirstOrDefault(e => e.entryId == entryId && e.userId == userId && !e.isTrashed);
            if (entry != null)
            {
                entry.summary = summary;
                entry.datemodified = DateTime.UtcNow;
                _context.SaveChanges();
            }
        }

        public void UpdateTitle(int userId, int entryId, string title)
        {
            var entry = _context.Entries.FirstOrDefault(e => e.entryId == entryId && e.userId == userId && !e.isTrashed);
            if (entry != null)
            {
                entry.title = title;
                entry.datemodified = DateTime.UtcNow;
                _context.SaveChanges();
            }
        }

        public void Update(int entryId, int bookId, DateTime dateCreated, string title, string summary = "", int chapter = 0)
        {
            var entry = _context.Entries.FirstOrDefault(e => e.entryId == entryId && !e.isTrashed);
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
