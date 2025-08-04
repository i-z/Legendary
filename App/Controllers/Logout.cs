using Legendary.Data.Models;

namespace Legendary.Controllers
{
    public class Logout : Controller
    {

        public Logout(UserModel userModel) : base(userModel)
        {
            // Constructor logic if needed
        }
        public override string Render(string body = "")
        {
            User.LogOut();

            return Redirect("/login");
        }
    }
}
