using Legendary.Data.Models;
using Legendary.ViewModels;
using System;

namespace Legendary.Services
{
    public class EntryService : Service
    {
        private readonly EntryViewModel _entryViewModel;
        private readonly UserModel _userModel;

        
        public EntryService(EntryViewModel entryViewModel, UserModel userModel) : base(userModel)
        {
            _entryViewModel = entryViewModel;
            _userModel = userModel;
        }

        public string GetList(int bookId, int entryId, int start = 1, int length = 500, int sort = 0)
        {
            if (!CheckSecurity()) { return AccessDenied(); }
            return _entryViewModel.GetList(User.userId, bookId, entryId, start, length, (EntryViewModel.SortType)sort);

        }

        public string CreateEntry(int bookId, string title, string summary, int chapter = 0, int sort = 0)
        {
            if (!CheckSecurity()) { return AccessDenied(); }
            try
            {
                var entryId = _entryViewModel.CreateEntry(User.userId, bookId, title, summary, chapter);
                return
                    entryId + "|" +
                    _entryViewModel.GetList(User.userId, bookId, entryId, 1, 500, (EntryViewModel.SortType)sort);
            }
            catch (ServiceErrorException ex)
            {
                return Error(ex.Message);
            }
        }

        public string SaveEntry(int entryId, string content)
        {
            if (!CheckSecurity()) { return AccessDenied(); }
            try
            {
                _entryViewModel.SaveEntry(User.userId, entryId, content);
            }
            catch (ServiceErrorException)
            {
                return Error("An error occurred while saving your entry");
            }
            return Success();
        }

        public string LoadEntry(int entryId, int bookId)
        {
            if (!CheckSecurity()) { return AccessDenied(); }
            try
            {
                return _entryViewModel.LoadEntry(entryId, bookId);
            }
            catch (ServiceErrorException ex)
            {
                return Error(ex.Message);
            }
        }

        public string LoadEntryInfo(int entryId, int bookId)
        {
            if (!CheckSecurity()) { return AccessDenied(); }
            try
            {
                return _entryViewModel.LoadEntryInfo(User.userId, entryId, bookId);
            }
            catch (ServiceErrorException ex)
            {
                return Error(ex.Message);
            }
        }

        public string UpdateEntryInfo(int entryId, int bookId, DateTime datecreated, string title, string summary, int chapter)
        {
            if (!CheckSecurity()) { return AccessDenied(); }
            try
            {
                _entryViewModel.UpdateEntryInfo(entryId, bookId, datecreated, title, summary, chapter);
                return Success();
            }
            catch (ServiceErrorException ex)
            {
                return Error(ex.Message);
            }
        }

        public string TrashEntry(int entryId)
        {
            if (!CheckSecurity()) { return AccessDenied(); }
            try
            {
                return _entryViewModel.TrashEntry(User.userId, entryId).ToString();
            }
            catch (ServiceErrorException ex)
            {
                return Error(ex.Message);
            }
        }
    }
}
