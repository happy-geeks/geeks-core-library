using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using GeeksCoreLibrary.Core.Helpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GeeksCoreLibrary.Core.Services;

public class FolderCleanupBackgroundService : BackgroundService
{
    private readonly ILogger<FolderCleanupBackgroundService> logger;

    private readonly TimeSpan cleanupInterval = TimeSpan.FromHours(1);
    private readonly TimeSpan maxCacheDuration;

    private readonly string filesCachePath;
    private readonly string outputCachePath;

    public FolderCleanupBackgroundService(ILogger<FolderCleanupBackgroundService> logger, IWebHostEnvironment webHostEnvironment, IConfiguration configuration)
    {
        this.logger = logger;

        var maxCacheDurationHours = configuration.GetValue("CacheCleanup:MaxCacheDurationHours", 24);
        maxCacheDuration = TimeSpan.FromHours(maxCacheDurationHours);

        filesCachePath = FileSystemHelpers.GetFileCacheDirectory(webHostEnvironment);
        outputCachePath = FileSystemHelpers.GetOutputCacheDirectory(webHostEnvironment);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("FolderCleanupBackgroundService started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                CleanUpFolder(filesCachePath);
                CleanUpFolder(outputCachePath);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred during cache cleanup.");
            }

            await Task.Delay(cleanupInterval, stoppingToken);
        }
    }

    private void CleanUpFolder(string folderPath)
    {
        if (!Directory.Exists(folderPath)) return;

        var files = Directory.GetFiles(folderPath);
        var now = DateTime.UtcNow;

        foreach (var file in files)
        {
            var fileInfo = new FileInfo(file);
            if (now - fileInfo.LastWriteTimeUtc > maxCacheDuration)
            {
                try
                {
                    fileInfo.Delete();
                    logger.LogInformation("Deleting file: {FilePath}", file);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to delete file: {FilePath}", file);
                }
            }
        }
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("FolderCleanupBackgroundService stopped.");
        await base.StopAsync(stoppingToken);
    }
}