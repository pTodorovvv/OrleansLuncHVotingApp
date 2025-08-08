using OrleansLunchVoting.Grains;
using OrleansLunchVoting.Grains.Contracts;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOrleansClient(client =>
{
    client.UseLocalhostClustering();
});

var app = builder.Build();

// Middleware: store ?user= in cookie
app.Use(async (context, next) =>
{
    var queryUser = context.Request.Query["user"].ToString();

    if (!string.IsNullOrWhiteSpace(queryUser))
    {
        context.Response.Cookies.Append("VotingUser", queryUser, new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.AddDays(1)
        });
    }

    var cookieUser = context.Request.Cookies["VotingUser"];
    if (!string.IsNullOrWhiteSpace(cookieUser))
    {
        context.Items["VotingUser"] = cookieUser;
    }

    await next();
});

app.MapGet("/", () => "Orleans Lunch Voting Client running");

// Create vote for today
app.MapPost("/create-vote", async (IGrainFactory grains) =>
{
    var clock = grains.GetGrain<IClockGrain>(0);
    var now = await clock.GetUtcNow();
    var grainId = now.ToString("yyyy-MM-dd"); // daily voting

    Console.WriteLine($"[CREATE VOTE] Time: {now:O} | Grain ID: {grainId}");

    var voteGrain = grains.GetGrain<IVoteGrain>(grainId);
    var created = await voteGrain.CreateVote(now);

    return created ? Results.Ok("Vote created") : Results.BadRequest("Vote already exists");
});

// Vote
app.MapPost("/vote", async (HttpContext context, string place, IGrainFactory grains) =>
{
    var user = context.Items["VotingUser"]?.ToString();
    if (string.IsNullOrWhiteSpace(user))
        return Results.BadRequest("User not logged in. Add ?user=YourName to the URL");

    if (string.IsNullOrWhiteSpace(place))
        return Results.BadRequest("Place is required");

    if (user.ToLower() == "clock")
        return Results.BadRequest("Clock user cannot vote");

    var clock = grains.GetGrain<IClockGrain>(0);
    var now = await clock.GetUtcNow();

    var cutoff = new DateTime(now.Year, now.Month, now.Day, 13, 30, 0, DateTimeKind.Utc);
    if (now > cutoff)
        return Results.BadRequest("Voting is closed for today (after 13:30 UTC)");

    var grainId = now.ToString("yyyy-MM-dd");
    var voteGrain = grains.GetGrain<IVoteGrain>(grainId);
    var canVote = await voteGrain.CanVote(user);

    if (!canVote)
        return Results.BadRequest("You have already voted or voting is not open");

    var voted = await voteGrain.Vote(user, place);
    return voted ? Results.Ok("Vote accepted") : Results.BadRequest("Vote failed");
});

// Update vote
app.MapPost("/update-vote", async (HttpContext context, string newPlace, IGrainFactory grains) =>
{
    var user = context.Items["VotingUser"]?.ToString();
    if (string.IsNullOrWhiteSpace(user))
        return Results.BadRequest("User not logged in. Add ?user=YourName to the URL");

    if (string.IsNullOrWhiteSpace(newPlace))
        return Results.BadRequest("New place is required");

    if (user.ToLower() == "clock")
        return Results.BadRequest("Clock user cannot vote");

    var clock = grains.GetGrain<IClockGrain>(0);
    var now = await clock.GetUtcNow();

    var cutoff = new DateTime(now.Year, now.Month, now.Day, 13, 30, 0, DateTimeKind.Utc);
    if (now > cutoff)
        return Results.BadRequest("Voting is closed for today (after 13:30 UTC)");

    var grainId = now.ToString("yyyy-MM-dd");
    var voteGrain = grains.GetGrain<IVoteGrain>(grainId);
    var updated = await voteGrain.UpdateVote(user, newPlace);
    return updated ? Results.Ok("Vote updated") : Results.BadRequest("Vote update failed");
});

// Get results
app.MapGet("/results", async (IGrainFactory grains) =>
{
    var clock = grains.GetGrain<IClockGrain>(0);
    var now = await clock.GetUtcNow();

    var cutoff = new DateTime(now.Year, now.Month, now.Day, 13, 30, 0, DateTimeKind.Utc);
    if (now > cutoff)
        return Results.BadRequest("Results are no longer available (after 13:30 UTC)");

    var grainId = now.ToString("yyyy-MM-dd");
    var voteGrain = grains.GetGrain<IVoteGrain>(grainId);
    var results = await voteGrain.GetResults();

    if (results == null)
        return Results.BadRequest("Results are not visible at this time.");

    return Results.Ok(results);
});

