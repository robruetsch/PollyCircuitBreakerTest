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
        private readonly PolicyWrap<String> _policyWrap;

        public PollyServiceClient()
        {
            _client = new HttpClient();
            _gatewayUri = _primaryUri;

            var circuitBreakerPolicy = Policy
                .Handle<Exception>()
                .CircuitBreakerAsync(
                    exceptionsAllowedBeforeBreaking: 4,
                    durationOfBreak: TimeSpan.FromSeconds(3),
                    onBreak: (ex, breakDelay) =>
                    {
                        //Called when circuit first open
                        //Setting the backupUri so calls now go to backup gateway
                        _gatewayUri = _backupUri;
                    },
                    onReset: () =>
                    {
                        //Called when circuit is closed again.
                        //Primary gateway is responding again, setting to primaryUri.
                        _gatewayUri = _primaryUri;
                    },
                    onHalfOpen: () =>
                    {
                        //Called when the policy is going to check the original call to see if there are no exceptions.
                        //Send the call to the primary gateway to see if it's operational again.
                        _gatewayUri = _primaryUri; ;
                    }
                );

            var waitAndRetryPolicy = Policy
                .Handle<Exception>(e => !(e is BrokenCircuitException)) // Exception filtering!  We don't retry if the inner circuit-breaker judges the underlying system is out of commission!
                .WaitAndRetryForeverAsync(
                attempt => TimeSpan.FromMilliseconds(200),
                (exception, calculatedWaitDuration) =>
                {
                    //Handle the exception here.
                });

            FallbackPolicy<String> fallbackForCircuitBreaker = Policy<String>
                .Handle<BrokenCircuitException>()
                .FallbackAsync(
                    fallbackAction: /* Demonstrates fallback action/func syntax */ async ct =>
                    {
                        await Task.FromResult(true);
                        
                        //This is called after the circuit breaker is tripped
                        //and the gatewayUri is changed to point to the backup
                        //gateway.  This call runs on the backup gateway.
                        return await ClientWrapper();
                    },
                    onFallbackAsync: async e =>
                    {
                        await Task.FromResult(true);
                    }
                );

            //Something really bad has happened.
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
            _policyWrap = fallbackForAnyException.WrapAsync(fallbackForCircuitBreaker.WrapAsync(myResilienceStrategy));

        }

        public async Task<string> GetSiteNamePolly(CancellationToken cancellationToken)
        {
            var myException = "";

            try
            {
                return await _policyWrap.ExecuteAsync(ct =>
                                        ClientWrapper(), cancellationToken);
            }
            catch (Exception e)
            {
                myException = e.Message;
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
