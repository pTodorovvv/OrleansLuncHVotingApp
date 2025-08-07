using Orleans;
using OrleansLunchVoting.Grains.Contracts;

namespace OrleansLunchVoting.Grains;

public class VoteGrain : Grain, IVoteGrain
{
    private DateTime _voteDateTime;
    private Dictionary<string, string> _votes = new Dictionary<string, string>();
    private bool _isCreated = false;
    private DateTime _voteStartTime;
    private readonly IGrainFactory _grains;

    public VoteGrain(IGrainFactory grains)
    {
        _grains = grains;
    }

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _votes = new Dictionary<string, string>();
        return base.OnActivateAsync(cancellationToken);
    }

    public async Task<bool> CreateVote(DateTime dateTimeSlotUtc)
    {
        if (_isCreated) return false;

        _voteDateTime = dateTimeSlotUtc;
        var clock = _grains.GetGrain<IClockGrain>(0);
        _voteStartTime = await clock.GetUtcNow();

        _isCreated = true;

        return true;
    }

    public async Task<bool> Vote(string user, string place)
    {
        if (!_isCreated || !LunchPlaces.Places.Contains(place) || !await IsVoteCurrentlyOpen() || _votes.ContainsKey(user))
            return false;

        _votes[user] = place;
        return true;
    }

    public async Task<bool> UpdateVote(string user, string newPlace)
    {
        if (!_isCreated || !LunchPlaces.Places.Contains(newPlace) || !await IsVoteCurrentlyOpen() || !_votes.ContainsKey(user))
            return false;

        if (_votes[user] == newPlace)
            return false;

        _votes[user] = newPlace;
        return true;
    }

    public async Task<Dictionary<string, int>?> GetResults()
    {
        var clock = _grains.GetGrain<IClockGrain>(0);
        var now = await clock.GetUtcNow();

        if (!IsResultsVisibleInternal(now))
            return null;

        var results = _votes.Values
            .GroupBy(p => p)
            .ToDictionary(g => g.Key, g => g.Count());

        foreach (var place in LunchPlaces.Places)
        {
            if (!results.ContainsKey(place))
                results[place] = 0;
        }

        return results;
    }

    public async Task<bool> CanVote(string user)
    {
        return !_votes.ContainsKey(user) && await IsVoteCurrentlyOpen();
    }

    public async Task<bool> IsVoteOpen()
    {
        return await IsVoteCurrentlyOpen();
    }

    public async Task<bool> IsResultsVisible()
    {
        var clock = _grains.GetGrain<IClockGrain>(0);
        var now = await clock.GetUtcNow();

        return IsResultsVisibleInternal(now);
    }

    public Task<DateTime> GetVoteStartTime()
    {
        return Task.FromResult(_voteStartTime);
    }
    // Make sure the vote is not closed yet
    private async Task<bool> IsVoteCurrentlyOpen()
    {
        if (!_isCreated)
            return false;

        var clock = _grains.GetGrain<IClockGrain>(0);
        var now = await clock.GetUtcNow();

        var voteEndTime = new DateTime(_voteStartTime.Year, _voteStartTime.Month, _voteStartTime.Day, 13, 30, 0, DateTimeKind.Utc);

        return now >= _voteStartTime && now <= voteEndTime;
    }

    private bool IsResultsVisibleInternal(DateTime now)
    {
        if (!_isCreated)
            return false;

        var voteEndTime = new DateTime(_voteStartTime.Year, _voteStartTime.Month, _voteStartTime.Day, 11, 30, 0, DateTimeKind.Utc);
        var resultsEndTime = new DateTime(_voteStartTime.Year, _voteStartTime.Month, _voteStartTime.Day, 13, 30, 0, DateTimeKind.Utc);

        return now > voteEndTime && now <= resultsEndTime;
    }
}
