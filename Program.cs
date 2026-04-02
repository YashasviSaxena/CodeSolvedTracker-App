using Hangfire;

var builder = WebApplication.CreateBuilder(args);

// Hangfire (in-memory, built-in)
builder.Services.AddHangfire(config =>
    config.UseInMemoryStorage()); // ✅ built-in, no extra package needed
builder.Services.AddHangfireServer();

var app = builder.Build();

// Hangfire dashboard
app.UseHangfireDashboard();

app.Run();