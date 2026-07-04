using BCrypt.Net;
using Datasilk.Core.Web;
using Legendary.Data.Models;
using Legendary.ViewModels;
using System.Text;

namespace Legendary.Controllers
{
    public class Dashboard : Controller
    {
        private readonly BookModel _bookModel;
        private readonly EntryModel _entryModel;
        private readonly EntryViewModel _entryViewModel;
        private readonly TrashModel _trash;
        private readonly UserModel _userModel;

        public Dashboard(BookModel bookModel, EntryModel entryModel, EntryViewModel entryViewModel, TrashModel trash, UserModel userModel) : base(userModel)
        {
            _bookModel = bookModel;
            _entryModel = entryModel;
            _entryViewModel = entryViewModel;
            _trash = trash;
            _userModel = userModel;
        }


        public override string Render(string body = "")
        {
            if (!CheckSecurity()) { return AccessDenied<Login>(); }

            //add Scripts to page
            AddScript("/js/dashboard.js?v=" + Server.Version);
            AddCSS("/css/dashboard.css?v=" + Server.Version);

            var dash = new View("/Views/Dashboard/dashboard.html");

            //get list of books
            var html = new StringBuilder();
            var books = _bookModel.GetList(User.userId);
            if(books.Count > 0)
            {
                //books exist
                var list = new View("/Views/Books/list-item.html");
                var i = 0;
                books.ForEach((Book book) =>
                {
                    if (i == 0)
                    {
                        list["selected"] = "selected";
                    }
                    else
                    {
                        list["selected"] = "";
                    }
                    list["id"] = book.bookId.ToString();
                    list["title"] = book.title;
                    html.Append(list.Render());
                    i++;
                });
                dash["books"] = html.ToString();

                //get list of entries for top book
                var bookId = 0;
                var entryId = 0;
                if (books.Count > 0)
                {
                    bookId = books[0].bookId;
                    var first = _entryModel.GetFirst(User.userId, bookId, (int)EntryViewModel.SortType.byChapter);
                    var script = new StringBuilder("<script language=\"javascript\">S.entries.bookId=" + bookId + ";");
                    
                    if (first != null)
                    {
                        entryId = first.entryId;
                        //load content of first entry
                        dash["editor-content"] = _entryViewModel.LoadEntry(first.entryId, bookId);
                        script.Append("S.editor.entryId=" + entryId.ToString() + ";$('.editor').removeClass('hide');");
                    }
                    else
                    {
                        dash["no-entries"] = "hide";
                        script.Append("S.entries.noentries();");
                    }
                    Scripts.Append(script.ToString() + "S.dash.init();</script>");
                }
                dash["entries"] = _entryViewModel.GetList(User.userId, bookId, entryId, 1, 500, EntryViewModel.SortType.byChapter);
            }
            else
            {
                dash["no-books"] = "hide";
                dash["no-entries"] = "hide";
                dash["no-content"] = Server.LoadFileFromCache("/Views/Dashboard/templates/nobooks.html");

                Scripts.Append("<script language=\"javascript\">S.dash.init();</script>");
            }

            //get count for trash
            dash["trash-count"] = _trash.GetCount(User.userId).ToString();
            dash["admin-tools"] = User.userType == 1
                ? "<li class=\"menu-divider\"></li><li class=\"root item-newuser row hover\"><a href=\"javascript:\" class=\"btn-newuser\"><span class=\"col icon small pad-width\"><svg viewBox=\"0 0 32 32\"><use xlink:href=\"#icon-add\" x=\"0\" y=\"0\" width=\"32\" height=\"32\"></use></svg></span><span>Add User</span></a></li><li class=\"root item-users row hover\"><a href=\"javascript:\" class=\"btn-users\"><span class=\"col icon small pad-width\"><svg viewBox=\"0 0 32 32\"><use xlink:href=\"#icon-user\" x=\"0\" y=\"0\" width=\"32\" height=\"32\"></use></svg></span><span>Users</span></a></li><li class=\"menu-divider\"></li>"
                : "";

            //load script templates (for popups)
            dash["templates"] = 
                Server.LoadFileFromCache("/Views/Dashboard/templates/newbook.html") + 
                Server.LoadFileFromCache("/Views/Dashboard/templates/newentry.html") +
                Server.LoadFileFromCache("/Views/Dashboard/templates/newchapter.html") +
                Server.LoadFileFromCache("/Views/Dashboard/templates/newuser.html") +
                Server.LoadFileFromCache("/Views/Dashboard/templates/renamebook.html") +
                Server.LoadFileFromCache("/Views/Dashboard/templates/renameentry.html") +
                Server.LoadFileFromCache("/Views/Dashboard/templates/noentries.html");
            
            return base.Render(dash.Render());
        }
    }
}
