using System;
using System.Text;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using SW.Bus;
using SW.CloudFiles.AS.Extensions;
using SW.CqApi;
using SW.CloudFiles.Extensions;
using SW.Serverless;
using SW.EfCoreExtensions;
using SW.HttpExtensions;
using SW.Infolink.JsonConverters;
using SW.Logger;
using SW.Infolink.Sdk;
using SW.PrimitiveTypes;
using SW.SimplyRazor;
using Newtonsoft.Json.Serialization;
using SW.Infolink.Domain;

namespace SW.Infolink.Web
{
    public class Startup
    {
        private static readonly string ApiXchangeCreatedEventQueueName = "XchangeService.ApiXchangeCreatedEvent";

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        private IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            var infolinkOptions = new InfolinkOptions();

            Configuration.GetSection(InfolinkOptions.ConfigurationSection).Bind(infolinkOptions);

            services.AddSingleton(infolinkOptions);
            services.AddMemoryCache();
            services.AddSingleton<IInfolinkCache, InMemoryInfolinkCache>();
            services.AddSingleton<FilterService>();
            services.AddScoped<XchangeService>();

            services.AddHostedService<AggregationService>();


            services.AddHostedService<ReceivingService>();

            services.AddBus(config =>
            {
                config.ApplicationName = "infolink";
                config.DefaultQueuePrefetch = infolinkOptions.BusDefaultQueuePrefetch!.Value;
                config.AddQueueOption("XchangeService.ApiXchangeCreatedEvent", priority: 10);
            });
            services.AddBusPublish();
            services.AddBusConsume(typeof(InfolinkDbContext).Assembly);

            var serializer = new JsonSerializer();
            serializer.Converters.Add(new PropertyMatchSpecificationJsonConverter());
            serializer.ContractResolver = new CamelCasePropertyNamesContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy
                {
                    ProcessDictionaryKeys = false
                }
            };

            services.AddCqApi(configure =>
                {
                    configure.RolePrefix = "Infolink";
                    configure.UrlPrefix = "api";
                    configure.ProtectAll = true;
                    configure.Serializer = serializer;
                },
                typeof(InfolinkDbContext).Assembly
            );

            services.AddApiClient<InfolinkClient, InfolinkClientOptions>();
            if (infolinkOptions.StorageProvider.ToUpper().Equals("AS"))
            {
                services.AddAsCloudFiles();
            }
            else
            {
                services.AddS3CloudFiles();
            }

            services.AddServerless(configure =>
            {
                configure.CommandTimeout = infolinkOptions.ServerlessCommandTimeout;
            });
            services.AddScoped<RequestContext>();

            if (string.Equals(infolinkOptions.DatabaseType, RelationalDbType.PgSql.ToString(),
                    StringComparison.CurrentCultureIgnoreCase))
            {
                services.AddDbContext<InfolinkDbContext, PgSql.InfolinkDbContext>(c =>
                {
                    c.EnableSensitiveDataLogging();
                    c.UseSnakeCaseNamingConvention();
                    c.UseNpgsql(Configuration.GetConnectionString(InfolinkDbContext.ConnectionString), b =>
                    {
                        b.MigrationsHistoryTable("_ef_migrations_history", PgSql.InfolinkDbContext.Schema);
                        b.MigrationsAssembly(typeof(PgSql.DbType).Assembly.FullName);
                        b.UseAdminDatabase(infolinkOptions.AdminDatabaseName);
                    });
                });
            }
            else
            {
                services.AddDbContext<InfolinkDbContext>(c =>
                {
                    c.EnableSensitiveDataLogging();
                    if (string.Equals(infolinkOptions.DatabaseType, RelationalDbType.MySql.ToString(),
                            StringComparison.CurrentCultureIgnoreCase))
                    {
                        c.UseMySql(Configuration.GetConnectionString(InfolinkDbContext.ConnectionString),
                            new MySqlServerVersion(new Version(8, 0, 18)),
                            b => { b.MigrationsAssembly(typeof(MySql.DbType).Assembly.FullName); });
                    }
                    else if (infolinkOptions.DatabaseType.ToLower() == RelationalDbType.MsSql.ToString().ToLower())
                    {
                        c.UseSqlServer(Configuration.GetConnectionString(InfolinkDbContext.ConnectionString),
                            b => { b.MigrationsAssembly(typeof(MsSql.DbType).Assembly.FullName); });
                    }
                });
            }


            services.AddHealthChecks();
            services.AddRazorPages(options =>
            {
                options.Conventions.AuthorizeFolder("/");
                options.Conventions.AllowAnonymousToPage("/Login");
            });

            services.AddServerSideBlazor().AddHubOptions(
                options => { options.MaximumReceiveMessageSize = 131072; });

            services.AddSimplyRazor(config =>
            {
                config.DefaultApiClientFactory = sp => sp.GetService<InfolinkClient>();
            });
            services.AddJwtTokenParameters();

            services.AddScoped<RunFlagUpdater>();

            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddJwtBearer(configureOptions =>
                {
                    configureOptions.RequireHttpsMetadata = false;
                    configureOptions.SaveToken = true;
                    configureOptions.TokenValidationParameters = new TokenValidationParameters()
                    {
                        ValidIssuer = Configuration["Token:Issuer"],
                        ValidAudience = Configuration["Token:Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["Token:Key"]))
                    };
                })
                .AddCookie(options =>
                {
                    options.Cookie.Name = "InfolinkUser";
                    options.LoginPath = "/login";
                });

            services.AddCors(options =>
            {
                options.AddDefaultPolicy(
                    builder =>
                    {
                        builder.AllowAnyOrigin();
                        builder.AllowAnyHeader();
                        builder.AllowAnyMethod();
                    });
            });
        }


        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseForwardedHeaders();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseCors();
            app.UsePathBase("/infolink");
            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseHttpAsRequestContext();
            app.UseRequestContextLogEnricher();

            app.UseSwaggerUI(c => { c.SwaggerEndpoint("/api/swagger.json", "Infolink Api"); });


            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHealthChecks("/health");
                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/_Host");
            });
        }
    }
}