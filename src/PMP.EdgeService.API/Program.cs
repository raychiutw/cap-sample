using DotNetCore.CAP.Messages;
using PMP.EdgeService.Application;
using PMP.EdgeService.Common.Options;
using PMP.EdgeService.Persistence;
using System.Text.Encodings.Web;
using System.Text.Unicode;

namespace PMP.EdgeService.API;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.

        builder.Services.AddControllers();
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        builder.Services.AddPersistence(builder.Configuration);
        builder.Services.AddApplication();

        builder.Services.AddCap(x =>
        {
            x.TopicNamePrefix = "cap.pmp.edge";
            x.GroupNamePrefix = "cap.pmp.edge";

            x.UseEntityFramework<CapDbContext>();

            x.UseRabbitMQ(config =>
            {
                var options = builder.Configuration
                    .GetSection("RabbitMqOptions")
                    .Get<RabbitMqOptions>();

                config.UserName = options.UserName;
                config.Password = options.Password;
                config.HostName = options.HostName;
                config.Port = options.Port;
                config.ExchangeName = options.ExchangeName;
            });

            x.UseDashboard();

            x.FailedRetryCount = 5;

            x.FailedThresholdCallback = failed =>
            {
                var logger = failed.ServiceProvider.GetRequiredService<ILogger<Program>>();
                logger.LogError($@"A message of type {failed.MessageType} failed after executing {x.FailedRetryCount} several times,
                        requiring manual troubleshooting. Message name: {failed.Message.GetName()}");
            };

            x.JsonSerializerOptions.Encoder = JavaScriptEncoder.Create(UnicodeRanges.All);
        });

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}