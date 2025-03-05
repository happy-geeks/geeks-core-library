using System;
using System.IO;
using System.Threading.Tasks;

namespace GeeksCoreLibrary.Core.Interfaces;

/// <summary>
/// Provides methods for managing file-based caching.
/// </summary>
public interface IFileCacheService
{
    /// <summary>
    /// Retrieves content from a file if it is not expired; otherwise, regenerates and writes new content.
    /// </summary>
    /// <param name="filePath">The path of the file to read or write.</param>
    /// <param name="generateContent">A function to generate string content if the file is expired.</param>
    /// <param name="cachingTime">The time span defining the expiration duration. If the file's age exceeds this, it will be overwritten.</param>
    /// <returns>The content of the file as a string.</returns>
    Task<string> GetOrAddAsync(string filePath, Func<Task<string>> generateContent, TimeSpan? cachingTime = null);

    /// <summary>
    /// Retrieves content from a file if it is not expired; otherwise, regenerates and writes new content.
    /// </summary>
    /// <param name="filePath">The path of the file to read or write.</param>
    /// <param name="generateContent">A function to generate string content if the file is expired.</param>
    /// <param name="cachingTime">The time span defining the expiration duration. If the file's age exceeds this, it will be overwritten.</param>
    /// <returns>The content of the file as a string.</returns>
    Task<(byte[] FileBytes, DateTime LastModified)> GetOrAddAsync(string filePath, Func<Task<(byte[] Content, bool IsCacheable)>> generateContent, TimeSpan? cachingTime = null);

    /// <summary>
    /// Retrieves the byte content from a file if it is not expired.
    /// </summary>
    /// <param name="filePath">The path of the file to read.</param>
    /// <param name="cachingTime">The time span defining the expiration duration. If the file's age exceeds this, it will be overwritten.</param>
    /// <returns>The byte content of the file.</returns>
    Task<(byte[] FileBytes, DateTime LastModified)> GetBytesAsync(string filePath, TimeSpan? cachingTime = null);

    /// <summary>
    /// Retrieves the text content from a file if it is not expired.
    /// </summary>
    /// <param name="filePath">The path of the file to read.</param>
    /// <param name="cachingTime">The time span defining the expiration duration. If the file's age exceeds this, it will be overwritten.</param>
    /// <returns>The text content of the file as a string, or null if the file does not exist or is expired.</returns>
    Task<string> GetTextAsync(string filePath, TimeSpan? cachingTime = null);

    /// <summary>
    /// Writes a file with string content if it does not exist or if the existing file has expired based on the specified caching time.
    /// </summary>
    /// <param name="filePath">The path of the file to write.</param>
    /// <param name="content">The content to write to the file as a string.</param>
    /// <param name="cachingTime">The time span defining the expiration duration. If the file's age exceeds this, it will be overwritten.</param>
    Task WriteFileIfNotExistsOrExpiredAsync(string filePath, string content, TimeSpan? cachingTime = null);

    /// <summary>
    /// Writes a file with byte[] content if it does not exist or if the existing file is older than the specified caching minutes.
    /// </summary>
    /// <param name="filePath">The path of the file to write.</param>
    /// <param name="content">The byte array content to write to the file.</param>
    /// <param name="cachingTime">The time span defining the expiration duration. If the file's age exceeds this, it will be overwritten.</param>
    Task WriteFileIfNotExistsOrExpiredAsync(string filePath, byte[] content, TimeSpan? cachingTime = null);

    /// <summary>
    /// Writes a file with content from a stream if it does not exist or if the existing file is older than the specified caching minutes.
    /// </summary>
    /// <param name="filePath">The path of the file to write.</param>
    /// <param name="content">The stream to write to the file.</param>
    /// <param name="cachingTime">The time span defining the expiration duration. If the file's age exceeds this, it will be overwritten.</param>
    Task WriteFileIfNotExistsOrExpiredAsync(string filePath, Stream content, TimeSpan? cachingTime = null);
}