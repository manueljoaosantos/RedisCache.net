using API.Middleware;
using API.Extensions;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);
ConfigurationManager configuration = builder.Configuration;

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.Services.AddDbContext<FeedContext>(x =>
    x.UseSqlite(configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddStackExchangeRedisCache(options => { options.Configuration = configuration["RedisCacheUrl"]; });

builder.Services.AddApplicationServices();
builder.Services.AddSwaggerDocumentation();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionMiddleware>();
app.UseSwaggerDocumentation();
app.UseStatusCodePagesWithReExecute("/errors/{0}");
app.UseHttpsRedirection();

app.UseRouting();
app.UseStaticFiles();

app.UseAuthorization();

//app.MapControllers();
    app.UseEndpoints(endpoints =>
    {
        endpoints.MapControllers();
    });

    var loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();
/*
    try
    {
        using (var scope = app.Services.CreateScope())
        {
            var services = scope.ServiceProvider;

            var context = services.GetRequiredService<FeedContext>();
            await context.Database.MigrateAsync();
            await FeedContextSeed.SeedAsync(context, loggerFactory);
            //DbInitializer.Initialize(context);
        }
    }
    catch (Exception ex)
    {
        var logger = loggerFactory.CreateLogger<Program>();
        logger.LogError(ex, "An error occurred during migration");
    }*/
/*
    try
    {
        var context = app.Services.GetRequiredService<FeedContext>();
        await context.Database.MigrateAsync();
        await FeedContextSeed.SeedAsync(context, loggerFactory);
    }
    catch (Exception ex)
    {
        var logger = loggerFactory.CreateLogger<Program>();
        logger.LogError(ex, "An error occurred during migration");
    }
*/

app.Run();
