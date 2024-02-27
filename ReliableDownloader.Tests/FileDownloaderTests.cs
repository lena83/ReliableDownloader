using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using ReliableDownloader.Lib;
using ReliableDownloader.Lib.DownloadPolicy;
using ReliableDownloader.Lib.HttpCalls;
using System.IO;
using System.Net.Http;
using System.Net;
using System.Threading.Tasks;
using System.Threading;
using Polly;
using System;
using System.Reflection.Metadata;

namespace ReliableDownloader.Tests;

[TestFixture]
public class FileDownloaderTests
{
    private Mock<IWebSystemCalls> mockWebSystemCalls;
    private Mock<IDownloadPolicyProvider> mockPolicyProvider;
    private Mock<IRefFileDataProvider> mockRefFileDataProvider;
    private Mock<IOptions<DownloadFilePolicy>> mockOptions;
    private Mock<ILogger<FileDownloader>> mockLogger;
    private Mock<Action<FileProgress>> mockProgressAction;
    private FileDownloader fileDownloader;
    private string fileName;
    private string contentFileUrl;

    [SetUp]
    public void Setup()
    {
        fileName = "myfirstdownload.msi";
        mockWebSystemCalls = new Mock<IWebSystemCalls>();

        mockPolicyProvider = new Mock<IDownloadPolicyProvider>();
        var customPolicy = Policy<bool>.Handle<Exception>().RetryAsync(3);
        mockPolicyProvider.Setup(p => p.GetDownloadPolicy()).Returns(customPolicy);

        mockRefFileDataProvider = new Mock<IRefFileDataProvider>();
        mockOptions = new Mock<IOptions<DownloadFilePolicy>>();

        var downloadFilePolicy = new DownloadFilePolicy
        {
            BufferSize = 8192, 
            DownloadTimeOut = 30, 
            RetryCount = 3 
        };
        mockOptions.Setup(p => p.Value).Returns(downloadFilePolicy);

        mockLogger = new Mock<ILogger<FileDownloader>>();
        contentFileUrl = "https://installer.demo.accurx.com/chain/4.22.50587.0/accuRx.Installer.Local.msi";

        mockProgressAction = new Mock<Action<FileProgress>>();
        mockProgressAction.Setup(action => action(It.IsAny<FileProgress>()));
    }

    [Test]
    public async Task TryDownloadFileAsync_FileExistsAndUpToDate_ReturnsFalse()
    {
        var localFilePath = Path.Combine(Directory.GetCurrentDirectory(), "FileExists", fileName);
        var fileInfo = new FileInfo(localFilePath);

        var cancellationToken = CancellationToken.None;

        var refFile = Path.Combine(Directory.GetCurrentDirectory(), "Ref", fileName);
        var refFileInfo = new FileInfo(refFile);

        mockRefFileDataProvider.SetupGet(p => p.ExpectedFileSize).Returns(refFileInfo.Length);
        // Act
        var fileDownloader = new FileDownloader(mockWebSystemCalls.Object, mockPolicyProvider.Object, mockRefFileDataProvider.Object, mockOptions.Object, mockLogger.Object);
        var result = await fileDownloader.TryDownloadFileAsync(contentFileUrl, localFilePath, mockProgressAction.Object, cancellationToken);

        //file won't be downloaded
        Assert.IsFalse(result);
        mockWebSystemCalls.VerifyNoOtherCalls();
    }

    [Test]
    public async Task TryDownloadFileAsync_FileExistsButNotUpToDate_DownloadsPartialContentAndReturnsTrue()
    {
      
        var cancellationToken = CancellationToken.None;


        var refFile = Path.Combine(Directory.GetCurrentDirectory(), "Ref", fileName);
        var refFileInfo = new FileInfo(refFile);

        mockRefFileDataProvider.SetupGet(p => p.ExpectedFileSize).Returns(refFileInfo.Length + 100);
       
        var localPath = Path.Combine(Directory.GetCurrentDirectory(), "FileExistsPartially", fileName);
     
        var fileInfo = new FileInfo(localPath);
        mockWebSystemCalls.Setup(w => w.DownloadPartialContentAsync(contentFileUrl, fileInfo.Length, null, cancellationToken))
                          .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        // Act
        var fileDownloader = new FileDownloader(mockWebSystemCalls.Object, mockPolicyProvider.Object, mockRefFileDataProvider.Object, mockOptions.Object, mockLogger.Object);
        var result = await fileDownloader.TryDownloadFileAsync(contentFileUrl, localPath, mockProgressAction.Object, cancellationToken);

        // Assert
        Assert.IsTrue(result);
        mockWebSystemCalls.Verify(w => w.DownloadPartialContentAsync(contentFileUrl, fileInfo.Length, null, cancellationToken), Times.Once);
    }

    [Test]
    public async Task TryDownloadFileAsync_FileDoesNotExist_DownloadsContentAndReturnsTrue()
    {
        var cancellationToken = CancellationToken.None;

        mockWebSystemCalls.Setup(w => w.DownloadContentAsync(contentFileUrl, cancellationToken))
                          .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));
        
        var localPath = Path.Combine(Directory.GetCurrentDirectory(), fileName);
        // Act
        var downloader = new FileDownloader(mockWebSystemCalls.Object, mockPolicyProvider.Object, mockRefFileDataProvider.Object, mockOptions.Object, mockLogger.Object);
        var result = await downloader.TryDownloadFileAsync(contentFileUrl, localPath, mockProgressAction.Object, CancellationToken.None);
        
        Assert.True(result);
        mockWebSystemCalls.Verify(w => w.DownloadContentAsync(contentFileUrl, cancellationToken), Times.Once);
       
    }

    [TearDown]
    public void Cleanup()
    {
        var localPath = Path.Combine(Directory.GetCurrentDirectory(), fileName);
        // delete file 
        if (File.Exists(localPath))
        {
            File.Delete(localPath);
        }
    }

}