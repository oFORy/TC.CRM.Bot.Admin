using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using TC.CRM.Bot.Admin.GoogleSheets;
using TC.CRM.BotClient.Admin.Persistence;
using TC.CRM.BotClient.Admin.Bot;
using TC.CRM.BotClient.Admin.Filters;
using TC.CRM.BotClient.Admin.Services;
using Telegram.Bot;
using TC.CRM.BotClient.Admin.Persistance;

namespace TC.CRM.AdminBot.Admin
{
    public class Program
    {
        readonly static string MySpecificCorsOrigins = "_corspolicy";

        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Logging.ClearProviders();


            builder.Configuration
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", false, true)
                .AddEnvironmentVariables();

            // Add services to the container.

            builder.Services.AddControllers();


            var origins = builder.Configuration.GetSection("CorsOrigins:Urls").Get<string[]>();
            builder.Services.AddCors(options =>
            {
                options.AddPolicy(name: MySpecificCorsOrigins,
                                  builder =>
                                  {
                                      builder.WithOrigins(origins);
                                      builder.AllowAnyHeader();
                                      builder.AllowAnyMethod();
                                      builder.AllowCredentials();
                                  });
            });

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();

            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v2", new OpenApiInfo
                {
                    Version = "2",
                    Title = "ToDo API",
                    Description = "ASP.NET Core Web API",
                    Contact = new OpenApiContact
                    {
                        Name = "Turadzhanov Timur",
                        Email = string.Empty,
                        Url = new Uri("https://t.me/tyama0007"),
                    },
                });

            });

            builder.Services.AddDbContext<BotDbContext>(Options =>
            {
                Options.UseNpgsql(Environment.GetEnvironmentVariable("DB_CS"));
            });


            // There are several strategies for completing asynchronous tasks during startup.
            // Some of them could be found in this article https://andrewlock.net/running-async-tasks-on-app-startup-in-asp-net-core-part-1/
            // We are going to use IHostedService to add and later remove Webhook
            builder.Services.AddHostedService<ConfigureWebhook>();

            // Register named HttpClient to get benefits of IHttpClientFactory
            // and consume it with ITelegramBotClient typed client.
            // More read:
            //  https://docs.microsoft.com/en-us/aspnet/core/fundamentals/http-requests?view=aspnetcore-5.0#typed-clients
            //  https://docs.microsoft.com/en-us/dotnet/architecture/microservices/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests
            builder.Services.AddHttpClient("tgwebhook")
                    .AddTypedClient<ITelegramBotClient>(httpClient
                        => new TelegramBotClient(Environment.GetEnvironmentVariable("BotToken"), httpClient));

            // The Telegram.Bot library heavily depends on Newtonsoft.Json library to deserialize
            // incoming webhook updates and send serialized responses back.
            // Read more about adding Newtonsoft.Json to ASP.NET Core pipeline:
            //   https://docs.microsoft.com/en-us/aspnet/core/web-api/advanced/formatting?view=aspnetcore-6.0#add-newtonsoftjson-based-json-format-support
            builder.Services
                .AddControllers(config =>
                {
                    config.Filters.Add(typeof(ExceptionFilter));
                })
                .AddNewtonsoftJson();

            var botToken = Environment.GetEnvironmentVariable("BotToken");

            builder.Services.AddScoped<ITelegramService, TelegramService>();
            builder.Services.AddScoped<IRepositoryAdminBot, RepositoryAdminBot>();

            builder.Services.AddScoped<HandleUpdateService>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            app.UseSwagger(c =>
            {
                c.SerializeAsV2 = true;
                c.RouteTemplate = "api/swagger/{documentname}/swagger.json";
            });
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/api/swagger/v2/swagger.json", "My API V2");
                c.RoutePrefix = "api/swagger";
            });

            app.UseRouting();

            app.UseCors();

            //app.UseHttpsRedirection();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(name: "tgwebhook",
                         pattern: $"bot/{botToken}",
                         new { controller = "Webhook", action = "Post" });
                endpoints.MapControllers().RequireCors(MySpecificCorsOrigins);
            });

            app.Run();
        }
    }
}