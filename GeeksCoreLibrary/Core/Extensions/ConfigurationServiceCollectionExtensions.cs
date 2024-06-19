using System;
using System.Collections.Generic;
using System.Reflection;
using GeeksCoreLibrary.Components.WebPage.Interfaces;
using GeeksCoreLibrary.Components.WebPage.Middlewares;
using GeeksCoreLibrary.Components.WebPage.Services;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Interfaces;
using GeeksCoreLibrary.Core.Middlewares;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Core.Services;
using GeeksCoreLibrary.Modules.ItemFiles.Interfaces;
using GeeksCoreLibrary.Modules.ItemFiles.Services;
using GeeksCoreLibrary.Modules.Languages.Interfaces;
using GeeksCoreLibrary.Modules.Languages.Services;
using GeeksCoreLibrary.Modules.Objects.Interfaces;
using GeeksCoreLibrary.Modules.Objects.Services;
using GeeksCoreLibrary.Modules.Payments.Interfaces;
using GeeksCoreLibrary.Modules.Redirect.Interfaces;
using GeeksCoreLibrary.Modules.Redirect.Middlewares;
using GeeksCoreLibrary.Modules.Redirect.Services;
using GeeksCoreLibrary.Modules.Seo.Interfaces;
using GeeksCoreLibrary.Modules.Seo.Services;
using GeeksCoreLibrary.Modules.Templates.Controllers;
using GeeksCoreLibrary.Modules.Templates.Interfaces;
using GeeksCoreLibrary.Modules.Templates.Middlewares;
using GeeksCoreLibrary.Modules.Templates.Services;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using GeeksCoreLibrary.Components.DataSelectorParser.Interfaces;
using GeeksCoreLibrary.Components.DataSelectorParser.Services;
using GeeksCoreLibrary.Components.OrderProcess.Interfaces;
using GeeksCoreLibrary.Components.OrderProcess.Middlewares;
using GeeksCoreLibrary.Components.OrderProcess.Services;
using GeeksCoreLibrary.Components.ShoppingBasket.Interfaces;
using GeeksCoreLibrary.Components.ShoppingBasket.Services;
using GeeksCoreLibrary.Modules.Barcodes.Interfaces;
using GeeksCoreLibrary.Modules.Barcodes.Services;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.Databases.Services;
using GeeksCoreLibrary.Modules.HealthChecks.Services;
using GeeksCoreLibrary.Modules.ItemFiles.Middlewares;
using JetBrains.Annotations;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WebMarkupMin.AspNetCore7;
using WebMarkupMin.Core;

[assembly: AspMvcAreaViewLocationFormat("/Modules/{2}/Views/{1}/{0}.cshtml")]
[assembly: AspMvcAreaViewLocationFormat("/Modules/{2}/Views/Shared/{0}.cshtml")]
[assembly: AspMvcAreaViewLocationFormat("/Core/Views/{1}/{0}.cshtml")]
[assembly: AspMvcAreaViewLocationFormat("/Core/Views/Shared/{0}.cshtml")]
[assembly: AspMvcViewLocationFormat("/Core/Views/{1}/{0}.cshtml")]
[assembly: AspMvcViewLocationFormat("/Core/Views/Shared/{0}.cshtml")]

namespace GeeksCoreLibrary.Core.Extensions
{
    public static class ConfigurationServiceCollectionExtensions
    {
        /// <summary>
        /// Adds middle wares that are needed for the GCL.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="env"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseGclMiddleware(this IApplicationBuilder builder, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                builder.UseDeveloperExceptionPage();
            }
            else
            {
                builder.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                builder.UseHsts();
            }

            builder.UseMiddleware<RequestLoggingMiddleware>();

            builder.UseStatusCodePagesWithReExecute("/webpage.gcl", "?errorCode={0}");

            builder.UseSession();

            builder.UseMiddleware<IpAccessMiddleware>();
            builder.UseMiddleware<ClearCacheMiddleware>();

            builder.UseMiddleware<RedirectMiddleWare>();

            builder.UseWebMarkupMin();
            builder.UseMiddleware<WiserItemFilesMiddleware>();
            builder.UseMiddleware<RewriteUrlToOrderProcessMiddleware>();
            builder.UseMiddleware<RewriteUrlToWebPageMiddleware>();
            builder.UseMiddleware<RewriteUrlToTemplateMiddleware>();

            builder.UseMiddleware<AddAntiForgeryMiddleware>();
            builder.UseMiddleware<OutputCachingMiddleware>();
            builder.UseStaticFiles();

            builder.UseRouting();

            builder.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");

                endpoints.MapHealthChecks("/health", new HealthCheckOptions
                {
                    Predicate = _ => true,
                    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                });

