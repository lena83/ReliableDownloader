using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReliableDownloader.Lib.DownloadPolicy;
using ReliableDownloader.Lib.HttpCalls;
using System.Diagnostics;
using System.Security.Cryptography;

namespace ReliableDownloader.Lib
{
    public class FileDownloader : IFileDownloader
    {
        private readonly ILogger<FileDownloader> _logger;
        private readonly IWebSystemCalls _webSystemCalls;
        private readonly DownloadFilePolicy _downloadFilePolicy;
        private readonly IDownloadPolicyProvider _downloadPolicyProvider;
        private readonly IRefFileDataProvider _refFileDataProvider;


        public FileDownloader(IWebSystemCalls webSystemCalls, IDownloadPolicyProvider policyProvider, IRefFileDataProvider refFileDataProvider, IOptions<DownloadFilePolicy> options, ILogger<FileDownloader> logger)
        {
            _logger = logger;
            _downloadPolicyProvider = policyProvider;
            _webSystemCalls = webSystemCalls;
            _downloadFilePolicy = options.Value;
            _refFileDataProvider = refFileDataProvider;
        }

        public async Task<bool> PerformIntegrityCheckAsync(string filePath, byte[] expectedMd5)
        {
            await using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);

            using var md5 = MD5.Create();
            var hash = await md5.ComputeHashAsync(fileStream);
            if (!expectedMd5.SequenceEqual(hash))
            {
                File.Delete(filePath);
                return false;
            }

            return true;
        }




        public async Task<bool> TryDownloadFileAsync(string contentFileUrl, string localFilePath, Action<FileProgress> onProgressChanged, CancellationToken cancellationToken)
        {
            var result = await _downloadPolicyProvider.GetDownloadPolicy().ExecuteAsync(async () =>
            {
                _logger.LogInformation($"Trying to download file from {contentFileUrl}");
                HttpResponseMessage response = await DownloadFileAsync(contentFileUrl, localFilePath, cancellationToken);

                if (response == null)
                {
                    return false;
                }

                long? contentLength = response.Content.Headers.ContentLength;
                if (contentLength == null)
                {
                    _logger.LogWarning($"Downloaded content file size is null");
                    return false;
                }

                await DownloadToFileAsync(response, localFilePath, contentLength, onProgressChanged, cancellationToken);

                return true;
            });

            return result;
        }

        private async Task<HttpResponseMessage> DownloadFileAsync(string contentFileUrl, string localFilePath, CancellationToken cancellationToken)
        {
            try
            {
                HttpResponseMessage response;
                if (File.Exists(localFilePath))
                {
                    var fileInfo = new FileInfo(localFilePath);
                    if (_refFileDataProvider.ExpectedFileSize != fileInfo.Length)
                    {
                        _logger.LogInformation($"Trying to resume download. CancelationRequested: {cancellationToken.IsCancellationRequested}");
                        response = await _webSystemCalls.DownloadPartialContentAsync(contentFileUrl, fileInfo.Length, null, cancellationToken);
                    }
                    else
                    {
                        _logger.LogInformation($"File {localFilePath} already downloaded and up to date");
                        return null;
                    }
                }
                else
                {
                    _logger.LogInformation($"Downloading file. CancelationRequested: {cancellationToken.IsCancellationRequested}");
                    response = await _webSystemCalls.DownloadContentAsync(contentFileUrl, cancellationToken);
                }

                if (response == null)
                {
                    _logger.LogError($"Failed to download file from {contentFileUrl}. Status code {response?.StatusCode}");
                    return null;
                }

                _logger.LogInformation($"Status code: {response.StatusCode}");
                return response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred while downloading file from {contentFileUrl}: {ex.Message}");
                throw;
            }
        }

        private async Task DownloadToFileAsync(HttpResponseMessage response, string localFilePath, long? contentLength, Action<FileProgress> onProgressChanged, CancellationToken cancellationToken)
        {
            using (var fileStream = new FileStream(localFilePath, FileMode.Create, FileAccess.Write))
            {
                var progress = new Progress<FileProgress>(onProgressChanged);

                await response.Content.LoadIntoBufferAsync();

                using (var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken))
                {
                    var bufferSize = _downloadFilePolicy.BufferSize;
                    var buffer = new byte[bufferSize];
                    var totalBytesRead = 0;
                    var stopWatch = new Stopwatch();
                    var lastReportedProgress = 0;

                    while (true)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                        if (bytesRead == 0)
                        {
                            break;
                        }

                        await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                        totalBytesRead += bytesRead;
                        var elapsedTime = stopWatch.Elapsed;
                        var progressPercentage = contentLength.HasValue ? (int)((double)totalBytesRead / contentLength.Value * 100) : 0;

                        if (progressPercentage - lastReportedProgress >= 1)
                        {
                            var bytesPerSecond = totalBytesRead / elapsedTime.TotalSeconds;
                            var remainingBytes = contentLength - totalBytesRead;
                            TimeSpan? estimatedTimeLeft = bytesPerSecond > 0 ? TimeSpan.FromSeconds(remainingBytes.Value / bytesPerSecond) : null;

                            var downloadProgress = new FileProgress(totalBytesRead, contentLength.Value, progressPercentage, estimatedTimeLeft);
                            ((IProgress<FileProgress>)progress).Report(downloadProgress);
                            lastReportedProgress = progressPercentage;
                        }
                    }
                    stopWatch.Stop();
                }
            }
        }

    }
}
