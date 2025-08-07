using OrleansLunchVoting.Grains;
using OrleansLunchVoting.Grains.Contracts;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOrleansClient(client =>
{
    client.UseLocalhostClustering();
});

var app = builder.Build();

app.MapGet("/", () => "Orleans Lunch Voting Client running");

// Create vote
app.MapPost("/create-vote", async (IGrainFactory grains) =>
{
    var clock = grains.GetGrain<IClockGrain>(0);
    var now = await clock.GetUtcNow();
    var rounded = RoundToNearestFiveMinutes(now);
    var grainId = rounded.ToString("yyyy-MM-ddTHH:mm");

    Console.WriteLine($"[CREATE VOTE] Time: {now:O} | Grain ID: {grainId}");

    var voteGrain = grains.GetGrain<IVoteGrain>(grainId);
    var created = await voteGrain.CreateVote(rounded);

    return created ? Results.Ok("Vote created") : Results.BadRequest("Vote already exists");
});

// Vote for a certain restaurant
app.MapPost("/vote", async (string user, string place, IGrainFactory grains) =>
{
    if (string.IsNullOrWhiteSpace(user))
        return Results.BadRequest("User is required");
    if (string.IsNullOrWhiteSpace(place))
        return Results.BadRequest("Place is required");

    if (user.ToLower() == "clock")
        return Results.BadRequest("Clock user cannot vote");

    var clock = grains.GetGrain<IClockGrain>(0);
    var now = await clock.GetUtcNow();

    var cutoff = new DateTime(now.Year, now.Month, now.Day, 13, 30, 0, DateTimeKind.Utc);
    if (now > cutoff)
        return Results.BadRequest("Voting is closed for today (after 13:30 UTC)");

    var rounded = RoundToNearestFiveMinutes(now);
    var grainId = rounded.ToString("yyyy-MM-ddTHH:mm");

    var voteGrain = grains.GetGrain<IVoteGrain>(grainId);
    var canVote = await voteGrain.CanVote(user);

    if (!canVote)
        return Results.BadRequest("User cannot vote as either the voting is not opened or user has already voted");

    var voted = await voteGrain.Vote(user, place);
    return voted ? Results.Ok("Vote accepted") : Results.BadRequest("Vote failed");
});

// Update vote the vote
app.MapPost("/update-vote", async (string user, string newPlace, IGrainFactory grains) =>
{
    if (string.IsNullOrWhiteSpace(user))
        return Results.BadRequest("User is required");
    if (string.IsNullOrWhiteSpace(newPlace))
        return Results.BadRequest("New place is required");

    if (user.ToLower() == "clock")
        return Results.BadRequest("Clock user cannot vote");

    var clock = grains.GetGrain<IClockGrain>(0);
    var now = await clock.GetUtcNow();

    var cutoff = new DateTime(now.Year, now.Month, now.Day, 13, 30, 0, DateTimeKind.Utc);
    if (now > cutoff)
        return Results.BadRequest("Voting is closed for today (after 13:30 UTC)");

    var rounded = RoundToNearestFiveMinutes(now);
    var grainId = rounded.ToString("yyyy-MM-ddTHH:mm");

    var voteGrain = grains.GetGrain<IVoteGrain>(grainId);
    var updated = await voteGrain.UpdateVote(user, newPlace);
    return updated ? Results.Ok("Vote updated") : Results.BadRequest("Vote update failed");
});

// Get the result.
app.MapGet("/results", async (IGrainFactory grains) =>
{
    var clock = grains.GetGrain<IClockGrain>(0);
    var now = await clock.GetUtcNow();

    var cutoff = new DateTime(now.Year, now.Month, now.Day, 13, 30, 0, DateTimeKind.Utc);
    if (now > cutoff)
        return Results.BadRequest("Results are no longer available (after 13:30 UTC)");

    var rounded = RoundToNearestFiveMinutes(now);
    var grainId = rounded.ToString("yyyy-MM-ddTHH:mm");

    var voteGrain = grains.GetGrain<IVoteGrain>(grainId);
    var results = await voteGrain.GetResults();

    if (results == null)
        return Results.BadRequest("Results are not visible at this time.");

    return Results.Ok(results);
});
// Set whatever time you want to.
app.MapPost("/set-time", async (string user, DateTime newUtcTime, IGrainFactory grains) =>
{
    if (user.ToLower() != "clock")
        return Results.BadRequest("Only 'clock' user can set the server time");

    var clockGrain = grains.GetGrain<IClockGrain>(0);
    await clockGrain.SetUtcNow(newUtcTime);
    Console.WriteLine($"[TIME SET] (via ClockGrain) Server time set to {newUtcTime:O}");
    return Results.Ok($"Server time set to {newUtcTime:O}");
});

app.Run();

static DateTime RoundToNearestFiveMinutes(DateTime dt)
{
    return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute / 5 * 5, 0, DateTimeKind.Utc);
}
