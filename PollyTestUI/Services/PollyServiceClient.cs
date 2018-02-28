using Polly;
using Polly.CircuitBreaker;
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
        //private readonly Policy _waitAndRetryPolicy;
        //private readonly Policy _circuitBreaker;
        private readonly string _primaryUri = "http://localhost:56376/";
        private readonly string _backupUri = "http://localhost:57720/";

        public PollyServiceClient()
        {
            _client = new HttpClient
            {
                BaseAddress = new Uri(_primaryUri)
            };

            //var _waitAndRetryPolicy = Policy
            //    .Handle<Exception>(e => !(e is BrokenCircuitException)) // Don't retry if inner circuit breaker is causing this failure
            //    .WaitAndRetryForeverAsync(
            //        attempt => TimeSpan.FromMilliseconds(200),
            //        (exception, calculatedWaitDuration) =>
            //        {
            //            //Log Exception here
            //        });

            //var _circuitBreaker = Policy
            //    .Handle<Exception>()
            //    .CircuitBreakerAsync(
            //        exceptionsAllowedBeforeBreaking: 2,
            //        durationOfBreak: TimeSpan.FromMinutes(1),
            //        onBreak: (ex, breakDelay) => {
            //            //Log Exception
            //            _client.BaseAddress = new Uri(_backupUri); //Start sending to backup
            //        },
            //        onReset: () => _client.BaseAddress = new Uri(_primaryUri), //Gateway is back online
            //        onHalfOpen: () => _client.BaseAddress = new Uri(_primaryUri)  //this is the experimental retry to see if gateway is back up
            //    );
        }

        public async Task<string> GetSiteNamePolly(CancellationToken cancellationToken)
        {
            var myException = "";

            var waitAndRetryPolicy = Policy
                .Handle<Exception>(e => !(e is BrokenCircuitException)) // Don't retry if inner circuit breaker is causing this failure
                .WaitAndRetryForeverAsync(
                    attempt => TimeSpan.FromMilliseconds(1000),
                    (exception, calculatedWaitDuration) =>
                    {
                        myException = exception.Message;
                    });

            var retryPolicy = Policy
                .Handle<HttpRequestException>()
                .WaitAndRetryAsync(3, i => TimeSpan.FromSeconds(2));

            await retryPolicy.ExecuteAsync(async () =>
            {
                return await _client.GetStringAsync("api/Test/Site");
            });

            return "Error";

            //try
            //{
            //    return await _client.GetStringAsync("api/Test/Site");
            //    //await waitAndRetryPolicy.ExecuteAsync(async token =>
            //    //{
            //    //    return await _client.GetStringAsync("api/Test/Site");
            //    //    //string response = await _circuitBreaker.ExecuteAsync<String>(
            //    //    //    () =>
            //    //    //    {
            //    //    //        return _client.GetStringAsync("api/Test/Site");
            //    //    //    });
            //    //}, cancellationToken);
            //}
            //catch (Exception ex)
            //{
            //    //Log failure
            //    throw ex;
            //}
            //throw new Exception(myException);


        }
        public async Task<string> GetSiteName()
        {
            CancellationTokenSource cancellationSource = new CancellationTokenSource();
            CancellationToken cancellationToken = cancellationSource.Token;

            return await GetSiteNamePolly(cancellationToken);

            //HttpResponseMessage response = await _client.GetAsync("api/Test/Site");
            //if (response.IsSuccessStatusCode)
            //{
            //    return await response.Content.ReadAsStringAsync();
            //}
            //else
            //{
            //    throw new Exception($"GetSiteName() returned status {response.StatusCode}.");
            //}
        }

        public void SetBackupUri()
        {
            _client.BaseAddress = new Uri(_backupUri);
        }
    }
}
