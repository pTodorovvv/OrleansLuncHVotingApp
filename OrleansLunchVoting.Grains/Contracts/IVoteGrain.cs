using Orleans;

namespace OrleansLunchVoting.Grains;
public interface IVoteGrain : IGrainWithStringKey
{
    Task<bool> CreateVote(DateTime dateTimeSlotUtc);
    Task<bool> Vote(string user, string place);
    Task<Dictionary<string, int>> GetResults();
    Task<bool> CanVote(string user);
    Task<bool> IsVoteOpen();
    Task<DateTime> GetVoteStartTime();
    Task<bool> UpdateVote(string user, string newPlace);
}
