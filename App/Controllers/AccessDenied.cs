using Legendary.Data.Models;

namespace Legendary.Controllers
{
    public class AccessDenied : Controller
    {
        public AccessDenied(UserModel userModel) : base(userModel)
        {
            // Constructor logic if needed
        }


        public override string Render(string body = "")
        {
            return base.Render(AccessDenied<Login>());
        }
    }
}
