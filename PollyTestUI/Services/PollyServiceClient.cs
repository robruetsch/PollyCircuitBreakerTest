using Polly;
using Polly.CircuitBreaker;
using Polly.Fallback;
using Polly.Wrap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace PollyTestUI.Services
{
    public class PollyServiceClient : IPollyServiceClient
    {
        private readonly HttpClient _client;
        private readonly string _primaryUri = "http://localhost:56376/";
        private readonly string _backupUri = "http://localhost:57720/";
        private string _gatewayUri = "http://localhost:56376/";


        public PollyServiceClient()
        {
            _client = new HttpClient();
            _gatewayUri = _primaryUri;
        }

        public async Task<string> GetSiteNamePolly(CancellationToken cancellationToken)
        {
            var myException = "";

            var circuitBreakerPolicy = Policy
                .Handle<Exception>()
                .CircuitBreakerAsync(
                    exceptionsAllowedBeforeBreaking: 4,
                    durationOfBreak: TimeSpan.FromSeconds(3),
                    onBreak: (ex, breakDelay) =>
                    {
                        myException += $" | Circuit Breaker Broken | ";
                        _gatewayUri = _backupUri;
                    },
                    onReset: () =>
                    {
                        myException += $" | Circuit Reset | ";
                        _gatewayUri = _primaryUri;
                    },
                    onHalfOpen: () => 
                    {
                        myException += $" | Circuit Reset | ";
                        _gatewayUri = _primaryUri; ;
                    }
                );

            var waitAndRetryPolicy = Policy
                .Handle<Exception>(e => !(e is BrokenCircuitException)) // Exception filtering!  We don't retry if the inner circuit-breaker judges the underlying system is out of commission!
                .WaitAndRetryForeverAsync(
                attempt => TimeSpan.FromMilliseconds(200),
                (exception, calculatedWaitDuration) =>
                {
                    myException += $" | Exception reached {exception.Message} | ";
                });

            FallbackPolicy<String> fallbackForCircuitBreaker = Policy<String>
                .Handle<BrokenCircuitException>()
                .FallbackAsync(
                    fallbackAction: /* Demonstrates fallback action/func syntax */ async ct =>
                    {
                        await Task.FromResult(true);
                        /* do something else async if desired */
                        return await ClientWrapper();
                    },
                    onFallbackAsync: async e =>
                    {
                        await Task.FromResult(true);
                    }
                );

            FallbackPolicy<String> fallbackForAnyException = Policy<String>
                .Handle<Exception>()
                .FallbackAsync(
                    fallbackAction: /* Demonstrates fallback action/func syntax */ async ct =>
                    {
                        await Task.FromResult(true);
                        return "Another exception occurred.";
                    },
                    onFallbackAsync: async e =>
                    {
                        await Task.FromResult(true);
                    }
                );

            PolicyWrap myResilienceStrategy = Policy.Wrap(waitAndRetryPolicy, circuitBreakerPolicy);
            PolicyWrap<String> policyWrap = fallbackForAnyException.WrapAsync(fallbackForCircuitBreaker.WrapAsync(myResilienceStrategy));

            try
            {
                string response = await policyWrap.ExecuteAsync(ct =>
                                        ClientWrapper(), cancellationToken);

                myException += response;
            }
            catch (Exception e)
            {
                myException += e.Message;
            }

            return myException;
        }

        public async Task<string> GetSiteName()
        {
            CancellationTokenSource cancellationSource = new CancellationTokenSource();
            CancellationToken cancellationToken = cancellationSource.Token;

            return await GetSiteNamePolly(cancellationToken);
        }

        public async Task<string> ClientWrapper()
        {
            return await _client.GetStringAsync($"{_gatewayUri}api/Test/Site");
        }
    }
}
