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

        private static readonly byte[] legacyAesKey = Convert.FromBase64String("w7NbbXYW9uNQGZ3tZ8EEMqzPAlkTf7m8YZkA34vSQQY=");
        private static readonly byte[] legacyAesIV = Convert.FromBase64String("H+8K1I8bK1Q3g+jZ+zX8eA==");
        private static readonly byte[] encryptedMagic = Encoding.ASCII.GetBytes("LGE1");
        private const int KdfIterations = 120000;


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

        public void SaveEntry(int userId, int entryId, string content, string contentKey)
        {
            if (userId == 0) return;
            if (string.IsNullOrWhiteSpace(contentKey))
            {
                throw new ServiceErrorException("Secure session expired. Please log out and sign in with password.");
            }

            var entry = _entryModel.GetDetails(userId, entryId);
            var path = "/Content/books/" + entry.bookId + "/";
            var fullPath = Server.MapPath(path);

            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
            }

            var encrypted = EncryptContent(Encoding.UTF8.GetBytes(content ?? ""), contentKey);
            File.WriteAllBytes(Path.Combine(fullPath, entryId + ".dat"), encrypted);
        }

        public string LoadEntry(int entryId, int bookId, string contentKey)
        {
            var path = "/Content/books/" + bookId + "/";
            var file = Server.MapPath(path + entryId + ".dat");

            if (!File.Exists(file))
                return "";

            try
            {
                var encryptedData = File.ReadAllBytes(file);
                if (IsEncryptedV1(encryptedData) && string.IsNullOrWhiteSpace(contentKey))
                {
                    throw new ServiceErrorException("Secure session expired. Please log out and sign in with password.");
                }
                if (TryDecryptContent(encryptedData, contentKey, out var content))
                {
                    return content;
                }
                throw new ServiceErrorException("Could not decrypt entry content");
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

        public void RenameEntry(int userId, int entryId, string title)
        {
            try
            {
                _entryModel.UpdateTitle(userId, entryId, title);
            }
            catch (Exception)
            {
                throw new ServiceErrorException("Error renaming entry");
            }
        }

        private byte[] GetBytes(string str)
        {
            byte[] bytes = new byte[str.Length * sizeof(char)];
            Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }

        private static byte[] EncryptContent(byte[] plaintext, string contentKey)
        {
            var salt = RandomNumberGenerator.GetBytes(16);
            var nonce = RandomNumberGenerator.GetBytes(12);
            var key = DeriveKey(contentKey, salt);
            var ciphertext = new byte[plaintext.Length];
            var tag = new byte[16];
            using var aes = new AesGcm(key, 16);
            aes.Encrypt(nonce, plaintext, ciphertext, tag);

            var result = new byte[encryptedMagic.Length + salt.Length + nonce.Length + tag.Length + ciphertext.Length];
            Buffer.BlockCopy(encryptedMagic, 0, result, 0, encryptedMagic.Length);
            Buffer.BlockCopy(salt, 0, result, encryptedMagic.Length, salt.Length);
            Buffer.BlockCopy(nonce, 0, result, encryptedMagic.Length + salt.Length, nonce.Length);
            Buffer.BlockCopy(tag, 0, result, encryptedMagic.Length + salt.Length + nonce.Length, tag.Length);
            Buffer.BlockCopy(ciphertext, 0, result, encryptedMagic.Length + salt.Length + nonce.Length + tag.Length, ciphertext.Length);
            return result;
        }

        private static bool TryDecryptContent(byte[] data, string contentKey, out string content)
        {
            content = "";
            if (data == null || data.Length == 0) { return true; }

            if (IsEncryptedV1(data))
            {
                if (string.IsNullOrWhiteSpace(contentKey))
                {
                    return false;
                }

                var offset = encryptedMagic.Length;
                var salt = data.Skip(offset).Take(16).ToArray();
                offset += 16;
                var nonce = data.Skip(offset).Take(12).ToArray();
                offset += 12;
                var tag = data.Skip(offset).Take(16).ToArray();
                offset += 16;
                var ciphertext = data.Skip(offset).ToArray();

                var plaintext = new byte[ciphertext.Length];
                var key = DeriveKey(contentKey, salt);
                using var aes = new AesGcm(key, 16);
                aes.Decrypt(nonce, ciphertext, tag, plaintext);
                content = Encoding.UTF8.GetString(plaintext);
                return true;
            }

            // legacy fallback
            using var legacyAes = Aes.Create();
            legacyAes.Key = legacyAesKey;
            legacyAes.IV = legacyAesIV;
            using var decryptor = legacyAes.CreateDecryptor(legacyAes.Key, legacyAes.IV);
            using var ms = new MemoryStream(data);
            using var cryptoStream = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var sr = new StreamReader(cryptoStream, Encoding.UTF8);
            content = sr.ReadToEnd();
            return true;
        }

        private static bool IsEncryptedV1(byte[] data)
        {
            if (data.Length < encryptedMagic.Length + 16 + 12 + 16) { return false; }
            for (var i = 0; i < encryptedMagic.Length; i++)
            {
                if (data[i] != encryptedMagic[i]) { return false; }
            }
            return true;
        }

        private static byte[] DeriveKey(string contentKey, byte[] salt)
        {
            using var kdf = new Rfc2898DeriveBytes(contentKey, salt, KdfIterations, HashAlgorithmName.SHA256);
            return kdf.GetBytes(32);
        }
    }
}
