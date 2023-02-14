
using SignalR.Orleans;
using Silo;

var builder = WebApplication.CreateBuilder(args);

// Create a host that can cohost aspnetcore AND orleans together in a single process.
builder.Host.UseOrleans(siloBuilder =>
{
    siloBuilder.UseLocalhostClustering();
    siloBuilder.UseSignalR(); // Adds ability #1 and #2 to Orleans.
    siloBuilder.RegisterHub<ChatHub>(); // Required for each hub type if the backplane ability #1 is being used.
});

builder.Services
    .AddSignalR()  // Adds SignalR hubs to the web application
    .AddOrleans(); // Tells the SignalR hubs in the web application to use Orleans as a backplane (ability #1)

var app = builder.Build();
app.UseStaticFiles();
app.UseDefaultFiles();
app.MapFallbackToFile("index.html");

app.MapHub<ChatHub>("/chat");
await app.RunAsync();