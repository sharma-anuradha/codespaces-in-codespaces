﻿using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;

namespace Microsoft.VsCloudKernel.Services.Portal.WebSite
{
    public class Startup
    {
        private static readonly TimeSpan CacheControl_MaxAgeValue = TimeSpan.FromHours(2);

        public Startup(IConfiguration configuration, IWebHostEnvironment hostEnvironment)
        {
            Configuration = configuration;
            HostEnvironment = hostEnvironment;
        }

        private IWebHostEnvironment HostEnvironment { get; }

        public AppSettings AppSettings { get; set; }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddResponseCompression(options =>
            {
                options.Providers.Add<BrotliCompressionProvider>();
                options.Providers.Add<GzipCompressionProvider>();
            });
            services.AddControllersWithViews();
            services.AddRazorPages();

            // In production, the React files will be served from this directory
            services.AddSpaStaticFiles(configuration =>
            {
                configuration.RootPath = "ClientApp/build";
            });

            // Configuration
            var appSettingsConfiguration = Configuration.GetSection("AppSettings");
            var appSettings = appSettingsConfiguration.Get<AppSettings>();
            services.Configure<AppSettings>(appSettingsConfiguration);
            services.AddSingleton(appSettings);
            AppSettings = appSettings;

            //ConfigureAuthentication(services, appSettings);

            // Forwarded headers
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders =
                    ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            });
        }

        private void ConfigureAuthentication(IServiceCollection services, AppSettings appSettings)
        {
            // Add Data protection
            if (!HostEnvironment.IsDevelopment() || !appSettings.IsLocal)
            {
                var redis = ConnectionMultiplexer.Connect(appSettings.VsClkRedisConnectionString);
                services.AddDataProtection()
                    .SetApplicationName("VS Sass")
                    .PersistKeysToStackExchangeRedis(redis, "DataProtection-Keys");
            }
            else
            {
                services.AddDataProtection()
                    .SetApplicationName("VS Sass");
            }

            // AuthenticationConfigureAuthentication
            services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;

            })

            .AddCookie(options =>
            {
                options.LoginPath = "/login";
                options.LogoutPath = "/signout";
                options.AccessDeniedPath = "/accessdenied";
                options.Cookie.Name = ".AspNet.SharedCookie";
                options.Cookie.HttpOnly = true; // Not accessible to JavaScript
                options.Events.OnRedirectToLogin = ctx =>
                {
                    ctx.Response.StatusCode = 401;
                    return Task.CompletedTask;
                };
            })

            .AddMicrosoftAccount(options =>
            {
                options.CallbackPath = new PathString("/signin-microsoft");
                options.ClientId = appSettings.MicrosoftAppClientId;
                options.ClientSecret = appSettings.MicrosoftAppClientSecret;
                options.SaveTokens = true;
                options.Events.OnCreatingTicket = ctx =>
                {
                    var handler = new JwtSecurityToken(ctx.AccessToken);
                    var identity = handler.Claims.ToList();

                    // Tenant ID is required.
                    var tenantId = identity.Find(x => x.Type == "tid")?.Value;
                    if (tenantId == null)
                    {
                        return Task.FromException(new System.Exception("MissingTenantIdClaim"));
                    }

                    // Temporary: Check if the user is in the Microsoft tenant
                    if (!tenantId.Equals("72f988bf-86f1-41af-91ab-2d7cd011db47"))
                    {
                        return Task.FromException(new System.Exception("UnauthorizedTenant"));
                    }

                    // User ID is required.
                    // Try both AAD and MSA claims for identifying a user
                    var msaAltSecId = identity.Find(x => x.Type == "altsecid")?.Value;
                    var userId = identity.Find(x => x.Type == "oid")?.Value ?? msaAltSecId;
                    if (userId == null)
                    {
                        return Task.FromException(new System.Exception("MissingObjectIdClaim"));
                    }

                    // Use an underscore to separate the two sections because we use this as the primary key for a user
                    // in CosmosDB. Certain characters are not allowed in  CosmosDB keys, including /\?#
                    // See https://docs.microsoft.com/en-us/dotnet/api/microsoft.azure.documents.resource.id?view=azure-dotnet
                    var fullyQualifiedUserId = $"{tenantId}_{userId}";

                    var claimsIdentity = ctx.Principal.Identity as ClaimsIdentity;
                    claimsIdentity.AddClaim(new Claim(
                        "FullyQualifiedUserId",
                        fullyQualifiedUserId
                    ));

                    return Task.CompletedTask;
                };
            })
            ;
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseResponseCompression();

            app.UseForwardedHeaders();
            // TODO: the following is temporary until we get forwarded headers properly configured in nginx
            // For the short term this is safe as we know all public traffic is https
            // https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/proxy-load-balancer?view=aspnetcore-3.1
            if (!HostEnvironment.IsDevelopment() || !AppSettings.IsLocal)
            {
                app.Use((context, next) =>
                {
                    context.Request.Scheme = "https";
                    return next();
                });
            }


            if (env.IsDevelopment() || AppSettings.IsLocal)
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            //app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseSpaStaticFiles(new StaticFileOptions
            {
                OnPrepareResponse = (ctx) =>
                {
                    // TODO: Remove later.
                    ctx.Context.Response.Headers.Add("Service-Worker-Allowed", "/");
                }
            });
            app.UseRouting();
            app.Use(async (context, next) =>
            {
                if (!context.Request.Path.StartsWithSegments("/static", StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.GetTypedHeaders().CacheControl = 
                        new Microsoft.Net.Http.Headers.CacheControlHeaderValue()
                        {
                            Public = true,
                            MaxAge = CacheControl_MaxAgeValue
                        };
                }

                await next();
            });
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(routes =>
            {
                routes.MapControllerRoute("default", "{controller}/{action=Index}/{id?}");
            });
            app.UseSpa(spa =>
            {
                spa.Options.SourcePath = "ClientApp";
                if (AppSettings.IsLocal)
                {
                    // For development purposes, uncomment out if you want dotnet to load your react dev server, otherwise run 'yarn start' inside ClientApp
                    // spa.UseReactDevelopmentServer(npmScript: "start");
                    spa.UseProxyToSpaDevelopmentServer("http://localhost:3030");
                }
            });
        }
    }
}
