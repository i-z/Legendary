using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Query.Models;
using Query.Data;

namespace Query
{
    public class Chapters
    {
        private readonly AppDbContext _context;

        public Chapters(AppDbContext context)
        {
            _context = context;
        }

        public void CreateChapter(int bookId, int chapter, string title, string summary)
        {
            var ch = new Chapter
            {
                bookId = bookId,
                chapter = chapter,
                title = title,
                summary = summary
            };

            _context.Chapters.Add(ch);
            _context.SaveChanges();
        }

        public void TrashChapter(int bookId, int chapter, bool entries = false)
        {
            var ch = _context.Chapters.FirstOrDefault(c => c.bookId == bookId && c.chapter == chapter);
            if (ch != null)
            {
                _context.Chapters.Remove(ch);
                _context.SaveChanges();

                // Optional: delete associated entries if required
                if (entries)
                {
                    var relatedEntries = _context.Entries
                        .Where(e => e.bookId == bookId && e.chapter == chapter)
                        .ToList();

                    _context.Entries.RemoveRange(relatedEntries);
                    _context.SaveChanges();
                }
            }
        }

        public void RestoreChapter(int bookId, int chapter)
        {
            // This depends on having soft-delete (e.g., "isTrashed" flag).
            // If you're using hard deletes, restoring won't be possible.
            // You'd need to implement a "trash bin" table or flag.

            // Example (if soft-delete is implemented):
            /*
            var ch = _context.Chapters
                .IgnoreQueryFilters()
                .FirstOrDefault(c => c.bookId == bookId && c.chapter == chapter && c.isTrashed);
            if (ch != null)
            {
                ch.isTrashed = false;
                _context.SaveChanges();
            }
            */
        }

        public void DeleteChapter(int bookId, int chapter)
        {
            TrashChapter(bookId, chapter);
        }

        public void UpdateChapter(int bookId, int chapter, string title, string summary)
        {
            var ch = _context.Chapters.FirstOrDefault(c => c.bookId == bookId && c.chapter == chapter);
            if (ch != null)
            {
                ch.title = title;
                ch.summary = summary;
                _context.SaveChanges();
            }
        }

        public int GetMax(int bookId)
        {
            return _context.Chapters
                .Where(c => c.bookId == bookId)
                .Select(c => (int?)c.chapter)
                .Max() ?? 0;
        }

        public List<Chapter> GetList(int bookId)
        {
            return _context.Chapters
                .Where(c => c.bookId == bookId)
                .OrderBy(c => c.chapter)
                .ToList();
        }
    }
}
