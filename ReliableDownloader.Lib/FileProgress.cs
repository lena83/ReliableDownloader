namespace ReliableDownloader.Lib
{
    public record FileProgress(
    long? TotalFileSize,
    long TotalBytesDownloaded,
    double? ProgressPercent,
    TimeSpan? EstimatedRemaining);
}
