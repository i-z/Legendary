using Legendary.Data.Models;
using System;
using System.Text;

namespace Legendary.Services
{
    public class BookService : Service
    {
        private readonly BookModel _bookModel;

        
        public BookService(BookModel bookModel, UserModel userModel) : base(userModel)
        {
            _bookModel = bookModel;
        }

        public string CreateBook(string title)
        {
            if (!CheckSecurity()) { return AccessDenied(); }
            var bookId = 0;
            try { 
                bookId = _bookModel.CreateBook(User.userId, title, false);
            }
            catch (Exception)
            {
                return Error();
            }
            return "success|" + bookId + "|" + GetBooksList();
        }

        public string GetBooksList()
        {
            if (!CheckSecurity()) { return AccessDenied(); }
            var html = new StringBuilder();
            var view = new View("/Views/Books/list-item.html");
            var books = _bookModel.GetList(User.userId);
            books.ForEach((Book book) =>
            {
                view["id"] = book.bookId.ToString();
                view["title"] = book.title;
                html.Append(view.Render());
            });
            return html.ToString();
        }

        public string RenameBook(int bookId, string title)
        {
            if (!CheckSecurity()) { return AccessDenied(); }
            title = (title ?? "").Trim();
            if (bookId <= 0 || title == "") { return BadRequest("Please provide a valid book title"); }

            try
            {
                _bookModel.UpdateBook(User.userId, bookId, title);
            }
            catch (Exception)
            {
                return Error();
            }

            return Success();
        }

    }
}
