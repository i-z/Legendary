using System;
using System.Collections.Generic;
using System.Text.Json;
using Legendary.Data.Models;

namespace Legendary.Services
{
    public class ChapterService : Service
    {
        private readonly ChapterModel _сharacterModel;
        private readonly UserModel _userModel;

         
        public ChapterService(ChapterModel characterModel, UserModel userModel) : base(userModel)
        {
            _сharacterModel = characterModel;
            _userModel = userModel;
        }

        public string GetList(int bookId)
        {
            if (!CheckSecurity()) { return AccessDenied(); }
            var list = new List<Chapter>();
            _сharacterModel.GetList(bookId).ForEach((Chapter chap) =>
            {
                list.Add(new Chapter()
                {
                    chapter = chap.chapter,
                    title = chap.title
                });
            });
            return JsonSerializer.Serialize(list);
        }

        public string GetMax(int bookId)
        {
            if (!CheckSecurity()) { return AccessDenied(); }
            return _сharacterModel.GetMax(bookId).ToString();

        }

        public string CreateChapter(int bookId, int chapter, string title, string summary)
        {
            if (!CheckSecurity()) { return AccessDenied(); }
            try
            {
                _сharacterModel.CreateChapter(bookId, chapter, title, summary);
            }
            catch (Exception)
            {
                return Error();
            }
            return Success();
        }
    }
}
