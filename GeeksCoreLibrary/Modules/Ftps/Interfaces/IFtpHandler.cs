using System.Collections.Generic;
using System.Threading.Tasks;
using GeeksCoreLibrary.Modules.Ftps.Models;

namespace GeeksCoreLibrary.Modules.Ftps.Interfaces;

public interface IFtpHandler
{
    /// <summary>
    /// Open the connection to an FTP server.
    /// </summary>
    /// <param name="ftpSettings">The <see cref="FtpSettings"/> containting the information needed to open the connection to the server.</param>
    /// <returns></returns>
    Task OpenConnectionAsync(FtpSettings ftpSettings);

    /// <summary>
    /// Close the connection to an FTP server.
    /// </summary>
    /// <returns></returns>
    Task CloseConnectionAsync();

    /// <summary>
    /// Upload a file to an FTP server from the disk.
    /// </summary>
    /// <param name="allFilesInFolder">If the action applies to all files in the folder.</param>
    /// <param name="uploadPath">The full path to the file that will be uploaded, if for all files the full path to the folder.</param>
    /// <param name="fromPath">The full path to the file from where it will be uploaded, if for all files the full path to the folder to upload.</param>
    /// <returns>Returns if upload was successful.</returns>
    Task<bool> UploadAsync(bool allFilesInFolder, string uploadPath, string fromPath);

    /// <summary>
    /// Upload a file to an FTP server from bytes.
    /// </summary>
    /// <param name="uploadPath">The full path to file to.</param>
    /// <param name="fileBytes">The bytes of the file to upload.</param>
    /// <returns>Returns if upload was successful.</returns>
    Task<bool> UploadAsync(string uploadPath, byte[] fileBytes);

    /// <summary>
    /// Download a file from an FTP server.
    /// </summary>
    /// <param name="allFilesInFolder">If the action applies to all files in the folder.</param>
    /// <param name="downloadPath">The full path to download the file from or the folder path if all files are downloaded.</param>
    /// <param name="writePath">The full path to write the file to or the folder path if all files are downloaded.</param>
    /// <returns>If the file is downloaded.</returns>
    Task<bool> DownloadAsync(bool allFilesInFolder, string downloadPath, string writePath);

    /// <summary>
    /// Get all names of files in a folder in an FTP server.
    /// </summary>
    /// <param name="folderPath">The full path to the folder </param>
    /// <returns>Returns a list with all file names.</returns>
    Task<List<string>> GetFilesInFolderAsync(string folderPath);

    /// <summary>
    /// Delete a file from an FTP server.
    /// </summary>
    /// <param name="allFilesInFolder">If the action applies to all files in the folder.</param>
    /// <param name="filePath">The full path to the file to delete.</param>
    /// <returns>Returns if the file has been deleted.</returns>
    Task<bool> DeleteFileAsync(bool allFilesInFolder, string filePath);

    /// <summary>
    /// Move a file on the FTP server.
    /// </summary>
    /// <param name="fromPath">The full path the the file that needs to be moved.</param>
    /// <param name="toPath">The full path to where the file needs to be moved.</param>
    /// <returns>Returns if the file has been moved.</returns>
    Task<bool> MoveFileAsync(string fromPath, string toPath);
}