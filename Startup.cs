using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace ASPNETBoilerPlateTemplate
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();

            //This is how the website knows to redirect to the SSO page
            //and what to do when te user has signed in
            //If you do not need SSO, delete all lines refrencing authentication or
            //authorization in this file
            services.AddAuthentication(options =>
            {
                //This allows the user to skip the sign in if they have
                //cookies in their browser
                options.DefaultScheme = "Cookies";
                options.DefaultChallengeScheme = "oidc";
                //Add the authentication scheme to the browser cookies once
                //The sign in is complete 
            }).AddCookie().AddOpenIdConnect("oidc", options =>
            {
                //These next fields should only be edited through enviroment variables
                //Set these in OKD (or in properties/launchSettings.json for testing)
                //Ask an RTP for these values

                //This is the authority (the url for the sso)
                options.Authority= Environment.GetEnvironmentVariable("OIDC_AUTHORITY");
                //This is the id the OIDC service (in this case keycloak) knows this website by
                options.ClientId = Environment.GetEnvironmentVariable("CLIENT_ID");
                //This is the key the website sends the OIDC service to confirm it has permission
                //To use the soo service
                options.ClientSecret = Environment.GetEnvironmentVariable("CLIENT_SECRET");
            });

            //This ensures the redirect URI the website send the OIDC service usses https
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;
                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();
            });

            //Tell the website to require users to be signed in
            services.AddAuthorization (options =>
            {
                options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            //Ensures the redirect URI is https since asp likes to switch it to http
            app.Use((context, next) =>
            {
                context.Request.Scheme = "https";
                return next();
            });

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            //Telling the website that the user needs to be signed in again
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDefaultControllerRoute().RequireAuthorization();
            });
        }
    }
}
