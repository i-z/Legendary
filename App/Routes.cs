using System.Linq;
using Microsoft.AspNetCore.Http;
using Datasilk.Core.Web;
using Legendary;
using System;
using Microsoft.Extensions.DependencyInjection;

public class Routes : Datasilk.Core.Web.Routes
{
    public override IController FromControllerRoutes(HttpContext context, Parameters parameters, string name)
    {
        switch (name)
        {
            case "": case "home": default:

                var userModel = context.RequestServices.GetService<Legendary.Data.Models.UserModel>();

                var user = WebUser.Get(context, userModel);
                if (user.userId > 0)
                {
                    var dash = context.RequestServices.GetService<Legendary.Controllers.Dashboard>();
                    dash.User = user;
                    return dash;
                }
                else
                {
                    var login = context.RequestServices.GetService<Legendary.Controllers.Login>();
                    login.User = user;
                    return login;
                }
                
            case "dashboard": return context.RequestServices.GetService<Legendary.Controllers.Dashboard>();
            case "login": return context.RequestServices.GetService<Legendary.Controllers.Login>();
            case "logout": return context.RequestServices.GetService<Legendary.Controllers.Logout>();
            case "access-denied": return context.RequestServices.GetService<Legendary.Controllers.AccessDenied>();
            case "upload": return context.RequestServices.GetService<Legendary.Controllers.Upload>();
            case "file": return context.RequestServices.GetService<Legendary.Controllers.File>();
        }
    }

    public override IService FromServiceRoutes(HttpContext context, Parameters parameters, string name)
    {
        switch(name)
        {
            case "user": return context.RequestServices.GetService<Legendary.Services.UserService>();
            case "chapters": return context.RequestServices.GetService<Legendary.Services.ChapterService>();
            case "entries": return context.RequestServices.GetService<Legendary.Services.EntryService>();
            case "trash": return context.RequestServices.GetService<Legendary.Services.TrashService>();
                case "books": return context.RequestServices.GetService<Legendary.Services.BookService>();
            default: return null;
        }
    }

}