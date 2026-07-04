using Legendary.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Legendary.Services
{
    public class TrashService : Service
    {
        private readonly TrashModel _trash;
        private readonly UserModel _userModel;

       
        public TrashService(TrashModel trash, UserModel userModel) : base(userModel)
        {
            _trash = trash;
            _userModel = userModel;
        }

        public string LoadTrash()
        {
            if (!CheckSecurity()) { return AccessDenied(); }

            var view = new View("/Views/Trash/trash.html");
            var scaffHeader = new View("/Views/Trash/section-header.html");
            var scaffBook = new View("/Views/Trash/item-book.html");
            var scaffChapter = new View("/Views/Trash/item-chapter.html");
            var scaffEntry = new View("/Views/Trash/item-entry.html");

            var trash = _trash.GetList(User.userId);
            var content = new StringBuilder();

            //render list of books
            if(trash.Item1.Count > 0)
            {
                scaffHeader["title"] = "Books";
                content.Append(scaffHeader.Render() + "\n");
                foreach (var book in trash.Item1)
                {
                    var id = "b-" + book.bookId;
                    scaffBook["id"] = id;
                    scaffBook["title"] = book.title;
                    scaffBook["checkbox"] = Common.UI.Checkbox.Render("checkbox-" + id, false, "S.trash.select()");
                    content.Append(scaffBook.Render() + "\n");
                }
            }

            //render list of chapters
            if (trash.Item2.Count > 0)
            {
                scaffHeader["title"] = "Chapters";
                content.Append(scaffHeader.Render() + "\n");
                foreach (var chapter in trash.Item2)
                {
                    var id = "c-" + chapter.bookId + "-" + chapter.chapter;
                    scaffChapter["id"] = id;
                    scaffChapter["checkbox"] = Common.UI.Checkbox.Render("checkbox-" + id, false, "S.trash.select()");
                    scaffChapter["title"] = chapter.title;
                    content.Append(scaffChapter.Render() + "\n");
                }
            }

            //render list of entries
            if (trash.Item3.Count > 0)
            {
                scaffHeader["title"] = "Entries";
                content.Append(scaffHeader.Render() + "\n");
                foreach (var entry in trash.Item3)
                {
                    var id = "e-" + entry.entryId;
                    scaffEntry["id"] = id;
                    scaffEntry["title"] = entry.title;
                    scaffEntry["checkbox"] = Common.UI.Checkbox.Render("checkbox-" + id, false, "S.trash.select()");
                    scaffEntry["date-created"] = entry.datecreated.ToString("M/dd/yyyy");
                    content.Append(scaffEntry.Render() + "\n");
                }
            }

            view["content"] = content.ToString();

            return view.Render();
        }

        public string Empty()
        {
            if (!CheckSecurity()) { return AccessDenied(); }
            try
            {
                _trash.Empty(User.userId);
                return Success();
            }
            catch (Exception)
            {
                return Error();
            }
        }

        public string RestoreAll()
        {
            if (!CheckSecurity()) { return AccessDenied(); }
            try
            {
                _trash.RestoreAll(User.userId);
                return Success();
            }
            catch (Exception)
            {
                return Error();
            }
        }

        public string EmptySelected(string ids)
        {
            if (!CheckSecurity()) { return AccessDenied(); }
            try
            {
                ParseSelectedIds(ids, out var bookIds, out var chapterIds, out var entryIds);
                _trash.EmptySelected(User.userId, bookIds, chapterIds, entryIds);
                return Success();
            }
            catch (Exception)
            {
                return Error();
            }
        }

        public string RestoreSelected(string ids)
        {
            if (!CheckSecurity()) { return AccessDenied(); }
            try
            {
                ParseSelectedIds(ids, out var bookIds, out var chapterIds, out var entryIds);
                _trash.RestoreSelected(User.userId, bookIds, chapterIds, entryIds);
                return Success();
            }
            catch (Exception)
            {
                return Error();
            }
        }

        private static void ParseSelectedIds(string ids,
            out List<int> bookIds,
            out List<(int bookId, int chapter)> chapterIds,
            out List<int> entryIds)
        {
            bookIds = new List<int>();
            chapterIds = new List<(int bookId, int chapter)>();
            entryIds = new List<int>();

            if (string.IsNullOrWhiteSpace(ids)) { return; }

            var tokens = ids.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var token in tokens)
            {
                var parts = token.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (parts.Length < 2) { continue; }
                switch (parts[0].ToLowerInvariant())
                {
                    case "b":
                        if (parts.Length == 2 && int.TryParse(parts[1], out var bookId))
                        {
                            bookIds.Add(bookId);
                        }
                        break;
                    case "c":
                        if (parts.Length == 3
                            && int.TryParse(parts[1], out var chapterBookId)
                            && int.TryParse(parts[2], out var chapterNo))
                        {
                            chapterIds.Add((chapterBookId, chapterNo));
                        }
                        break;
                    case "e":
                        if (parts.Length == 2 && int.TryParse(parts[1], out var entryId))
                        {
                            entryIds.Add(entryId);
                        }
                        break;
                }
            }

            bookIds = bookIds.Distinct().ToList();
            chapterIds = chapterIds.Distinct().ToList();
            entryIds = entryIds.Distinct().ToList();
        }
    }
}
