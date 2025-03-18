using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using Renci.SshNet;
using GeeksCoreLibrary.Modules.Ftps.Interfaces;
using GeeksCoreLibrary.Modules.Ftps.Models;

namespace GeeksCoreLibrary.Modules.Ftps.Handlers;

public class SftpHandler : IFtpHandler, IScopedService
{
    private SftpClient client;

    /// <inheritdoc />
    public Task OpenConnectionAsync(FtpSettings ftpSettings)
    {
        AuthenticationMethod[] authenticationMethods;

        // Use SSH or username/password if none is set.
        if (!String.IsNullOrWhiteSpace(ftpSettings.SshPrivateKeyPath))
        {
            authenticationMethods =
            [
                new PrivateKeyAuthenticationMethod(ftpSettings.User, new PrivateKeyFile(ftpSettings.SshPrivateKeyPath, ftpSettings.SshPrivateKeyPassphrase))
            ];
        }
        else
        {
            authenticationMethods =
            [
                new PasswordAuthenticationMethod(ftpSettings.User, ftpSettings.Password)
            ];
        }

        var connectionInfo = new ConnectionInfo(ftpSettings.Host, ftpSettings.Port, ftpSettings.User, authenticationMethods);
        client = new SftpClient(connectionInfo);
        client.Connect();

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task CloseConnectionAsync()
    {
        client.Disconnect();
        client.Dispose();
        return Task.CompletedTask;
    }

    public Task<bool> UploadAsync(bool allFilesInFolder, string uploadPath, string fromPath)
    {
        if (!allFilesInFolder)
        {
            using var stream = File.OpenRead(fromPath);
            client.UploadFile(stream, uploadPath);
            return Task.FromResult(true);
        }

        foreach (var file in Directory.GetFiles(fromPath))
        {
            // Fix upload path, make dynamic with file name.
            using var stream = File.OpenRead(file);
            client.UploadFile(stream, Path.Combine(uploadPath, file.Split(Path.DirectorySeparatorChar)[^1]));
        }

        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public Task<bool> UploadAsync(string uploadPath, byte[] fileBytes)
    {
        using var stream = new MemoryStream(fileBytes);
        client.UploadFile(stream, uploadPath);
        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public async Task<bool> DownloadAsync(bool allFilesInFolder, string downloadPath, string localPath)
    {
        if (!allFilesInFolder)
        {
            await using var stream = File.OpenWrite(localPath);
            client.DownloadFile(downloadPath, stream);
            await stream.FlushAsync();
            return true;
        }

        // Get the names of the files that need to be downloaded.
        var filesToDownload = await GetFilesInFolderAsync(downloadPath);
        if (!filesToDownload.Any())
        {
            return true;
        }

        // Combine the name with the path to the folder.
        foreach (var file in filesToDownload)
        {
            await using var stream = File.OpenWrite(Path.Combine(localPath, file));
            client.DownloadFile(Path.Combine(downloadPath, file), stream);
            await stream.FlushAsync();
        }

        return true;
    }

    /// <inheritdoc />
    public Task<byte[]> DownloadAsBytesAsync(string downloadPath)
    {
        using var stream = new MemoryStream();
        client.DownloadFile(downloadPath, stream);
        return Task.FromResult(stream.ToArray());
    }

    /// <inheritdoc />
    public Task<List<string>> GetFilesInFolderAsync(string folderPath)
    {
        var listing = client.ListDirectory(folderPath)
            .Where(item => item.IsRegularFile)
            .Select(file => file.Name)
            .ToList();

        return Task.FromResult(listing);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteFileAsync(bool allFilesInFolder, string filePath)
    {
        if (allFilesInFolder)
        {
            var files = await GetFilesInFolderAsync(filePath);
            foreach (var file in files)
            {
                await client.DeleteAsync(Path.Combine(filePath, file));
            }

            return true;
        }

        await client.DeleteAsync(filePath);
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> MoveFileAsync(string fromPath, string toPath)
    {
        await client.RenameFileAsync(fromPath, toPath, default);
        return await client.ExistsAsync(toPath);
    }
}