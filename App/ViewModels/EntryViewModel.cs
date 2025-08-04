using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using Legendary.Data.Models;

namespace Legendary.ViewModels
{
    public class EntryViewModel
    {
        public enum SortType
        {
            byChapter = 0,
            byNewest = 1,
            byOldest = 2,
            byTitle = 3
        }

        private static readonly byte[] aesKey = Convert.FromBase64String("w7NbbXYW9uNQGZ3tZ8EEMqzPAlkTf7m8YZkA34vSQQY=");
        private static readonly byte[] aesIV = Convert.FromBase64String("H+8K1I8bK1Q3g+jZ+zX8eA==");


        private readonly ChapterModel _chapterModle;
        private readonly EntryModel _entryModel;
        private readonly BookModel _bookModel;

        public EntryViewModel(ChapterModel characterModel, EntryModel entityModel, BookModel bookModel)
        {
            _chapterModle = characterModel;
            _entryModel = entityModel;
            _bookModel = bookModel;
        }


        public string GetList(int userId, int bookId, int entryId, int start = 1, int length = 500, SortType sort = 0)
        {
            var html = new StringBuilder();
            var entries = new View("/Views/Entries/entries.html");
            var item = new View("/Views/Entries/list-item.html");
            var chapter = new View("/Views/Entries/chapter.html");
            var chapterlist = _chapterModle.GetList(bookId);
            var list = _entryModel.GetList(userId, bookId, start, length, (int)sort);
            var chapterInc = -1;
            var chapterShown = list.Where(a => a.entryId == entryId).FirstOrDefault()?.chapter ?? 0;
            var entryIndex = 0;
            var book = _bookModel.GetDetails(userId, bookId);
            entries["book-title"] = book.title;

            if (list.Count > 0)
            {
                list.ForEach((entry) =>
                {
                    entryIndex++;
                    if (chapterInc != entry.chapter && sort == 0)
                    {
                        if (entry.chapter > 0)
                        {
                            //display chapter
                            chapter["chapter"] = "Chapter " + entry.chapter.ToString() + ": " +
                                chapterlist.Find((c) => { return c.chapter == entry.chapter; }).title;
                            chapter["id"] = entry.chapter.ToString();
                            if(chapterShown != 0 && chapterShown == entry.chapter)
                            {
                                chapter["expanded"] = "expanded";
                            }
                            html.Append(chapter.Render());
                            chapter.Clear();
                        }
                        chapterInc = entry.chapter;
                    }
                    if(chapterShown != 0 && entry.chapter != chapterShown)
                    {
                        item.Show("hide-entry");
                    }
                    item["id"] = entry.entryId.ToString();
                    item["chapter-id"] = entry.chapter.ToString();
                    item["selected"] = entry.entryId == entryId ? "selected" : entryId == 0 && entryIndex == 1 ? "selected" : "";
                    item["title"] = entry.title;
                    item["summary"] = entry.summary;
                    item["date-created"] = entry.datecreated.ToString("M/dd/yyyy");
                    html.Append(item.Render());
                    item.Clear();
                });
                entries["entries"] = html.ToString();
            }
            else
            {
                html.Append(Server.LoadFileFromCache("/Views/Entries/no-entries.html"));
            }

            return entries.Render();
        }

        public int CreateEntry(int userId, int bookId, string title, string summary, int chapter)
        {
            try
            {
                return _entryModel.CreateEntry(userId, bookId, DateTime.Now, title, summary, chapter);
            }
            catch (Exception)
            {
                throw new ServiceErrorException("Error creating new entry");
            }
        }

        public void SaveEntry(int userId, int entryId, string content)
        {
            if (userId == 0) return;

            var entry = _entryModel.GetDetails(userId, entryId);
            var path = "/Content/books/" + entry.bookId + "/";
            var fullPath = Server.MapPath(path);

            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
            }

            var data = Encoding.UTF8.GetBytes(content);

            using var aes = Aes.Create();
            aes.Key = aesKey;
            aes.IV = aesIV;

            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream();
            using (var cryptoStream = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            {
                cryptoStream.Write(data, 0, data.Length);
            }

            File.WriteAllBytes(Path.Combine(fullPath, entryId + ".dat"), ms.ToArray());
        }

        public string LoadEntry(int entryId, int bookId)
        {
            var path = "/Content/books/" + bookId + "/";
            var file = Server.MapPath(path + entryId + ".dat");

            if (!File.Exists(file))
                return "";

            try
            {
                var encryptedData = File.ReadAllBytes(file);

                using var aes = Aes.Create();
                aes.Key = aesKey;
                aes.IV = aesIV;

                using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                using var ms = new MemoryStream(encryptedData);
                using var cryptoStream = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
                using var sr = new StreamReader(cryptoStream, Encoding.UTF8);

                return sr.ReadToEnd();
            }
            catch (IOException)
            {
                throw new ServiceErrorException("Could not read file");
            }
            catch (CryptographicException)
            {
                throw new ServiceErrorException("Could not decrypt file");
            }
        }

        public string LoadEntryInfo(int userId, int entryId, int bookId)
        {
            var info = new View("/Views/Dashboard/templates/entryinfo.html");
            var details = _entryModel.GetDetails(userId, entryId);
            info["title"] = details.title.Replace("\"", "&quot;");
            info["summary"] = details.summary.Replace("\"", "&quot;");
            info["datecreated"] = details.datecreated.ToString("M/dd/yyyy h:mm:ss tt");

            //get list of chapters
            var chapters = new StringBuilder();
            chapters.Append("<option value=\"0\">[No Chapter]</option>");
            _chapterModle.GetList(bookId).ForEach((chapter) =>
            {
                chapters.Append("<option value=\"" + chapter.chapter + "\"" + (details.chapter == chapter.chapter ? " selected" : "") + ">" + chapter.chapter + ": " + chapter.title + "</option>");
            });

            //get list of books
            var books = new StringBuilder();
            _bookModel.GetList(bookId).ForEach((book) =>
            {
                books.Append("<option value=\"" + book.bookId + "\"" + (book.bookId == bookId ? " selected" : "") + ">" + book.title + "</option>");
            });

            info["chapters"] = chapters.ToString();
            info["books"] = books.ToString();
            return info.Render();
        }

        public void UpdateEntryInfo(int entryId, int bookId, DateTime datecreated, string title, string summary, int chapter)
        {
            try
            {
                _entryModel.Update(entryId, bookId, datecreated, title, summary, chapter);
            }
            catch (Exception)
            {
                throw new ServiceErrorException("Error updating existing entry");
            }
        }

        public int TrashEntry(int userId, int entryId)
        {
            return _entryModel.TrashEntry(userId, entryId);
        }

        private byte[] GetBytes(string str)
        {
            byte[] bytes = new byte[str.Length * sizeof(char)];
            Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }
    }
}
