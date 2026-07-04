using Datasilk.Core.Extensions;
using Legendary.Controllers;
using Legendary.Data.Context;
using Legendary.Data.Models;
using Legendary.Services;
using Legendary.ViewModels;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;

namespace Legendary
{
    public class Startup
    {
        private static IConfigurationRoot config;

        public virtual void ConfigureServices(IServiceCollection services)
        {
            var dataProtectionPath = Path.Combine(AppContext.BaseDirectory, "DataProtection-Keys");
            Directory.CreateDirectory(dataProtectionPath);
            services.AddDataProtection()
                .SetApplicationName("Legendary")
                .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionPath));

            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite("Data Source=app.db"));


            //set up Server-side memory cache
            services.AddDistributedMemoryCache();
            services.AddMemoryCache();

            //configure request form options
            services.Configure<FormOptions>(x =>
            {
                x.ValueLengthLimit = int.MaxValue;
                x.MultipartBodyLengthLimit = int.MaxValue;
                x.MultipartHeadersLengthLimit = int.MaxValue;
            });

            //add session
            services.AddSession();

            //add health checks
            services.AddHealthChecks();

            services.AddScoped<BookModel>();
            services.AddScoped<ChapterModel>();
            services.AddScoped<EntryModel>();
            services.AddScoped<TrashModel>();
            services.AddScoped<UserModel>();

            services.AddScoped<EntryViewModel>();

            services.AddScoped<BookService>();
            services.AddScoped<ChapterService>();
            services.AddScoped<EntryService>();
            services.AddScoped<TrashService>();
            services.AddScoped<UserService>();

            services.AddScoped<Dashboard>();
            services.AddScoped<AccessDenied>();
            services.AddScoped<Controllers.File>();
            services.AddScoped<Home>();
            services.AddScoped<Login>();
            services.AddScoped<Logout>();
            services.AddScoped<Upload>();

        }

        public virtual void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            Server.IsDocker = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";

            //get environment based on application build
            switch (env.EnvironmentName.ToLower())
            {
                case "production":
                    Server.environment = Server.Environment.production;
                    break;
                case "staging":
                    Server.environment = Server.Environment.staging;
                    break;
                default:
                    Server.environment = Server.Environment.development;
                    break;
            }

            //load application-wide cache
            var configFile = "config" +
                (Server.IsDocker ? ".docker" : "") +
                (Server.environment == Server.Environment.production ? ".prod" : "") + ".json";

            var configBuilder = new ConfigurationBuilder();
            var baseConfigPath = Server.MapPath("config.json");
            var envConfigPath = Server.MapPath(configFile);
            configBuilder.AddJsonFile(baseConfigPath, optional: false, reloadOnChange: true);
            if (!string.Equals(baseConfigPath, envConfigPath, StringComparison.OrdinalIgnoreCase))
            {
                configBuilder.AddJsonFile(envConfigPath, optional: true, reloadOnChange: true);
            }
            config = configBuilder
                .AddEnvironmentVariables()
                .Build();

            Server.config = config;

            //configure Server defaults
            Server.hostUri = config.GetSection("hostUri").Value;
            var servicepaths = config.GetSection("servicePaths").Value;
            if (servicepaths != null && servicepaths != "")
            {
                Server.servicePaths = servicepaths.Replace(" ", "").Split(',');
            }
            if (config.GetSection("version").Value != null)
            {
                Server.Version = config.GetSection("version").Value;
            }


            //configure Server security
            Server.bcrypt_workfactor = int.Parse(config.GetSection("Encryption:bcrypt_work_factor").Value);
            Server.salt = config.GetSection("Encryption:salt").Value;

            //configure cookie-based authentication
            var expires = !string.IsNullOrWhiteSpace(config.GetSection("Session:Expires").Value) ? int.Parse(config.GetSection("Session:Expires").Value) : 60;

            //use session
            var sessionOpts = new SessionOptions();
            sessionOpts.Cookie.Name = "Legendary";
            sessionOpts.IdleTimeout = TimeSpan.FromMinutes(expires);

            app.UseSession(sessionOpts);

            //handle static files
            var provider = new FileExtensionContentTypeProvider();

            // Add static file mappings
            provider.Mappings[".svg"] = "image/svg";
            var options = new StaticFileOptions
            {
                ContentTypeProvider = provider
            };
            app.UseStaticFiles(options);

            //exception handling
            if (Server.environment == Server.Environment.development)
            {
                app.UseDeveloperExceptionPage(new DeveloperExceptionPageOptions
                {
                    SourceCodeLineCount = 10
                });
            }
            else
            {
                //use HTTPS
                app.UseHsts();
                app.UseHttpsRedirection();

                //use health checks
                app.UseHealthChecks("/health");
            }

            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            //set up database

            //run Datasilk Core MVC Middleware
            app.UseDatasilkMvc(new MvcOptions()
            {
                InvokeNext = false,
                WriteDebugInfoToConsole = true,
                IgnoreRequestBodySize = true,
                Routes = new Routes()
            });
            
            using (var scope = app.ApplicationServices.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Database.EnsureCreated(); // ��� db.Database.Migrate()

                var users = scope.ServiceProvider.GetRequiredService<UserModel>();
                Server.hasAdmin = users.HasAdmin();

                if (!Server.hasAdmin)
                {
                    var bootstrapName = config.GetSection("BootstrapAdmin:Name").Value;
                    var bootstrapEmail = config.GetSection("BootstrapAdmin:Email").Value;
                    var bootstrapPassword = config.GetSection("BootstrapAdmin:Password").Value;

                    if (!string.IsNullOrWhiteSpace(bootstrapName)
                        && !string.IsNullOrWhiteSpace(bootstrapEmail)
                        && !string.IsNullOrWhiteSpace(bootstrapPassword))
                    {
                        users.CreateUser(new User()
                        {
                            usertype = 1,
                            name = bootstrapName,
                            email = bootstrapEmail,
                            password = BCrypt.Net.BCrypt.HashPassword(
                                bootstrapEmail + Server.salt + bootstrapPassword,
                                Server.bcrypt_workfactor
                            ),
                            active = true
                        });
                        Server.hasAdmin = true;
                    }
                }
            }

            Console.WriteLine("Running Legendary Server in " + Server.environment.ToString() + " environment");
        }
    }
}
