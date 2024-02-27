using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using ReliableDownloader.Lib.HttpCalls;

namespace ReliableDownloader.Tests;

[TestFixture]
public class WebSystemCallsTests
{
    private Mock<HttpClient> mockHttpClient;
    private Mock<ILogger<WebSystemCalls>> mockLogger;
    private WebSystemCalls webSystemCalls;
    private string url;

    [SetUp]
    public void Setup()
    {
        mockHttpClient = new Mock<HttpClient>();
        mockLogger = new Mock<ILogger<WebSystemCalls>>();
        webSystemCalls = new WebSystemCalls(mockLogger.Object);
        url = "https://installer.demo.accurx.com/chain/4.22.50587.0/accuRx.Installer.Local.msi";
    }

    [Test]
    public async Task DownloadContentAsync_ResponseMessage()
    {
        var expectedResponse = new HttpResponseMessage(HttpStatusCode.OK);

        mockHttpClient
            .Setup(client => client.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var result = await webSystemCalls.DownloadContentAsync(url, CancellationToken.None);

        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
    }

    [Test]
    public async Task DownloadPartialContentAsync_MessageWithRangeHeader()
    {
        long? from = 100;
        long? to = null;
        var expectedResponse = new HttpResponseMessage(HttpStatusCode.PartialContent);

        mockHttpClient
            .Setup(client => client.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse)
            .Callback<HttpRequestMessage, CancellationToken>((request, token) =>
            {
                var rangeHeader = request.Headers.Range;
                Assert.NotNull(rangeHeader);
                var expectedRangeHeader = new RangeHeaderValue(from, to);
                Assert.AreEqual(expectedRangeHeader.ToString(), rangeHeader.ToString());
            });

        var result = await webSystemCalls.DownloadPartialContentAsync(url, from, to, CancellationToken.None);

        Assert.AreEqual(HttpStatusCode.PartialContent, result.StatusCode);
    }

    [Test]
    public async Task GetHeadersAsync_MessageWithHeadMethod()
    {
        var expectedResponse = new HttpResponseMessage(HttpStatusCode.OK);

        mockHttpClient
            .Setup(client => client.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse)
            .Callback<HttpRequestMessage, CancellationToken>((request, token) =>
            {
                Assert.AreEqual(HttpMethod.Head, request.Method);
            });

        var result = await webSystemCalls.GetHeadersAsync(url, CancellationToken.None);

        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
    }

    [Test]
    public async Task GetHeadersAsync_TokenIsCancelled()
    {
        mockHttpClient
           .Setup(client => client.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK))
           .Callback<HttpRequestMessage, CancellationToken>((request, token) =>
           {
               Task.Delay(5000, token).Wait();
           });
        var cancellationTokenSource = new CancellationTokenSource();

        // Cancel download
        cancellationTokenSource.Cancel();

        var task = webSystemCalls.GetHeadersAsync(url, cancellationTokenSource.Token);

        Assert.IsTrue(task.IsCanceled);
    }
}

