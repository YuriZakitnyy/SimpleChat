using ChatServer.Core.Hubs;

try
{
    Console.WriteLine("Starting server");
    var builder = WebApplication.CreateBuilder(args);
    var portEnv = Environment.GetEnvironmentVariable("PORT") ?? "31777";

    if (!int.TryParse(portEnv, out var port))
    {
        port = 31777;
    }
    Console.WriteLine("Using port {0}", port);

    builder.WebHost.ConfigureKestrel(options =>
    {
        options.ListenAnyIP(port);
    });

    // Add services
    Console.WriteLine("Adding services");
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", policy =>
        {
            policy.AllowAnyHeader()
                  .AllowAnyMethod()
                  .SetIsOriginAllowed(_ => true)
                  .AllowCredentials();
        });
    });
    Console.WriteLine("Configure signalR");

    const long MaxMessageBytes = 5 * 1024 * 1024;
    
    builder.Services.AddSignalR(options =>
    {
        options.MaximumReceiveMessageSize = MaxMessageBytes;
    });

    Console.WriteLine("Build");
    var app = builder.Build();

    app.UseRouting();
    app.UseCors("AllowAll");

    app.MapGet("/", () => Results.Ok("ChatServer.Core running"));
    app.MapHub<ChatHub>("/chatHub");

    Console.WriteLine("Run");
    app.Run();
}
catch (Exception ex)
{
    Console.WriteLine("Fatal error: {0}", ex);
}