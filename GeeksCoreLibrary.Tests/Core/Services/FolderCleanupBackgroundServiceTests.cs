using System;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using GeeksCoreLibrary.Core.Services;
using Moq;
using NUnit.Framework;
using FluentAssertions;
using GeeksCoreLibrary.Core.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GeeksCoreLibrary.Tests.Core.Services;

[TestFixture]
public class FolderCleanupBackgroundServiceTests
{
    private Mock<ILogger<FolderCleanupBackgroundService>> mockLogger;
    private Mock<IWebHostEnvironment> mockWebHostEnvironment;
    private Mock<IOptions<GclSettings>> mockGclSettings;
    private MockFileSystem mockFileSystem;

    private FolderCleanupBackgroundService service;

    [SetUp]
    public void SetUp()
    {
        mockLogger = new Mock<ILogger<FolderCleanupBackgroundService>>();
        mockWebHostEnvironment = new Mock<IWebHostEnvironment>();
        mockGclSettings = new Mock<IOptions<GclSettings>>();
        mockFileSystem = new MockFileSystem();

        var testSettings = new GclSettings
        {
            CacheCleanUpOptions = new CacheCleanUpOptions
            {
                CleanUpIntervalDays = 1,
                MaxCacheDurationDays = 30
            }
        };

        mockGclSettings.Setup(s => s.Value).Returns(testSettings);
        mockWebHostEnvironment.Setup(env => env.ContentRootPath).Returns(Path.Combine("MockRoot"));

        // Create test files
        var testFolder = Path.Combine("MockRoot", "Cache");
        mockFileSystem.AddDirectory(testFolder);

        // Adding old and new files
        mockFileSystem.AddFile(Path.Combine(testFolder, "oldFile.txt"), new MockFileData("") { LastWriteTime = DateTime.UtcNow.AddDays(-40) });
        mockFileSystem.AddFile(Path.Combine(testFolder, "newFile.txt"), new MockFileData("") { LastWriteTime = DateTime.UtcNow });
        mockFileSystem.AddFile(Path.Combine(testFolder, "validFile.txt"), new MockFileData("") { LastWriteTime = DateTime.UtcNow.AddDays(-15) });

        // Adding subdirectory with files
        var subFolder = Path.Combine(testFolder, "SubCache");
        mockFileSystem.AddDirectory(subFolder);
        mockFileSystem.AddFile(Path.Combine(subFolder, "subOldFile.txt"), new MockFileData("") { LastWriteTime = DateTime.UtcNow.AddDays(-50) });
        mockFileSystem.AddFile(Path.Combine(subFolder, "subValidFile.txt"), new MockFileData("") { LastWriteTime = DateTime.UtcNow.AddDays(-10) });

        service = new FolderCleanupBackgroundService(
            mockLogger.Object,
            mockWebHostEnvironment.Object,
            mockGclSettings.Object,
            mockFileSystem
        );
    }

    [Test]
    public void FolderCleanupBackgroundService_Should_Delete_Old_Cache_Files()
    {
        // Arrange
        var folderPath = Path.Combine("MockRoot", "Cache");
        var oldFilePath = Path.Combine(folderPath, "oldFile.txt");
        var newFilePath = Path.Combine(folderPath, "newFile.txt");
        var validFilePath = Path.Combine(folderPath, "validFile.txt");

        // Act
        service.GetType()
            .GetMethod("CleanUpFolder", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.Invoke(service, [folderPath]);

        // Assert
        mockFileSystem.FileExists(oldFilePath).Should().BeFalse(); // Ensure old file is deleted
        mockFileSystem.FileExists(newFilePath).Should().BeTrue(); // Ensure new file is not deleted
        mockFileSystem.FileExists(validFilePath).Should().BeTrue(); // Ensure valid file is not deleted (it's within the allowed duration)

        var remainingFiles = mockFileSystem.Directory.GetFiles(folderPath);
        remainingFiles.Should().HaveCount(2); // validFile.txt and newFile.txt should remain
    }

    [Test]
    public void FolderCleanupBackgroundService_Should_Delete_Files_In_Subdirectories()
    {
        // Arrange
        var folderPath = Path.Combine("MockRoot", "Cache");
        var subFolderPath = Path.Combine(folderPath, "SubCache");

        var subOldFilePath = Path.Combine(subFolderPath, "subOldFile.txt");
        var subValidFilePath = Path.Combine(subFolderPath, "subValidFile.txt");

        // Act
        service.GetType()
            .GetMethod("CleanUpFolder", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.Invoke(service, [folderPath]);

        // Assert
        mockFileSystem.FileExists(subOldFilePath).Should().BeFalse(); // Ensure the old file in subdirectory is deleted
        mockFileSystem.FileExists(subValidFilePath).Should().BeTrue(); // Ensure the valid file in subdirectory is not deleted

        var remainingFilesInSubFolder = mockFileSystem.Directory.GetFiles(subFolderPath);
        remainingFilesInSubFolder.Should().HaveCount(1); // Only subValidFile.txt should remain
    }
}