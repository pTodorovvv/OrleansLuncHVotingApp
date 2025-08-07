# Orleans Lunch Voting

A simple Orleans-based voting app for office lunch places. Users can create votes for lunch spots at 5-minute intervals, vote once per lunch, and see results after voting ends. Special "clock" user can set the server time for testing.

## Features

- Create a vote for a specific 5-minute time slot if none exists.
- Vote for one of 5 predefined lunch places.
- Users can only vote once per lunch.
- Votes close by 11:30 UTC; results visible until 13:30 UTC.
- "Clock" user can set the server time via API.
- Uses Orleans for distributed grain management.
- Adjustable time via `AdjustableTimeProvider` for testing.
- Simple REST API endpoints to interact with the voting system.

## Requirements

- .NET 9 SDK
- Orleans 7.x
- Localhost clustering (default)

## Running the app

1. Clone the repo.
2. Build the solution:  
3. In the OrleansLunchVoting.Silo folder open a terminal and run the dotnet run command
4. Then start the LunchVoting.Web from the visual studio.
5. Open postman
6. Make a post in order to set time: https://localhost:7051/set-time?user=clock&newUtcTime=2025-08-07T12:00:00Z --  set the time between the interval 11:30 and 13:30 so people are able to vote
7. Make a post in order to open the vote: https://localhost:7051/create-vote
8. Example vote: https://localhost:7051/vote?user=user4&place=BurgerKing (There are 5 places to choose from - PizzaHut / McDonalds / BurgerKing / TacoBell / Happy
9. You could also update your vote: https://localhost:7051/update-vote?user=user4&newPlace=TacoBell
10. Get the results: https://localhost:7051/results

