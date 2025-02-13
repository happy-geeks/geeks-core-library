using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Interfaces;

namespace GeeksCoreLibrary.Core.Services;

/// <summary>
/// Service for managing file-based caching.
/// </summary>
public class FileCacheService : IFileCacheService, ISingletonService
{
    /// <summary>
    /// A thread-safe collection to manage active asynchronous write operations in the file caching service.
    /// Maps file paths to lazily initialized tasks that handle the file write process.
    /// Used to prevent concurrent file write operations for the same file path.
    /// </summary>
    private readonly ConcurrentDictionary<string, Lazy<Task<byte[]>>> activeWrites = new();
    
    /// <inheritdoc />
    public async Task<byte[]> GetOrAddAsync(string filePath, Func<Task<(byte[] Content, bool IsCachable)>> generateContentAsync, TimeSpan? cachingTime = null)
    {
        var buffer = await GetBytesAsync(filePath, cachingTime);
        if (buffer != null)
        {
            return buffer;
        }

        var lazyWriteTask = activeWrites.GetOrAdd(filePath, _ =>
            new(async () =>
            {
                var (content, cachable) = await generateContentAsync();
                if (cachable)
                {
                    await WriteFileInternalAsync(filePath, content, cachingTime);
                }
                return content;
            }));
        return await lazyWriteTask.Value;
    }
    
    /// <inheritdoc />
    public async Task<string> GetOrAddAsync(string filePath, Func<Task<string>> generateContentAsync, TimeSpan? cachingTime = null)
    {
        var contentBytes = await GetOrAddAsync(filePath, async () =>
        {
            var content = await generateContentAsync();
            return (Encoding.UTF8.GetBytes(content), true);
        }, cachingTime);
        return Encoding.UTF8.GetString(contentBytes);
    }

    /// <inheritdoc />
    public async Task<byte[]> GetBytesAsync(string filePath, TimeSpan? cachingTime = null)
    {
        var fileInfo = new FileInfo(filePath);
        if (!CreateDirectoryIfNotExist(fileInfo) && !IsFileExpired(fileInfo, cachingTime))
        {
            if (activeWrites.TryGetValue(filePath, out var writeTask))
            {
                return await writeTask.Value;
            }
            
            await using var fileStream = fileInfo.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
            var buffer = new byte[fileStream.Length];
            _ = await fileStream.ReadAsync(buffer);
            return buffer;
        }
        return null;
    }
    
    /// <inheritdoc />
    public async Task<string> GetTextAsync(string filePath, TimeSpan? cachingTime = null)
    {
        var buffer = await GetBytesAsync(filePath, cachingTime);
        return buffer != null ? Encoding.UTF8.GetString(buffer) : null;
    }

    /// <inheritdoc />
    public async Task WriteFileIfNotExistsOrExpiredAsync(string filePath, byte[] content, TimeSpan? cachingTime = null)
    {
        var lazyWriteTask = activeWrites.GetOrAdd(filePath, _ =>
            new Lazy<Task<byte[]>>( async () =>
            {
                await WriteFileInternalAsync(filePath, content, cachingTime);
                return content;
            }));
        
        await lazyWriteTask.Value;
    }
    
    /// <inheritdoc />
    public async Task WriteFileIfNotExistsOrExpiredAsync(string filePath, string content, TimeSpan? cachingTime)
    {
        var contentBytes = Encoding.UTF8.GetBytes(content);
        await WriteFileIfNotExistsOrExpiredAsync(filePath, contentBytes, cachingTime);
    }

    /// <summary>
    /// Writes a file with byte[] content if it does not exist or is expired.
    /// </summary>
    private async Task WriteFileInternalAsync(string filePath, byte[] content, TimeSpan? cachingTime)
    {
        // if the caching time is zero then don't make the file.
        if (cachingTime == TimeSpan.Zero)
        {
            return;
        }
        
        try 
        {
            var fileInfo = new FileInfo(filePath);
            if (IsFileExpired(fileInfo, cachingTime))
            {
                await using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
                await fileStream.WriteAsync(content);
            }
        }
        finally
        {
            activeWrites.TryRemove(filePath, out _);
        }
    }

    /// <summary>
    /// Determines if a file is expired or does not exist based on its last write time and the specified caching duration.
    /// </summary>
    /// <param name="fileInfo">The <see cref="FileInfo"/> object representing the file to be checked.</param>
    /// <param name="cachingTime">The time span defining the expiration duration. If the file's age exceeds this, it will be overwritten.</param>
    /// <returns><c>true</c> if the file is expired or does not exist; otherwise, <c>false</c>.</returns>
    private static bool IsFileExpired(FileInfo fileInfo, TimeSpan? cachingTime)
    {
        return !fileInfo.Exists || (cachingTime is not null &&
               DateTime.UtcNow - fileInfo.LastWriteTimeUtc > cachingTime);
    }

    /// <summary>
    /// Ensures that the directory for the specified file path exists.
    /// If the directory does not exist, it is created.
    /// </summary>
    /// <param name="fileInfo">The file information object that includes the directory to check or create.</param>
    /// <returns>
    /// Returns <c>true</c> if the directory did not exist and was successfully created;
    /// otherwise, <c>false</c> if the directory already exists.
    /// </returns>
    private static bool CreateDirectoryIfNotExist(FileInfo fileInfo)
    {
        if (fileInfo.Directory is not { Exists: false })
        {
            return false;
        }

        fileInfo.Directory.Create();
        return true;
    }
}