                endpoints.MapHealthChecks("/health/wts", new HealthCheckOptions()
                {
                    Predicate = healthCheck => healthCheck.Tags.Contains("WTS"),
                    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                });
            });

            builder.HandleStartupFunctions();

            return builder;
        }

        /// <summary>
        /// Handle and execute some functions that are needed to be done during startup of the application.
        /// Don't call this method if you're already calling UseGclMiddleware, because this is called inside that.
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IApplicationBuilder HandleStartupFunctions(this IApplicationBuilder builder)
        {
            var applicationLifetime = builder.ApplicationServices.GetService<IHostApplicationLifetime>();
            applicationLifetime.ApplicationStarted.Register(async () =>
            {
                // Make sure all important tables exist and are up to date, while starting the application.
                try
                {
                    using var scope = builder.ApplicationServices.CreateScope();
                    var databaseHelpersService = scope.ServiceProvider.GetRequiredService<IDatabaseHelpersService>();

                    var gclSettings = scope.ServiceProvider.GetRequiredService<IOptions<GclSettings>>();
                    var tablesToUpdate = new List<string>
                    {
                        WiserTableNames.WiserEntity,
                        WiserTableNames.WiserPermission,
                        WiserTableNames.WiserTemplate,
                        WiserTableNames.WiserDynamicContent,
                        WiserTableNames.WiserTemplateDynamicContent,
                        WiserTableNames.WiserTemplateExternalFiles,
                        WiserTableNames.WiserItem,
                        WiserTableNames.WiserItemDetail,
                        WiserTableNames.WiserItemFile,
                        WiserTableNames.WiserItemLink,
                        WiserTableNames.WiserItemLinkDetail
                    };

                    if (gclSettings.Value.LogOpeningAndClosingOfConnections)
                    {
                        tablesToUpdate.Add(Modules.Databases.Models.Constants.DatabaseConnectionLogTableName);
                    }

                    if (gclSettings.Value.RequestLoggingOptions.Enabled)
                    {
                        tablesToUpdate.Add(WiserTableNames.GclRequestLog);
                    }

                    await databaseHelpersService.CheckAndUpdateTablesAsync(tablesToUpdate);
                }
                catch (Exception exception)
                {
                    builder.ApplicationServices.GetService<ILogger>().LogError(exception, "Error while updating tables.");
                }
            });

            return builder;
        }

        /// <summary>
        /// Adds all services that are required to run the GCL on a website or API.
        /// This handles the GclSettings, MVC, where to find the views of the GCL and dependency injection.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> of the startup.</param>
        /// <param name="configuration">The <see cref="IConfiguration"/> for reading the app settings.</param>
        /// <param name="useCaching">Whether or not to use caching. Default is true.</param>
        /// <param name="isApi">Set this to true if you're using the GCL in an API, so that no XSRF protection will be added.</param>
        /// <returns>The <see cref="IServiceCollection"/> of the startup.</returns>
        public static IServiceCollection AddGclServices(this IServiceCollection services, IConfiguration configuration, bool useCaching = true, bool isApi = false, bool isWeb = true)
        {
            // MVC looks in the directory "Areas" by default, but we use the directory "Modules", so we have to tell MC that.
            if (isWeb)
            {
                services.Configure<RazorViewEngineOptions>(options =>
                {
                    options.AreaViewLocationFormats.Add("/Modules/{2}/Views/{1}/{0}" + RazorViewEngine.ViewExtension);
                    options.AreaViewLocationFormats.Add("/Modules/{2}/Views/Shared/{0}" + RazorViewEngine.ViewExtension);
                    options.AreaViewLocationFormats.Add("/Core/Views/{1}/{0}" + RazorViewEngine.ViewExtension);
                    options.AreaViewLocationFormats.Add("/Core/Views/Shared/{0}" + RazorViewEngine.ViewExtension);

                    options.ViewLocationFormats.Add("/Core/Views/{1}/{0}" + RazorViewEngine.ViewExtension);
                    options.ViewLocationFormats.Add("/Core/Views/Shared/{0}" + RazorViewEngine.ViewExtension);
                });
            }

            // Use the options pattern for all GCL settings in appSettings.json.
            var configurationSection = configuration.GetSection("GCL");
            services.Configure<GclSettings>(configurationSection);
            var gclSettings = configurationSection.Get<GclSettings>();

            // Add MySql health checks.
            if (isWeb)
            {
                services.AddHealthChecks()
                    .AddMySql(gclSettings.ConnectionString, name: "MySqlRead")
                    .AddCheck<WtsHealthService>("WTS Health Check", HealthStatus.Degraded, new []{"WTS", "Wiser Task Scheduler"});
                if (!String.IsNullOrWhiteSpace(gclSettings.ConnectionStringForWriting))
                {
                    services.AddHealthChecks().AddMySql(gclSettings.ConnectionString, name: "MySqlWrite");
                }

                // Set default settings for JSON.NET.
                JsonConvert.DefaultSettings = () => new JsonSerializerSettings {NullValueHandling = NullValueHandling.Ignore};

                // Default MVC controllers and tell them to use JSON.NET instead of the default dotnet Core JSON.
                // Also add a global filter to validate anti forgery tokens, to protect against CSRF attacks.
                if (!isApi && !gclSettings.DisableXsrfProtection)
                {
                    services.AddControllersWithViews(options => options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute())).AddNewtonsoftJson();
                    services.AddAntiforgery(options =>
                    {
                        options.HeaderName = "X-CSRF-TOKEN";
                        options.Cookie.Name = "CSRF-TOKEN";
                        options.SuppressXFrameOptionsHeader = gclSettings.SuppressXFrameOptionHeader;
                    });
                }
                else
                {
                    services.AddControllersWithViews().AddNewtonsoftJson();
                    // the call to AddControllersWithViews() (or AddMvc() for that matter) will always call AddAntiforgery() no matter what, so DisableXsrfProtection might need another look
                    // setting the XFrameOptions setting here as well makes sure this setting will always work no matter what happens with DisableXsrfProtection
                    services.AddAntiforgery(options =>
                    {
                        options.SuppressXFrameOptionsHeader = gclSettings.SuppressXFrameOptionHeader;
                    });
                }

                // Let MVC know about the GCL controllers.
                services.AddMvc().AddApplicationPart(typeof(TemplatesController).GetTypeInfo().Assembly);

                // Enable HTML output minifier.
                services.AddWebMarkupMin(
                        options =>
                        {
                            options.AllowMinificationInDevelopmentEnvironment = false;
                            options.AllowCompressionInDevelopmentEnvironment = false;
                        })
                    .AddHtmlMinification(
                        options =>
                        {
                            options.MinificationSettings.RemoveRedundantAttributes = true;
                            options.MinificationSettings.MinifyInlineCssCode = true;
                            options.MinificationSettings.MinifyInlineJsCode = true;
                            options.MinificationSettings.PreservableAttributeCollection.Add(new HtmlAttributeExpression("input", "type", "text"));
                        })
                    .AddHttpCompression();
            }

            // Enable caching.
            services.AddLazyCache();

            // Enable session.
            if (isWeb)
            {
                services.AddSession(options => { options.IdleTimeout = TimeSpan.FromMinutes(30); });
            }

            // Manual additions.
            services.AddHttpContextAccessor();
            services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();

            // Templates service.
            if (gclSettings.UseLegacyWiser1TemplateModule)
            {
                services.AddScoped<ITemplatesService, LegacyTemplatesService>();
            }
            else
            {
                services.AddScoped<ITemplatesService, TemplatesService>();
            }

            // Configure automatic scanning of classes for dependency injection.
            services.Scan(scan => scan
                // We start out with all types in the current assembly.
                .FromApplicationDependencies()
                // AddClasses starts out with all public, non-abstract types in this assembly.
                // These types are then filtered by the delegate passed to the method.
                // In this case, we filter out only the classes that are assignable to ITransientService.
                .AddClasses(classes => classes.AssignableTo<ITransientService>())
                // We then specify what type we want to register these classes as.
                // In this case, we want to register the types as all of its implemented interfaces.
                // So if a type implements 3 interfaces; A, B, C, we'd end up with three separate registrations.
                .AsImplementedInterfaces()
                // And lastly, we specify the lifetime of these registrations.
                .WithTransientLifetime()
                // Here we start again, with a new full set of classes from the assembly above.
                // This time, filtering out only the classes assignable to IScopedService.
                .AddClasses(classes => classes.AssignableTo<IScopedService>())
                .AsImplementedInterfaces()
                .WithScopedLifetime()
                // Here we start again, with a new full set of classes from the assembly above.
                // This time, filtering out only the classes assignable to IScopedService.
                .AddClasses(classes => classes.AssignableTo<ISingletonService>())
                .AsImplementedInterfaces()
                .WithSingletonLifetime()

                // Payment service providers need to be added with their own type, otherwise the factory won't work.
                .AddClasses(classes => classes.AssignableTo<IPaymentServiceProviderService>())
                .AsSelf()
                .WithScopedLifetime());

            if (!useCaching)
            {
                return services;
            }

            // Decorate cached services, to use caching.
            services.Decorate<IDatabaseConnection, CachedDatabaseConnection>();
            services.Decorate<IDatabaseHelpersService, CachedDatabaseHelpersService>();
            services.Decorate<ILanguagesService, CachedLanguagesService>();
            services.Decorate<IObjectsService, CachedObjectsService>();
            services.Decorate<IItemFilesService, CachedItemFilesService>();
            services.Decorate<ISeoService, CachedSeoService>();
            services.Decorate<IRedirectService, CachedRedirectService>();
            services.Decorate<IWebPagesService, CachedWebPagesService>();
            services.Decorate<IWiserItemsService, CachedWiserItemsService>();
            services.Decorate<IShoppingBasketsService, CachedShoppingBasketsService>();
            services.Decorate<IDataSelectorParsersService, CachedDataSelectorParsersService>();
            services.Decorate<IOrderProcessesService, CachedOrderProcessesService>();
            services.Decorate<IRolesService, CachedRolesService>();
            services.Decorate<IBarcodesService, CachedBarcodesService>();

            if (gclSettings.UseLegacyWiser1TemplateModule)
            {
                services.Decorate<ITemplatesService, LegacyCachedTemplatesService>();
            }
            else
            {
                services.Decorate<ITemplatesService, CachedTemplatesService>();
            }

            return services;
        }
    }
}