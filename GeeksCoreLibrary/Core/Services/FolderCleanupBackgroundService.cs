using System;
using System.IO;
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
    IOptions<GclSettings> gclSettings,
    IFileSystem fileSystem,
    IWebHostEnvironment webHostEnvironment = null)
    : BackgroundService
{
    private readonly string filesCachePath = FileSystemHelpers.GetFileCacheDirectory(webHostEnvironment);
    private readonly string outputCachePath = FileSystemHelpers.GetOutputCacheDirectory(webHostEnvironment);

    private readonly TimeSpan cleanUpInterval = gclSettings.Value.CacheCleanUpOptions.CleanUpInterval > TimeSpan.Zero ? gclSettings.Value.CacheCleanUpOptions.CleanUpInterval : TimeSpan.FromDays(1);
    private readonly TimeSpan maxCacheDuration = gclSettings.Value.CacheCleanUpOptions.MaxCacheDuration > TimeSpan.Zero ? gclSettings.Value.CacheCleanUpOptions.MaxCacheDuration : TimeSpan.FromDays(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation($"{nameof(FolderCleanupBackgroundService)} started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                CleanUpFolder(filesCachePath);
                CleanUpFolder(outputCachePath);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Error occurred during cache cleanup.");
            }

            await Task.Delay(cleanUpInterval, stoppingToken);
        }
    }

    private void CleanUpFolder(string folderPath)
    {
        if (String.IsNullOrEmpty(folderPath) || !fileSystem.Directory.Exists(folderPath))
        {
            return;
        }

        var files = fileSystem.Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories);

        var now = DateTime.UtcNow;

        foreach (var file in files)
        {
            var fileLastWriteTime = fileSystem.File.GetLastWriteTimeUtc(file);

            if (now - fileLastWriteTime <= maxCacheDuration)
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
        logger.LogInformation($"{nameof(FolderCleanupBackgroundService)} stopped.");
        await base.StopAsync(stoppingToken);
    }
}