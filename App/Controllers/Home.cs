using Legendary.Data.Models;
using System.Text;

namespace Legendary.Controllers
{
    public class Home : Controller
    {

        public Home(UserModel userModel) : base(userModel)
        {
            // Constructor logic if needed
        }

        public override string Render(string body = "")
        {
            var html = new StringBuilder();
            html.Append(Redirect("/login"));
            return base.Render(html.ToString());
        }
    }
}
