using System.Text.Json;
using System.Text.Json.Serialization;
using LiteDB;
using Mocha2021.Classes;
using Mocha2021.Hub;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://0.0.0.0:5000");

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = null;
    options.JsonSerializerOptions.DictionaryKeyPolicy = null;
    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.Never;
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var dataDir = Path.Combine(builder.Environment.ContentRootPath, "Data");
Directory.CreateDirectory(dataDir);
Directory.CreateDirectory(Path.Combine(builder.Environment.ContentRootPath, "images"));
Directory.CreateDirectory(Path.Combine(builder.Environment.ContentRootPath, "videos"));
Directory.CreateDirectory(Path.Combine(builder.Environment.ContentRootPath, "uploads"));

builder.Services.AddSingleton(new LiteDatabase(Path.Combine(dataDir, "emerald.db")));
builder.Services.AddSingleton<PlayerDB>();
builder.Services.AddSingleton<RoomDB>();
builder.Services.AddSingleton<SessionManager>();
builder.Services.AddSingleton<RoomInstanceManager>();
builder.Services.AddSingleton<HubState>();
builder.Services.AddSingleton<HubWebSocketHandler>();
builder.Services.AddSingleton<ServerState>();
builder.Services.AddSingleton<CommerceStore>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.Use(async (context, next) =>
{
    context.Request.EnableBuffering();
    Console.WriteLine($"[req] {context.Request.Method} {context.Request.Path}{context.Request.QueryString}");
    await next();
});

app.UseWebSockets();

app.Use(async (context, next) =>
{
    if ((context.Request.Path == "/hub/v1" || context.Request.Path == "/Notifications/hub/v1") &&
        context.WebSockets.IsWebSocketRequest)
    {
        var handler = context.RequestServices.GetRequiredService<HubWebSocketHandler>();
        await handler.HandleAsync(context);
        return;
    }
    await next();
});

app.MapControllers();

app.Run();
