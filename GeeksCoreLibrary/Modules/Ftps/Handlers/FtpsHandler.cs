using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GeeksCoreLibrary.Modules.Ftps.Extensions;
using GeeksCoreLibrary.Modules.Ftps.Interfaces;
using FluentFTP;
using FluentFTP.Helpers;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Modules.Ftps.Models;

namespace GeeksCoreLibrary.Modules.Ftps.Handlers;

public class FtpsHandler : IFtpHandler, IScopedService
{
    private AsyncFtpClient client;

    /// <inheritdoc />
    public async Task OpenConnectionAsync(FtpSettings ftpSettings)
    {
        client = new AsyncFtpClient(ftpSettings.Host, ftpSettings.User, ftpSettings.Password, ftpSettings.Port);
        client.Config.EncryptionMode = ftpSettings.EncryptionMode.ConvertToFtpsEncryptionMode();
        client.Config.ValidateAnyCertificate = ftpSettings.Host.StartsWith("localhost", StringComparison.OrdinalIgnoreCase);
        client.Config.DataConnectionType = ftpSettings.UsePassive ? FtpDataConnectionType.AutoPassive : FtpDataConnectionType.AutoActive;

        await client.Connect();
    }

    /// <inheritdoc />
    public Task CloseConnectionAsync()
    {
        client.Dispose();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<bool> UploadAsync(bool allFilesInFolder, string uploadPath, string fromPath)
    {
        if (!allFilesInFolder)
        {
            return (await client.UploadFile(fromPath, uploadPath, createRemoteDir: true)).IsSuccess();
        }
        
        var ftpResults = await client.UploadDirectory(fromPath, uploadPath, existsMode: FtpRemoteExists.Overwrite);
        return ftpResults.Any(ftpResult => ftpResult.IsSuccess);
    }

    /// <inheritdoc />
    public async Task<bool> UploadAsync(string uploadPath, byte[] fileBytes)
    {
        return (await client.UploadBytes(fileBytes, uploadPath, createRemoteDir: true)).IsSuccess();
    }

    /// <inheritdoc />
    public async Task<bool> DownloadAsync(bool allFilesInFolder, string downloadPath, string writePath)
    {
        if (!allFilesInFolder)
        {
            return (await client.DownloadFile(writePath, downloadPath)).IsSuccess();
        }
        
        // Get the names of the files that need to be downloaded.
        var filesToDownload = await GetFilesInFolderAsync(downloadPath);
        if (!filesToDownload.Any())
        {
            return true;
        }
            
        // Combine the name with the path to the folder.
        for (var i = 0; i < filesToDownload.Count; i++)
        {
            filesToDownload[i] = Path.Combine(downloadPath, filesToDownload[i]);
        }
        
        var downloadCount = await client.DownloadFiles(writePath, filesToDownload);
        return downloadCount.Count == filesToDownload.Count;
    }

    /// <inheritdoc />
    public async Task<List<string>> GetFilesInFolderAsync(string folderPath)
    {
        var listing = await client.GetListing(folderPath);

        return listing.Select(file => file.Name).ToList();
    }

    /// <inheritdoc />
    public async Task<bool> DeleteFileAsync(bool allFilesInFolder, string filePath)
    {
        if (allFilesInFolder)
        {
            var files = await GetFilesInFolderAsync(filePath);
            foreach (var file in files)
            {
                await client.DeleteFile(Path.Combine(filePath, file));
            }

            return true;
        }

        await client.DeleteFile(filePath);
        return true;
    }
}