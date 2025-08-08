
# Orleans Lunch Voting

A simple Orleans-based voting app for office lunch places. Users can create daily votes for lunch spots, vote once per lunch, update their vote, and see results during a fixed time window. The special "clock" user can set server time for testing and administration.

## Features

- Create a daily vote for lunch spots if one doesn’t exist.
- Vote for one of 5 predefined places: PizzaHut, McDonalds, BurgerKing, TacoBell, Happy.
- Users can only vote once per lunch and update their vote before voting closes.
- Voting closes at 13:30 UTC; results visible only before then.
- The "clock" user can set the server time via API (used for testing).
- To open a new vote, the "clock" user must set the server time to after 14:30 local time (UTC+3) to enable voting for the current day.
- Uses Orleans for distributed grain management and client-server interaction.
- Stores the logged-in user via cookie from the `?user=` query parameter.
- Provides basic web pages for voting and admin controls.
- Adjustable time via `AdjustableTimeProvider` for testing.

## Requirements

- .NET 9 SDK
- Orleans 7.x
- Localhost clustering (default)

## Running the app

1. Clone the repo.
2. Build the solution.
3. Run the Silo project with 'dotnet run'
3. Run the Orleans client project from Visual Studio and a page should pop up.
4. Open your browser and use the following endpoints or UI pages:

### Typical usage flow

1. Voting Page (/vote-page)
Users log in by passing ?user=yourName in the URL.

Select a lunch place from 5 options: PizzaHut, McDonalds, BurgerKing, TacoBell, Happy.

Submit a vote or update an existing vote.

View live voting results that refresh every 5 seconds.

2. Admin Panel (/admin-page)
Accessible by any logged-in user (typically clock user).

Displays the current voting results updated live every 5 seconds.

Provides an overview of the day's vote results.

3. Admin Controls (/admin)
Only the user named clock can access this page.

Allows setting the server time (UTC) for testing purposes.

Enables opening a vote for the current day.

Displays status messages for actions performed.

- Set the current user by adding `?user=yourname` query parameter to any URL. This stores the username in a cookie.
- **Set server time (clock user only):**  
  ```
  POST https://localhost:7051/set-time?user=clock&newUtcTime=2025-08-07T12:00:00Z
  ```  
  The server time must be set to after 14:30 local time (UTC+3) to open today's vote.

- **Create today's vote:**  
  ```
  POST https://localhost:7051/create-vote
  ```

- **Cast your vote:**  
  ```
  POST https://localhost:7051/vote?place=BurgerKing
  ```

- **Update your vote:**  
  ```
  POST https://localhost:7051/update-vote?newPlace=TacoBell 
  ```

- **Access voting page:**  
  ```
  GET https://localhost:7051/vote-page?user=yourname
  ```

- **Access admin panel (clock user only):**  
  ```
  GET https://localhost:7051/admin?user=clock
  ```

## Notes

- Users named "clock" cannot vote, only set server time and manage the system.
- Votes are only accepted before 13:30 UTC.
- Results are visible between 11:30 and 13:30 UTC.
- Server time is adjustable for testing scenarios.
- Usernames are stored in cookies via middleware.
- The app uses Orleans grains to manage vote state per day.
- One important thing set time after 14:30 UTC up to 16:30 UTC as the UTC is +3 hours so the results are visible
  
