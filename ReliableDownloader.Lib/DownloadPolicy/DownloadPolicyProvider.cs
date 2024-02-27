using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Timeout;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ReliableDownloader.Lib.DownloadPolicy
{

    public class DownloadPolicyProvider : IDownloadPolicyProvider
    {
        private readonly DownloadFilePolicy _downloadFilePolicy;
        private readonly ILogger<DownloadPolicyProvider> _logger;
        
        public DownloadPolicyProvider(IOptions<DownloadFilePolicy> dowloadFilePolicy, ILogger<DownloadPolicyProvider> logger)
        {
            _downloadFilePolicy = dowloadFilePolicy.Value;
            _logger = logger;
        }
        public IAsyncPolicy<bool> GetDownloadPolicy()
        {
            var timeoutPolicy = Policy.TimeoutAsync(TimeSpan.FromSeconds(_downloadFilePolicy.DownloadTimeOut));


            var retryPolicy = Policy
                            .Handle<HttpRequestException>()
                            .WaitAndRetryAsync(_downloadFilePolicy.RetryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

            var fallbackPolicy = Policy<bool>
                .Handle<TimeoutRejectedException>()
                .Or<OperationCanceledException>() 
                .FallbackAsync(false, (context, cancelationToken) =>
                {
                    _logger.LogError("The download operation timed out or was cancelled");
                    return Task.FromResult(false);
                });

            return fallbackPolicy.WrapAsync(retryPolicy).WrapAsync(timeoutPolicy);
        }
    }
}
