using Orleans;
using OrleansLunchVoting.Grains.Contracts;

namespace OrleansLunchVoting.Grains
{
    public class ClockGrain : Grain, IClockGrain
    {
        //Setup custom Time provider to manage time correctly
        private DateTime? _mockTime;

        public Task<DateTime> GetUtcNow()
        {
            return Task.FromResult(_mockTime ?? DateTime.UtcNow);
        }

        public Task SetUtcNow(DateTime utcTime)
        {
            _mockTime = utcTime;
            Console.WriteLine($"[ClockGrain] Time set to {utcTime:O}");
            return Task.CompletedTask;
        }
    }
}