// Set time
app.MapPost("/set-time", async (string user, DateTime newUtcTime, IGrainFactory grains) =>
{
    if (user.ToLower() != "clock")
        return Results.BadRequest("Only 'clock' user can set the server time");

    var clockGrain = grains.GetGrain<IClockGrain>(0);
    await clockGrain.SetUtcNow(newUtcTime);
    Console.WriteLine($"[TIME SET] (via ClockGrain) Server time set to {newUtcTime:O}");
    return Results.Ok($"Server time set to {newUtcTime:O}");
});
app.MapGet("/admin-results", async (HttpContext context, IGrainFactory grains) =>
{
    var user = context.Items["VotingUser"]?.ToString();
    if (string.IsNullOrWhiteSpace(user) || user.ToLower() != "clock")
        return Results.BadRequest("Only 'clock' user can view admin results");

    var clock = grains.GetGrain<IClockGrain>(0);
    var now = await clock.GetUtcNow();
    var grainId = now.ToString("yyyy-MM-dd");

    var voteGrain = grains.GetGrain<IVoteGrain>(grainId);
    var results = await voteGrain.GetResults();

    if (results == null)
        return Results.BadRequest("Results are not visible at this time.");

    return Results.Ok(results);
});

app.MapGet("/admin-page", () => Results.Content(@"
<!DOCTYPE html>
<html>
<head>
    <title>Lunch Voting Admin</title>
</head>
<body>
    <h1>Lunch Voting - Admin Panel</h1>
    <div id='user'></div>

    <h2>Admin Results</h2>
    <pre id='results'></pre>

<script>
    const user = new URLSearchParams(window.location.search).get('user');
    document.getElementById('user').innerText = 'Logged in as: ' + user;

    async function loadResults() {
        const res = await fetch(`/admin-results`);
        if (res.ok) {
            const data = await res.json();
            document.getElementById('results').innerText = JSON.stringify(data, null, 2);
        } else {
            document.getElementById('results').innerText = await res.text();
        }
    }

    setInterval(loadResults, 5000);
    loadResults();
</script>
</body>
</html>
", "text/html"));

app.MapGet("/vote-page", () => Results.Content(@"
<!DOCTYPE html>
<html>
<head>
    <title>Lunch Voting</title>
</head>
<body>
    <h1>Lunch Voting</h1>
    <div id='user'></div>
    <h3>Place Selection</h3>
    <select id='place'></select>
    <button onclick='vote()'>Vote</button>
    <button onclick='updateVote()'>Update Vote</button>

    <h2>Results</h2>
    <pre id='results'></pre>

<script>
    const user = new URLSearchParams(window.location.search).get('user');
    document.getElementById('user').innerText = 'Logged in as: ' + user;

    const places = ['PizzaHut', 'McDonalds', 'BurgerKing', 'TacoBell', 'Happy'];
    const placeSelect = document.getElementById('place');
    places.forEach(p => {
        const opt = document.createElement('option');
        opt.value = p;
        opt.innerText = p;
        placeSelect.appendChild(opt);
    });

    async function vote() {
        const place = placeSelect.value;
        const res = await fetch(`/vote?place=${place}`, { method: 'POST' });
        alert(await res.text());
        loadResults();
    }

    async function updateVote() {
        const newPlace = placeSelect.value;
        const res = await fetch(`/update-vote?newPlace=${newPlace}`, { method: 'POST' });
        alert(await res.text());
        loadResults();
    }

    async function loadResults() {
        const res = await fetch(`/results`);
        if (res.ok) {
            const data = await res.json();
            document.getElementById('results').innerText = JSON.stringify(data, null, 2);
        } else {
            document.getElementById('results').innerText = await res.text();
        }
    }

    setInterval(loadResults, 5000);
    loadResults();
</script>
</body>
</html>
", "text/html"));


// Admin page (for clock user)
app.MapGet("/admin", (HttpContext context) =>
{
    var user = context.Items["VotingUser"]?.ToString() ?? "";

    var page = $@"
<!DOCTYPE html>
<html>
<head>
    <title>Admin - Lunch Voting</title>
</head>
<body>
    <h1>Admin Panel</h1>
    <p>Logged in as: <b>{user}</b></p>
    <p>(Only 'clock' user can set server time)</p>

    <label>Set server time (UTC):</label>
    <input type='datetime-local' id='newTime' />
    <button onclick='setTime()'>Set Time</button>

    <h2>Create Vote</h2>
    <button onclick='createVote()'>Open Today's Vote</button>

    <pre id='output'></pre>

<script>
async function setTime() {{
    const timeInput = document.getElementById('newTime').value;
    if (!timeInput) {{
        alert('Please select a time');
        return;
    }}
    const utcTime = new Date(timeInput).toISOString();
    const res = await fetch(`/set-time?user={user}&newUtcTime=${{utcTime}}`, {{ method: 'POST' }});
    document.getElementById('output').innerText = await res.text();
}}

async function createVote() {{
    const res = await fetch('/create-vote', {{ method: 'POST' }});
    document.getElementById('output').innerText = await res.text();
}}
</script>
</body>
</html>";

    return Results.Content(page, "text/html");
});

app.Run();
