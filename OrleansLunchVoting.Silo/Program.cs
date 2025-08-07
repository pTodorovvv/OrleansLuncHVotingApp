var builder = WebApplication.CreateBuilder(args);

// Add Orleans silo
builder.Host.UseOrleans(siloBuilder =>
{
    siloBuilder.UseLocalhostClustering();
});

var app = builder.Build();

app.MapGet("/", () => "Orleans Lunch Voting Silo running");

app.Run();
