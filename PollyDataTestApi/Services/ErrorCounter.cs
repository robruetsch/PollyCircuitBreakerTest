using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PollyDataTestApi.Services
{
    public class ErrorCounter : IErrorCounter 
    {
        public int Counter { get; set; }

        public int GetCounter()
        {
            return Counter;
        }
        public void Increment()
        {
            Counter++;
        }
    }
}
