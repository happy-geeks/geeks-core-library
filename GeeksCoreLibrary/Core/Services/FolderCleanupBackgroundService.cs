using System;
using System.IO.Abstractions;
using System.Threading;
using System.Threading.Tasks;
using GeeksCoreLibrary.Core.Helpers;
using GeeksCoreLibrary.Core.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GeeksCoreLibrary.Core.Services;

public class FolderCleanupBackgroundService(
    ILogger<FolderCleanupBackgroundService> logger,
    IWebHostEnvironment webHostEnvironment,
    IOptions<GclSettings> gclSettings,
    IFileSystem fileSystem)
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
        if (!fileSystem.Directory.Exists(folderPath))
        {
            return;
        }

        var files = fileSystem.Directory.GetFiles(folderPath);

        var now = DateTime.UtcNow;

        foreach (var file in files)
        {
            var fileLastWriteTime = fileSystem.File.GetLastWriteTimeUtc(file);

            if (now - fileLastWriteTime <= maxCacheDurationDays)
            {
                continue;
            }

            if (!fileSystem.File.Exists(file))
            {
                continue;
            }

            fileSystem.File.Delete(file);
        }
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("FolderCleanupBackgroundService stopped.");
        await base.StopAsync(stoppingToken);
    }
}