using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace ReliableDownloader.Lib.HttpCalls
{
    public class WebSystemCalls : IWebSystemCalls
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<WebSystemCalls> _logger;

        public WebSystemCalls(ILogger<WebSystemCalls> logger)
        {
            _httpClient = new HttpClient();
            _logger = logger;
        }

        public async Task<HttpResponseMessage> DownloadContentAsync(string url, CancellationToken token)
        {
            try
            {
                using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, url);
                return await _httpClient.SendAsync(httpRequestMessage, token);
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogError($"File download {url} cancelled");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error while downloading content from url {url}");
                throw;
            }
        }

        public async Task<HttpResponseMessage> DownloadPartialContentAsync(string url, long? from, long? to, CancellationToken token)
        {
            try
            {
                using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, url);
                httpRequestMessage.Headers.Range = new RangeHeaderValue(from, to);
                return await _httpClient.SendAsync(httpRequestMessage, token);
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogError($"File partial download {url} cancelled");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error while downloading partial content from url {url}");
                throw;
            }
        }

        public async Task<HttpResponseMessage> GetHeadersAsync(string url, CancellationToken token)
        {
            try
            {
                using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Head, url);
                return await _httpClient.SendAsync(httpRequestMessage, token);
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogError($"Het headers operations {url} is cancelled");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error while making HEAD request to url {url}");
                throw;
            }
        }
    }
}
