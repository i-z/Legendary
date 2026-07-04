using System.Collections.Generic;
using System.Linq;
using Legendary.Data.Context;

namespace Legendary.Data.Models
{
    public class ChapterModel
    {
        private readonly AppDbContext _context;

        public ChapterModel(AppDbContext context)
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
            var ch = _context.Chapters.FirstOrDefault(c => c.bookId == bookId && c.chapter == chapter && !c.isTrashed);
            if (ch != null)
            {
                ch.isTrashed = true;

                // Optional: delete associated entries if required
                if (entries)
                {
                    var relatedEntries = _context.Entries
                        .Where(e => e.bookId == bookId && e.chapter == chapter && !e.isTrashed)
                        .ToList();

                    foreach (var entry in relatedEntries)
                    {
                        entry.isTrashed = true;
                    }
                }

                _context.SaveChanges();
            }
        }

        public void RestoreChapter(int bookId, int chapter)
        {
            var ch = _context.Chapters
                .FirstOrDefault(c => c.bookId == bookId && c.chapter == chapter && c.isTrashed);
            if (ch != null)
            {
                ch.isTrashed = false;
                _context.SaveChanges();
            }
        }

        public void DeleteChapter(int bookId, int chapter)
        {
            TrashChapter(bookId, chapter);
        }

        public void UpdateChapter(int bookId, int chapter, string title, string summary)
        {
            var ch = _context.Chapters.FirstOrDefault(c => c.bookId == bookId && c.chapter == chapter && !c.isTrashed);
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
                .Where(c => c.bookId == bookId && !c.isTrashed)
                .Select(c => (int?)c.chapter)
                .Max() ?? 0;
        }

        public List<Chapter> GetList(int bookId)
        {
            return _context.Chapters
                .Where(c => c.bookId == bookId && !c.isTrashed)
                .OrderBy(c => c.chapter)
                .ToList();
        }
    }
}
