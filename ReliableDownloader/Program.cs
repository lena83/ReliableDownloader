using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using ReliableDownloader.Lib;
using ReliableDownloader.Lib.DownloadPolicy;
using ReliableDownloader.Lib.HttpCalls;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ReliableDownloader;

internal class Program
{
    public static async Task Main(string[] args)
    {
        var services = new ServiceCollection();

        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();

        services.AddSingleton<IConfiguration>(configuration);
        services.Configure<DownloadFilePolicy>(configuration.GetSection("DownloadFilePolicy"));

        string refFilePath = Path.Combine(Directory.GetCurrentDirectory(), "Ref", "myfirstdownload.msi");
        services.AddSingleton<IRefFileDataProvider>(provider => CreateRefFileDataProvider(provider, refFilePath));
        services.AddSingleton<IDownloadPolicyProvider, DownloadPolicyProvider>();
        services.AddTransient<FileDownloader1234>();
        services.AddTransient<IWebSystemCalls, WebSystemCalls>();
        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.AddConsole();
            loggingBuilder.AddDebug();
            loggingBuilder.AddNLog();
        });

        var serviceProvider = services.BuildServiceProvider();

        var fileDownloader = serviceProvider.GetService<FileDownloader1234>();
        var exampleUrl = "https://installer.demo.accurx.com/chain/4.22.50587.0/accuRx.Installer.Local.msi";
        var exampleFilePath = Path.Combine(Directory.GetCurrentDirectory(), "myfirstdownload.msi");
        // If this url 404's, you can get a live one from https://installer.demo.accurx.com/chain/latest.json.

        var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger<Program>();

        CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        var didDownloadSuccessfully = fileDownloader.TryDownloadFileAsync(
           exampleUrl,
           exampleFilePath,
           progress => logger.LogInformation($"Percent progress is {progress.ProgressPercent}"),
          cancellationTokenSource.Token);

        logger.LogInformation($"Trying to cancel");
        await Task.Delay(TimeSpan.FromMilliseconds(3));

        // Cancel the download using the CancellationTokenSource
        cancellationTokenSource.Cancel();

        bool result = await didDownloadSuccessfully;

        logger.LogInformation($"Trying to download again");

        didDownloadSuccessfully = fileDownloader.TryDownloadFileAsync(
                                                    exampleUrl,
                                                    exampleFilePath,
                                                    progress => logger.LogInformation($"Percent progress is {progress.ProgressPercent}"),
                                                    CancellationToken.None);

        if (didDownloadSuccessfully.Result)
            logger.LogInformation($"File download ended! Success: {didDownloadSuccessfully.Result}");
        else
            logger.LogInformation($"File was not downloaded");

        
    }

    private static RefFileDataProvider CreateRefFileDataProvider(IServiceProvider serviceProvider, string refFilePath)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<RefFileDataProvider>>();
        return new RefFileDataProvider(refFilePath, logger);
    }
}