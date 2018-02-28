using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PollyTestUI.Services
{
    public interface IPollyServiceClient
    {
        Task<string> GetSiteName();
    }
}
