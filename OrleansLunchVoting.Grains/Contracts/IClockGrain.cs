using Orleans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrleansLunchVoting.Grains.Contracts
{
    public interface IClockGrain : IGrainWithIntegerKey
    {
        Task<DateTime> GetUtcNow();
        Task SetUtcNow(DateTime utcTime);
    }

}
