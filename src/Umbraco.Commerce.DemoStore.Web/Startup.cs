using Flurl.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Commerce.DemoStore.Web.Component;
using Umbraco.Extensions;

namespace Umbraco.Commerce.DemoStore.Web
{
    public class Startup
    {
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _config;

        public Startup(IWebHostEnvironment webHostEnvironment, IConfiguration config)
        {
            _env = webHostEnvironment ?? throw new ArgumentNullException(nameof(webHostEnvironment));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public void ConfigureServices(IServiceCollection services)
        {
          
            services.AddRequestTimeouts();
            services.AddCors(options =>
            {
                options.AddPolicy("AllowReactApp", builder =>
                {
                    builder.WithOrigins(
                            "https://fashionhub.netlify.app/",
                            "http://localhost:3000", 
                            "https://localhost:3000"
                        )
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials(); 
                });
                
                options.AddPolicy("AllowAll", builder =>
                {
                    builder.AllowAnyOrigin()
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            });
            //services.AddHostedService<ProductImportBackgroundService>();
            services.AddTransient<ProductImporterLogic>();
            services.AddSingleton<BackgroundImportService>();
            //services.AddTransient<ProductImporterSosetaria>();
            //services.AddTransient<ProductImporterLogicOtterDays>();

            //services.AddTransient<ProductImportService>();


            services.AddUmbraco(_env, _config)
                .AddBackOffice()
                .AddWebsite()
                .AddDemoStore()
                .AddComposers()
                .AddDeliveryApi()
                .Build();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseCors("AllowAll");

            app.UseRouting();
            app.UseRequestTimeouts();
            app.Use(async (context, next) =>
            {
         
                context.Response.Headers.Add("Access-Control-Allow-Origin", "https://fashionhub.netlify.app");
                context.Response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
                context.Response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization, X-Requested-With");

                context.Response.Headers.Add("X-Frame-Options", "SAMEORIGIN");
                context.Response.Headers.Add("X-Content-Type-Options", "nosniff");

                if (context.Request.Method == "OPTIONS")
                {
                    context.Response.StatusCode = 200;
                    await context.Response.WriteAsync("");
                    return;
                }

                await next();
            });

            FlurlHttp.Clients.WithDefaults(cfg => cfg.OnError(async (req) =>
            {
                try
                {
                    ILogger<IFlurlRequest> logger = app.ApplicationServices.GetRequiredService<ILogger<IFlurlRequest>>();
                    string responseBody = await req.Response.GetStringAsync();
                    logger.LogError("Http request failed. Response body: \"{responseBody}\"", responseBody);
                }
                catch { }
            }));

            app.UseUmbraco()
                .WithMiddleware(u =>
                {
                    u.UseBackOffice();
                    u.UseWebsite();
                })
                .WithEndpoints(u =>
                {
                    u.UseInstallerEndpoints();
                    u.UseBackOfficeEndpoints();
                    u.UseWebsiteEndpoints();
                });
        }
    }
}