using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PollyDataTestApi.Services
{
    public interface IErrorCounter
    {
        int GetCounter();
        void Increment();
    }
}
