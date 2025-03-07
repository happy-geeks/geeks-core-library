using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using GeeksCoreLibrary.Core.Helpers;
using GeeksCoreLibrary.Core.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GeeksCoreLibrary.Core.Services;

public class FolderCleanupBackgroundService(
    ILogger<FolderCleanupBackgroundService> logger,
    IWebHostEnvironment webHostEnvironment,
    IConfiguration configuration,
    IOptions<GclSettings> gclSettings)
    : BackgroundService
{
    private readonly string filesCachePath = FileSystemHelpers.GetFileCacheDirectory(webHostEnvironment);
    private readonly string outputCachePath = FileSystemHelpers.GetOutputCacheDirectory(webHostEnvironment);

    private readonly TimeSpan cleanUpIntervalDays = TimeSpan.FromDays(gclSettings.Value.CacheCleanUpOptions.CleanUpIntervalDays > 0 ? gclSettings.Value.CacheCleanUpOptions.CleanUpIntervalDays : 1);
    private readonly TimeSpan maxCacheDurationDays = TimeSpan.FromDays(gclSettings.Value.CacheCleanUpOptions.MaxCacheDurationDays > 0 ? gclSettings.Value.CacheCleanUpOptions.MaxCacheDurationDays : 30);

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

            await Task.Delay(cleanUpIntervalDays, stoppingToken);
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
            if (now - fileInfo.LastWriteTimeUtc > maxCacheDurationDays)
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