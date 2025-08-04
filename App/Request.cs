using Legendary.Data.Models;

namespace Legendary
{
    public class Request : Datasilk.Core.Web.Request
    {
        private readonly UserModel _userModel;

        public Request(UserModel userModel) : base()
        {
            _userModel = userModel;
        }

        protected WebUser user;
        public WebUser User
        {
            get
            {
                if (user == null)
                {
                    user = WebUser.Get(Context, _userModel);
                }
                return user;
            }
            set
            {
                user = value;
            }
        }

        public virtual bool CheckSecurity()
        {
            return true;
        }
    }
}
